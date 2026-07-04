using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Roguebane.Core;
using Roguebane.Core.Content;
using Roguebane.Core.Layout;

namespace Roguebane.Game;

// Thin shell over Core: Update turns input into Session intents and advances the fixed tick;
// Draw only reads Session state and paints placeholder shapes. No game rules live here.
public partial class Game1 : Microsoft.Xna.Framework.Game
{
    private static readonly Keys[] TechniqueKeys =
        { Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6 };

    // Screen names per the 2026-06-30 rename directive; the manifest side is renamed too
    // (newgame/equipment/encounter/citymap/campaignmap), so lookups use the new ids everywhere.
    private enum Screen { NewGame, Equipment, Run }

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private AssetRegistry _assets = null!;
    private readonly LayoutRegistry _layout = new();
    private ManifestUi _ui = null!;

    private Screen _screen = Screen.NewGame;
    private BuildSession _build = null!;
    private Campaign _campaign = null!;
    private bool _paused;
    private bool _merchantOpen; // design/07 full-screen merchant; opens on arrival, LEAVE returns to the map
    private Screen _equipReturnTo = Screen.Run; // where BACK leads from the (in-run) Equipment screen
    private string? _mfScreen;  // dev: RB_MF=<screenId> renders that screen straight from the manifest (RESCUE arc)
    private readonly CombatTargeting _ctrl = new(); // the targeting FSM (headless, in Core); shell just feeds it intents
    private KeyboardState _prevKeys;
    private KeyboardState _keys; // current frame's keys, read in Draw for button pressed-state

    private const double CombatTickSeconds = 0.1; // fixed 10 ticks/sec combat clock
    private double _combatAccum;

    private MouseState _prevMouse;
    private Point _cursor;  // mouse position mapped into design space (through the letterbox)
    private bool _clicked;  // left button went down this frame
    private bool _rclicked; // right button went down this frame (FSM: dismiss target / deactivate card)

    // The leg under way is the campaign's current Expedition — most of the run screen reads it.
    private Expedition Exp => _campaign.Current;

    // Smoke mode (RB_SMOKE=1): load, drive to RB_SCREEN, render, optionally save RB_SHOT, exit. Lets
    // the headless loop verify the pipeline builds, every asset binds, AND the screen renders without
    // a human at the window — a saved PNG is the visual receipt.
    private readonly bool _smoke = Environment.GetEnvironmentVariable("RB_SMOKE") == "1";
    private readonly string? _shotPath = Environment.GetEnvironmentVariable("RB_SHOT");
    private readonly string? _smokeScreen = Environment.GetEnvironmentVariable("RB_SCREEN");
    private int _frames;

    private const int W = 960, H = 540; // the fixed DESIGN space; the world renders here then scales
    // P0 native-res (2026-07-02): the scene is sized to the NATIVE backbuffer fit each frame — the old
    // fixed 960x540x2 target UPSCALED soft on >1080 displays. SS is now the design->scene scale (float,
    // = _viewScale); design COORDS are unchanged, the blit into the backbuffer is 1:1.
    private float SS = 2f;
    private const float FontBake = 3f;   // font rasters are built 3x design px (see *.spritefont sizes)
    private const float ChromeBake = 2f; // button/frame skins are painted at 2x design (1080-class art)

    private RenderTarget2D _scene = null!; // world painted at design res, then letterboxed to the window
    private Rectangle _viewDest;           // where the scaled scene lands in the backbuffer
    private float _viewScale = 1f;         // design->backbuffer factor (mouse maps back through it)
    private bool _fullscreen;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true; // Update runs on a fixed step; the combat clock is a sub-accumulator
        _graphics.PreferredBackBufferWidth = 1600;
        _graphics.PreferredBackBufferHeight = 900;
        // RB_SIZE=WxH pins the backbuffer for headless shots — the scene target aspect-fits to it, so
        // a 960K x 540K size yields shots at EXACT reference resolution (1:1 fidelity diff, no resample).
        if (Environment.GetEnvironmentVariable("RB_SIZE") is { } rbSize
            && rbSize.Split('x', 'X') is [var rw, var rh]
            && int.TryParse(rw, out var rbw) && int.TryParse(rh, out var rbh) && rbw > 0 && rbh > 0)
        {
            _graphics.PreferredBackBufferWidth = rbw;
            _graphics.PreferredBackBufferHeight = rbh;
        }
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        _build = Sessions.NewBuild(); // start on the build screen; Launch threads into the siege

        // Smoke: RB_CHASSIS=<index> selects a chassis on the build screen, so every chassis figure
        // (humanoid + robed) can be RB_SMOKE-verified, not just the default.
        if (_smoke && int.TryParse(Environment.GetEnvironmentVariable("RB_CHASSIS"), out var ci))
            _build.CycleCoreRune(ci - _build.CoreRuneIndex);

        _mfScreen = Environment.GetEnvironmentVariable("RB_MF"); // dev: render this screen from the manifest

