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
public class Game1 : Microsoft.Xna.Framework.Game
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
    private bool _loadoutOpen; // between-fights Equipment view over the CityMap (read + re-slot techniques)
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
    private const int SS = 2; // §11 supersample: the scene target is SS x design so text/glyphs rasterize
                              // at 1080-class density (fonts built 2x, drawn 1/SS). Design COORDS unchanged.

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
            _build.CycleCoreRune(3);          // -> the Summoner (3 bays; fields Skeleton+Shade) for the bay lane
            _build.Toggle(Techniques.Jab);   // add a STR card for variety on the bar
            _campaign = _build.Redeploy(Maps.StandardLegs(3));
            _screen = Screen.Run;
            foreach (var t in Exp.Equipment) _campaign.Toggle(t); // power the bar (both shots)
            // (build/newrun smoke handled after this block)
            void Resolve() { for (var i = 0; i < 200 && Exp.State == ExpeditionState.Fighting; i++) _campaign.Tick(); }

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
                _campaign.Enter("a1"); Resolve();  // a resource hold -> banks 1 support
                _campaign.Enter("b");              // merchant — no fight
                Exp.BuyWeapon(Armory.Dagger); Exp.EquipWeapon(Armory.Dagger); // wield -> hand marker
                Exp.Stash.AddArmor(Shops.Plate); Exp.EquipArmor(Shops.Plate);  // wear -> chest ring
                _campaign.Enter("c2"); Resolve();  // another hold -> banks a 2nd
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
                _campaign.Enter("a1"); Resolve(); _campaign.Redeploy(); // clear a node -> back at the chart (Choosing)
                _loadoutOpen = true;
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
        _scene = new RenderTarget2D(GraphicsDevice, W * SS, H * SS);
    }

    protected override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();
        _keys = keys;
        if (keys.IsKeyDown(Keys.Escape)) Exit();

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
        if (go) _screen = Screen.Equipment; // on to the equipment screen for the chosen core
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
        var r = ManifestUi.Rect(s, e);
        return ListLayout.Cells(new LayoutRect(r.X, r.Y, r.Width, r.Height), e.Item, count, tmpl.Size);
    }

    private Rectangle? ManifestElementRect(string screenId, string binds)
    {
        var s = _ui.ScreenDef(screenId);
        var e = s?.Elements.FirstOrDefault(x => x.Binds == binds);
        return s is null || e is null ? null : ManifestUi.Rect(s, e);
    }

    // Equipment is a BETWEEN-FIGHTS loadout for the CURRENT core (design/02) — no core switching here;
    // that choice lives on NewGame. Geometry comes from the manifest by binds, like NewGame.
    private void UpdateEquipment(KeyboardState keys)
    {
        for (var i = 0; i < TechniqueKeys.Length && i < _build.Palette.Count; i++)
            if (Pressed(keys, TechniqueKeys[i]))
                _build.Toggle(_build.Palette[i]);

        var tabs = ManifestListCells("equipment", "tabs", InvTabCount);
        for (var i = 0; i < tabs.Count; i++)
            if (Click(RectOf(tabs[i]))) _invTab = i;

        // TECHNIQUES tab: click an inventory card to slot/unslot it; a slotted action-bar card unslots.
        if (_invTab == 1)
        {
            var cards = ManifestListCells("equipment", "invItems", _build.Palette.Count);
            for (var i = 0; i < cards.Count; i++)
                if (Click(RectOf(cards[i]))) _build.Toggle(_build.Palette[i]);
        }
        var slotted = ManifestListCells("equipment", "loadout", _build.Equipment.Count);
        for (var i = 0; i < slotted.Count && i < _build.Equipment.Count; i++)
            if (Click(RectOf(slotted[i]))) _build.Toggle(_build.Equipment[i]);

        // Rune Bag: click a group card to climb its ladder's next rung (spends budget; Core gates it).
        var groups = ManifestListCells("equipment", "runeGroups", _build.Paths.Count);
        for (var i = 0; i < groups.Count && i < _build.Paths.Count; i++)
            if (Click(RectOf(groups[i]))) _build.Climb(_build.Paths[i]);

        // March the campaign. The chassis ships a fixed kit so the bar is never empty — no gate.
        // Alt+Enter is the fullscreen toggle, not a march. (The design's READY TO MARCH chip was
        // flattened into the status strip by extraction — Needs-CD; Enter carries the march until then.)
        var march = Pressed(keys, Keys.Enter) && !keys.IsKeyDown(Keys.LeftAlt);
        if (march)
        {
            _campaign = _build.Redeploy(Maps.StandardLegs(3));
            // Techniques start INACTIVE: the bar is slotted but nothing is reserved/aimed/firing until
            // the player clicks a card. (No auto-arm — that bug had the whole bar auto-targeting.)
            _screen = Screen.Run;
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
            || (ManifestElementRect("encounter", "combat.flee") is { } fr && Click(fr))) Exp.Retreat();
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
        // Between-fights Equipment view: opens over the map (E), re-slots techniques (click a card ->
        // power/unpower via the existing Toggle), closes with Esc/BACK. While open it eats map input.
        if (_loadoutOpen)
        {
            for (var i = 0; i < Exp.Equipment.Count; i++)
                if (Click(LoadoutCardRect(i))) _campaign.Toggle(Exp.Equipment[i]);
            if (Pressed(keys, Keys.Escape) || Click(LoadoutBackRect)) _loadoutOpen = false;
            return;
        }
        if (Pressed(keys, Keys.E) || Click(EquipOpenRect)) { _loadoutOpen = true; return; }

        if (Exp.AtMerchant)
        {
            if (Pressed(keys, Keys.H) || Click(MerchHealRect)) Exp.BuyHeal(); // HP healing only (no potions)

            // Buy a gear chip into the Stash pack (weapons first, then armor — same order as drawn).
            var ws = Exp.OfferedWeapons;
            for (var i = 0; i < ws.Count; i++) if (Click(MerchGearRect(i))) { Exp.BuyWeapon(ws[i]); break; }
            var ars = Exp.OfferedArmor;
            for (var i = 0; i < ars.Count; i++) if (Click(MerchGearRect(ws.Count + i))) { Exp.BuyArmor(ars[i]); break; }
        }

        // Equip a carried pack item onto the body (out of combat, any beacon). Weapons then armor —
        // the same order DrawGearBar lays the chips out.
        var pw = Exp.Stash.Weapons;
        for (var i = 0; i < pw.Count; i++) if (Click(PackChipRect(i))) { Exp.EquipWeapon(pw[i]); break; }
        var pa = Exp.Stash.Armor;
        for (var i = 0; i < pa.Count; i++) if (Click(PackChipRect(pw.Count + i))) { Exp.EquipArmor(pa[i]); break; }

        // Pick an onward jump: a number key, or clicking the destination beacon on the chart.
        var options = Exp.Options;
        for (var i = 0; i < options.Count; i++)
            if ((i < TechniqueKeys.Length && Pressed(keys, TechniqueKeys[i]))
                || Click(NodeRect(options[i])))
            {
                _campaign.Enter(options[i].Id); // may win the leg and roll to the next city
                foreach (var t in Exp.Equipment)  // keep the bar armed into the next fight/leg
                    if (!_campaign.IsActive(t)) _campaign.Toggle(t);
                break;
            }
    }

    // Chart layout: a node's screen rect from its grid coords (Col = depth, Row = lane). Shared by
    // the chart render and the click hit-test so a beacon is selectable exactly where it is drawn.
    // The chart's region, inset to clear the supply panels (top), legend (right) and gear bar (bottom).
    private static readonly Rectangle ChartRegion = new(44, 200, 676, 210);
    private const int ChartIcon = 48;

    // Node position = f(region, grid extents) via GraphLayout — nodes SPREAD to fill the region, so the
    // chart is viewport-independent instead of pinned to a fixed pixel origin.
    private Rectangle NodeRect(MapNode n)
    {
        var nodes = Exp.Map.Nodes;
        var cols = nodes.Max(x => x.Col) + 1;
        var rows = nodes.Max(x => x.Row) + 1;
        var cell = GraphLayout.Cell(
            new LayoutRect(ChartRegion.X, ChartRegion.Y, ChartRegion.Width, ChartRegion.Height),
            cols, rows, n.Col, n.Row, ChartIcon, ChartIcon);
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

    // The foe PART under a screen point (structured foe only), else null = whole-HP aim.
    private BodyPart? FoePartAt(Foe foe, Point p)
    {
        if (foe.Frame is null) return null;
        foreach (var part in foe.Frame.Parts)
            if (FoePartRect(part.Stat).Contains(p)) return part;
        return null;
    }

    // Merchant verb button + gear chips — mirror DrawMerchant's panel origin (560,300) + offsets.
    private static readonly Rectangle MerchHealRect = new(574, 344, 330, 30);
    private static Rectangle MerchGearRect(int i) => new(574 + i * 112, 472, 104, 30);

    // Between-fights Equipment: open button (CityMap) + the overlay's technique cards & close button.
    private static readonly Rectangle EquipOpenRect = new(16, 190, 150, 30); // left column, clear of the merchant panel
    private static readonly Rectangle LoadoutPanel = new(180, 70, 600, 400);
    private static readonly Rectangle LoadoutBackRect = new(600, 430, 150, 30);
    private static Rectangle LoadoutCardRect(int i) => new(430 + (i % 5) * 62, 240 + (i / 5) * 62, 56, 56);

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
        // The world always paints at the fixed design resolution into the scene target...
        GraphicsDevice.SetRenderTarget(_scene);
        GraphicsDevice.Clear(new Color(0x17, 0x11, 0x0b)); // panel-dark base from the locked palette
        // §11 supersample: paint design-space (960x540) into the SSx target via a scale matrix, so glyphs
        // rasterize at 1080-class density; coordinates are unchanged.
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Matrix.CreateScale(SS));
        if (_mfScreen is not null) DrawManifestScreen(_mfScreen); // dev: render a screen straight from the manifest
        else if (_screen == Screen.NewGame) DrawManifestScreen("newgame"); // LIVE cut-over (design/05)
        else if (_screen == Screen.Equipment) DrawManifestScreen("equipment"); // LIVE cut-over (design/02)
        else DrawRunScreen();
        _spriteBatch.End();

        if (_smoke && _shotPath is not null) // headless receipt: save the design-res scene verbatim
        {
            GraphicsDevice.SetRenderTarget(null);
            using var fs = System.IO.File.Create(_shotPath!);
            _scene.SaveAsPng(fs, W * SS, H * SS);
        }
        else // ...then letterbox-scale it into the window backbuffer
        {
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black); // letterbox bars
            UpdateViewport();
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_scene, _viewDest, Color.White);
            _spriteBatch.End();
        }

        base.Draw(gameTime);

        if (_smoke && ++_frames >= 1) SmokeReportAndExit();
    }

    // Fit the design scene into the backbuffer aspect-preserving: scale by the FULL fractional fit so
    // the scene FILLS the window (16:9 design in a 16:9 window = no bars; thin bars only on an
    // aspect-mismatch axis). PointClamp keeps it crisp. (Integer-only scaling was wasteful — a wide
    // window capped at 2x and left fat side bars.)
    private void UpdateViewport()
    {
        var bw = GraphicsDevice.PresentationParameters.BackBufferWidth;
        var bh = GraphicsDevice.PresentationParameters.BackBufferHeight;
        _viewScale = Math.Min((float)bw / W, (float)bh / H);
        var dw = (int)(W * _viewScale);
        var dh = (int)(H * _viewScale);
        _viewDest = new Rectangle((bw - dw) / 2, (bh - dh) / 2, dw, dh);
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

    // The run renders one of two faces of the Expedition: the chart when choosing the next jump
    // (design/03), the battlefield when a fight is under way (design/01).
    private void DrawRunScreen()
    {
        // A cleared fight HOLDS on the battlefield (with the Redeploy overlay) — no auto-return to the
        // chart. The chart shows only once the player has redeployed (Choosing).
        if (Exp.State is ExpeditionState.Fighting or ExpeditionState.Cleared) DrawEncounterScreen();
        else DrawCityMapScreen();
    }

    // Combat screen (design/01): rendered from the manifest; only the cleared/lost state overlay is
    // legacy (it isn't part of the encounter design). The battlefield backdrop stays under the chrome.
    private void DrawEncounterScreen()
    {
        Stretch(_assets.Background("combat_field"), 0, 0, W, H);
        DrawManifestScreen("encounter");
        DrawStateOverlay();
    }

    // Run-map screen (design/03): the resources, the current beacon, and the charted jumps as cards
    // (fog-aware icons). At a merchant the shop verbs are live instead of a fight ahead.
    private void DrawCityMapScreen()
    {
        Stretch(_assets.Background("map_chart"), 0, 0, W, H);
        Panel(0, 0, W, 40);
        Text(_assets.Display, "REDEPLOY", 16, 8, Ink);
        DrawRunResources(200, 10);
        DrawSpine(720, 12);

        DrawSupplyPanels(16, 48);
        DrawWarParty(300, 64, 300);
        DrawChart();
        DrawMapLegend(756, 64); // top-right; clears the header, war party, and the merchant panel below
        DrawCastlePanel(740, 158);
        if (Exp.AtMerchant) DrawMerchant(560, 300);
        DrawGearBar(20, H - 44);
        if (!_loadoutOpen)
            DrawButton("EQUIPMENT [E]", EquipOpenRect.X, EquipOpenRect.Y,
                EquipOpenRect.Width, EquipOpenRect.Height, true, Keys.E);

        DrawStateOverlay();
        if (_loadoutOpen) DrawLoadoutOverlay();
    }

    // Between-fights Equipment (design #4, flow-only slice): a read view of the RUN's loadout over the
    // CityMap, with technique RE-SLOTTING via the existing Toggle (power/unpower on the bar). Mid-run
    // gear/rune changes stay deferred (that's the design-open part); gear-equip already lives on the map.
    private static readonly IReadOnlyDictionary<Stat, int> EmptyDemand =
        new System.Collections.Generic.Dictionary<Stat, int>();

    private void DrawLoadoutOverlay()
    {
        Rect(0, 0, W, H, new Color(0, 0, 0, 160)); // dim the chart behind
        var p = LoadoutPanel;
        Panel(p.X, p.Y, p.Width, p.Height);
        Text(_assets.Display, "LOADOUT", p.X + 16, p.Y + 12, Ink);
        Text(_assets.Mono, "between fights - click a technique to power / unpower it",
            p.X + 16, p.Y + 44, Muted);

        var body = Exp.Player.Body;
        DrawHumanoid(body, Exp.FigureId, p.X + 116, p.Y + 250, 190);
        Text(_assets.Mono, $"HP {Exp.Player.Hp}/{Exp.Player.MaxHp}", p.X + 16, p.Y + 72, Ink);
        Text(_assets.Mono, $"GOLD {Exp.Gold}", p.X + 150, p.Y + 72, Amber);
        DrawAttributeReadout(body, body, p.X + 16, p.Y + 100, EmptyDemand);

        Text(_assets.Mono, "TECHNIQUES", 430, 218, Muted);
        for (var i = 0; i < Exp.Equipment.Count; i++)
        {
            var t = Exp.Equipment[i];
            var r = LoadoutCardRect(i);
            var on = Exp.IsActive(t);
            Panel(r.X, r.Y, r.Width, r.Height);
            Sprite(_assets.Technique(t.Id), r.X + 6, r.Y + 6, r.Width - 12, r.Height - 12,
                on ? Color.White : new Color(120, 110, 100));
            Border(r.X, r.Y, r.Width, r.Height, on ? Amber : Hover(r) ? Ink : Border0);
            Text(_assets.Mono, t.Reserve.ToString(), r.Right - 12, r.Bottom - 12, StatColor(t.Stat));
        }
        Text(_assets.Mono, "MINIONS  " + Exp.MinionCount, 430, p.Bottom - 58, Muted);

        DrawButton("BACK [Esc]", LoadoutBackRect.X, LoadoutBackRect.Y,
            LoadoutBackRect.Width, LoadoutBackRect.Height, true, Keys.Escape);
    }

    // design/03 right-side card: the run's destination. The castle is the structural boss the whole
    // leg presses toward (its layers fall in order; banked support rallies on it). Display-only.
    private void DrawCastlePanel(int x, int y)
    {
        Panel(x, y, 200, 132);
        Sprite(_assets.Node(NodeType.Castle), x + 10, y + 12, 26, 26, Color.White);
        Text(_assets.Mono, "THE CASTLE", x + 44, y + 14, Ink);
        Text(_assets.Mono, "the exit", x + 44, y + 30, Amber);
        Text(_assets.Mono, "STRUCTURED FOE", x + 12, y + 56, Muted);
        Text(_assets.Mono, "gate / wall / keep", x + 12, y + 74, Muted);
        Text(_assets.Mono, "banked support", x + 12, y + 96, Muted);
        Text(_assets.Mono, "rallies here", x + 12, y + 114, Muted);
    }

    // design/03 signature: the two top-left gauges as PANELS with pip bars + flavor (the jump budget
    // and the support you can rally), in place of bare top-bar counts.
    private void DrawSupplyPanels(int x, int y)
    {
        var map = Exp.Map;
        var holds = map.Nodes.Count(n => n.Type == NodeType.ResourceHold);

        Panel(x, y, 250, 64);
        Text(_assets.Mono, "SUPPLIES", x + 12, y + 8, Muted);
        Text(_assets.Mono, $"{map.Supplies}/{map.MaxSupplies}", x + 200, y + 8, map.Supplies > 0 ? Ink : Blood);
        DrawPipStrip(x + 12, y + 28, map.Supplies, map.MaxSupplies, map.Supplies > 0 ? Amber : Blood);
        Text(_assets.Mono, "1 supply per deployment", x + 12, y + 44, Muted);

        var sy = y + 72;
        Panel(x, sy, 250, 64);
        Text(_assets.Mono, "MUSTERED SUPPORT", x + 12, sy + 8, Muted);
        Text(_assets.Mono, $"{map.SupportBank}/{holds}", x + 200, sy + 8, Ink);
        DrawPipStrip(x + 12, sy + 28, map.SupportBank, holds, new Color(120, 160, 200));
        Text(_assets.Mono, "banked from held beacons", x + 12, sy + 44, Muted);
    }

    // A row of filled/empty segments (design/03 gauges). Filled in col, the remainder a dim outline.
    private void DrawPipStrip(int x, int y, int filled, int total, Color col)
    {
        const int seg = 16, gap = 4, h = 10;
        for (var i = 0; i < total; i++)
        {
            var sx = x + i * (seg + gap);
            if (i < filled) Rect(sx, y, seg, h, col);
            else Border(sx, y, seg, h, new Color(80, 65, 60));
        }
    }

    // Node-type key (design/03): what the chart icons mean. Display-only; tucked top-right where the
    // chart is sparse (hidden behind the merchant panel at a merchant).
    private void DrawMapLegend(int x, int y)
    {
        Text(_assets.Mono, "CHART", x, y - 16, Muted);
        (NodeType Type, string Label)[] rows =
        {
            (NodeType.Castle, "castle / exit"),
            (NodeType.Merchant, "merchant"),
            (NodeType.ResourceHold, "resource hold"),
            (NodeType.Unknown, "unknown/fight"),
        };
        for (var i = 0; i < rows.Length; i++)
        {
            var ry = y + i * 20;
            Sprite(_assets.Node(rows[i].Type), x, ry, 16, 16, Color.White);
            Text(_assets.Mono, rows[i].Label, x + 22, ry + 3, Muted);
        }
    }

    // Out-of-combat gear bar (map screen): the body's EQUIPPED gear (wielded weapons + worn armor) and
    // the carried PACK as click-to-equip chips. Equipping moves a piece pack -> body via Expedition.
    private void DrawGearBar(int x, int y)
    {
        var body = Exp.Player.Body;
        Text(_assets.Mono, "EQUIPPED", x, y - 16, Muted);
        var ex = x;
        foreach (var w in body.Hands) { GearTag(ex, w.Id, Amber); ex += 86; }
        foreach (var (s, _) in StatColors)
            if (body.ArmorOn(s) is { } a) { GearTag(ex, a.Id, StatColor(s)); ex += 86; }
        if (ex == x) Text(_assets.Mono, "-", x, y + 4, Muted);

        Text(_assets.Mono, "PACK  (click to equip)", x + 360, y - 16, Muted);
        for (var i = 0; i < PackCount; i++)
        {
            var r = PackChipRect(i);
            var (id, col) = PackItem(i);
            Panel(r.X, r.Y, r.Width, r.Height);
            Text(_assets.Mono, id, r.X + 6, r.Y + 6, Ink);
            Border(r.X, r.Y, r.Width, r.Height, Hover(r) ? Amber : col);
        }

        void GearTag(int gx, string id, Color col)
        {
            Panel(gx, y, 80, 28);
            Text(_assets.Mono, id, gx + 6, y + 6, col);
        }
    }

    private int PackCount => Exp.Stash.Weapons.Count + Exp.Stash.Armor.Count;
    private static Rectangle PackChipRect(int i) => new(380 + i * 86, H - 44, 80, 28);

    // The pack as one indexed list: weapons first, then armor (matching the click handler's order).
    private (string Id, Color Border) PackItem(int i)
    {
        var ws = Exp.Stash.Weapons;
        if (i < ws.Count) return (ws[i].Id, StatColor(ws[i].Stat));
        var a = Exp.Stash.Armor[i - ws.Count];
        return (a.Id, StatColor(a.Group));
    }

    // The half-blind beacon chart as a GRAPH (design/03): nodes placed by their grid coords, links
    // drawn solid where charted (from a visited beacon) and dotted where still uncharted; fog hides a
    // beacon's true kind behind a `?`. The current beacon reads "you are here"; reachable beacons ring
    // and number as the onward jumps.
    private void DrawChart()
    {
        var map = Exp.Map;

        // Links first, so the beacons sit on top of their connecting lines.
        foreach (var node in map.Nodes)
        {
            var from = NodeRect(node);
            var fx = from.X + ChartIcon / 2;
            var fy = from.Y + ChartIcon / 2;
            foreach (var nid in node.Next)
            {
                var to = NodeRect(map.Node(nid));
                var charted = node.Visited; // a link out of a charted beacon is itself charted
                Line(fx, fy, to.X + ChartIcon / 2, to.Y + ChartIcon / 2, 2,
                    charted ? new Color(150, 130, 95) : new Color(90, 78, 66), dashed: !charted);
            }
        }

        var options = map.Options;
        foreach (var node in map.Nodes)
        {
            var r = NodeRect(node);
            var seen = map.Sees(node);
            var isCurrent = ReferenceEquals(node, map.Current);
            Sprite(_assets.Node(seen), r.X, r.Y, ChartIcon, ChartIcon, isCurrent ? Color.White : new Color(210, 200, 190));

            var oi = IndexOf(options, node);
            if (isCurrent)
            {
                Border(r.X - 3, r.Y - 3, ChartIcon + 6, ChartIcon + 6, Amber);
                Text(_assets.Mono, "you are here", r.X - 8, r.Y + ChartIcon + 2, Amber);
            }
            else if (oi >= 0) // a reachable onward jump
            {
                Border(r.X - 2, r.Y - 2, ChartIcon + 4, ChartIcon + 4, Hover(r) ? Ink : new Color(150, 130, 95));
                Text(_assets.Mono, $"[{oi + 1}] {seen.ToString().ToLower()}", r.X - 6, r.Y + ChartIcon + 2, Ink);
            }
        }
    }

    private static int IndexOf(IReadOnlyList<MapNode> list, MapNode node)
    {
        for (var i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], node)) return i;
        return -1;
    }

    // The forward-pressure track: the war party marches on the camp one step per jump. The marker
    // slides toward the camp (left) as the distance closes; reaching it overruns the run.
    private void DrawWarParty(int x, int y, int w)
    {
        var map = Exp.Map;
        Text(_assets.Mono, "WAR PARTY", x + 22, y - 16, Muted);
        Rect(x, y, w, 6, new Color(70, 55, 50));
        Sprite(_assets.Node(map.Sees(map.Current)), x - 8, y - 8, 20, 20, Color.White); // camp end
        var frac = map.MarchLength > 0 ? (float)map.WarPartyDistance / map.MarchLength : 0f;
        var mx = x + (int)((1f - frac) * (w - 22));
        // The closing war-party host: its own icon, swapping to the "near" variant when it's about to
        // reach camp (the loss timer). Falls back to the castle glyph if the art is missing.
        var near = map.WarPartyDistance <= 2;
        var host = _assets.Texture("icons/map/enemy_host" + (near ? "_near" : ""));
        if (host is not null) Sprite(host, mx, y - 12, 28, 28, Color.White);
        else Sprite(_assets.Node(NodeType.Castle), mx, y - 10, 24, 24, Blood);
        Text(_assets.Mono, map.WarPartyDistance + " to camp", x + w + 12, y - 6, Blood);
    }

    private void DrawMerchant(int x, int y)
    {
        Panel(x, y, 360, 220);
        Text(_assets.Display, "MERCHANT", x + 14, y + 10, Ink);
        // HP healing only — part-heals are in-combat techniques now, not buyable potions. Per-HP price
        // is set by the merchant (§10): buying restores as much HP as the gold affords.
        DrawButton($"H  heal hp  ({Exp.HealPricePerHp}/hp)", x + 14, y + 44, 330, 30,
            Exp.Gold >= Exp.HealPricePerHp && Exp.Player.Hp < Exp.Player.MaxHp, Keys.H);

        // The gear stock as a compact row of buy chips (name + price); dim when unaffordable / sold.
        Text(_assets.Mono, "GEAR", x + 14, y + 150, Muted);
        var ws = Exp.OfferedWeapons;
        var ars = Exp.OfferedArmor;
        for (var i = 0; i < ws.Count; i++) GearChip(i, ws[i].Id, Expedition.Price(ws[i]));
        for (var i = 0; i < ars.Count; i++) GearChip(ws.Count + i, ars[i].Id, Expedition.Price(ars[i]));

        void GearChip(int idx, string name, int price)
        {
            var r = MerchGearRect(idx);
            var ok = Exp.Gold >= price;
            Panel(r.X, r.Y, r.Width, r.Height);
            Text(_assets.Mono, name, r.X + 6, r.Y + 6, ok ? Ink : Muted);
            Text(_assets.Mono, price.ToString(), r.X + r.Width - 16, r.Y + 6, ok ? Amber : Muted);
            Border(r.X, r.Y, r.Width, r.Height, Hover(r) && ok ? Amber : Border0);
        }
    }

    // The compact top-bar readout: gold only. Supplies + mustered support are in their design/03 panels
    // (DrawSupplyPanels); the war-party distance reads off its own track; potions are gone.
    private void DrawRunResources(int x, int y)
    {
        Sprite(_assets.Resource("spoils"), x, y, 22, 22, Color.White);
        Text(_assets.Mono, Exp.Gold.ToString(), x + 26, y + 4, Ink);
    }

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


    private void DrawAttributeReadout(Body cur, Body baseBody, int x, int y, IReadOnlyDictionary<Stat, int> demand)
    {
        static string Part(Stat s) => s switch
        { Stat.Str => "arms", Stat.Int => "head", Stat.Dex => "legs", Stat.Con => "chest", _ => "" };

        for (var i = 0; i < StatColors.Length; i++)
        {
            var (s, color) = StatColors[i];
            var top = y + i * 34;
            Sprite(_assets.Attr(s), x, top, 20, 20, Color.White);
            Text(_assets.Mono, s.ToString().ToUpperInvariant(), x + 26, top, color);
            Text(_assets.Mono, Part(s), x + 26, top + 11, Muted);

            var b = baseBody.Capacity(s);
            var c = cur.Capacity(s);
            var marks = c - b;
            var gate = demand.TryGetValue(s, out var g) ? g : 0;

            const int bx0 = 64, bw = 116, bh = 14;
            var bx = x + bx0;
            var top2 = top + 2;
            var maxPts = Math.Max(Math.Max(c, gate), 6);
            float unit = (float)bw / maxPts;
            Rect(bx, top2, bw, bh, new Color(0x24, 0x1b, 0x14));            // slot
            Rect(bx, top2, (int)(b * unit), bh, color);                     // base
            if (marks > 0) Rect(bx + (int)(b * unit), top2, (int)(marks * unit), bh, new Color(color, 150)); // +marks
            if (gate > 0) Rect(bx + (int)(gate * unit) - 1, top2 - 2, 2, bh + 4, gate <= c ? Amber : Blood);  // gate
            Text(_assets.Mono, c.ToString(), bx + bw + 8, top2, Ink);
            if (marks > 0) Text(_assets.Mono, "+" + marks, bx + bw + 26, top2 + 2, Muted);
        }
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
            Sprite(_assets.Texture(p.SpriteKey), SX(r[0]), SY(r[1]), (int)(r[2] * f), (int)(r[3] * f), color);
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

    // The campaign spine (design/04): a pip per leg to the Capital, taken cities lit amber.
    // The campaign-spine strip (design/04): the legs to the Capital as a chain of castles — taken
    // (amber), here (white), unreached (dim) — the last leg marked as the Capital/peak, plus a
    // cities-taken counter. (The full branching city-graph picker waits on a branching campaign model.)
    private void DrawSpine(int x, int y)
    {
        Text(_assets.Mono, "SPINE", x, y, Muted);
        var n = _campaign.LegCount;
        for (var i = 0; i < n; i++)
        {
            var left = x + 56 + i * 22;
            var taken = i < _campaign.LegIndex;
            var here = i == _campaign.LegIndex;
            var capital = i == n - 1; // the Capital: the peak castle at the end of the road
            var sz = capital ? 22 : 18;
            Sprite(_assets.Node(NodeType.Castle), left, y - (capital ? 4 : 2), sz, sz,
                taken ? Amber : here ? Color.White : new Color(110, 95, 80));
            if (capital) Text(_assets.Mono, "^", left + 6, y - 14, Amber); // peak marker
        }
        Text(_assets.Mono, _campaign.LegIndex + "/" + n, x + 56 + n * 22 + 8, y, Amber); // cities taken
    }

    // Locked palette (ASSET_MANIFEST): ink/muted text, amber highlight, ember/blood, panel + borders.
    private static readonly Color Ink = new(0xec, 0xe0, 0xcb);
    private static readonly Color Muted = new(0x9a, 0x84, 0x68);
    private static readonly Color Amber = new(0xd9, 0xa4, 0x41);
    private static readonly Color Blood = new(0xb2, 0x3b, 0x32);
    private static readonly Color Panel0 = new(0x1d, 0x15, 0x0e);
    private static readonly Color Border0 = new(0x5a, 0x46, 0x36);
    // §10 gradient chrome: panels fade a touch lighter at the top -> Panel0 at the base (soft lit depth).
    private static readonly Color PanelTop = new(0x2b, 0x21, 0x17, 220);
    private static readonly Color PanelBot = new(0x1d, 0x15, 0x0e, 220);

    private static Color StatColor(Stat s)
    {
        foreach (var (stat, color) in StatColors) if (stat == s) return color;
        return Ink;
    }

    private void Sprite(Texture2D? tex, int x, int y, int w, int h, Color tint)
    {
        if (tex is null) { Border(x, y, w, h, Border0); return; } // visible gap, not a crash
        _spriteBatch.Draw(tex, new Rectangle(x, y, w, h), tint);
    }

    private void Stretch(Texture2D? tex, int x, int y, int w, int h) => Sprite(tex, x, y, w, h, Color.White);

    // Fonts are built at SSx; under the scene's SS scale matrix we draw at 1/SS so the ON-SCREEN size
    // matches the design space while the glyph rasterizes at full 1080-class density.
    private void Text(SpriteFont font, string s, int x, int y, Color color) =>
        _spriteBatch.DrawString(font, Safe(font, s), new Vector2(x, y), color, 0f, Vector2.Zero,
            1f / SS, SpriteEffects.None, 0f);

    // The DESIGN-space px each built font draws at through Text() (its SSx size / SS): mono 28/2=14,
    // display 40/2=20. A manifest `fontPx` is honoured by scaling relative to this base.
    private const float MonoDesignPx = 14f, DisplayDesignPx = 20f;

    // Draw text at a manifest-specified `fontPx` (design px): scale the built glyph so its on-screen size
    // is fontPx, keeping the 1/SS supersample factor. Falls back to the plain size when fontPx <= 0.
    private void TextPx(SpriteFont font, string s, int x, int y, Color color, double fontPx)
    {
        if (fontPx <= 0) { Text(font, s, x, y, color); return; }
        var basePx = font == _assets.Display ? DisplayDesignPx : MonoDesignPx;
        var scale = (1f / SS) * (float)(fontPx / basePx);
        _spriteBatch.DrawString(font, Safe(font, s), new Vector2(x, y), color, 0f, Vector2.Zero,
            scale, SpriteEffects.None, 0f);
    }

    // Design-space text size: MeasureString is at the SSx raster, so scale back by 1/SS to match Text().
    private Vector2 MeasureText(SpriteFont font, string s) => font.MeasureString(Safe(font, s)) / SS;

    // Manifest text inside a rect: greedy word-wrap to the rect width, capped at the lines the rect
    // HEIGHT can hold (a one-line-high rect never wraps, so names/values stay single-line).
    private void TextPxWrapped(SpriteFont font, string s, Rectangle r, Color color, double fontPx)
    {
        var basePx = font == _assets.Display ? DisplayDesignPx : MonoDesignPx;
        var sc = fontPx <= 0 ? 1f : (float)(fontPx / basePx);
        var lineH = MeasureText(font, "Ay").Y * sc;
        var maxLines = Math.Max(1, (int)Math.Round(r.Height / lineH)); // a near-2-line box (0.9x) still wraps
        if (maxLines == 1 || MeasureText(font, s).X * sc <= r.Width)
        {
            TextPx(font, s, r.X, r.Y, color, fontPx);
            return;
        }
        var line = "";
        var ly = (float)r.Y;
        var lines = 0;
        foreach (var word in s.Split(' '))
        {
            var trial = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && MeasureText(font, trial).X * sc > r.Width)
            {
                TextPx(font, line, r.X, (int)ly, color, fontPx);
                ly += lineH;
                if (++lines >= maxLines) return;
                line = word;
            }
            else line = trial;
        }
        if (line.Length > 0) TextPx(font, line, r.X, (int)ly, color, fontPx);
    }

    // SpriteFonts are ASCII-only and THROW on an unknown glyph. The fold-to-ASCII policy + algorithm
    // live in Core.GlyphSafe (headless-tested); here we just cache each font's glyph set and call it.
    private readonly System.Collections.Generic.Dictionary<SpriteFont, System.Collections.Generic.HashSet<char>> _fontGlyphs = new();
    private string Safe(SpriteFont font, string s)
    {
        if (!_fontGlyphs.TryGetValue(font, out var set))
            _fontGlyphs[font] = set = new System.Collections.Generic.HashSet<char>(font.Characters);
        return Roguebane.Core.GlyphSafe.Sanitize(s, set);
    }

    private static readonly int[] PanelSlice = { 60, 60, 60, 60 }; // style.frames.panel (240px asset)
    private static readonly int[] CardSlice = { 36, 36, 36, 36 };  // style.frames.card (144px asset)

    private void Panel(int x, int y, int w, int h)
    {
        DrawShadow(x, y, w, h, dx: 2, dy: 3, blur: 3, opacity: 0.40f); // §10 depth under the chrome
        // Carved nine-slice frames by size: the PANEL frame (60px corners) for large panels, the lighter
        // CARD frame (36px corners) for card-sized elements, and the clean gradient for small cards/bars
        // (whose corner ornament would be crushed by either frame).
        if (w >= 220 && h >= 170 && DrawFrame(x, y, w, h, "panel", PanelSlice)) return;
        if (w >= 100 && h >= 80 && DrawFrame(x, y, w, h, "card", CardSlice)) return;
        DrawGradient(x, y, w, h, PanelTop, PanelBot, GradientDir.Vertical);
        Border(x, y, w, h, Border0);
    }

    // §10 nine-slice frame: blit a painted frame asset around the rect -- fixed corners, stretched edges
    // + centre (geometry from Core.NineSlice). False if the asset isn't loaded (caller keeps its fallback).
    private bool DrawFrame(int x, int y, int w, int h, string name, int[] slice)
    {
        var tex = _assets.Frame(name);
        if (tex is null) return false;
        var dst = new Roguebane.Core.Layout.LayoutRect(x, y, w, h);
        foreach (var p in Roguebane.Core.Layout.NineSlice.Patches(tex.Width, tex.Height, slice, dst))
            _spriteBatch.Draw(tex, new Rectangle(p.Dst.X, p.Dst.Y, p.Dst.W, p.Dst.H),
                new Rectangle(p.Src.X, p.Src.Y, p.Src.W, p.Src.H), Color.White);
        return true;
    }

    // ===== Generic manifest-driven screen renderer (RESCUE arc) =====
    // Draw a screen's elements straight from layout.json, in Z order. Static types (panel/text/icon/
    // button) render fully here; bound types (list/figure/graph/bar) draw only their chrome for now and
    // get live content in later slices. Each element: shadow -> frame OR fill -> border -> content (§10).
    private void DrawManifestScreen(string screenId)
    {
        var s = _ui.ScreenDef(screenId);
        if (s is null) return;
        // Manifest z is DEPTH (extracted container panels carry high z, leaf content z=1) — draw
        // back-to-front, so a filled panel never paints over the content it contains.
        foreach (var e in s.Elements.OrderByDescending(x => x.Z))
            DrawManifestElement(e, ManifestUi.Rect(s, e));
    }

    private void DrawManifestElement(Element e, Rectangle r)
    {
        // A TEXT element's shadow is a text shadow (offset glyph copy, drawn with the text below) —
        // rect-shadowing it would paint a solid box behind the words.
        if (e.Shadow is { } sh && e.Type != "text")
            DrawShadow(r.X, r.Y, r.Width, r.Height, sh.Dx, sh.Dy, sh.Blur, (float)sh.Opacity);
        // §10: fill is the element's BACKGROUND, the nine-slice frame is chrome layered on it — an
        // element may carry both (topBar), so draw fill first, then frame.
        if (e.Fill is { } fill)
            DrawFill(r, fill);
        if (e.Frame is { } fr && fr.Slice.Length == 4 && _assets.Texture(fr.Asset) is { } ftex)
            DrawFrameTex(ftex, fr, r);
        if (e.Border is { } b)
            Border(r.X, r.Y, r.Width, r.Height, _ui.Color(b.Color ?? "border", Border0));

        switch (e.Type)
        {
            case "text":
                var txt = e.Content ?? ResolveScreenBind(e.Binds);
                if (!string.IsNullOrEmpty(txt))
                {
                    var font = e.Font == "display" ? _assets.Display : _assets.Mono;
                    var px = e.FontPx ?? 0;
                    if (e.Shadow is { } tsh)
                        TextPxWrapped(font, txt!, new Rectangle(r.X + tsh.Dx, r.Y + tsh.Dy, r.Width, r.Height),
                            _ui.Color(tsh.Color ?? "outline", Color.Black) * (float)tsh.Opacity, px);
                    TextPxWrapped(font, txt!, r, _ui.Color(e.Color ?? "ink", Ink), px);
                }
                break;
            case "icon" when !string.IsNullOrEmpty(e.Image):
                Sprite(_assets.Texture(e.Image!), r.X, r.Y, r.Width, r.Height, Color.White);
                break;
            case "button":
                DrawButton(e.Content ?? "", r.X, r.Y, r.Width, r.Height, true, Keys.None);
                break;
            case "list" when e.Item is not null:
                DrawManifestList(e, r);
                break;
            case "figure" when e.Binds is "preview.fig" or "Body":
                // Composed figure: feet at the box bottom-centre, scaled to the box height. In a run
                // the LIVE body draws (part conditions, worn gear); pre-run the build preview does.
                if (InRun)
                    DrawHumanoid(Exp.Player.Body, Exp.FigureId, r.X + r.Width / 2, r.Y + r.Height, r.Height);
                else
                    DrawHumanoid(_build.Preview(), _build.CoreRune.FigureKey(_build.Race),
                        r.X + r.Width / 2, r.Y + r.Height, r.Height);
                break;
            case "figure" when e.Binds == "encounter.foe" && InRun && Exp.Enemy is { } ef:
                if (ef.Frame is { } frame)
                    DrawHumanoid(frame, ef.Figure, r.X + r.Width / 2, r.Y + r.Height, r.Height,
                        ef.Down ? new Color(70, 60, 55) : Color.White, allowBare: false);
                // Part-aim affordance while a module is picking (design/01): limb bands over the
                // structured foe, the band under the cursor highlighted. No persistent aim ring.
                if (!ef.Down && _ctrl.IsTargeting(Exp))
                {
                    if (ef.Frame is not null)
                    {
                        var band = r.Height / 4;
                        for (var bd = 1; bd < 4; bd++) Rect(r.X, r.Y + bd * band, r.Width, 1, new Color(Ink, 90));
                        if (FoePartAt(ef, _cursor) is { } hov)
                            Border(r.X, r.Y + PartBand(hov.Stat) * band, r.Width, band, Ink);
                    }
                    else Border(r.X, r.Y, r.Width, r.Height, new Color(Ink, 110));
                }
                else if (!ef.Down && Hover(r)) Border(r.X, r.Y, r.Width, r.Height, Ink);
                break;
            case "graph" when e.Binds == "map" && InRun && e.Item is not null:
                DrawManifestGraph(e, r);
                break;
            case "graph" when e.Binds == "campaign" && InRun && e.Item is not null:
                DrawCampaignGraph(e, r);
                break;
            case "figure" when e.Binds == "encounter.minions" && InRun:
                // The fielded retinue on the battlefield: each minion's sprite, feet on the box floor.
                for (var mi = 0; mi < Exp.Minions.Count; mi++)
                {
                    var tex = _assets.Minion(Exp.Minions[mi].Id);
                    var mw = r.Height * 2 / 3;
                    var mx = r.X + mi * (mw + 8);
                    if (tex is not null) Sprite(tex, mx, r.Y, mw, r.Height, Color.White);
                    else Rect(mx + 4, r.Y + 8, mw - 8, r.Height - 16, new Color(Amber, 70));
                }
                break;
        }
    }

    // The city chart (design/03): map nodes spread over the graph element's region via GraphLayout —
    // links first (dashed when uncharted), then a beacon per node (fog-aware icon), the current node
    // ringed and reachable deployments numbered. Same live rules as the legacy chart, manifest geometry.
    private void DrawManifestGraph(Element e, Rectangle region)
    {
        var map = Exp.Map;
        Template? tmplNode = null;
        _ui.Manifest?.Templates.TryGetValue(e.Item!.Template, out tmplNode);
        var nodes = map.Nodes;
        var cols = nodes.Max(x => x.Col) + 1;
        var rows = nodes.Max(x => x.Row) + 1;
        var cw = e.Item!.Size.Length == 2 ? e.Item.Size[0] : 28;
        var ch = e.Item.Size.Length == 2 ? e.Item.Size[1] : 28;
        Rectangle Cell(MapNode n) => RectOf(GraphLayout.Cell(
            new LayoutRect(region.X, region.Y, region.Width, region.Height), cols, rows, n.Col, n.Row, cw, ch));

        foreach (var node in nodes)
        {
            var from = Cell(node);
            foreach (var nid in node.Next)
            {
                var to = Cell(map.Node(nid));
                var charted = node.Visited; // a link out of a charted beacon is itself charted
                Line(from.X + cw / 2, from.Y + ch / 2, to.X + cw / 2, to.Y + ch / 2, 2,
                    charted ? new Color(150, 130, 95) : new Color(90, 78, 66), dashed: !charted);
            }
        }

        var options = map.Options;
        foreach (var node in nodes)
        {
            var r = Cell(node);
            var seen = map.Sees(node);
            var isCurrent = ReferenceEquals(node, map.Current);
            // The icon comes from the template's imageBind path ("icons/node/{node.type}"), resolved
            // with the FOG-AWARE type so an unrevealed beacon blits the unknown token.
            var tex = _assets.Node(seen);
            var iconBind = tmplNode?.Parts.FirstOrDefault(pt => pt.ImageBind is not null)?.ImageBind;
            if (iconBind is not null)
                tex = _assets.Texture(iconBind.Replace("{node.type}", AssetRegistry.NodeToken(seen))) ?? tex;
            Sprite(tex, r.X, r.Y, cw, ch, isCurrent ? Color.White : new Color(210, 200, 190));

            var oi = IndexOf(options, node);
            if (isCurrent)
            {
                Border(r.X - 3, r.Y - 3, cw + 6, ch + 6, Amber);
                Text(_assets.Mono, "you are here", r.X - 8, r.Y + ch + 2, Amber);
            }
            else if (oi >= 0) // a reachable onward deployment
            {
                Border(r.X - 2, r.Y - 2, cw + 4, ch + 4, Hover(r) ? Ink : new Color(150, 130, 95));
                Text(_assets.Mono, $"[{oi + 1}] {seen.ToString().ToLower()}", r.X - 6, r.Y + ch + 2, Ink);
            }
        }
    }

    // The campaign chart (design/04): one city marker per campaign leg, spread across the graph
    // region — taken legs link solid (good), the current leg is framed, onward legs run dotted.
    // City NAMES are OPEN content (§12/§17: count + procgen-vs-authored undecided) so only the tier/
    // status label draws; the design's castle icons aren't in the manifest template (Needs-CD).
    private void DrawCampaignGraph(Element e, Rectangle region)
    {
        var cw = e.Item!.Size.Length == 2 ? e.Item.Size[0] : 8;
        var ch = e.Item.Size.Length == 2 ? e.Item.Size[1] : 8;
        var count = _campaign.LegCount;
        var m = _ui.Manifest;
        Template? tmpl = null;
        if (m is not null) m.Templates.TryGetValue(e.Item.Template, out tmpl);
        Rectangle Cell(int i) => RectOf(GraphLayout.Cell(
            new LayoutRect(region.X, region.Y, region.Width, region.Height), count, 1, i, 0, cw, ch));

        for (var i = 0; i < count - 1; i++)
        {
            var a = Cell(i); var b = Cell(i + 1);
            var taken = i < _campaign.LegIndex;
            Line(a.X + cw / 2, a.Y + ch / 2, b.X + cw / 2, b.Y + ch / 2, 2,
                taken ? _ui.Color("good", Amber) : i == _campaign.LegIndex
                    ? Amber : new Color(90, 78, 66), dashed: !taken && i != _campaign.LegIndex);
        }
        for (var i = 0; i < count; i++)
        {
            var c = Cell(i);
            var taken = i < _campaign.LegIndex;
            var current = i == _campaign.LegIndex;
            Rect(c.X, c.Y, c.Width, c.Height,
                taken ? _ui.Color("good", Amber) : current ? Amber : new Color(90, 78, 66));
            if (current) Border(c.X - 4, c.Y - 4, c.Width + 8, c.Height + 8, Amber);
            if (tmpl is null) continue;
            foreach (var pp in CardTemplate.Place(tmpl, c.X, c.Y))
            {
                // Only the tier/status line has data; city names are OPEN content — draw nothing there.
                if (pp.Binds != "city.tier") continue;
                var label = "Tier " + (i + 1) + (taken ? " - TAKEN" : current ? " - CURRENT" : "");
                TextPxWrapped(pp.Font == "display" ? _assets.Display : _assets.Mono,
                    label, RectOf(pp.Rect), _ui.Color(pp.Color ?? "ink", Ink), pp.FontPx);
            }
        }
    }

    // Screen-level (non-list) binds -> display text: the NewGame Loadout preview reads the BuildSession.
    private string? ResolveScreenBind(string? bind) => bind switch
    {
        "preview.name" => _build.Race.Name + " " + _build.CoreRune.Title,
        "preview.role" => _build.CoreRune.Archetype,
        "preview.hp" => _build.Race.Hp.ToString(),
        "preview.budget" => _build.CoreRune.RuneBudget.ToString(),
        "preview.techniques" => _build.CoreRune.Kit.Count.ToString(),
        "preview.bays" => _build.CoreRune.Bays.ToString(),
        "preview.apexName" => _build.CoreRune.ApexName,
        "preview.apexDesc" => _build.CoreRune.ApexDesc,
        "core" => _build.Race.Name + " " + _build.CoreRune.Title,
        "runes.budget" => _build.Runes.Available + " free / " + _build.Runes.Budget,
        "Body.hp" => InRun ? Exp.Player.Hp + " / " + Exp.Player.MaxHp : null,
        "encounter.foe.hp" => InRun && Exp.Enemy is { } foe ? foe.Hp + " / " + foe.MaxHp : null,
        // Combat verbs (design/01 chips; labels were flattened by extraction -> authored here).
        "combat.autoAttack" => InRun ? (Exp.IsAuto() ? "AUTO-ATTACK ON" : "AUTO-ATTACK") : null,
        "combat.flee" => InRun ? "FLEE" : null,
        "combat.paused" => _paused ? "HELD" : null, // badge shows only while the fight is held
        "campaign.taken" => InRun ? _campaign.LegIndex + " / " + _campaign.LegCount : null,
        _ => null,
    };

    // Run state exists only after a march; encounter binds fall back to samples until then.
    private bool InRun => _campaign is not null;

    // A list container: stamp its item template into each cell (ListLayout), filling each part from the
    // i-th LIVE datum's `binds` (falling back to the manifest `sample` where a bind isn't mapped yet).
    private void DrawManifestList(Element e, Rectangle r)
    {
        var m = _ui.Manifest;
        if (m is null || e.Item is null || !m.Templates.TryGetValue(e.Item.Template, out var tmpl)) return;
        var data = ListData(e.Binds);
        var count = data?.Count ?? ListCountFor(e.Binds);
        var region = new LayoutRect(r.X, r.Y, r.Width, r.Height);
        var cells = ListLayout.Cells(region, e.Item, count, tmpl.Size);
        // Which datum is CHOSEN, for `.selection` parts (the ring/chip only the picked card wears).
        var selIx = e.Binds switch
        {
            "races" => _build.RaceIndex,
            "cores" => _build.CoreRuneIndex,
            _ => -1,
        };
        for (var i = 0; i < cells.Count; i++)
        {
            var datum = data is not null && i < data.Count ? data[i] : null;
            var cell = cells[i];
            // Positional binds repeat the SAME bind N times per card (attr tiles 4x, attr-bar pips 12x,
            // in template order); count each occurrence per card to pick the right datum slice.
            int valIx = 0, keyIx = 0, pipIx = 0;
            var occ = new System.Collections.Generic.Dictionary<string, int>(); // per-bind occurrence (rune rows)
            foreach (var pp in CardTemplate.Place(tmpl, cell.X, cell.Y))
            {
                if (pp.Binds is { } sel && sel.EndsWith(".selection") && i != selIx)
                    continue; // only the chosen card wears its selection chip
                // FSM state parts resolve from the LIVE run — an idle card shows no chip/label at all
                // (never the sample), so resolve BEFORE chrome and bail when there's nothing to say.
                string? stateText = null;
                var isStatePart = pp.Binds is "technique.state" or "bay.state" or "technique.cooldownLabel";
                if (isStatePart)
                {
                    // The card the targeting FSM is picking a foe for reads AIMING / "locking on".
                    var aiming = e.Binds == "loadout.techniques" && InRun && _ctrl.Targeting == i;
                    stateText = ResolveStateBind(datum, pp.Binds!, aiming);
                    if (string.IsNullOrEmpty(stateText)) continue;
                }
                // Rune-bag rows (g.runes.* repeats twice per group): a LIVE ladder shows its held
                // rung (or the first) then the next — resolved copy only, never the template samples.
                if (pp.Binds is { } gb && gb.StartsWith("g.runes") && datum is not null)
                {
                    var row = occ.GetValueOrDefault(gb);
                    occ[gb] = row + 1;
                    var rtxt = RuneRow(datum, row) is { } mk ? RuneBind(mk, gb) : null;
                    if (!string.IsNullOrEmpty(rtxt))
                        TextPxWrapped(pp.Font == "display" ? _assets.Display : _assets.Mono,
                            rtxt!, RectOf(pp.Rect), _ui.Color(pp.Color ?? "ink", Ink), pp.FontPx);
                    continue;
                }
                // Charge/cooldown progress: the fill bar's WIDTH is the resolved fraction.
                if (pp.Binds == "technique.chargePct")
                {
                    if (InRun && datum is Roguebane.Core.Technique ct && pp.Fill is { } cf)
                    {
                        var st = Exp.Status(ct);
                        var pct = st.Ready ? 1f
                            : st.Active && st.Cooldown > 0 ? 1f - (float)st.Countdown / st.Cooldown : 0f;
                        if (pct > 0f)
                            DrawFill(new Rectangle(pp.Rect.X, pp.Rect.Y,
                                (int)(pp.Rect.W * Math.Clamp(pct, 0f, 1f)), pp.Rect.H), cf);
                    }
                    continue;
                }
                // Remaining state-driven chrome (.rarity) still needs its model — keep gated.
                var stateBound = pp.Binds is { } sb && sb.EndsWith(".rarity");
                if (!stateBound)
                {
                    // attr.color binds the swatch's fill TOKEN to the datum (str/int/dex/con);
                    // attrs.pip picks per PIP INDEX: filled -> the attr's token, allocatable -> slot,
                    // beyond the cap -> nothing.
                    string? fillTok = null;
                    var skipFill = false;
                    if (pp.Binds == "attr.color" && datum is not null)
                        fillTok = ResolveBind(datum, pp.Binds);
                    else if (pp.Binds == "attrs.pip" && datum is ValueTuple<string, string, int, int, string> ab)
                    {
                        var p = pipIx++;
                        fillTok = p < ab.Item3 ? ab.Item5 : p < ab.Item4 ? "slot" : null;
                        skipFill = fillTok is null;
                    }
                    // Glyph tiles colour by the datum's stat (technique/minion cards).
                    else if (pp.Binds is "technique.icon" or "loadout.glyph" && datum is Roguebane.Core.Technique tq)
                        fillTok = tq.Stat.ToString().ToLowerInvariant();
                    else if (pp.Binds is "technique.icon" or "loadout.glyph" && datum is Roguebane.Core.Minion mnq)
                        fillTok = mnq.Stat.ToString().ToLowerInvariant();
                    if (fillTok is not null) DrawFill(RectOf(pp.Rect), new Fill { Token = fillTok });
                    else if (!skipFill && pp.Fill is { } pf) DrawFill(RectOf(pp.Rect), pf);
                    if (pp.Border is { } pb)
                        Border(pp.Rect.X, pp.Rect.Y, pp.Rect.W, pp.Rect.H, _ui.Color(pb.Color, Border0));
                }
                // imageBind (CD #15): a Content path template whose {bind} placeholders resolve
                // from the bound item — the part blits that PNG instead of text/fill glyphs.
                var img = pp.Image;
                if (pp.ImageBind is { } ib && datum is not null)
                    img = System.Text.RegularExpressions.Regex.Replace(ib, @"\{(.+?)\}",
                        mm => ResolveBind(datum, mm.Groups[1].Value) ?? "");
                if (!string.IsNullOrEmpty(img))
                {
                    Sprite(_assets.Texture(img!), pp.Rect.X, pp.Rect.Y, pp.Rect.W, pp.Rect.H, Color.White);
                    continue;
                }
                var text = isStatePart ? stateText : pp.Binds switch
                {
                    "race.attrs.value" => AttrTile(datum, valIx++)?.value,
                    "race.attrs.key" => AttrTile(datum, keyIx++)?.key,
                    "attr.color" => null, // the swatch is pure fill, no text
                    _ => datum is not null ? ResolveBind(datum, pp.Binds) : null,
                } ?? pp.Sample;
                if (!string.IsNullOrEmpty(text))
                    TextPxWrapped(pp.Font == "display" ? _assets.Display : _assets.Mono,
                        text!, RectOf(pp.Rect), _ui.Color(pp.Color ?? "ink", Ink), pp.FontPx);
            }
        }
    }

    // The i-th attribute tile (STR/INT/DEX/CON order) of a Race datum, for the per-attr card tiles.
    private static (string key, string value)? AttrTile(object? datum, int i) => datum is Roguebane.Core.Race r
        ? i switch
        {
            0 => ("STR", r.Str.ToString()),
            1 => ("INT", r.Int.ToString()),
            2 => ("DEX", r.Dex.ToString()),
            3 => ("CON", r.Con.ToString()),
            _ => null,
        }
        : null;

    private static Rectangle RectOf(LayoutRect r) => new(r.X, r.Y, r.W, r.H);

    // The live data a bound list stamps one card per, or null for an unmapped bind (falls back to samples).
    private System.Collections.Generic.IReadOnlyList<object>? ListData(string? bind) => bind switch
    {
        "races" => Roguebane.Core.Content.Races.Roster.Cast<object>().ToList(),
        "cores" => Roguebane.Core.Content.CoreRunes.Roster.Cast<object>().ToList(),
        "preview.attrs" => PreviewAttrs(),
        "attrs" => AttrBars(),
        "loadout" => _build.Equipment.Cast<object>().ToList(),
        "minions" => _build.CoreRune.MinionKit.Concat(_build.Runes.GrantedMinions).Cast<object>().ToList(),
        // Inventory follows the tab strip: GEAR = the run's wielded/worn/packed pieces (empty pre-run
        // — gear only exists once marching), TECHNIQUES = the palette, MINIONS = the retinue.
        "invItems" => _invTab switch
        {
            0 => InRun
                ? Exp.Player.Body.Hands.Cast<object>()
                    .Concat(Exp.Stash.Weapons).Concat(Exp.Stash.Armor).ToList()
                : new List<object>(),
            1 => _build.Palette.Cast<object>().ToList(),
            2 => _build.CoreRune.MinionKit.Concat(_build.Runes.GrantedMinions).Cast<object>().ToList(),
            _ => null,
        },
        // The Rune Bag (design/02): one group per PATH ladder — the MARKS/PATHS/KEYSTONES taxonomy
        // is OPEN (§17), so the model's actual grouping (ladders) is what renders.
        "runeGroups" => _build.Paths.Cast<object>().ToList(),
        // Encounter (design/01): the combat pool + action bar read the RUN body once marching.
        "pool" => AttrBars(),
        "loadout.techniques" => InRun ? Exp.Equipment.Cast<object>().ToList()
                                      : _build.Equipment.Cast<object>().ToList(),
        "loadout.bays" => InRun ? Exp.Minions.Cast<object>().ToList()
                                : _build.CoreRune.MinionKit.Cast<object>().ToList(),
        _ => null,
    };

    // The attribute bars/pool rows: one datum per stat — (key, part label §6, free pool, capacity,
    // pip colour token). In a run the LIVE body supplies them (actives reserve, damage shrinks caps);
    // pre-run it's the build preview, where nothing is reserved so free == capacity.
    private System.Collections.Generic.IReadOnlyList<object> AttrBars()
    {
        var b = InRun ? Exp.Player.Body : _build.Preview();
        return new object[]
        {
            ("STR", "Arms", b.Available(Stat.Str), b.Capacity(Stat.Str), "str"),
            ("INT", "Head", b.Available(Stat.Int), b.Capacity(Stat.Int), "int"),
            ("DEX", "Legs", b.Available(Stat.Dex), b.Capacity(Stat.Dex), "dex"),
            ("CON", "Chest", b.Available(Stat.Con), b.Capacity(Stat.Con), "con"),
        };
    }

    // The Loadout preview's 4 attribute tiles: key/value/swatch-token per stat, from the LIVE build.
    private System.Collections.Generic.IReadOnlyList<object> PreviewAttrs()
    {
        var b = _build.Preview();
        return new object[]
        {
            ("STR", b.Capacity(Stat.Str).ToString(), "str"),
            ("INT", b.Capacity(Stat.Int).ToString(), "int"),
            ("DEX", b.Capacity(Stat.Dex).ToString(), "dex"),
            ("CON", b.Capacity(Stat.Con).ToString(), "con"),
        };
    }

    // Resolve a template part's `binds` against a live datum -> display text, or null to use the sample.
    // Missing-data binds (race tag/blurb, per-attr tiles, apex text) return null pending their data.
    private static string? ResolveBind(object datum, string? bind) => datum switch
    {
        Roguebane.Core.Race r => bind switch
        {
            "race.name" => r.Name,
            "race.hp" => r.Hp.ToString(),
            "race.tag" => r.Tag,
            "race.blurb" => r.Blurb,
            _ => null,
        },
        Roguebane.Core.CoreRune c => bind switch
        {
            "core.name" => c.Title,
            "core.role" => c.Archetype,
            "core.budget" => c.RuneBudget.ToString(),
            "core.bays" => c.Bays.ToString(),
            "core.actionSlots" => c.Kit.Count.ToString(),
            "core.apexName" => c.ApexName,
            "core.apexDescription" => c.ApexDesc,
            _ => null,
        },
        ValueTuple<string, string, string> a => bind switch // (key, value, swatch-token) attr tile
        {
            "attr.key" => a.Item1,
            "attr.value" => a.Item2,
            "attr.color" => a.Item3,
            _ => null,
        },
        ValueTuple<string, string, int, int, string> ab => bind switch // (key, part, free, cap, token) attr bar
        {
            "attrs.key" or "pool.attr.key" => ab.Item1,
            "attrs.part" or "pool.attr.part" => ab.Item2,
            "attrs.alloc" or "pool.attr.alloc" => ab.Item3.ToString(),
            "attrs.available" or "pool.attr.available" => ab.Item4.ToString(),
            _ => null,
        },
        Roguebane.Core.Technique t => bind switch
        {
            "loadout.name" or "invItems.name" or "technique.name" => DisplayName(t.Id),
            "loadout.attr" => t.Stat.ToString().ToUpperInvariant() + " " + t.Reserve,
            "invItems.badgeLabel" or "technique.cost" => t.Stat.ToString().ToUpperInvariant(),
            "invItems.badgeNum" => t.Reserve.ToString(),
            "technique.description" => t.DescText,
            _ => null,
        },
        Roguebane.Core.Weapon w => bind switch
        {
            "invItems.name" => DisplayName(w.Id),
            "invItems.badgeLabel" => w.Stat.ToString().ToUpperInvariant(),
            "invItems.badgeNum" => w.Reserve.ToString(),
            _ => null,
        },
        Roguebane.Core.Armor ar => bind switch
        {
            "invItems.name" => DisplayName(ar.Id),
            "invItems.badgeLabel" => ar.Group.ToString().ToUpperInvariant(),
            "invItems.badgeNum" => ar.Value.ToString(),
            _ => null,
        },
        Roguebane.Core.Minion mn => bind switch
        {
            "loadout.name" or "invItems.name" or "bay.name" => DisplayName(mn.Id),
            "loadout.attr" => mn.Stat.ToString().ToUpperInvariant() + " " + mn.Reserve,
            "invItems.badgeLabel" or "bay.cost" => mn.Stat.ToString().ToUpperInvariant(),
            "invItems.badgeNum" => mn.Reserve.ToString(),
            "bay.description" => mn.DescText,
            _ => null,
        },
        _ => null,
    };

    // Content ids are lower-case ("swing", "skeleton"); cards show them capitalised, per design/02.
    private static string DisplayName(string id) =>
        id.Length == 0 ? id : char.ToUpperInvariant(id[0]) + id[1..];

    // A card's live FSM read (design/01 chips + cooldown label): null = idle, show nothing.
    // Countdown is in fixed ticks (10/s) -> seconds for the label.
    private string? ResolveStateBind(object? datum, string bind, bool aiming = false)
    {
        if (!InRun) return null;
        if (datum is Roguebane.Core.Technique t)
        {
            var st = Exp.Status(t);
            return bind switch
            {
                "technique.state" => aiming ? "AIMING"
                    : st.ChargeDry ? "DRY"
                    : st.Sustained && st.Active ? "HELD"
                    : st.Ready ? "READY"
                    : st.Active && st.Countdown > 0 ? "COOLDOWN" : null,
                "technique.cooldownLabel" => aiming ? "locking on"
                    : st.Ready ? "ready"
                    : st.Sustained && st.Active ? "held"
                    : st.Active && st.Countdown > 0 ? (st.Countdown / 10f).ToString("0.0") + "s" : null,
                _ => null,
            };
        }
        // A fielded minion is ACTIVE by definition (the bays list holds the live retinue in a run).
        return datum is Roguebane.Core.Minion && bind == "bay.state" ? "ACTIVE" : null;
    }

    // Which rung a rune-group row shows: the held rung (or the first) then the next — the pair the
    // player acts on. Rows past the ladder clamp to the keystone.
    private Roguebane.Core.Mark? RuneRow(object? datum, int row)
    {
        if (datum is not System.Collections.Generic.IReadOnlyList<Roguebane.Core.Mark> ladder
            || ladder.Count == 0) return null;
        var held = _build.Runes.CurrentRank(ladder[0].Path);
        var first = held > 0 ? Math.Min(held - 1, ladder.Count - 1) : 0;
        return ladder[Math.Min(first + row, ladder.Count - 1)];
    }

    private string? RuneBind(Roguebane.Core.Mark m, string bind) => bind switch
    {
        "g.runes.name" => m.DisplayName,
        "g.runes.effect" => RuneEffect(m),
        "g.runes.state" => _build.Runes.Has(m) ? "EQUIPPED"
            : _build.Runes.CurrentRank(m.Path) == m.Rank - 1
                && _build.Runes.Available >= _build.Runes.EffectiveCost(m) ? "EQUIPPABLE" : "LOCKED",
        "g.runes.cost" => _build.Runes.EffectiveCost(m) + "p",
        _ => null, // icon glyph + stack countLabel have no model — draw nothing, never the sample
    };

    // A rune's effect line, derived from what the rung actually grants so copy can't drift from data.
    private static string RuneEffect(Roguebane.Core.Mark m)
    {
        var bits = new System.Collections.Generic.List<string>();
        foreach (var p in m.Granted) bits.Add("Sockets +" + p.Capacity + " " + p.Stat.ToString().ToUpperInvariant());
        foreach (var t in m.GrantedTechniques) bits.Add("Grants the " + DisplayName(t.Id) + " technique");
        foreach (var mn in m.GrantedMinions) bits.Add("Grants the " + DisplayName(mn.Id) + " minion");
        if (bits.Count == 0) bits.Add("Rung " + m.Rank + " of the " + m.Path + " ladder");
        return string.Join("; ", bits) + ".";
    }

    private static int ListCountFor(string? bind) => 3; // sample-count fallback for unmapped binds

    private void DrawFrameTex(Texture2D tex, Frame fr, Rectangle r)
    {
        var dst = new LayoutRect(r.X, r.Y, r.Width, r.Height);
        foreach (var p in NineSlice.Patches(tex.Width, tex.Height, fr.Slice, dst,
                     tile: fr.Repeat == "tile", centerFill: fr.CenterFill))
            _spriteBatch.Draw(tex, new Rectangle(p.Dst.X, p.Dst.Y, p.Dst.W, p.Dst.H),
                new Rectangle(p.Src.X, p.Src.Y, p.Src.W, p.Src.H), Color.White);
    }

    private void DrawFill(Rectangle r, Fill fill)
    {
        if (fill.IsGradient)
            DrawGradient(r.X, r.Y, r.Width, r.Height,
                _ui.Color(fill.From ?? "panel", PanelTop), _ui.Color(fill.To ?? "border", PanelBot),
                fill.Dir == "horizontal" ? GradientDir.Horizontal : GradientDir.Vertical);
        else if (!string.IsNullOrEmpty(fill.Token))
            Rect(r.X, r.Y, r.Width, r.Height, _ui.Color(fill.Token!, Panel0));
    }

    private enum GradientDir { Vertical, Horizontal }

    // §10 gradient fill, ENGINE-drawn: interpolate `from`->`to` across the rect in 1px strips (the
    // PointClamp sampler rules out a stretched-texture lerp). Diagonal isn't needed yet -> vertical.
    private void DrawGradient(int x, int y, int w, int h, Color from, Color to, GradientDir dir)
    {
        if (w <= 0 || h <= 0) return;
        if (dir == GradientDir.Horizontal)
            for (var i = 0; i < w; i++)
                Rect(x + i, y, 1, h, Color.Lerp(from, to, w <= 1 ? 0f : (float)i / (w - 1)));
        else
            for (var i = 0; i < h; i++)
                Rect(x, y + i, w, 1, Color.Lerp(from, to, h <= 1 ? 0f : (float)i / (h - 1)));
    }

    // §10 drop shadow, ENGINE-drawn (never baked into art -> resolution-independent): the element
    // silhouette offset by (dx,dy), softened by `blur` concentric rings of decaying alpha, UNDER the
    // element. Drawn outer-faint -> inner-dark so the core reads solid and the edge fades.
    private void DrawShadow(int x, int y, int w, int h, int dx, int dy, int blur, float opacity)
    {
        if (w <= 0 || h <= 0) return;
        var peak = (int)(Math.Clamp(opacity, 0f, 1f) * 255);
        for (var i = blur; i >= 0; i--)
        {
            var a = peak * (blur - i + 1) / (blur + 1); // outer rings fainter
            Rect(x + dx - i, y + dy - i, w + 2 * i, h + 2 * i, new Color(0, 0, 0, a));
        }
    }

    // A skinned button whose state is driven by input (manifest: drives-from input/interaction).
    // Stretch-scaled for now; true 9-slice is polish. Returns nothing — input lives in Update.
    private void DrawButton(string label, int x, int y, int w, int h, bool enabled, Keys key)
    {
        var hovered = enabled && Hover(new Rectangle(x, y, w, h));
        var state = !enabled ? "disabled" : _keys.IsKeyDown(key) || hovered ? "down" : "normal";
        var skin = _assets.Button(state);
        if (skin is not null) Sprite(skin, x, y, w, h, Color.White);
        else Panel(x, y, w, h);
        var size = MeasureText(_assets.Mono, label);
        Text(_assets.Mono, label, (int)(x + w / 2 - size.X / 2), (int)(y + h / 2 - size.Y / 2),
            enabled ? Ink : Muted);
        if (hovered) Border(x, y, w, h, Amber);
    }

    // A labelled value bar (HP, etc.): icon, filled track, and the mono "cur/max" readout.
    private void DrawBar(int x, int y, int w, Texture2D? icon, int cur, int max, Color fill)
    {
        Sprite(icon, x, y, 20, 20, Color.White);
        var bx = x + 24;
        var bw = w - 24;
        Rect(bx, y + 2, bw, 16, new Color(40, 30, 24));
        if (max > 0) Rect(bx, y + 2, bw * Math.Max(0, cur) / max, 16, fill);
        Text(_assets.Mono, $"{cur}/{max}", bx + 6, y + 2, Ink);
    }

    private void Rect(int x, int y, int w, int h, Color color)
    {
        if (w <= 0 || h <= 0) return;
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), color);
    }

    private void Border(int x, int y, int w, int h, Color color)
    {
        Rect(x, y, w, 2, color);
        Rect(x, y + h - 2, w, 2, color);
        Rect(x, y, 2, h, color);
        Rect(x + w - 2, y, 2, h, color);
    }

    // A straight line between two points (a stretched, rotated 1px texture) for the chart's links.
    // dashed => draw only alternating segments, so an uncharted jump reads dotted.
    private void Line(int x1, int y1, int x2, int y2, int thickness, Color color, bool dashed = false)
    {
        var a = new Vector2(x1, y1);
        var b = new Vector2(x2, y2);
        var delta = b - a;
        var len = delta.Length();
        if (len < 1f) return;
        var angle = (float)Math.Atan2(delta.Y, delta.X);
        if (!dashed)
        {
            _spriteBatch.Draw(_pixel, a, null, color, angle,
                new Vector2(0, 0.5f), new Vector2(len, thickness), SpriteEffects.None, 0f);
            return;
        }
        var dir = delta / len;
        const float seg = 6f;
        for (var d = 0f; d < len; d += seg * 2)
        {
            var p = a + dir * d;
            var segLen = Math.Min(seg, len - d);
            _spriteBatch.Draw(_pixel, p, null, color, angle,
                new Vector2(0, 0.5f), new Vector2(segLen, thickness), SpriteEffects.None, 0f);
        }
    }
}
