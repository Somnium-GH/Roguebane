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

    // Screen names per the 2026-06-30 rename directive. (Manifest lookup ids stay "newrun"/"build"/
    // "runmap" until Claude Design renames the manifest side in parallel.)
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

        if (_smokeScreen is "combat" or "map" or "loadout") // march the real loop for the screenshot
        {
            _build.CycleCoreRune(3);          // -> the Summoner (3 bays; fields Skeleton+Shade) for the bay lane
            _build.Toggle(Techniques.Jab);   // add a STR card for variety on the bar
            _campaign = _build.Redeploy(Maps.StandardLegs(3));
            _screen = Screen.Run;
            foreach (var t in Exp.Equipment) _campaign.Toggle(t); // power the bar (both shots)
            // (build/newrun smoke handled after this block)
            void Resolve() { for (var i = 0; i < 200 && Exp.State == ExpeditionState.Fighting; i++) _campaign.Tick(); }

            if (_smokeScreen == "map") // stop at the merchant so the shot shows the gear stock + gear bar
            {
                _campaign.Enter("a1"); Resolve(); _campaign.Redeploy(); // earn gold, then back to the chart
                _campaign.Enter("b");             // the merchant
                Exp.BuyWeapon(Armory.Dagger);     // dagger 2 -> pack
                Exp.EquipWeapon(Armory.Dagger);   // -> EQUIPPED
                Exp.Stash.AddArmor(Shops.Plate);  // seed a PACK item for the click-to-equip chip in the shot
            }
            else if (_smokeScreen == "combat")
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
        else if (_smokeScreen == "build") _screen = Screen.Equipment; // else fall through to the New Run grid

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

    // New Run (design/05): the core grid. Pick with arrows or a card click; BEGIN goes to the equipment.
    // Card positions come from the manifest `coreCards` list container (region + item), falling back to
    // a hand grid if the manifest is absent — the first screen container driven off layout.json.
    private Rectangle NewGameCardRect(int i)
    {
        var cells = _ui.ListCells("newrun", "coreCards", _build.Roster.Count);
        return cells is not null && i < cells.Count ? cells[i] : new Rectangle(14 + i * 188, 66, 176, 420);
    }
    private static readonly Rectangle NewGameBeginRect = new(W - 258, H - 44, 240, 34);

    // Race selector chips (design/05's Race column, minimal strip form until the 3-column redesign):
    // bottom-left, clear of the manifest-driven core-card grid and the bottom-right BEGIN button.
    private static Rectangle NewGameRaceRect(int i) => new(24 + i * 104, H - 48, 96, 40);

    private void UpdateNewGame(KeyboardState keys)
    {
        if (Pressed(keys, Keys.Left)) _build.CycleCoreRune(-1);
        if (Pressed(keys, Keys.Right)) _build.CycleCoreRune(1);
        for (var i = 0; i < _build.Roster.Count; i++)
            if (Click(NewGameCardRect(i))) _build.CycleCoreRune(i - _build.CoreRuneIndex);

        // Race axis: Tab cycles, or click a chip. Attrs/HP + the composed figure follow the choice.
        if (Pressed(keys, Keys.Tab)) _build.CycleRace(1);
        for (var i = 0; i < _build.RaceCount; i++)
            if (Click(NewGameRaceRect(i))) _build.CycleRace(i - _build.RaceIndex);

        var go = (Pressed(keys, Keys.Enter) && !keys.IsKeyDown(Keys.LeftAlt)) || Click(NewGameBeginRect);
        if (go) _screen = Screen.Equipment; // on to the equipment screen for the chosen core
    }

    private void UpdateEquipment(KeyboardState keys)
    {
        if (Pressed(keys, Keys.Left)) _build.CycleCoreRune(-1);
        if (Pressed(keys, Keys.Right)) _build.CycleCoreRune(1);

        for (var i = 0; i < TechniqueKeys.Length && i < _build.Palette.Count; i++)
            if (Pressed(keys, TechniqueKeys[i]))
                _build.Toggle(_build.Palette[i]);

        // Mouse: click a chassis, a palette card to toggle, the march CTA. (The throwaway rune-ladder
        // test is retired; the real rune-bag UI, design/02, is the deferred replacement.)
        for (var i = 0; i < _build.CoreRuneCount; i++)
            if (Click(CoreRuneRect(i))) _build.CycleCoreRune(i - _build.CoreRuneIndex);
        for (var i = 0; i < _build.Palette.Count; i++)
            if (Click(PaletteRect(i))) _build.Toggle(_build.Palette[i]);

        // March the campaign. The chassis ships a fixed kit so the bar is never empty — no gate.
        // Alt+Enter is the fullscreen toggle, not a march.
        var march = (Pressed(keys, Keys.Enter) && !keys.IsKeyDown(Keys.LeftAlt)) || Click(RedeployRect);
        if (march)
        {
            _campaign = _build.Redeploy(Maps.StandardLegs(3));
            // Techniques start INACTIVE: the bar is slotted but nothing is reserved/aimed/firing until
            // the player clicks a card. (No auto-arm — that bug had the whole bar auto-targeting.)
            _screen = Screen.Run;
        }
    }

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
        if (Pressed(keys, Keys.Space) || Click(PauseRect)) _paused = !_paused;
        if (Pressed(keys, Keys.F) || Click(RetreatRect)) Exp.Retreat();
        if (Pressed(keys, Keys.Tab) || Click(AutoRect)) _ctrl.ToggleAuto(Exp); // ONE global toggle

        // The targeting FSM lives in Core (CombatTargeting); the shell only feeds it press intents.
        // Card LEFT-press powers/enters-targeting; card RIGHT-press unpowers.
        var rclickOnCard = false;
        for (var i = 0; i < TechniqueKeys.Length && i < Exp.Equipment.Count; i++)
        {
            if (Pressed(keys, TechniqueKeys[i]) || Click(ActionCardRect(i))) _ctrl.CardPress(Exp, i);
            if (RightClick(ActionCardRect(i))) { rclickOnCard = true; _ctrl.CardRightPress(Exp, i); }
        }

        // While a module is targeting: LEFT-press a live foe (clicked limb -> part aim) to set + exit;
        // RIGHT-press the battlefield cancels. A charged + targeted module fires on its own (no button).
        if (_ctrl.IsTargeting(Exp))
        {
            if (Exp.Enemy is { Down: false } foe && Click(FoeRect(0)))
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
    private static Rectangle CoreRuneRect(int i) => new(180 + i * 110 - 2, 4, 100, 32);
    private static Rectangle PaletteRect(int i) => new(320 + i * 52, 300, 48, 48); // fits 7 before the bay preview
    private static readonly Rectangle RedeployRect = new(40, H - 52, 300, 44);
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
    // Single-foe (canon): ONE enemy, drawn large on the right. The index is vestigial (always 0).
    private static Rectangle FoeRect(int i = 0) => new(632, 96, 224, 252);
    private static readonly Rectangle PauseRect = new(W - 156, H - 84, 110, 26);
    private static readonly Rectangle RetreatRect = new(W - 156, H - 50, 110, 26);
    private static readonly Rectangle AutoRect = new(W - 272, H - 50, 110, 26);

    // Which foe a target points at (for the per-card target tag), or -1 for the default front / none.
    private int FoeIndexOf(ICombatTarget? target)
    {
        if (target is null) return -1;
        return ReferenceEquals(Exp.Enemy, target) ? 0 : -1; // single-foe: the lone enemy is index 0
    }

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

    private static Rectangle FoePartRect(int foeIndex, Stat stat)
    {
        var r = FoeRect(foeIndex);
        var band = r.Height / 4;
        return new Rectangle(r.X, r.Y + PartBand(stat) * band, r.Width, band);
    }

    // The foe PART under a screen point (structured foe only), else null = whole-HP aim.
    private BodyPart? FoePartAt(Foe foe, Point p)
    {
        if (foe.Frame is null) return null;
        var fi = FoeIndexOf(foe);
        foreach (var part in foe.Frame.Parts)
            if (FoePartRect(fi, part.Stat).Contains(p)) return part;
        return null;
    }

    // One-letter limb glyph for the per-card target tag (head/arm/chest/legs).
    private static char PartGlyph(Stat stat) => stat switch
    {
        Stat.Int => 'H', Stat.Str => 'A', Stat.Con => 'C', Stat.Dex => 'L', _ => '?',
    };
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
        if (_screen == Screen.NewGame) DrawNewGameScreen();
        else if (_screen == Screen.Equipment) DrawEquipmentScreen();
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

    // Combat screen (design/01): backdrop, run resources up top, you on the left (part composite +
    // HP + attribute pips), the foe on the right, the action bar along the bottom.
    private void DrawEncounterScreen()
    {
        Stretch(_assets.Background("combat_field"), 0, 0, W, H);
        Panel(0, 0, W, 40);
        // Title by the node under fight (design/01 names the encounter), not a fixed "SIEGE".
        var title = Exp.Map.Current.Type switch
        {
            NodeType.Castle => "SIEGE",
            NodeType.ResourceHold => "RESOURCE HOLD",
            NodeType.Skirmish => "SKIRMISH",
            _ => "BATTLE",
        };
        Text(_assets.Display, title, 16, 8, Ink);
        DrawRunResources(200, 10);
        DrawSpine(720, 12);

        DrawFighter(40, 90);
        DrawBays(300, 120);
        DrawSupport(300, 220);
        DrawFoe();
        // Control hint (POC): the targeting scheme is non-obvious (no fire button). In the header gap.
        Text(_assets.Mono, "click a foe limb to aim   right-click cancels", 330, 70, Muted);
        DrawAttributePool(16, H - 160);
        DrawActionBar();
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

    // New Run screen (design/05): the roster as a card grid — figure, identity, stat block, flavor —
    // one ringed as SELECTED. Reads the BuildSession roster; picking cycles the chassis it will build.
    private void DrawNewGameScreen()
    {
        Stretch(_assets.Background("build_alcove"), 0, 0, W, H);
        // design/05 centred header, driven from the manifest (content + position; fallback centred).
        var eyebrow = _ui.ElementRect("newrun", "eyebrow");
        var title = _ui.ElementRect("newrun", "title");
        var sub = _ui.ElementRect("newrun", "subtitle");
        DrawCentered(_assets.Mono, _ui.ElementContent("newrun", "eyebrow") ?? "A NEW RUN BEGINS",
            Muted, eyebrow?.Center.X ?? 480, eyebrow?.Y ?? 8);
        DrawCentered(_assets.Display, _ui.ElementContent("newrun", "title") ?? "CHOOSE YOUR CORE",
            Ink, title?.Center.X ?? 480, title?.Y ?? 18);
        // manifest subtitle content is truncated in layout.json -> keep hand copy, positioned by manifest.
        DrawCentered(_assets.Mono, "the Core is the body you wear all run", Muted, 480, sub?.Y ?? 58);

        var roster = _build.Roster;
        for (var i = 0; i < roster.Count; i++)
        {
            var c = roster[i];
            var r = NewGameCardRect(i);
            var selected = i == _build.CoreRuneIndex;
            Panel(r.X, r.Y, r.Width, r.Height);
            if (selected)
            {
                Border(r.X, r.Y, r.Width, r.Height, Amber);
                Text(_assets.Mono, "SELECTED", r.X + r.Width - 74, r.Y + 8, Amber);
            }

            DrawHumanoid(_build.Race.NewBody(), c.FigureKey(_build.Race), r.X + r.Width / 2, r.Y + 168, 140);
            Text(_assets.Mono, c.Title.ToUpper(), r.X + 12, r.Y + 176, Ink);
            Text(_assets.Mono, c.Archetype, r.X + 12, r.Y + 192, Amber);

            var body = _build.Race.NewBody();
            var sy = r.Y + 216;
            void Row(string k, int v)
            {
                Text(_assets.Mono, k, r.X + 12, sy, Muted);
                Text(_assets.Mono, v.ToString(), r.X + r.Width - 30, sy, Ink);
                sy += 15;
            }
            Row("str", body.Capacity(Stat.Str)); Row("int", body.Capacity(Stat.Int));
            Row("dex", body.Capacity(Stat.Dex)); Row("con", body.Capacity(Stat.Con));
            Row("bays", c.Bays); Row("budget", c.RuneBudget);

            DrawWrapped(c.Flavor, r.X + 12, sy + 8, r.Width - 24, Muted);
        }

        // Race selector: the chosen race drives every card's attrs/HP + the composed figure above.
        Text(_assets.Mono, "RACE  [tab]", 24, H - 64, Muted);
        for (var i = 0; i < _build.RaceCount; i++)
        {
            var rr = NewGameRaceRect(i);
            var race = _build.RaceRoster[i];
            var sel = i == _build.RaceIndex;
            Panel(rr.X, rr.Y, rr.Width, rr.Height);
            if (sel) Border(rr.X, rr.Y, rr.Width, rr.Height, Amber);
            Text(_assets.Mono, race.Name.ToUpper(), rr.X + 8, rr.Y + 6, sel ? Amber : Ink);
            Text(_assets.Mono, "hp" + race.Hp, rr.X + 8, rr.Y + 22, Muted);
        }

        DrawButton("BEGIN", NewGameBeginRect.X, NewGameBeginRect.Y,
            NewGameBeginRect.Width, NewGameBeginRect.Height, true, Keys.Enter);
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

    // Build screen (design/02): chassis anatomy + attribute readout on the left, the chassis line-up
    // up top, rune ladders and the technique palette on the right. All read from the BuildSession.
    private void DrawEquipmentScreen()
    {
        Stretch(_assets.Background("build_alcove"), 0, 0, W, H);
        Panel(0, 0, W, 40);
        Text(_assets.Display, "EQUIPMENT", 16, 8, Ink);
        var runes = _build.Runes;
        Text(_assets.Mono, $"runes {runes.Spent}/{runes.Budget}", 760, 12, Amber);

        DrawCoreRuneSelector(180, 6);

        var preview = _build.Preview();
        Panel(40, 90, 240, 410);
        Text(_assets.Mono, _build.CoreRune.Title.ToUpper(), 56, 100, Muted);
        var figBox = _ui.ElementRect("build", "paperDoll") ?? new Rectangle(100, 104, 120, 215);
        DrawFigureIn(preview, _build.CoreRune.FigureKey(_build.Race), "build", "paperDoll", 160, 470, 360);
        DrawAnatomyTags(figBox);
        DrawAttributeReadout(preview, _build.Race.NewBody(), 56, 318, KitDemand());

        DrawCoreBlock(700, 90);
        DrawMinionPreview(700, 320);
        DrawPalette(320, 300);
        DrawLoadoutStrip(320, 400);

        DrawButton("ENTER  redeploy", 40, H - 52, 300, 44, true, Keys.Enter);
        // Control hint (rune-bag UI deferred; techniques toggle for now).
        Text(_assets.Mono, "1-9 toggle techniques", 360, H - 30, Muted);
    }

    // CURRENT CORE stat block (design/02): the chassis's identity at a glance — base attributes,
    // bays, rune budget, and how many techniques are slotted on the action bar.
    private void DrawCoreBlock(int x, int y)
    {
        var c = _build.CoreRune;
        var baseBody = _build.Race.NewBody();
        Panel(x, y, 220, 190);
        Text(_assets.Display, "CURRENT CORE", x + 12, y + 10, Ink);
        Text(_assets.Mono, c.Archetype, x + 12, y + 28, Amber); // design/05 archetype tagline
        Sprite(_assets.Rune("core_" + c.Id), x + 184, y + 40, 26, 26, Color.White); // core identity glyph
        var row = y + 44;
        void Line(string k, string v) { Text(_assets.Mono, k, x + 12, row, Muted);
            Text(_assets.Mono, v, x + 150, row, Ink); row += 22; }
        Line("str", baseBody.Capacity(Stat.Str).ToString());
        Line("int", baseBody.Capacity(Stat.Int).ToString());
        Line("dex", baseBody.Capacity(Stat.Dex).ToString());
        Line("con", baseBody.Capacity(Stat.Con).ToString());
        Line("bays", c.Bays.ToString());
        Line("budget", c.RuneBudget.ToString());
        Line("actions", _build.Equipment.Count.ToString());
    }

    // The action-bar equipment strip: the chassis's FIXED starting kit, pre-slotted (no pick gate).
    // Mirrors the combat action bar so the player reads the bar they will fight with.
    // Build-screen MINION BAYS preview (design/02): a slot per chassis bay, filled with the kit's
    // minion sprite (or empty outline), so the player previews the retinue they'll field. Mirrors the
    // combat bay lane; display-only.
    private void DrawMinionPreview(int x, int y)
    {
        var c = _build.CoreRune;
        if (c.Bays <= 0) return;
        Text(_assets.Mono, "MINION BAYS", x, y - 18, Muted);
        var kit = c.MinionKit;
        const int slot = 44, gap = 8;
        for (var i = 0; i < c.Bays; i++)
        {
            var sx = x + i * (slot + gap);
            Panel(sx, y, slot, slot);
            if (i < kit.Count)
            {
                var m = kit[i];
                var tex = _assets.Minion(m.Id);
                if (tex is not null) Sprite(tex, sx + 4, y + 4, slot - 8, slot - 8, Color.White);
                else
                {
                    Rect(sx + 6, y + 6, slot - 12, slot - 12, new Color(Amber, 70));
                    Text(_assets.Mono, (m.Id.Length >= 2 ? m.Id[..2] : m.Id).ToUpperInvariant(), sx + 8, y + 12, Ink);
                }
                Text(_assets.Mono, m.Power.ToString(), sx + slot - 14, y + slot - 18, Amber);
            }
            else Border(sx, y, slot, slot, Border0); // empty bay
        }
    }

    private void DrawLoadoutStrip(int x, int y)
    {
        Text(_assets.Mono, "ACTION BAR", x, y - 18, Muted);
        var kit = _build.Equipment;
        if (kit.Count == 0) { Text(_assets.Mono, "-", x, y, Muted); return; }
        for (var i = 0; i < kit.Count; i++)
        {
            var t = kit[i];
            var left = x + i * 64;
            Panel(left, y, 56, 56);
            Sprite(_assets.Technique(t.Id), left + 4, y + 4, 48, 48, Color.White);
            Text(_assets.Mono, t.Reserve.ToString(), left + 42, y + 40, StatColor(t.Stat));
            Border(left, y, 56, 56, Amber);
        }
    }

    private void DrawCoreRuneSelector(int x, int y)
    {
        for (var i = 0; i < _build.CoreRuneCount; i++)
        {
            var left = x + i * 110;
            var selected = i == _build.CoreRuneIndex;
            var id = CoreRunes.Roster[i].Id;
            Sprite(_assets.CoreRuneFigure(id), left, y, 28, 28, selected ? Color.White : new Color(150, 140, 130));
            Text(_assets.Mono, id, left + 32, y + 8, selected ? Ink : Muted);
            if (selected) Border(left - 2, y - 2, 100, 32, Amber);
            else if (Hover(CoreRuneRect(i))) Border(left - 2, y - 2, 100, 32, Ink);
        }
    }

    // Rune ladders: a row per path, a rune glyph per rung (keystone glyph at the top), filled rungs
    // tinted, the rest dim. Climbing in order spends the budget toward a keystone.

    private void DrawPalette(int x, int y)
    {
        Text(_assets.Mono, "TECHNIQUES", x, y - 20, Muted);
        for (var i = 0; i < _build.Palette.Count; i++)
        {
            var t = _build.Palette[i];
            var left = x + i * 52;
            var selected = _build.IsSelected(t);
            Panel(left, y, 48, 48);
            Sprite(_assets.Technique(t.Id), left + 3, y + 3, 42, 42, Color.White);
            Text(_assets.Mono, (i + 1).ToString(), left + 3, y + 36, Muted);
            Border(left, y, 48, 48, selected ? Amber : Hover(PaletteRect(i)) ? Ink : Border0);
        }
    }

    // Player side: a part composite (each limb's sprite chosen by its condition), the HP life total,
    // and the attribute-pool pip widget below.
    // The YOU side of the battlefield (design/01): the player figure + HP. The attribute POOL moved to
    // its own prominent bottom-left panel (DrawAttributePool), per design/01.
    private void DrawFighter(int x, int y)
    {
        var body = Exp.Player.Body;
        Panel(x, y, 220, 250);
        Text(_assets.Mono, "YOU - " + Exp.FigureId.ToUpperInvariant(), x + 12, y + 8, Muted);
        DrawFigureIn(body, Exp.FigureId, "combat", "heroFigure", x + 110, y + 226, 210);

        var hp = Exp.Player;
        DrawBar(x + 16, y + 224, 188, _assets.Resource("hp"), hp.Hp, hp.MaxHp, Blood);
    }

    // design/01 signature: the prominent bottom-left ATTRIBUTE POOL — per-stat pips (free / reserved /
    // damaged), the read the whole build revolves around.
    private void DrawAttributePool(int x, int y)
    {
        Panel(x, y, 344, 156);
        Text(_assets.Mono, "ATTRIBUTE POOL", x + 12, y + 8, Muted);
        DrawPips(Exp.Player.Body, x + 12, y + 30);
    }

    // The attribute-pool pip widget: one row per stat — attribute icon, then pips for damaged (dim),
    // free (stat colour) and reserved (dark) capacity, anchored by the live number in mono. When a
    // `demand` map is supplied (the build screen), each row also gets a GATE MARKER: a notch at the
    // stat the slotted kit reserves, plus "/N", red when the kit out-demands the pool (can't all power).
    private void DrawPips(Body body, int x, int y, IReadOnlyDictionary<Stat, int>? demand = null)
    {
        for (var i = 0; i < StatColors.Length; i++)
        {
            var (s, _) = StatColors[i];
            var top = y + i * 30;
            Sprite(_assets.Attr(s), x, top, 24, 24, Color.White);

            var max = 0;
            foreach (var p in body.Parts) if (p.Stat == s) max += p.Capacity;
            var cur = body.Capacity(s);
            var reserved = body.Reserved(s);
            var free = cur - reserved;

            var px = x + 30;
            var suffix = s.ToString().ToLowerInvariant(); // str/int/dex/con -> per-stat coloured pips
            for (var k = 0; k < max; k++)
            {
                // Drop ships pre-coloured per-stat pips: free=full_<stat>, reserved=reserved_<stat>,
                // damaged=damage (the old "damaged" name was removed). White tint — the art is coloured.
                var pip = k < free ? _assets.Pip("full_" + suffix)
                    : k < cur ? _assets.Pip("reserved_" + suffix)
                    : _assets.Pip("damage");
                Sprite(pip, px + k * 16, top, 14, 14, Color.White);
            }
            Text(_assets.Mono, cur.ToString(), px + max * 16 + 6, top + 4, Ink);

            if (demand is not null && demand.TryGetValue(s, out var need) && need > 0)
            {
                var fits = need <= cur;
                Rect(px + Math.Min(need, max) * 16 - 1, top - 2, 2, 18, fits ? Amber : Blood); // gate notch
                Text(_assets.Mono, "/" + need, px + max * 16 + 26, top + 4, fits ? Muted : Blood);
            }
        }
    }

    // What the slotted kit (techniques + the chassis's fielded minions) reserves per stat — the demand
    // the minted body must cover. Drives the build screen's gate markers.
    private Dictionary<Stat, int> KitDemand()
    {
        var d = new Dictionary<Stat, int>();
        foreach (var t in _build.Equipment) d[t.Stat] = d.GetValueOrDefault(t.Stat) + t.Reserve;
        foreach (var m in _build.CoreRune.MinionKit) d[m.Stat] = d.GetValueOrDefault(m.Stat) + m.Reserve;
        return d;
    }

    // CoreRune-anatomy callouts (design/02): a stat tag at each body region so the chassis structure
    // — which part sources which stat — reads at a glance on the figure (the core thesis). Tags sit
    // just outside the figure box: head top, arms mid-sides, chest centre, legs lower.
    private void DrawAnatomyTags(Rectangle b)
    {
        void Tag(Stat s, int px, int py) => Text(_assets.Mono, s.ToString().ToUpperInvariant(), px, py, StatColor(s));
        var rx = b.Right + 2;          // right gutter
        Tag(Stat.Int, rx, b.Y + (int)(b.Height * 0.06f));   // head
        Tag(Stat.Con, rx, b.Y + (int)(b.Height * 0.34f));   // chest
        Tag(Stat.Str, rx, b.Y + (int)(b.Height * 0.50f));   // arms
        Tag(Stat.Dex, rx, b.Y + (int)(b.Height * 0.74f));   // legs
    }

    // Build-screen ATTRIBUTE READOUT (design/02): a horizontal bar per stat — base (solid) + rune
    // marks (lighter extension), a gate notch at the kit's per-stat demand (amber if met, blood if
    // over-pool), and the current total. Distinct from combat's pip widget (design/01).
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

    // Foe side: each foe a sprite + HP bar. Foe/part HIGHLIGHTS come ONLY from active TARGETING (a
    // module is picking): every live foe shows a faint pick-prompt reticle, a structured foe shows its
    // limb bands, and the band under the cursor highlights. There is NO persistent locked-aim ring —
    // which module targets which foe/limb reads off the action-bar card tags (F1:H). AUTO never touches
    // this; it is purely a button state.
    // The ONE enemy (single-foe canon, design/01): drawn large on the right with its creature figure,
    // targetable PART bands, a name tag, and an HP bar beneath. Part-aim clicks a limb band.
    private void DrawFoe()
    {
        var foe = Exp.Battle!.Encounter.Enemy;
        var r = FoeRect();
        var tint = foe.Down ? new Color(70, 60, 55) : Color.White;

        if (foe.Frame is not null)
            DrawHumanoid(foe.Frame, foe.Figure, r.X + r.Width / 2, r.Y + r.Height, r.Height, tint, allowBare: false);
        else
            Sprite(_assets.Reticle("focus"), r.X + r.Width / 2 - 64, r.Y + 40, 128, 128, tint); // inert marker

        if (!foe.Down && _ctrl.IsTargeting(Exp))
        {
            if (foe.Frame is not null) // limb bands + the band under the cursor (hover highlight)
            {
                var band = r.Height / 4;
                for (var b = 1; b < 4; b++) Rect(r.X, r.Y + b * band, r.Width, 1, new Color(Ink, 90));
                if (FoePartAt(foe, _cursor) is { } hov)
                    Border(r.X, r.Y + PartBand(hov.Stat) * band, r.Width, band, Ink);
            }
            else Border(r.X, r.Y, r.Width, r.Height, new Color(Ink, 110));
        }
        else if (!foe.Down && Hover(r)) Border(r.X, r.Y, r.Width, r.Height, Ink);

        Text(_assets.Mono, foe.Figure.ToUpperInvariant(), r.X + 4, r.Y - 16, foe.Down ? Muted : Ink);
        DrawBar(r.X, r.Y + r.Height + 4, r.Width, _assets.Resource("hp"), foe.Hp, foe.MaxHp, Blood);
    }

    // The minion-bay lane: one slot per chassis bay, filled with its summoned occupant (its sprite,
    // or a tinted disc + 2-letter tag when no sprite is authored) or left an empty outline. Hidden
    // for a no-bay chassis.
    private void DrawBays(int x, int y)
    {
        var bays = Exp.Bays;
        if (bays <= 0) return;
        Text(_assets.Mono, "BAYS", x, y - 18, Muted);
        var minions = Exp.Minions;
        const int slot = 44, gap = 8;
        for (var i = 0; i < bays; i++)
        {
            var sx = x + i * (slot + gap);
            Panel(sx, y, slot, slot);
            if (i < minions.Count)
            {
                var m = minions[i];
                var tex = _assets.Minion(m.Id);
                if (tex is not null) Sprite(tex, sx + 4, y + 4, slot - 8, slot - 8, Color.White);
                else // fallback: tinted disc + 2-letter tag
                {
                    Rect(sx + 6, y + 6, slot - 12, slot - 12, new Color(Amber, 70));
                    Text(_assets.Mono, (m.Id.Length >= 2 ? m.Id[..2] : m.Id).ToUpperInvariant(), sx + 8, y + 12, Ink);
                }
                Text(_assets.Mono, m.Power.ToString(), sx + slot - 14, y + slot - 18, Amber); // power
            }
            else Border(sx, y, slot, slot, Border0); // empty bay
        }
    }

    // The rallied-support lane: the holds banked en route become an allied force that auto-fires on the
    // castle boss. Shows banked pips out of combat / at lesser nodes, and reads RALLIED +N (brighter)
    // during the castle fight where that banked force is firing on the boss. Hidden when there is none.
    private void DrawSupport(int x, int y)
    {
        var banked = Exp.Map.SupportBank;
        var rallied = Exp.Battle?.Encounter.SupportAmount ?? 0; // >0 only at the castle (fed by the bank)
        if (banked <= 0 && rallied <= 0) return;

        Text(_assets.Mono, "SUPPORT", x, y - 18, Muted);
        var pips = Math.Max(banked, rallied);
        for (var i = 0; i < pips; i++)
            Rect(x + i * 16, y, 12, 12, new Color(Amber, rallied > 0 ? 200 : 110)); // brighter while firing
        Text(_assets.Mono, rallied > 0 ? "RALLIED +" + rallied : "banked", x, y + 16, rallied > 0 ? Amber : Muted);
    }

    // The action bar (design/01, bottom-right): one card per equipment technique — icon, stat cost, active
    // ring, cooldown wipe, target tag. Card geometry comes from ActionCardRect so hit-tests line up.
    private void DrawActionBar()
    {
        Text(_assets.Mono, "ACTION BAR", ActBarX, ActBarY - 16, Muted);
        for (var i = 0; i < Exp.Equipment.Count; i++)
        {
            var t = Exp.Equipment[i];
            var r = ActionCardRect(i);
            var st = Exp.Status(t);
            var sz = r.Height - 22;            // icon square
            int ix = r.X + (r.Width - sz) / 2, iy = r.Y + 4;
            Panel(r.X, r.Y, r.Width, r.Height);
            Sprite(_assets.Technique(t.Id), ix, iy, sz, sz, st.Active ? Color.White : new Color(150, 140, 130));

            // Cooldown wipe (Timered) or a held tint (Sustained block).
            if (st.Active && !st.Sustained && st.Cooldown > 0 && st.Countdown > 0)
                Rect(ix, iy, sz, sz * st.Countdown / st.Cooldown, new Color(0, 0, 0, 150));
            else if (st.Sustained && st.Active)
                Rect(ix, iy, sz, sz, new Color(Amber, 60));

            var tag = st.ChargeDry ? "dry" : st.Sustained && st.Active ? "held" : st.Ready ? "RDY" : null;
            if (tag is not null) Text(_assets.Mono, tag, ix, iy - 2, st.ChargeDry ? Blood : Amber);
            if (st.Ready && !st.Auto && !st.Sustained) Border(ix, iy, sz, sz, Amber); // holding for a target

            // Current aim (which limb if part-aimed) — single foe, so just the part glyph.
            if (st.Active && Exp.AimOf(t) is not null)
                Text(_assets.Mono, Exp.PartOf(t) is { } pt ? PartGlyph(pt.Stat).ToString() : "*",
                    r.Right - 12, iy - 2, Amber);

            Text(_assets.Mono, "[" + (i + 1) + "]", r.X + 3, r.Bottom - 12, Muted);
            Text(_assets.Mono, t.Reserve.ToString(), r.Right - 12, r.Bottom - 12, StatColor(t.Stat));
            var border = i == _ctrl.Targeting ? Ink : st.Active ? Amber : Hover(r) ? Ink : Border0;
            Border(r.X, r.Y, r.Width, r.Height, border);
            if (i == _ctrl.Targeting) Border(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4, Ink);
        }

        // Combat verbs (keyboard Tab/Space/F still work). No FIRE button. AUTO is ONE global toggle.
        DrawToggleButton("AUTO", AutoRect, Exp.IsAuto(), Hover(AutoRect));
        DrawHotButton(_paused ? "RESUME" : "PAUSE", PauseRect, Hover(PauseRect));
        DrawHotButton("RETREAT", RetreatRect, Hover(RetreatRect));
    }

    // A compact skinned button at a fixed rect with a hover highlight (combat pause/flee).
    private void DrawHotButton(string label, Rectangle r, bool hovered)
    {
        var skin = _assets.Button(hovered ? "down" : "normal");
        if (skin is not null) Sprite(skin, r.X, r.Y, r.Width, r.Height, Color.White);
        else Panel(r.X, r.Y, r.Width, r.Height);
        var size = MeasureText(_assets.Mono, label);
        Text(_assets.Mono, label, (int)(r.X + r.Width / 2 - size.X / 2), (int)(r.Y + r.Height / 2 - size.Y / 2), Ink);
        if (hovered) Border(r.X, r.Y, r.Width, r.Height, Amber);
    }

    // A two-state toggle button: ON reads LIT (amber fill + dark label + amber border), OFF reads as a
    // normal button. No glyph — the lit/unlit state IS the indicator.
    private void DrawToggleButton(string label, Rectangle r, bool on, bool hovered)
    {
        var skin = _assets.Button(on || hovered ? "down" : "normal");
        if (skin is not null) Sprite(skin, r.X, r.Y, r.Width, r.Height, on ? Amber : Color.White);
        else { Panel(r.X, r.Y, r.Width, r.Height); if (on) Rect(r.X, r.Y, r.Width, r.Height, new Color(Amber, 110)); }
        var size = MeasureText(_assets.Mono, label);
        Text(_assets.Mono, label, (int)(r.X + r.Width / 2 - size.X / 2), (int)(r.Y + r.Height / 2 - size.Y / 2),
            on ? new Color(30, 24, 18) : Ink);
        if (on || hovered) Border(r.X, r.Y, r.Width, r.Height, Amber);
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

    // Design-space text size: MeasureString is at the SSx raster, so scale back by 1/SS to match Text().
    private Vector2 MeasureText(SpriteFont font, string s) => font.MeasureString(Safe(font, s)) / SS;

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
        // The carved nine-slice frame suits LARGE panels (its 60px corners need room); small cards +
        // thin bars keep the clean gradient chrome so the frame's corner ornament isn't crushed.
        if (w >= 220 && h >= 170 && DrawFrame(x, y, w, h, "panel", PanelSlice)) return;
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