        if (_smokeScreen is "encounter" or "citymap" or "loadout") // march the real loop for the screenshot
        {
            if (_smokeScreen is "encounter" or "citymap")
            {
                _build.CycleCoreRune(3);          // -> the Summoner (3 bays; fields Skeleton+Shade) for the bay lane
                _build.Toggle(Techniques.Jab);   // add a STR card for variety on the bar
            }
            // loadout keeps the DEFAULT GRUNT build — design/02's authored state (CORE GRUNT,
            // READY TO MARCH). M0.3: align the drive to the ref state, never mask the divergence.
            _campaign = _build.Redeploy(Maps.StandardLegs(3));
            _screen = Screen.Run;
            foreach (var t in Exp.Equipment) _campaign.Toggle(t); // power the bar (both shots)
            // (build/newrun smoke handled after this block)
            // 1000-tick cap: the grunt (loadout drive) clears a1 slower than the summoner's
            // minion screen — 200 left the fight running and the shot read FIGHTING.
            void Resolve() { for (var i = 0; i < 1000 && Exp.State == ExpeditionState.Fighting; i++) _campaign.Tick(); }

            if (_smokeScreen == "citymap") // stop at the merchant so the shot shows the gear stock + gear bar
            {
                _campaign.Enter("a1"); Resolve(); _campaign.Redeploy(); // earn gold, then back to the chart
                _campaign.Enter("b");             // the merchant
                Exp.BuyWeapon(Armory.Dagger);     // dagger 2 -> pack
                Exp.EquipWeapon(Armory.Dagger);   // -> EQUIPPED
                Exp.Stash.AddArmor(Shops.Plate);  // seed a PACK item for the click-to-equip chip in the shot
            }
            else if (_smokeScreen == "encounter")
            {
                // March to the tanky CASTLE fight — the Summoner's minions melt the light skirmishes
                // en route, so screenshot there (it survives long enough to show a stable combat frame).
                // A cleared fight HOLDS on the battlefield (2026-07-02), so each hop redeploys first —
                // without it every later Enter() silently no-ops and the "castle" smoke was really the
                // a1 hold (caught by the bind validator's live scene label).
                _campaign.Enter("a1"); Resolve(); _campaign.Redeploy(); // a resource hold -> banks 1 support
                _campaign.Enter("b");              // merchant — no fight
                Exp.BuyWeapon(Armory.Dagger); Exp.EquipWeapon(Armory.Dagger); // wield -> hand marker
                Exp.Stash.AddArmor(Shops.Plate); Exp.EquipArmor(Shops.Plate);  // wear -> chest ring
                _campaign.Enter("c2"); Resolve(); _campaign.Redeploy(); // another hold -> banks a 2nd
                _campaign.Enter("castle");         // the banked support rallies on the boss here

                // Show the targeting surface: card 0 LOCKED on a foe's head (F1:H + limb band) with AUTO
                // on, card 1 in TARGETING, plus the filled minion-bay lane. A few ticks charge the cards.
                if (Exp.Enemy is { } foe)
                {
                    var head = foe.Frame?.Parts.FirstOrDefault(p => p.Stat == Stat.Int);
                    if (head is not null) _campaign.Aim(Exp.Equipment[0], foe, head);
                    else _campaign.Aim(Exp.Equipment[0], foe);
                    _campaign.SetAuto(true);
                    if (Exp.Equipment.Count > 1) _ctrl.CardPress(Exp, 1);
                }
                for (var i = 0; i < 6; i++) _campaign.Tick(); // castle survives -> stay in combat
            }
            else if (_smokeScreen == "loadout") // between-fights Equipment overlay, open over the chart
            {
                // The grunt has no minions fighting for it — aim its lead card at the foe and arm
                // AUTO (an untargeted technique just holds), or a1 never clears.
                _campaign.Enter("a1");
                if (Exp.Enemy is { } sk) { _campaign.Aim(Exp.Equipment[0], sk); _campaign.SetAuto(true); }
                Resolve(); _campaign.Redeploy(); // clear a node -> back at the chart (Choosing)
                // design/02's doll is ARMED (sword + worn plate) — stash-seed and equip so the
                // gear rows/doll markers validate; no merchant detour (the stash is the source).
                Exp.Stash.AddWeapon(Armory.Dagger); Exp.EquipWeapon(Armory.Dagger);
                Exp.Stash.AddArmor(Shops.Plate); Exp.EquipArmor(Shops.Plate);
                _screen = Screen.Equipment; // 2026-07-02: the FULL Equipment screen replaced the popover
                _equipReturnTo = Screen.Run;
            }
        }
        else if (_smokeScreen == "equipment") _screen = Screen.Equipment; // else fall through to NewGame

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _assets = new AssetRegistry(Content);
        _ui = new ManifestUi(_layout);
        EnsureSceneMatchesBackbuffer();
    }

    // Recreate the scene target whenever the backbuffer changes (fullscreen toggle, resize): size it to
    // the aspect-fit of the NATIVE output so chrome/fonts rasterize at full density and the final blit
    // is 1:1 — never a soft upscale.
    private void EnsureSceneMatchesBackbuffer()
    {
        // §13 aspect-fill (2026-07-03): the scene covers the WHOLE backbuffer (no bars). SS stays the
        // min-fit design->scene scale, and the design space EXTENDS on the loose axis so edge anchors
        // pin to the real window edges (a 16:9 window resolves to exactly the authored 960x540).
        var bw = GraphicsDevice.PresentationParameters.BackBufferWidth;
        var bh = GraphicsDevice.PresentationParameters.BackBufferHeight;
        var scale = Math.Min((float)bw / W, (float)bh / H);
        if (_scene is not null && _scene.Width == bw && _scene.Height == bh) return;
        _scene?.Dispose();
        SS = scale;
        _scene = new RenderTarget2D(GraphicsDevice, Math.Max(1, bw), Math.Max(1, bh));
        if (_ui is not null)
        {
            _ui.DesignW = Math.Max(W, (int)(bw / SS));
            _ui.DesignH = Math.Max(H, (int)(bh / SS));
        }
    }

    // Fixed-step animation counter (authored `frames` cycle on it — render-only, sim untouched).
    private int _animTick;

    protected override void Update(GameTime gameTime)
    {
        _animTick++;
        var keys = Keyboard.GetState();
        _keys = keys;
        if (keys.IsKeyDown(Keys.Escape)) Exit();
        // §8: while a technique actively targets, the CURSOR IS THE RETICLE — hide the OS pointer.
        IsMouseVisible = !(InRun && _screen == Screen.Run && _ctrl.IsTargeting(Exp));

        var altEnter = keys.IsKeyDown(Keys.LeftAlt) && Pressed(keys, Keys.Enter);
        if (Pressed(keys, Keys.F11) || altEnter) ToggleFullscreen();

        UpdateViewport(); // keep the mouse->design transform current before hit-testing
        var mouse = Mouse.GetState();
        _cursor = new Point(
            _viewScale > 0 ? (int)((mouse.X - _viewDest.X) / _viewScale) : 0,
            _viewScale > 0 ? (int)((mouse.Y - _viewDest.Y) / _viewScale) : 0);
        _clicked = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
        _rclicked = mouse.RightButton == ButtonState.Pressed && _prevMouse.RightButton == ButtonState.Released;

        if (_screen == Screen.NewGame) UpdateNewGame(keys);
        else if (_screen == Screen.Equipment) UpdateEquipment(keys);
        else UpdateRun(keys, gameTime);

        _prevKeys = keys;
        _prevMouse = mouse;
        base.Update(gameTime);
    }

    private void UpdateNewGame(KeyboardState keys)
    {
        if (Pressed(keys, Keys.Left)) _build.CycleCoreRune(-1);
        if (Pressed(keys, Keys.Right)) _build.CycleCoreRune(1);
        var cores = ManifestListCells("newgame", "cores", _build.Roster.Count);
        for (var i = 0; i < cores.Count; i++)
            if (Click(RectOf(cores[i]))) _build.CycleCoreRune(i - _build.CoreRuneIndex);

        // Race axis: Tab cycles, or click a card. Attrs/HP + the composed figure follow the choice.
        if (Pressed(keys, Keys.Tab)) _build.CycleRace(1);
        var races = ManifestListCells("newgame", "races", _build.RaceCount);
        for (var i = 0; i < races.Count; i++)
            if (Click(RectOf(races[i]))) _build.CycleRace(i - _build.RaceIndex);

        var go = (Pressed(keys, Keys.Enter) && !keys.IsKeyDown(Keys.LeftAlt))
            || (ManifestElementRect("newgame", "begin") is { } b && Click(b));
        if (go)
        {
            // 2026-07-02 directive: BEGIN marches straight to the CityMap — no build-screen gate.
            // The chassis ships a fixed kit, so the bar is never empty; Equipment is reachable
            // BETWEEN fights to edit the loadout.
            _campaign = _build.Redeploy(Maps.StandardLegs(3));
            _screen = Screen.Run;
        }
    }

    // Input geometry from the manifest, located by BINDS (the data contract) — never by CD's element
    // ids, which are CD-owned and renameable. Cells mirror exactly what DrawManifestList draws.
    private System.Collections.Generic.IReadOnlyList<LayoutRect> ManifestListCells(
        string screenId, string binds, int count)
    {
        var s = _ui.ScreenDef(screenId);
        var m = _ui.Manifest;
        var e = s?.Elements.FirstOrDefault(x => x.Binds == binds && x.Item is not null);
        if (s is null || m is null || e is null || !m.Templates.TryGetValue(e.Item!.Template, out var tmpl))
            return Array.Empty<LayoutRect>();
        var r = _ui.Rect(s, e);
        return ListLayout.Cells(new LayoutRect(r.X, r.Y, r.Width, r.Height), e.Item, count, tmpl.Size);
    }

    private Rectangle? ManifestElementRect(string screenId, string binds)
    {
        var s = _ui.ScreenDef(screenId);
        var e = s?.Elements.FirstOrDefault(x => x.Binds == binds);
        return s is null || e is null ? null : _ui.Rect(s, e);
    }

    // Equipment is the BETWEEN-FIGHTS loadout for the CURRENT core (design/02) — no core switching
    // here; that choice lives on NewGame. Reached in-run (E / EQUIPMENT buttons); BACK/Esc returns to
    // the caller (2026-07-02: this full screen replaced the loadout popover). Geometry by binds.
    private void UpdateEquipment(KeyboardState keys)
    {
        // P0 (2026-07-02, Doug): E TOGGLES — the same key that opened Equipment closes it (with Esc).
        if (Pressed(keys, Keys.Escape) || Pressed(keys, Keys.E)) { _screen = _equipReturnTo; return; }

        // Toggling routes to the RUN when marching (power/unpower on the live bar) and to the build
        // session otherwise. Mid-run rune mutation stays design-open, so Climb is pre-run only.
        void ToggleTech(Roguebane.Core.Technique t)
        {
            if (InRun) _campaign.Toggle(t);
            else _build.Toggle(t);
        }

        for (var i = 0; i < TechniqueKeys.Length && i < _build.Palette.Count; i++)
            if (Pressed(keys, TechniqueKeys[i]))
                ToggleTech(_build.Palette[i]);

        var tabs = ManifestListCells("equipment", "inventory.tabs", InvTabCount);
        for (var i = 0; i < tabs.Count; i++)
            if (Click(RectOf(tabs[i]))) _invTab = i;

        // TECHNIQUES tab: click an inventory card to slot/unslot (pre-run) or power/unpower (in-run).
        if (_invTab == 1)
        {
            var cards = ManifestListCells("equipment", "inventory.activeTab.items", _build.Palette.Count);
            for (var i = 0; i < cards.Count; i++)
                if (Click(RectOf(cards[i]))) ToggleTech(_build.Palette[i]);
        }
        // GEAR tab clicks (§6e, out-of-combat by Expedition's own gate): equipped → unequip;
        // equippable → equip, AUTO-DISPLACING on conflict — hands-full melee benches the OFF-hand
        // (index 1; main is never displaced, §6d promotion), armor swaps its slot via Gearing.
        // LOCKED cards are inert because the Body's own wield gate rejects the equip.
        else if (_invTab == 0 && InRun)
        {
            var gear = GearTabItems();
            var cards = ManifestListCells("equipment", "inventory.activeTab.items", gear.Count);
            for (var i = 0; i < cards.Count && i < gear.Count; i++)
            {
                if (!Click(RectOf(cards[i]))) continue;
                switch (gear[i])
                {
                    case Roguebane.Core.Weapon w when Exp.Player.Body.Hands.Contains(w):
                        Exp.UnequipWeapon(w); break;
                    case Roguebane.Core.Weapon w:
                        if (Exp.Player.Body.Hands.Count >= 2)
                            Exp.UnequipWeapon(Exp.Player.Body.Hands[1]);
                        Exp.EquipWeapon(w); break;
                    case Roguebane.Core.Armor a when Exp.Player.Body.ArmorOn(a.Slot) == a:
                        Exp.UnequipArmor(a.Slot); break;
                    case Roguebane.Core.Armor a:
                        Exp.EquipArmor(a); break; // Gearing returns the displaced piece to the pack
                }
            }
        }
        var slottedData = InRun ? Exp.Equipment : _build.Equipment;
        var slotted = ManifestListCells("equipment", "loadout", slottedData.Count);
        for (var i = 0; i < slotted.Count && i < slottedData.Count; i++)
            if (Click(RectOf(slotted[i]))) ToggleTech(slottedData[i]);

        if (!InRun)
        {
            // Rune Bag: climb a ladder's next rung (spends budget; Core gates it). Pre-run only.
            var groups = ManifestListCells("equipment", "runeGroups", _build.Paths.Count);
            for (var i = 0; i < groups.Count && i < _build.Paths.Count; i++)
                if (Click(RectOf(groups[i]))) _build.Climb(_build.Paths[i]);
        }
    }

    // Inventory tab strip state (design/02): GEAR | TECHNIQUES | MINIONS. Render-side UI state only.
    private const int InvTabCount = 3;
    private int _invTab;

    private void UpdateRun(KeyboardState keys, GameTime gameTime)
    {
        if (_campaign.State != CampaignState.Redeploying) return; // settled: hold the end overlay
        if (Exp.State == ExpeditionState.Fighting) UpdateCombat(keys, gameTime);
        else if (Exp.State == ExpeditionState.Cleared)
        {
            if (Pressed(keys, Keys.Space) || Click(ClearedRedeployRect)) _campaign.Redeploy(); // back to the chart
        }
        else UpdateChoosing(keys);
    }

    private void UpdateCombat(KeyboardState keys, GameTime gameTime)
    {
        // Combat controls read manifest geometry by binds (design/01): the HELD badge pauses, the
        // FLEE chip retreats, AUTO-ATTACK is the one global toggle. Keyboard verbs unchanged.
        if (Pressed(keys, Keys.Space)
            || (ManifestElementRect("encounter", "combat.paused") is { } pr && Click(pr))) _paused = !_paused;
        if (Pressed(keys, Keys.F)
            || (ManifestElementRect("encounter", "combat.retreat") is { } fr && Click(fr))) Exp.Retreat();
        if (Pressed(keys, Keys.Tab)
            || (ManifestElementRect("encounter", "combat.autoAttack") is { } ar && Click(ar))) _ctrl.ToggleAuto(Exp);

        // The targeting FSM lives in Core (CombatTargeting); the shell only feeds it press intents.
        // Card LEFT-press powers/enters-targeting; card RIGHT-press unpowers.
        var rclickOnCard = false;
        var cards = ManifestListCells("encounter", "loadout.techniques", Exp.Equipment.Count);
        for (var i = 0; i < TechniqueKeys.Length && i < Exp.Equipment.Count; i++)
        {
            var cardRect = i < cards.Count ? RectOf(cards[i]) : Rectangle.Empty;
            if (Pressed(keys, TechniqueKeys[i]) || Click(cardRect)) _ctrl.CardPress(Exp, i);
            if (RightClick(cardRect)) { rclickOnCard = true; _ctrl.CardRightPress(Exp, i); }
        }

        // While a module is targeting: LEFT-press a live foe (clicked limb -> part aim) to set + exit;
        // RIGHT-press the battlefield cancels. A charged + targeted module fires on its own (no button).
        if (_ctrl.IsTargeting(Exp))
        {
            if (Exp.Enemy is { Down: false } foe && Click(FoeRect()))
                _ctrl.FoePress(Exp, foe, FoePartAt(foe, _cursor));
            if (_rclicked && !rclickOnCard) _ctrl.CancelTargeting();
        }
        else _ctrl.Sync(Exp); // module deactivated/gone -> leave targeting

        // The battle runs on a FIXED combat clock (10 ticks/sec) off a real-time accumulator, so the
        // deterministic sim is decoupled from the frame rate. Smoke freezes it for the screenshot.
        if (_paused || _smoke) return;
        _combatAccum += gameTime.ElapsedGameTime.TotalSeconds;
        var guard = 0;
        while (_combatAccum >= CombatTickSeconds && Exp.State == ExpeditionState.Fighting && guard++ < 8)
        {
            _campaign.Tick();
            _combatAccum -= CombatTickSeconds;
        }
    }

    // On the chart: number keys pick a charted jump; at a merchant, the shop verbs are live.
    private void UpdateChoosing(KeyboardState keys)
    {
        // 2026-07-02 directive: E / the EQUIPMENT button opens the REAL Equipment screen (the loadout
        // popover is gone); BACK/Esc returns here. The button IS the manifest equipmentBtn now.
        if (Pressed(keys, Keys.E)
            || (ManifestElementRect("citymap", "nav.equipment") is { } eqr && Click(eqr)))
        {
            _equipReturnTo = Screen.Run;
            _screen = Screen.Equipment;
            return;
        }

        // The design/07 merchant screen swallows input while open: row clicks buy, LEAVE/Esc returns
        // to the map (the node stays a merchant; keyboard verbs below still work map-side).
        if (_merchantOpen && Exp.AtMerchant)
        {
            if (Pressed(keys, Keys.Escape)
                || (ManifestElementRect("merchant", "merchant.leave") is { } lv && Click(lv)))
                { _merchantOpen = false; return; }
            var heals = ManifestListCells("merchant", "merchant.healing.offers", 2);
            if (heals.Count == 2)
            {
                if (Click(new Rectangle(heals[0].X, heals[0].Y, heals[0].W, heals[0].H))) Exp.BuyHeal();
                if (Click(new Rectangle(heals[1].X, heals[1].Y, heals[1].W, heals[1].H))) Exp.BuyFullHeal();
            }
            var lots = ManifestListCells("merchant", "merchant.provisions.stock", 3);
            if (lots.Count == 3)
            {
                if (Click(new Rectangle(lots[0].X, lots[0].Y, lots[0].W, lots[0].H))) Exp.BuySupplies();
                if (Click(new Rectangle(lots[1].X, lots[1].Y, lots[1].W, lots[1].H))) Exp.BuyCharge();
                if (Click(new Rectangle(lots[2].X, lots[2].Y, lots[2].W, lots[2].H))) Exp.BuySummons();
            }
            if (Pressed(keys, Keys.H)) Exp.BuyHeal();
            if (Pressed(keys, Keys.F)) Exp.BuyFullHeal();
            if (Pressed(keys, Keys.S)) Exp.BuySupplies();
            if (Pressed(keys, Keys.C)) Exp.BuyCharge();
            if (Pressed(keys, Keys.M)) Exp.BuySummons();

            // The wares shelves: page with the footer buttons, buy a card straight off a shelf
            // (weapons/armor land in the stash; the other categories aren't buyable yet).
            if (ManifestElementRect("merchant", "merchant.stock.pagePrev") is { } pv && Click(pv))
                _merchantPage = Math.Max(0, _merchantPage - 1);
            if (ManifestElementRect("merchant", "merchant.stock.pageNext") is { } nx && Click(nx))
                _merchantPage = Math.Min(MerchantPageCount() - 1, _merchantPage + 1);
            foreach (var (item, rect) in WareRects())
                if (Click(rect))
                {
                    // §12 receiving (LOCKED): every category buys into the run inventory.
                    if (item is Weapon bw) Exp.BuyWeapon(bw);
                    else if (item is Armor ba) Exp.BuyArmor(ba);
                    else if (item is Technique bt) Exp.BuyTechnique(bt);
                    else if (item is Roguebane.Core.Minion bm) Exp.BuyMinion(bm);
                    else if (item is Roguebane.Core.Mark bk) Exp.BuyMark(bk);
                    break; // one purchase per click
                }
            return; // no map click-through under the stall
        }

        // Closed the stall but still on the node? H walks back in.
        if (Exp.AtMerchant && Pressed(keys, Keys.H)) { _merchantOpen = true; return; }

        // Equip a carried pack item onto the body (out of combat, any beacon): the manifest
        // packChips cells ARE the click targets. Cell order mirrors the Body.gear list data —
        // wielded pieces first (no-op chips), then stash weapons, then stash armor.
        var pw = Exp.Stash.Weapons;
        var pa = Exp.Stash.Armor;
        var wielded = Exp.Player.Body.Hands.Count;
        var chips = ManifestListCells("citymap", "Body.gear", wielded + pw.Count + pa.Count);
        for (var i = 0; i < chips.Count; i++)
        {
            if (!Click(RectOf(chips[i]))) continue;
            var pi = i - wielded;
            if (pi >= 0 && pi < pw.Count) Exp.EquipWeapon(pw[pi]);
            else if (pi >= pw.Count && pi - pw.Count < pa.Count) Exp.EquipArmor(pa[pi - pw.Count]);
            break;
        }

        // Pick an onward jump: a number key, or clicking the destination beacon on the chart.
        var options = Exp.Options;
        for (var i = 0; i < options.Count; i++)
            if ((i < TechniqueKeys.Length && Pressed(keys, TechniqueKeys[i]))
                || Click(NodeRect(options[i])))
            {
                _campaign.Enter(options[i].Id); // may win the leg and roll to the next city
                foreach (var t in Exp.Equipment)  // keep the bar armed into the next fight/leg
                    if (!_campaign.IsActive(t)) _campaign.Toggle(t);
                _merchantOpen = Exp.AtMerchant; // arriving at a merchant opens the stall screen
                break;
            }
    }

    // Chart layout: a node's screen rect from its grid coords (Col = depth, Row = lane). Shared by
    // the chart render and the click hit-test so a beacon is selectable exactly where it is drawn.
    // The legacy region (inset to clear the panels/gear bar) is the fallback when no manifest exists.
    private static readonly Rectangle ChartRegion = new(44, 200, 676, 210);
    private const int ChartIcon = 48;

    // Node position = f(region, grid extents) via GraphLayout — nodes SPREAD to fill the region, so the
    // chart is viewport-independent instead of pinned to a fixed pixel origin. Cut-over step
    // (2026-07-02): the region + icon size come from the MANIFEST chart element (located by its "map"
    // bind, never CD's renameable ids) so clicks land exactly where DrawManifestGraph draws — the
    // legacy chart render shares this rect, keeping render and hit-test in lockstep either way.
    private Rectangle NodeRect(MapNode n)
    {
        var region = ChartRegion;
        int cw = ChartIcon, ch = ChartIcon;
        var s = _ui.ScreenDef("citymap");
        var chart = s?.Elements.FirstOrDefault(x => x.Binds == "map" && x.Type == "graph" && x.Item is not null);
        if (s is not null && chart is not null)
        {
            region = _ui.Rect(s, chart);
            if (chart.Item!.Size.Length == 2) { cw = chart.Item.Size[0]; ch = chart.Item.Size[1]; }
        }
        var nodes = Exp.Map.Nodes;
        var cols = nodes.Max(x => x.Col) + 1;
        var rows = nodes.Max(x => x.Row) + 1;
        var cell = GraphLayout.Cell(
            new LayoutRect(region.X, region.Y, region.Width, region.Height),
            cols, rows, n.Col, n.Row, cw, ch);
        return new Rectangle(cell.X, cell.Y, cell.W, cell.H);
    }

    private bool Pressed(KeyboardState keys, Keys key) => keys.IsKeyDown(key) && _prevKeys.IsKeyUp(key);

    // Mouse helpers: Hover = cursor over a rect (drives Draw highlight); Click = hover + this frame's
    // press (drives Update intents). Both read the design-space cursor mapped through the letterbox.
    private bool Hover(Rectangle r) => !_smoke && r.Contains(_cursor);
    private bool Click(Rectangle r) => _clicked && r.Contains(_cursor);
    private bool RightClick(Rectangle r) => _rclicked && r.Contains(_cursor);

    // Interactive layout rects — single source of truth shared by Update (hit-test) and Draw (paint
    // + hover). Mirrors the coordinates used in the Draw* methods.
    // The action bar sits bottom-RIGHT (design/01), right of the attribute pool and left of the combat
    // verb buttons. Card pitch adapts to the equipment size so N cards fit the region.
    private const int ActBarX = 366, ActBarY = H - 84, ActBarW = 314;
    private Rectangle ActionCardRect(int i)
    {
        var n = Math.Max(1, Exp.Equipment.Count);
        var pitch = Math.Min(80, ActBarW / n);
        var card = Math.Max(42, pitch - 6);
        return new(ActBarX + i * pitch, ActBarY, card, 60);
    }
    // Single-foe (canon): the foe hit-box IS the manifest foeFigure element; hand rect only as a
    // fallback for a missing manifest.
    private Rectangle FoeRect() => ManifestElementRect("encounter", "encounter.foe")
        ?? new Rectangle(632, 96, 224, 252);

    // Anatomical part bands stacked on the foe sprite, top->bottom: head, arms, chest, legs. A
    // structured foe is aimed limb-by-limb by clicking the band; an unstructured foe has no bands.
    private static int PartBand(Stat stat) => stat switch
    {
        Stat.Int => 0, // head
        Stat.Str => 1, // arms
        Stat.Con => 2, // chest
        Stat.Dex => 3, // legs
        _ => 1,
    };

    private Rectangle FoePartRect(Stat stat)
    {
        var r = FoeRect();
        var band = r.Height / 4;
        return new Rectangle(r.X, r.Y + PartBand(stat) * band, r.Width, band);
    }

    // The SCREEN rect a foe stat-group occupies (union of its visual part rects, e.g. both arms),
    // via the same manifest transform DrawHumanoid uses — so reticles land on the drawn limbs.
    private Rectangle? FoePartScreenRect(Foe foe, Stat stat, Rectangle box)
    {
        var manifest = _layout.Manifest;
        if (manifest is null || !manifest.Figures.TryGetValue(foe.Figure, out var fig)) return null;
        var f = (float)box.Height / fig.Size[1];
        int cx = box.X + box.Width / 2, cy = box.Y + box.Height;
        var px = fig.Pivot[0]; var py = fig.Pivot[1];
        Rectangle? acc = null;
        foreach (var (name, part) in fig.Parts)
        {
            if (FigureBinding.StatOf(name) != stat) continue;
            var rr = new Rectangle(cx + (int)((part.Rect[0] - px) * f), cy + (int)((part.Rect[1] - py) * f),
                (int)(part.Rect[2] * f), (int)(part.Rect[3] * f));
            acc = acc is null ? rr : Rectangle.Union(acc.Value, rr);
        }
        return acc;
    }

    // The foe PART under a screen point (structured foe only), else null = whole-HP aim.
    private BodyPart? FoePartAt(Foe foe, Point p)
    {
        if (foe.Frame is null) return null;
        foreach (var part in foe.Frame.Parts)
            if (FoePartRect(part.Stat).Contains(p)) return part;
        return null;
    }

    // Between-fights Equipment: open button (CityMap) + the overlay's technique cards & close button.

    private void ToggleFullscreen()
    {
        _fullscreen = !_fullscreen;
        _graphics.HardwareModeSwitch = false; // borderless desktop-res fullscreen
        if (_fullscreen)
        {
            _graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
        }
        else
        {
            _graphics.PreferredBackBufferWidth = 1600;
            _graphics.PreferredBackBufferHeight = 900;
        }
        _graphics.IsFullScreen = _fullscreen;
        _graphics.ApplyChanges();
    }

    private static readonly (Stat Stat, Color Color)[] StatColors =
    {
        (Stat.Str, new Color(220, 90, 70)),
        (Stat.Int, new Color(80, 150, 230)),
        (Stat.Dex, new Color(120, 200, 120)),
        (Stat.Con, new Color(200, 180, 90)),
    };

    protected override void Draw(GameTime gameTime)
    {
        EnsureSceneMatchesBackbuffer();
        // 2026-07-02 P0 guard: RB_MF=all smokes EVERY manifest screen with a paint-coverage check.
        if (_smoke && _mfScreen == "all") { SmokeAllScreensAndExit(); return; }
        // The world always paints at the fixed design resolution into the scene target...
        GraphicsDevice.SetRenderTarget(_scene);
        GraphicsDevice.Clear(new Color(0x17, 0x11, 0x0b)); // panel-dark base from the locked palette
        // §11 supersample: paint design-space (960x540) into the SSx target via a scale matrix, so glyphs
        // rasterize at 1080-class density; coordinates are unchanged.
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Matrix.CreateScale(SS));
        if (_mfScreen is not null) DrawManifestScreen(_mfScreen); // dev: render a screen straight from the manifest
        else if (_screen == Screen.NewGame) DrawManifestScreen("newgame"); // LIVE cut-over (design/05)
        else if (_screen == Screen.Equipment) DrawManifestScreen("equipment"); // LIVE cut-over (design/02); its manifest resourceStrip carries the run resources
        else DrawRunScreen();
        _spriteBatch.End();

        if (_smoke && _shotPath is not null) // headless receipt: save the design-res scene verbatim
        {
            GraphicsDevice.SetRenderTarget(null);
            using var fs = System.IO.File.Create(_shotPath!);
            _scene.SaveAsPng(fs, _scene.Width, _scene.Height);
        }
        else // §13: the scene IS the window — blit 1:1, no bars
        {
            GraphicsDevice.SetRenderTarget(null);
            UpdateViewport();
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_scene, _viewDest, Color.White);
            _spriteBatch.End();
        }

        base.Draw(gameTime);

        if (_smoke && ++_frames >= 1) SmokeReportAndExit();
    }

    // §13 aspect-fill: the scene target matches the backbuffer exactly — blit 1:1 at the origin,
    // zero bars. Mouse maps back through _viewScale = SS (design->scene factor).
    private void UpdateViewport()
    {
        _viewScale = SS;
        _viewDest = new Rectangle(0, 0, _scene.Width, _scene.Height);
    }

    // Touch one asset of every kind through the registry, then report what bound. A null is a gap in
    // the content set, not a crash — the report counts them so the loop sees coverage at a glance.
    private void SmokeReportAndExit()
    {
        (string, Texture2D?)[] probes =
        {
            ("node/castle", _assets.Node(NodeType.Castle)),
            ("node/camp", _assets.Camp),
            ("attr/str", _assets.Attr(Stat.Str)),
            ("attr/con", _assets.Attr(Stat.Con)),
            ("technique/cleave", _assets.Technique("cleave")),
            ("technique/jab(fallback)", _assets.Technique("jab")),
            ("resource/supplies", _assets.Resource("supplies")),
            ("rune/keystone", _assets.Rune("keystone")),
            ("pip/full", _assets.Pip("full")),
            ("reticle/focus", _assets.Reticle("focus")),
            ("reticle/focus_p2", _assets.Reticle("focus_p2")), // drop-mirror guard: pulse frames built

            ("button/normal", _assets.Button("normal")),
            ("bg/combat_field", _assets.Background("combat_field")),
            ("chassis/grunt", _assets.CoreRuneFigure("grunt")),
        };
        var bound = 0;
        foreach (var (name, tex) in probes)
        {
            if (tex is not null) bound++;
            else Console.WriteLine($"SMOKE MISS: {name}");
        }
        Console.WriteLine($"SMOKE OK: fonts=2 probes={probes.Length} bound={bound}");
        Exit();
    }

    // 2026-07-02 P0: smoke ALL manifest screens every pass with a deterministic PAINT-COVERAGE check —
    // a screen must paint pixels beyond its z=0 scene backdrop. The asset probes above can't see a
    // blank screen (the combat regression rendered backdrop-only while every probe bound), so this
    // diffs a backdrop-only render against the full render per screen and fails the process if any
    // screen adds (almost) nothing. RB_SMOKE=1 RB_MF=all [RB_SHOT=x.png → x.<screen>.png each].
    private void SmokeAllScreensAndExit()
    {
        var screens = _ui.Manifest?.Screens.Keys.ToArray() ?? Array.Empty<string>();
        var blank = new List<string>();
        var total = _scene.Width * _scene.Height;
        var baseline = new Color[total];
        var full = new Color[total];
        var blankEls = new List<string>();
        foreach (var id in screens)
        {
            // M0.3 (Doug): ALIGN THE DRIVE TO THE REF, never mask the divergence — the encounter
            // drive picks the Summoner for combat richness, but design/05 renders the DEFAULT
            // Grunt build. Cycle the build to the ref state for the newgame shot, restore after.
            var cycled = 0;
            if (id == "newgame" && _build.CoreRuneIndex != 0)
            {
                cycled = _build.CoreRuneIndex;
                _build.CycleCoreRune(-cycled);
            }
            RenderSceneOnce(() => DrawManifestBackdrop(id));
            _scene.GetData(baseline);
            _textBoxes.Clear();
            _textTruncated.Clear();
            _collectText = true;
            RenderSceneOnce(() => DrawManifestScreen(id));
            _collectText = false;
            _scene.GetData(full);
            ReportTextGeometry(id);
            var painted = 0;
            for (var i = 0; i < total; i++)
                if (full[i] != baseline[i]) painted++;
            var pct = 100.0 * painted / total;
            Console.WriteLine($"SMOKE COVER: {id} painted={pct:0.0}%");
            if (pct < 1.0) blank.Add(id);
            if (_shotPath is not null)
            {
                var path = System.IO.Path.ChangeExtension(_shotPath, null) + "." + id + ".png";
                using var fs = System.IO.File.Create(path);
                _scene.SaveAsPng(fs, _scene.Width, _scene.Height);
                // Sidecar v2: every element's resolved DESIGN-space rect PLUS the authored style
                // numbers the probe tool checks the pixels against (P0-A.4) — fontPx for the
                // text-height probe, border width for the stroke probe.
                var def = _ui.ScreenDef(id)!;
                var rects = def.Elements.Where(x => !string.IsNullOrEmpty(x.Id)).Select(x =>
                {
                    var r = ScreenLayout.Resolve(def, x);
                    var fx = x.FontPx is { } f ? f.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0";
                    var bw = x.Border?.W ?? 0;
                    // skinned = a button-family or image-backed element: its ink is chrome, not
                    // glyphs — the text-height probe skips it.
                    var skinned = !string.IsNullOrEmpty(x.Image)
                        || x.States.ValueKind == System.Text.Json.JsonValueKind.Object ? "true" : "false";
                    return $"\"{x.Id}\":{{\"rect\":[{r.X},{r.Y},{r.W},{r.H}],\"fontPx\":{fx},"
                        + $"\"borderW\":{bw},\"type\":\"{x.Type}\",\"skinned\":{skinned}}}";
                });
                System.IO.File.WriteAllText(
                    System.IO.Path.ChangeExtension(_shotPath, null) + "." + id + ".rects.json",
                    "{" + string.Join(",", rects) + "}");
                // M0.4 geometry sidecar: what text ACTUALLY drew (element, drawn bbox, bound,
                // string, font family) — tools/geometry_diff.py checks it against the dc.html
                // authored geometry. Design px throughout.
                static string J(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
                var tg = _textBoxes.Select(t =>
                    $"{{\"el\":\"{J(t.El)}\",\"box\":[{t.Box.X},{t.Box.Y},{t.Box.Width},{t.Box.Height}],"
                    + $"\"bound\":[{t.Bound.X},{t.Bound.Y},{t.Bound.Width},{t.Bound.Height}],"
                    + $"\"text\":\"{J(t.Text)}\",\"font\":\"{t.Font}\"}}");
                System.IO.File.WriteAllText(
                    System.IO.Path.ChangeExtension(_shotPath, null) + "." + id + ".textgeom.json",
                    "[" + string.Join(",", tg) + "]");
            }
            // Per-ELEMENT coverage (the systemic validator): render the screen once per element with
            // that element left out — zero pixel difference means it contributed NOTHING.
            // Classes: an element with unconditional chrome/content (fill/frame/content/image/button)
            // must paint -> BLANK fails the run. Lists and bind-only elements go silent legitimately
            // pre-run (state-gated data) -> info. A border-only element can be wholly overpainted by
            // the mixed-z container fills (attrPool's divider under hudFooter) -> OCCLUDED info; the
            // z-convention normalization is a flagged Needs-CD.
            var silent = new List<string>();
            var occluded = new List<string>();
            var screenDef = _ui.ScreenDef(id)!;
            foreach (var el in screenDef.Elements.Where(x => !IsSceneElement(x)))
            {
                RenderSceneOnce(() => DrawManifestScreen(id, el));
                _scene.GetData(baseline);
                var contributes = false;
                for (var i = 0; i < total; i++)
                    if (full[i] != baseline[i]) { contributes = true; break; }
                if (contributes) continue;
                // Datum-driven fills (the bar IS the value) are legitimately empty at zero.
                var datumFill = el.Binds is "enemy.advancePct" or "runes.budgetPct"
                    or "ShieldPool.regen" or "ShieldPool.regenPct";
                // content+binds = bind-gated literal (§12): a closed gate legitimately paints nothing.
                // Bound icons gate the same way (mock-position stand-ins drawn live elsewhere).
                var gated = !string.IsNullOrEmpty(el.Binds)
                    && (el.Type == "icon" || (el.Type == "text" && !string.IsNullOrEmpty(el.Content)));
                var mustPaint = !datumFill && !gated && (el.Fill is not null || el.Frame is not null
                    || !string.IsNullOrEmpty(el.Content) || !string.IsNullOrEmpty(el.Image)
                    || el.Type == "button");
                if (mustPaint) blankEls.Add(id + "/" + (el.Id ?? "?"));
                else if (el.Border is not null) occluded.Add(el.Id ?? "?");
                else silent.Add(el.Id ?? "?");
            }
            if (silent.Count > 0)
                Console.WriteLine($"SMOKE SILENT: {id} (state-gated, ok): {string.Join(",", silent)}");
            if (occluded.Count > 0)
                Console.WriteLine($"SMOKE OCCLUDED: {id} (border overpainted, Needs-CD z): {string.Join(",", occluded)}");
            // Content validation: which BOUND elements resolve LIVE data in the current state. Drive a
            // run first (RB_SCREEN=encounter RB_MF=all) to validate the in-run screens' binds.
            var boundEls = screenDef.Elements.Where(x => !string.IsNullOrEmpty(x.Binds)).ToArray();
            var unresolved = boundEls.Where(x => !BindResolves(x)).Select(x => x.Id ?? "?").ToList();
            Console.WriteLine($"SMOKE BINDS: {id} resolved={boundEls.Length - unresolved.Count}/{boundEls.Length}"
                + (unresolved.Count > 0 ? $" unresolved=[{string.Join(",", unresolved)}]" : ""));
            if (cycled != 0) _build.CycleCoreRune(cycled); // restore the drive's build
        }
        ReportAssetResolution();
        if (blankEls.Count > 0)
            Console.WriteLine($"SMOKE ELEM-BLANK: {string.Join(",", blankEls)}");
        if (blank.Count > 0)
            Console.WriteLine($"SMOKE BLANK: {string.Join(",", blank)}");
        if (blank.Count > 0 || blankEls.Count > 0)
            Environment.ExitCode = 1;
        SmokeReportAndExit();
    }

    // P0-A.5: text-geometry validation over the FULL render — every drawn text footprint must fit
    // its element rect (+-2 design px), and must not intersect another element's footprint unless
    // one element NESTS the other (containers legitimately hold their children's text).
    private void ReportTextGeometry(string screenId)
    {
        const int tol = 2;
        var overflow = new List<string>();
        foreach (var (el, box, bound, _, _) in _textBoxes)
            if (box.X < bound.X - tol || box.Y < bound.Y - tol
                || box.Right > bound.Right + tol || box.Bottom > bound.Bottom + tol)
                if (!overflow.Contains(el)) overflow.Add(el);
        var collide = new List<string>();
        var def = _ui.ScreenDef(screenId)!;
        Rectangle ElemRect(string id) =>
            def.Elements.FirstOrDefault(x => x.Id == id) is { } e ? _ui.Rect(def, e) : Rectangle.Empty;
        for (var a = 0; a < _textBoxes.Count; a++)
            for (var b = a + 1; b < _textBoxes.Count; b++)
            {
                var (ea, boxA, _, _, _) = _textBoxes[a];
                var (eb, boxB, _, _, _) = _textBoxes[b];
                if (ea == eb || !boxA.Intersects(boxB)) continue;
                var ra = ElemRect(ea);
                var rb = ElemRect(eb);
                if (ra.Contains(rb) || rb.Contains(ra)) continue; // nesting exempt
                var key = ea + "x" + eb;
                if (!collide.Contains(key)) collide.Add(key);
            }
        // M1 detector gap: truncated strings (the wrap clamp dropped words) are their own counter —
        // an invisible label is a harder failure than a visible spill and never rides its baseline.
        var truncated = _textTruncated.Select(t => t.El).Distinct().ToList();
        Console.WriteLine($"SMOKE TEXTGEOM: {screenId} overflow={overflow.Count} collide={collide.Count}"
            + $" truncated={truncated.Count}"
            + (overflow.Count > 0 ? $" over=[{string.Join(",", overflow)}]" : "")
            + (collide.Count > 0 ? $" hits=[{string.Join(",", collide)}]" : "")
            + (truncated.Count > 0
                ? $" trunc=[{string.Join(",", _textTruncated.Select(t => $"{t.El}:'{Snip(t.Dropped)}'").Distinct())}]"
                : ""));
        static string Snip(string s) => s.Length <= 24 ? s : s[..24] + "...";
    }

    // M0.5 (Doug 2026-07-03 late): every authored image/imageBind path must resolve to a texture
    // the game's content build actually carries — silent misses (the race heads) hid as "art gaps".
    // imageBind placeholders enumerate their REACHABLE domains; unknown placeholders are reported
    // as unverifiable rather than skipped silently.
    private void ReportAssetResolution()
    {
        var m = _ui.Manifest;
        if (m is null) return;
        var missing = new SortedSet<string>();
        var unverifiable = new SortedSet<string>();
        var domains = new Dictionary<string, string[]>
        {
            ["race.id"] = Roguebane.Core.Content.Races.Roster.Select(r => r.Id).ToArray(),
            ["technique.id"] = Roguebane.Core.Content.Techniques.All.Select(t => t.Id).ToArray(),
            ["loadout.id"] = Roguebane.Core.Content.Techniques.All.Select(t => t.Id).ToArray(),
            ["node.type"] = new[] { "camp", "castle", "merchant", "resource", "unknown", "skirmish" },
            ["resource.id"] = new[] { "supplies", "spoils", "charge", "summons" },
            ["cell.asset"] = new[] { "pip_full_str", "pip_full_int", "pip_full_dex", "pip_full_con",
                "pip_reserved_str", "pip_reserved_int", "pip_reserved_dex", "pip_reserved_con" },
            ["point.asset"] = new[] { "pip_full_supplies", "pip_empty_supplies",
                "pip_full_support", "pip_empty_support" },
        };
        void Check(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var norm = NormalizeContentPath(path!);
            var ph = System.Text.RegularExpressions.Regex.Match(norm, @"\{(.+?)\}");
            if (!ph.Success)
            {
                if (_assets.Texture(norm) is null) missing.Add(norm);
                return;
            }
            if (!domains.TryGetValue(ph.Groups[1].Value, out var vals))
            {
                unverifiable.Add(norm);
                return;
            }
            foreach (var v in vals)
                if (_assets.Texture(norm.Replace(ph.Value, v)) is null)
                    missing.Add(norm.Replace(ph.Value, v));
        }
        foreach (var s in m.Screens.Values)
            foreach (var el in s.Elements)
            {
                Check(el.Image);
                Check(el.ImageBind); // static pattern paths probe like any texture; {bind} via domains
                if (el.Frames is { } fr) foreach (var f in fr) Check(f);
            }
        foreach (var t in m.Templates.Values)
        {
            Check(t.ImageBind);
            foreach (var p in t.Parts) { Check(p.Image); Check(p.ImageBind); }
        }
        Console.WriteLine($"SMOKE ASSETS: missing={missing.Count} unverifiable={unverifiable.Count}"
            + (missing.Count > 0 ? $" miss=[{string.Join(",", missing)}]" : "")
            + (unverifiable.Count > 0 ? $" unk=[{string.Join(",", unverifiable)}]" : ""));

        // Figure part-resolve probe (M1 directive): for EVERY figure def, attempt-resolve every
        // z-list part at every ARMORED condition row — those are mandatory (a miss is the class
        // that drew Warden's empty limb boxes). Bare rows are optional per figure (the composer
        // falls back); figures shipping no bare art are reported informationally, never failed.
        var figMissing = new SortedSet<string>();
        var bareLess = new SortedSet<string>();
        var armored = new[] { "ok", "damaged", "broken" };
        var bare = new[] { "bareOk", "bareDmg", "bareBroke" };
        string Suffix(string key) => m.Style.PartStates.GetValueOrDefault(key, key);
        foreach (var (figId, fig) in m.Figures)
        {
            var anyBare = false;
            foreach (var part in fig.Z)
            {
                if (!fig.Parts.ContainsKey(part)) continue;
                foreach (var st in armored)
                    if (_assets.Texture($"sprites/body/{figId}/{part}_{Suffix(st)}") is null)
                        figMissing.Add($"{figId}/{part}_{Suffix(st)}");
                foreach (var st in bare)
                    anyBare |= _assets.Texture($"sprites/body/{figId}/{part}_{Suffix(st)}") is not null;
            }
            if (!anyBare) bareLess.Add(figId);
        }
        Console.WriteLine($"SMOKE FIGURES: figures={m.Figures.Count} armored-missing={figMissing.Count}"
            + (figMissing.Count > 0 ? $" miss=[{string.Join(",", figMissing)}]" : "")
            + (bareLess.Count > 0 ? $" bare-less(fallback)=[{string.Join(",", bareLess)}]" : ""));
    }

    private void RenderSceneOnce(Action draw)
    {
        GraphicsDevice.SetRenderTarget(_scene);
        GraphicsDevice.Clear(new Color(0x17, 0x11, 0x0b));
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Matrix.CreateScale(SS));
        draw();
        _spriteBatch.End();
        GraphicsDevice.SetRenderTarget(null);
    }

    // The run renders one of two faces of the Expedition: the chart when choosing the next jump
    // (design/03), the battlefield when a fight is under way (design/01).
    private void DrawRunScreen()
    {
        // A cleared fight HOLDS on the battlefield (with the Redeploy overlay) — no auto-return to the
        // chart. The chart shows only once the player has redeployed (Choosing).
        if (Exp.State is ExpeditionState.Fighting or ExpeditionState.Cleared) DrawEncounterScreen();
        else if (_merchantOpen && Exp.AtMerchant) DrawManifestScreen("merchant"); // design/07 stall
        else DrawCityMapScreen();
    }

    // Combat screen (design/01): rendered from the manifest; only the cleared/lost state overlay is
    // legacy (it isn't part of the encounter design). The battlefield backdrop stays under the chrome.
    private void DrawEncounterScreen()
    {
        Stretch(_assets.Background("combat_field"), 0, 0, W, H);
        DrawManifestScreen("encounter"); // its manifest resourceStrip carries the run resources
        DrawStateOverlay();
    }

    // 2026-07-02 directive: the run's resource counts — supplies / gold / charge / Summons (§9) —
    // read top-right on every IN-RUN screen, under the status strip.

    // Draw text horizontally centred on cx at y (measures the font-safe form so centring matches draw).
    private void DrawCentered(SpriteFont font, string text, Color col, int cx, int y)
    {
        var w = MeasureText(font, text).X;
        Text(font, text, (int)(cx - w / 2), y, col);
    }

    // Greedy word-wrap for mono copy inside a width; steps ~14px per line.
    private void DrawWrapped(string text, int x, int y, int w, Color col)
    {
        var line = "";
        var ly = y;
        foreach (var word in text.Split(' '))
        {
            var trial = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && MeasureText(_assets.Mono, trial).X > w)
            {
                Text(_assets.Mono, line, x, ly, col);
                ly += 14;
                line = word;
            }
            else line = trial;
        }
        if (line.Length > 0) Text(_assets.Mono, line, x, ly, col);
    }


    // Lay a humanoid from its parts: head (INT), chest (CON), arms (STR ×2), legs (DEX ×2). Each
    // part's sprite is picked by condition; paired parts fan out to either side of the torso.
    // Assemble the figure from the layout manifest: each visual part drawn at its manifest rect with
    // a state-keyed sprite (condition x armored/bare, via FigureBinding), gear mounted at its socket.
    // The pure composition lives in Core (StageComposer/FigureBinding); the shell only blits + scales.
    // Falls back to the legacy stat-offset draw when the manifest is absent (no crash on a content gap).
    // Place the figure from a manifest screen element (feet at the box bottom-centre, scaled to the
    // box height). Falls back to the supplied magic coords when the element/manifest is absent.
    private void DrawFigureIn(Body body, string figureId, string screen, string elementId,
        int fbCx, int fbCy, int fbH)
    {
        if (_ui.ElementRect(screen, elementId) is { } b)
            DrawHumanoid(body, figureId, b.X + b.Width / 2, b.Y + b.Height, b.Height);
        else
            DrawHumanoid(body, figureId, fbCx, fbCy, fbH);
    }

    // allowBare=false forces the plain (armoured-row) sprites — for figures with no bare art (foes).
    private void DrawHumanoid(Body body, string figureId, int cx, int cy, int targetH,
        Color? tint = null, bool allowBare = true)
    {
        var manifest = _layout.Manifest;
        if (manifest is null || !manifest.Figures.ContainsKey(figureId)) { DrawHumanoidLegacy(body, cx, cy); return; }

        var fig = manifest.Figures[figureId];
        var composer = new StageComposer(manifest);
        var color = tint ?? Color.White;
        var f = (float)targetH / fig.Size[1];                 // world scale: fit the figure to the slot height
        var px = fig.Pivot[0]; var py = fig.Pivot[1];          // anchor the figure's pivot at (cx,cy)
        int SX(float fx) => cx + (int)((fx - px) * f);
        int SY(float fy) => cy + (int)((fy - py) * f);

        foreach (var p in composer.ComposeFigure(figureId,
                     part => FigureBinding.Condition(body, part),
                     part => allowBare && FigureBinding.UseBare(body, part)))
        {
            var r = p.Rect; // x,y,w,h in figure space
            // Optional sprite rows (bare art, a missing condition row) resolve through the
            // composer's ordered fallbacks — only a figure missing its WHOLE part row still
            // draws the null-texture gap box (a real Needs-CD art gap, probed headless).
            var ptex = _assets.Texture(p.SpriteKey);
            foreach (var fb in p.Fallbacks)
            {
                if (ptex is not null) break;
                ptex = _assets.Texture(fb);
            }
            Sprite(ptex, SX(r[0]), SY(r[1]), (int)(r[2] * f), (int)(r[3] * f), color);
            // Composed armour indicator for parts with no armoured sprite row (torso/head/boots):
            // ring the part so worn plate is visible (bare-capable parts already show armour via sprite).
            if (allowBare && !FigureBinding.HasBareVariant(p.Part) && FigureBinding.IsArmored(body, p.Part))
                Border(SX(r[0]), SY(r[1]), (int)(r[2] * f), (int)(r[3] * f), Amber);
        }

        // Gear: draw the body's ACTUAL wielded weapons at the hand sockets, each by its own gear
        // sprite (positioned by the manifest gear pivot). Dynamic equipment — not the figure's fixed
        // default mounts — so any equipped weapon shows. No sprite for a weapon id => simply unarmed.
        var hands = new[] { "handR", "handL" }; // dominant hand first
        for (var i = 0; i < body.Hands.Count && i < hands.Length; i++)
        {
            // §6 hard override: a broken arm's hand slot is physically gone — its weapon never draws.
            if (!FigureBinding.HandUsable(body, i)) continue;
            var w = body.Hands[i];
            var tex = _assets.Texture($"sprites/gear/{w.Id}");
            if (tex is null || !fig.Sockets.TryGetValue(hands[i], out var anchor)) continue;
            var gp = manifest.Gear.TryGetValue(w.Id, out var gd) && gd.Pivot.Length == 2
                ? gd.Pivot : new[] { tex.Width / 2, tex.Height / 2 };
            var gx = SX(anchor[0]) - (int)(gp[0] * f);
            var gy = SY(anchor[1]) - (int)(gp[1] * f);
            var gw = (int)(tex.Width * f); var gh = (int)(tex.Height * f);
            // Sprite-shaped drop shadow so a wielded weapon reads against the figure (a thin bow blends
            // into the body otherwise): the weapon's own alpha, offset + darkened, then the weapon on top.
            var off = Math.Max(1, (int)(2 * f));
            _spriteBatch.Draw(tex, new Rectangle(gx + off, gy + off, gw, gh), new Color(0, 0, 0, 110));
            Sprite(tex, gx, gy, gw, gh, color);
        }
    }

    // Legacy fallback (manifest missing): the old stat-offset composite with composed gear markers.
    private void DrawHumanoidLegacy(Body body, int cx, int cy)
    {
        const int s = 2;
        var arms = 0;
        var legs = 0;
        foreach (var part in body.Parts)
        {
            var (w, h, dx, dy) = part.Stat switch
            {
                Stat.Int => (32, 36, 0, -38),
                Stat.Con => (40, 40, 0, 0),
                Stat.Str => (20, 44, arms++ == 0 ? -30 : 30, -2),
                Stat.Dex => (20, 48, legs++ == 0 ? -10 : 10, 42),
                _ => (40, 40, 0, 0),
            };
            var rx = cx + dx * s - w * s / 2;
            var ry = cy + dy * s - h * s / 2;
            if (body.ArmorOn(part.Stat) is not null) Border(rx, ry, w * s, h * s, Amber);
        }
        var hand = 0;
        foreach (var weapon in body.Hands)
        {
            var side = hand++ == 0 ? -1 : 1;
            Rect(cx + 30 * side * s - 2, cy + 16 * s, 5, 20, StatColor(weapon.Stat));
        }
    }





    private static readonly Rectangle ClearedRedeployRect = new(W / 2 - 90, H / 2 + 24, 180, 34);

    private void DrawStateOverlay()
    {
        // A cleared fight: dim the field, name the win, and offer REDEPLOY (no silent return to the map).
        if (_campaign.State == CampaignState.Redeploying && Exp.State == ExpeditionState.Cleared)
        {
            Rect(0, 0, W, H, new Color(20, 45, 30, 120));
            var s = MeasureText(_assets.Display, "NODE CLEARED");
            Text(_assets.Display, "NODE CLEARED", (int)(W / 2 - s.X / 2), H / 2 - 40, Ink);
            DrawButton("REDEPLOY", ClearedRedeployRect.X, ClearedRedeployRect.Y,
                ClearedRedeployRect.Width, ClearedRedeployRect.Height, true, Keys.Space);
            return;
        }

        (Color tint, string label)? overlay = _campaign.State switch
        {
            CampaignState.Won => (new Color(40, 120, 60, 130), "THE CAPITAL FALLS"),
            CampaignState.Lost => (new Color(120, 40, 40, 140), "OVERRUN"),
            _ => _paused ? (new Color(0, 0, 0, 120), "PAUSED") : ((Color, string)?)null,
        };
        if (overlay is { } o)
        {
            Rect(0, 0, W, H, o.tint);
            var size = MeasureText(_assets.Display, o.label);
            Text(_assets.Display, o.label, (int)(W / 2 - size.X / 2), H / 2 - 12, Ink);
            if (_campaign.State != CampaignState.Redeploying)
                Text(_assets.Mono, "Esc to quit", W / 2 - 40, H / 2 + 20, Muted);
        }
    }
}
