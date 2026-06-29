using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Roguebane.Core;
using Roguebane.Core.Content;

namespace Roguebane.Game;

// Thin shell over Core: Update turns input into Session intents and advances the fixed tick;
// Draw only reads Session state and paints placeholder shapes. No game rules live here.
public class Game1 : Microsoft.Xna.Framework.Game
{
    private static readonly Keys[] TechniqueKeys =
        { Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6 };

    private static readonly Keys[] PathKeys = { Keys.Q, Keys.W };

    private enum Screen { Build, Run }

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private AssetRegistry _assets = null!;

    private Screen _screen = Screen.Build;
    private BuildSession _build = null!;
    private Campaign _campaign = null!;
    private bool _paused;
    private int _selTech; // selected action-bar card: the technique that aim-clicks and FIRE target
    private KeyboardState _prevKeys;
    private KeyboardState _keys; // current frame's keys, read in Draw for button pressed-state

    private const double CombatTickSeconds = 0.1; // fixed 10 ticks/sec combat clock
    private double _combatAccum;

    private MouseState _prevMouse;
    private Point _cursor;  // mouse position mapped into design space (through the letterbox)
    private bool _clicked;  // left button went down this frame

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

        if (_smokeScreen is "combat" or "map") // march the real loop for the screenshot
        {
            _build.Toggle(Techniques.Ember); // grow the Grunt kit (jab+brace) with a bolt for variety
            _campaign = _build.March(Maps.StandardLegs(3));
            _screen = Screen.Run;
            if (_smokeScreen == "combat")
            {
                foreach (var t in Exp.Loadout) _campaign.Toggle(t);
                _campaign.Enter(Exp.Options[0].Id); // jump into the first fight (fresh)

                // Put the selected technique on MANUAL aimed at a foe, then drive combat ticks so it
                // charges to ready and HOLDS — the screenshot then shows the full FTL surface
                // (holding "RDY" card + target tag + AUTO toggle state + live cooldown wipes).
                var sel = SelectedTechnique();
                if (sel is not null && Exp.Foes.Count > 0)
                {
                    _campaign.SetAuto(sel, false);
                    _campaign.Aim(sel, Exp.Foes[^1]);
                }
                for (var i = 0; i < 60; i++) _campaign.Tick();
            }
        }

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _assets = new AssetRegistry(Content);
        _scene = new RenderTarget2D(GraphicsDevice, W, H);
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

        if (_screen == Screen.Build) UpdateBuild(keys);
        else UpdateRun(keys, gameTime);

        _prevKeys = keys;
        _prevMouse = mouse;
        base.Update(gameTime);
    }

    private void UpdateBuild(KeyboardState keys)
    {
        if (Pressed(keys, Keys.Left)) _build.CycleChassis(-1);
        if (Pressed(keys, Keys.Right)) _build.CycleChassis(1);

        for (var i = 0; i < PathKeys.Length && i < _build.Paths.Count; i++)
            if (Pressed(keys, PathKeys[i]))
                _build.Climb(_build.Paths[i]);

        for (var i = 0; i < TechniqueKeys.Length && i < _build.Palette.Count; i++)
            if (Pressed(keys, TechniqueKeys[i]))
                _build.Toggle(_build.Palette[i]);

        // Mouse: click a chassis, a ladder row to climb, a palette card to toggle, the march CTA.
        for (var i = 0; i < _build.ChassisCount; i++)
            if (Click(ChassisRect(i))) _build.CycleChassis(i - _build.ChassisIndex);
        for (var p = 0; p < _build.Paths.Count; p++)
            if (_build.Paths[p].Count > 0 && Click(LadderRowRect(p, _build.Paths[p].Count)))
                _build.Climb(_build.Paths[p]);
        for (var i = 0; i < _build.Palette.Count; i++)
            if (Click(PaletteRect(i))) _build.Toggle(_build.Palette[i]);

        // March the campaign. The chassis ships a fixed kit so the bar is never empty — no gate.
        // Alt+Enter is the fullscreen toggle, not a march.
        var march = (Pressed(keys, Keys.Enter) && !keys.IsKeyDown(Keys.LeftAlt)) || Click(MarchRect);
        if (march)
        {
            _campaign = _build.March(Maps.StandardLegs(3));
            foreach (var t in Exp.Loadout) _campaign.Toggle(t); // arm the whole bar
            _screen = Screen.Run;
        }
    }

    private void UpdateRun(KeyboardState keys, GameTime gameTime)
    {
        if (_campaign.State != CampaignState.Marching) return; // settled: hold the end overlay
        if (Exp.State == ExpeditionState.Fighting) UpdateCombat(keys, gameTime);
        else UpdateChoosing(keys);
    }

    private void UpdateCombat(KeyboardState keys, GameTime gameTime)
    {
        if (Pressed(keys, Keys.Space) || Click(PauseRect)) _paused = !_paused;
        if (Pressed(keys, Keys.F) || Click(FleeRect)) Exp.Flee();

        // A number key / card click selects that card AND toggles it active (charges it).
        for (var i = 0; i < TechniqueKeys.Length && i < Exp.Loadout.Count; i++)
            if (Pressed(keys, TechniqueKeys[i]) || Click(ActionCardRect(i)))
            {
                _selTech = i;
                _campaign.Toggle(Exp.Loadout[i]);
            }

        // FTL targeting: click a live foe to AIM the selected technique at it; ENTER / FIRE fires it
        // on command when ready; TAB / AUTO toggles its self-firing.
        var sel = SelectedTechnique();
        if (sel is not null)
        {
            var foes = Exp.Foes;
            for (var i = 0; i < foes.Count; i++)
                if (!foes[i].Down && Click(FoeRect(i)))
                    _campaign.Aim(sel, foes[i]);

            if (Pressed(keys, Keys.Enter) || Click(FireRect)) _campaign.Fire(sel);
            if (Pressed(keys, Keys.Tab) || Click(AutoRect))
                _campaign.SetAuto(sel, !_campaign.IsAuto(sel));
        }

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
        if (Exp.AtMerchant)
        {
            if (Pressed(keys, Keys.P) || Click(MerchPotionRect)) Exp.BuyPotion();
            if (Pressed(keys, Keys.H) || Click(MerchHealRect)) Exp.BuyHeal();
            if (Pressed(keys, Keys.U) || Click(MerchUseRect)) Exp.UsePotion();
        }

        var options = Exp.Options;
        var origin = JumpOrigin;
        for (var i = 0; i < options.Count; i++)
            if ((i < TechniqueKeys.Length && Pressed(keys, TechniqueKeys[i]))
                || Click(JumpRect(i, origin.X, origin.Y)))
            {
                _campaign.Enter(options[i].Id); // may win the leg and roll to the next city
                foreach (var t in Exp.Loadout)  // keep the bar armed into the next fight/leg
                    if (!_campaign.IsActive(t)) _campaign.Toggle(t);
                break;
            }
    }

    private bool Pressed(KeyboardState keys, Keys key) => keys.IsKeyDown(key) && _prevKeys.IsKeyUp(key);

    // Mouse helpers: Hover = cursor over a rect (drives Draw highlight); Click = hover + this frame's
    // press (drives Update intents). Both read the design-space cursor mapped through the letterbox.
    private bool Hover(Rectangle r) => !_smoke && r.Contains(_cursor);
    private bool Click(Rectangle r) => _clicked && r.Contains(_cursor);

    // Interactive layout rects — single source of truth shared by Update (hit-test) and Draw (paint
    // + hover). Mirrors the coordinates used in the Draw* methods.
    private static Rectangle ChassisRect(int i) => new(180 + i * 110 - 2, 4, 100, 32);
    private static Rectangle PaletteRect(int i) => new(320 + i * 64, 300, 56, 56);
    private static Rectangle LadderRowRect(int p, int rungs) => new(320, 100 + p * 56, rungs * 56, 48);
    private static readonly Rectangle MarchRect = new(40, H - 52, 300, 44);
    private static Rectangle JumpRect(int i, int x, int y) => new(x + i * 130, y, 116, 116);
    private static Rectangle ActionCardRect(int i) => new(52 + i * 84, H - 84, 76, 60);
    private static Rectangle FoeRect(int i) => new(560, 90 + i * 150, 144, 156);
    private static readonly Rectangle PauseRect = new(W - 156, H - 84, 110, 26);
    private static readonly Rectangle FleeRect = new(W - 156, H - 50, 110, 26);
    private static readonly Rectangle FireRect = new(W - 272, H - 84, 110, 26);
    private static readonly Rectangle AutoRect = new(W - 272, H - 50, 110, 26);

    // The selected action-bar technique (the one aim-clicks and FIRE act on), or null if none loaded.
    private Technique? SelectedTechnique() =>
        _selTech < Exp.Loadout.Count ? Exp.Loadout[_selTech] : null;

    private bool SelAuto() => SelectedTechnique() is { } t && Exp.IsAuto(t);

    // Which foe a target points at (for the per-card target tag), or -1 for the default front / none.
    private int FoeIndexOf(ICombatTarget? target)
    {
        if (target is null) return -1;
        var foes = Exp.Foes;
        for (var i = 0; i < foes.Count; i++)
            if (ReferenceEquals(foes[i], target)) return i;
        return -1;
    }
    // Merchant verb buttons (base at 60,240).
    private static readonly Rectangle MerchPotionRect = new(74, 284, 330, 34);
    private static readonly Rectangle MerchUseRect = new(74, 322, 330, 34);
    private static readonly Rectangle MerchHealRect = new(74, 360, 330, 34);
    // The jump chooser sits at a different origin at a merchant vs an open chart.
    private Point JumpOrigin => Exp.AtMerchant ? new Point(460, 160) : new Point(200, 120);

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
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp); // HD pixel: crisp integer edges
        if (_screen == Screen.Build) DrawBuildScreen();
        else DrawRunScreen();
        _spriteBatch.End();

        if (_smoke && _shotPath is not null) // headless receipt: save the design-res scene verbatim
        {
            GraphicsDevice.SetRenderTarget(null);
            using var fs = System.IO.File.Create(_shotPath!);
            _scene.SaveAsPng(fs, W, H);
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
            ("chassis/grunt", _assets.ChassisFigure("grunt")),
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
        if (Exp.State == ExpeditionState.Fighting) DrawCombatScreen();
        else DrawMapScreen();
    }

    // Combat screen (design/01): backdrop, run resources up top, you on the left (part composite +
    // HP + attribute pips), the foe on the right, the action bar along the bottom.
    private void DrawCombatScreen()
    {
        Stretch(_assets.Background("combat_field"), 0, 0, W, H);
        Panel(0, 0, W, 40);
        Text(_assets.Display, "SIEGE", 16, 8, Ink);
        DrawRunResources(200, 10);
        DrawSpine(720, 12);

        DrawFighter(40, 90);
        DrawFoes(560, 90);
        DrawActionBar(40, H - 92);
        DrawStateOverlay();
    }

    // Run-map screen (design/03): the resources, the current beacon, and the charted jumps as cards
    // (fog-aware icons). At a merchant the shop verbs are live instead of a fight ahead.
    private void DrawMapScreen()
    {
        Stretch(_assets.Background("map_chart"), 0, 0, W, H);
        Panel(0, 0, W, 40);
        Text(_assets.Display, "MARCH", 16, 8, Ink);
        DrawRunResources(200, 10);
        DrawSpine(720, 12);

        var map = Exp.Map;
        Sprite(_assets.Node(map.Sees(map.Current)), 60, 120, 64, 64, Color.White);
        Text(_assets.Mono, map.Current.Type.ToString().ToLower(), 60, 190, Muted);

        if (Exp.AtMerchant) DrawMerchant(60, 240);
        else DrawJumpChooser(200, 120);

        DrawStateOverlay();
    }

    // The charted jumps as numbered cards, each a fog-aware node icon (a `?` while still fogged).
    private void DrawJumpChooser(int x, int y)
    {
        Text(_assets.Mono, "CHARTED JUMPS", x, y - 18, Muted);
        var map = Exp.Map;
        var options = Exp.Options;
        for (var i = 0; i < options.Count; i++)
        {
            var node = options[i];
            var seen = map.Sees(node);
            var left = x + i * 130;
            Panel(left, y, 116, 116);
            Sprite(_assets.Node(seen), left + 30, y + 14, 56, 56, Color.White);
            Text(_assets.Mono, $"[{i + 1}] {seen.ToString().ToLower()}", left + 8, y + 90, Ink);
            if (Hover(JumpRect(i, x, y))) Border(left, y, 116, 116, Ink);
        }
        if (options.Count == 0) Text(_assets.Mono, "no charted jumps", x, y, Muted);
    }

    private void DrawMerchant(int x, int y)
    {
        Panel(x, y, 360, 170);
        Text(_assets.Display, "MERCHANT", x + 14, y + 10, Ink);
        DrawButton($"P  buy potion (4)   x{Exp.Potions}", x + 14, y + 44, 330, 34,
            Exp.Gold >= 4, Keys.P);
        DrawButton("U  use potion  (repair)", x + 14, y + 82, 330, 34, Exp.Potions > 0, Keys.U);
        DrawButton("H  heal hp     (3)", x + 14, y + 120, 330, 34,
            Exp.Gold >= 3 && Exp.Player.Hp < Exp.Player.MaxHp, Keys.H);
        DrawJumpChooser(x + 400, y - 80);
    }

    // The run's resource readout: supplies, war-party distance, banked support, gold, potions.
    private void DrawRunResources(int x, int y)
    {
        var map = Exp.Map;
        DrawStat(_assets.Resource("supplies"), map.Supplies, x);
        DrawStat(_assets.Node(NodeType.Castle), map.WarPartyDistance, x + 110); // war party closing in
        DrawStat(_assets.Resource("support"), map.SupportBank, x + 220);
        DrawStat(_assets.Resource("spoils"), Exp.Gold, x + 330);
        DrawStat(_assets.Resource("hp"), Exp.Potions, x + 440);

        void DrawStat(Texture2D? icon, int value, int sx)
        {
            Sprite(icon, sx, y, 22, 22, Color.White);
            Text(_assets.Mono, value.ToString(), sx + 26, y + 4, Ink);
        }
    }

    // Build screen (design/02): chassis anatomy + attribute readout on the left, the chassis line-up
    // up top, rune ladders and the technique palette on the right. All read from the BuildSession.
    private void DrawBuildScreen()
    {
        Stretch(_assets.Background("build_alcove"), 0, 0, W, H);
        Panel(0, 0, W, 40);
        Text(_assets.Display, "BUILD", 16, 8, Ink);
        var runes = _build.Runes;
        Text(_assets.Mono, $"runes {runes.Spent}/{runes.Budget}", 760, 12, Amber);

        DrawChassisSelector(180, 6);

        var preview = _build.Preview();
        Panel(40, 90, 240, 410);
        Text(_assets.Mono, _build.Chassis.Id.ToUpper(), 56, 100, Muted);
        DrawHumanoid(preview, 160, 200, 2);
        DrawPips(preview, 56, 320);

        DrawLadders(320, 100);
        DrawCoreBlock(700, 90);
        DrawPalette(320, 300);
        DrawLoadoutStrip(320, 400);

        DrawButton("ENTER  begin the march", 40, H - 52, 300, 44, true, Keys.Enter);
    }

    // CURRENT CORE stat block (design/02): the chassis's identity at a glance — base attributes,
    // bays, rune budget, and how many techniques are slotted on the action bar.
    private void DrawCoreBlock(int x, int y)
    {
        var c = _build.Chassis;
        var baseBody = c.NewBody();
        Panel(x, y, 220, 190);
        Text(_assets.Display, "CURRENT CORE", x + 12, y + 10, Ink);
        var row = y + 44;
        void Line(string k, string v) { Text(_assets.Mono, k, x + 12, row, Muted);
            Text(_assets.Mono, v, x + 150, row, Ink); row += 22; }
        Line("str", baseBody.Capacity(Stat.Str).ToString());
        Line("int", baseBody.Capacity(Stat.Int).ToString());
        Line("dex", baseBody.Capacity(Stat.Dex).ToString());
        Line("con", baseBody.Capacity(Stat.Con).ToString());
        Line("bays", c.Bays.ToString());
        Line("budget", c.RuneBudget.ToString());
        Line("actions", _build.Loadout.Count.ToString());
    }

    // The action-bar loadout strip: the chassis's FIXED starting kit, pre-slotted (no pick gate).
    // Mirrors the combat action bar so the player reads the bar they will fight with.
    private void DrawLoadoutStrip(int x, int y)
    {
        Text(_assets.Mono, "ACTION BAR", x, y - 18, Muted);
        var kit = _build.Loadout;
        if (kit.Count == 0) { Text(_assets.Mono, "—", x, y, Muted); return; }
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

    private void DrawChassisSelector(int x, int y)
    {
        for (var i = 0; i < _build.ChassisCount; i++)
        {
            var left = x + i * 110;
            var selected = i == _build.ChassisIndex;
            var id = Chassrium.Roster[i].Id;
            Sprite(_assets.ChassisFigure(id), left, y, 28, 28, selected ? Color.White : new Color(150, 140, 130));
            Text(_assets.Mono, id, left + 32, y + 8, selected ? Ink : Muted);
            if (selected) Border(left - 2, y - 2, 100, 32, Amber);
            else if (Hover(ChassisRect(i))) Border(left - 2, y - 2, 100, 32, Ink);
        }
    }

    // Rune ladders: a row per path, a rune glyph per rung (keystone glyph at the top), filled rungs
    // tinted, the rest dim. Climbing in order spends the budget toward a keystone.
    private void DrawLadders(int x, int y)
    {
        for (var p = 0; p < _build.Paths.Count; p++)
        {
            var ladder = _build.Paths[p];
            if (ladder.Count == 0) continue;
            var held = _build.Runes.CurrentRank(ladder[0].Path);
            var top = y + p * 56;
            for (var r = 0; r < ladder.Count; r++)
            {
                var left = x + r * 56;
                var filled = r < held;
                var keystone = ladder[r].Keystone;
                var glyph = _assets.Rune(keystone ? "keystone" : r == ladder.Count - 1 ? "path" : "mark");
                Sprite(glyph, left, top, 48, 48, filled ? Color.White : new Color(110, 95, 80));
                if (keystone) Border(left - 2, top - 2, 52, 52, Amber);
                // Per-rung budget cost (discount-aware), so each rune reads as a priced card.
                Text(_assets.Mono, _build.Runes.EffectiveCost(ladder[r]).ToString(), left + 2, top - 12,
                    filled ? Amber : Muted);
            }
            if (Hover(LadderRowRect(p, ladder.Count))) Border(320, top, ladder.Count * 56, 48, Ink);
            // The ladder's path named once, to the right of its rungs (the key that climbs it).
            var nameX = x + ladder.Count * 56 + 8;
            Text(_assets.Mono, "Q W".Split(' ')[Math.Min(p, 1)] + " " + ladder[0].Path, nameX, top + 16, Muted);
        }
    }

    private void DrawPalette(int x, int y)
    {
        Text(_assets.Mono, "TECHNIQUES", x, y - 20, Muted);
        for (var i = 0; i < _build.Palette.Count; i++)
        {
            var t = _build.Palette[i];
            var left = x + i * 64;
            var selected = _build.IsSelected(t);
            Panel(left, y, 56, 56);
            Sprite(_assets.Technique(t.Id), left + 4, y + 4, 48, 48, Color.White);
            Text(_assets.Mono, (i + 1).ToString(), left + 4, y + 42, Muted);
            Border(left, y, 56, 56, selected ? Amber : Hover(PaletteRect(i)) ? Ink : Border0);
        }
    }

    // Player side: a part composite (each limb's sprite chosen by its condition), the HP life total,
    // and the attribute-pool pip widget below.
    private void DrawFighter(int x, int y)
    {
        var body = Exp.Player.Body;
        Panel(x, y, 220, 360);
        Text(_assets.Mono, "YOU", x + 12, y + 8, Muted);
        DrawHumanoid(body, x + 110, y + 70, 2);

        var hp = Exp.Player;
        DrawBar(x + 16, y + 188, 188, _assets.Resource("hp"), hp.Hp, hp.MaxHp, Blood);
        DrawPips(body, x + 16, y + 212);
    }

    // The attribute-pool pip widget: one row per stat — attribute icon, then pips for damaged (dim),
    // free (stat colour) and reserved (dark) capacity, anchored by the live number in mono.
    private void DrawPips(Body body, int x, int y)
    {
        for (var i = 0; i < StatColors.Length; i++)
        {
            var (s, color) = StatColors[i];
            var top = y + i * 30;
            Sprite(_assets.Attr(s), x, top, 24, 24, Color.White);

            var max = 0;
            foreach (var p in body.Parts) if (p.Stat == s) max += p.Capacity;
            var cur = body.Capacity(s);
            var reserved = body.Reserved(s);
            var free = cur - reserved;

            var px = x + 30;
            for (var k = 0; k < max; k++)
            {
                Texture2D? pip;
                Color tint;
                if (k < free) { pip = _assets.Pip("full"); tint = color; }
                else if (k < cur) { pip = _assets.Pip("full"); tint = new Color(color, 110); } // reserved
                else { pip = _assets.Pip("damaged"); tint = Color.White; }
                Sprite(pip, px + k * 16, top, 14, 14, tint);
            }
            Text(_assets.Mono, cur.ToString(), px + max * 16 + 6, top + 4, Ink);
        }
    }

    // Lay a humanoid from its parts: head (INT), chest (CON), arms (STR ×2), legs (DEX ×2). Each
    // part's sprite is picked by condition; paired parts fan out to either side of the torso.
    private void DrawHumanoid(Body body, int cx, int cy, int s)
    {
        var arms = 0;
        var legs = 0;
        foreach (var part in body.Parts)
        {
            var (asset, w, h, dx, dy) = part.Stat switch
            {
                Stat.Int => ("head", 32, 36, 0, -38),
                Stat.Con => ("chest", 40, 40, 0, 0),
                Stat.Str => ("arm", 20, 44, arms++ == 0 ? -30 : 30, -2),
                Stat.Dex => ("leg", 20, 48, legs++ == 0 ? -10 : 10, 42),
                _ => ("chest", 40, 40, 0, 0),
            };
            var tex = _assets.Texture($"sprites/body/{asset}/base_{Condition(body, part)}");
            Sprite(tex, cx + dx * s - w * s / 2, cy + dy * s - h * s / 2, w * s, h * s, Color.White);
        }
    }

    private static string Condition(Body body, BodyPart part)
    {
        if (part.Capacity == 0) return "healthy";
        var frac = (float)body.Contribution(part) / part.Capacity;
        return frac <= 0f ? "broken" : frac < 0.5f ? "damaged" : "healthy";
    }

    // Foe side: each foe a sprite + HP bar, the current target ringed by the focus reticle.
    private void DrawFoes(int x, int y)
    {
        var encounter = Exp.Battle!.Encounter;
        var sel = SelectedTechnique();
        var aim = sel is not null ? Exp.AimOf(sel) : null; // the selected technique's chosen target
        for (var i = 0; i < encounter.Foes.Count; i++)
        {
            var foe = encounter.Foes[i];
            var top = y + i * 150;
            var isTarget = ReferenceEquals(aim ?? encounter.CurrentTarget, foe);
            var tint = foe.Down ? new Color(70, 60, 55) : Color.White;
            Sprite(_assets.Texture("sprites/char/ogre"), x, top, 144, 156, tint);
            if (isTarget && !foe.Down) Sprite(_assets.Reticle("focus"), x + 24, top, 96, 96, Amber);
            if (!foe.Down && Hover(FoeRect(i))) Border(x, top, 144, 156, Ink); // click to aim
            DrawBar(x, top + 156, 144, _assets.Resource("hp"), foe.Hp, foe.MaxHp, Blood);
        }
    }

    // The action bar: one card per loadout technique — icon, mono stat cost, active ring.
    private void DrawActionBar(int x, int y)
    {
        Panel(x, y, W - 80, 76);
        for (var i = 0; i < Exp.Loadout.Count; i++)
        {
            var t = Exp.Loadout[i];
            var left = x + 12 + i * 84;
            var st = Exp.Status(t);
            const int ix = 14, iy = 12, sz = 48;
            Panel(left, y + 8, 76, 60);
            Sprite(_assets.Technique(t.Id), left + ix, y + iy, sz, sz, st.Active ? Color.White : new Color(150, 140, 130));

            // Cooldown wipe: a dark veil over the icon shrinks from full as the timer recharges, so
            // a ready technique shows clear. Sustained (held block) shows a steady held tint instead.
            if (st.Active && !st.Sustained && st.Cooldown > 0 && st.Countdown > 0)
            {
                var h = sz * st.Countdown / st.Cooldown;
                Rect(left + ix, y + iy, sz, h, new Color(0, 0, 0, 150));
            }
            else if (st.Sustained && st.Active)
                Rect(left + ix, y + iy, sz, sz, new Color(Amber, 60)); // held

            // Ready holds bright; auto-firing cards read "auto", a held block "held", a dry charge
            // "dry", a charging timer counts via the wipe.
            var tag = st.ChargeDry ? "dry" : st.Sustained && st.Active ? "held"
                : st.Ready ? (st.Auto ? "auto" : "RDY") : null;
            if (tag is not null)
                Text(_assets.Mono, tag, left + ix, y + iy - 2, st.ChargeDry ? Blood : Amber);
            if (st.Ready && !st.Auto && !st.Sustained)
                Border(left + ix, y + iy, sz, sz, Amber); // holding, awaiting FIRE

            // The technique's current target (which foe), top-right of the icon so the player can
            // read each card's aim without crowding the key/cost row below.
            var fi = st.Active ? FoeIndexOf(Exp.AimOf(t)) : -1;
            if (fi >= 0) Text(_assets.Mono, "F" + (fi + 1), left + 50, y + iy - 2, Amber);

            Text(_assets.Mono, "[" + (i + 1) + "]", left + 6, y + 50, Muted);
            Text(_assets.Mono, t.Reserve.ToString(), left + 58, y + 50, StatColor(t.Stat));
            var border = i == _selTech ? Ink : st.Active ? Amber : Hover(ActionCardRect(i)) ? Ink : Border0;
            Border(left, y + 8, 76, 60, border);
            if (i == _selTech) Border(left - 2, y + 6, 80, 64, Amber); // selected: outer ring
        }

        // Mouse-reachable combat verbs (keyboard Enter/Tab/Space/F still work).
        DrawHotButton("FIRE", FireRect, Hover(FireRect));
        DrawHotButton(SelAuto() ? "AUTO+" : "AUTO-", AutoRect, Hover(AutoRect));
        DrawHotButton(_paused ? "RESUME" : "PAUSE", PauseRect, Hover(PauseRect));
        DrawHotButton("FLEE", FleeRect, Hover(FleeRect));
    }

    // A compact skinned button at a fixed rect with a hover highlight (combat pause/flee).
    private void DrawHotButton(string label, Rectangle r, bool hovered)
    {
        var skin = _assets.Button(hovered ? "down" : "normal");
        if (skin is not null) Sprite(skin, r.X, r.Y, r.Width, r.Height, Color.White);
        else Panel(r.X, r.Y, r.Width, r.Height);
        var size = _assets.Mono.MeasureString(label);
        Text(_assets.Mono, label, (int)(r.X + r.Width / 2 - size.X / 2), (int)(r.Y + r.Height / 2 - size.Y / 2), Ink);
        if (hovered) Border(r.X, r.Y, r.Width, r.Height, Amber);
    }

    private void DrawStateOverlay()
    {
        (Color tint, string label)? overlay = _campaign.State switch
        {
            CampaignState.Won => (new Color(40, 120, 60, 130), "THE CAPITAL FALLS"),
            CampaignState.Lost => (new Color(120, 40, 40, 140), "OVERRUN"),
            _ => _paused ? (new Color(0, 0, 0, 120), "PAUSED") : ((Color, string)?)null,
        };
        if (overlay is { } o)
        {
            Rect(0, 0, W, H, o.tint);
            var size = _assets.Display.MeasureString(o.label);
            Text(_assets.Display, o.label, (int)(W / 2 - size.X / 2), H / 2 - 12, Ink);
            if (_campaign.State != CampaignState.Marching)
                Text(_assets.Mono, "Esc to quit", W / 2 - 40, H / 2 + 20, Muted);
        }
    }

    // The campaign spine (design/04): a pip per leg to the Capital, taken cities lit amber.
    private void DrawSpine(int x, int y)
    {
        Text(_assets.Mono, "SPINE", x, y, Muted);
        for (var i = 0; i < _campaign.LegCount; i++)
        {
            var left = x + 56 + i * 22;
            var taken = i < _campaign.LegIndex;
            var here = i == _campaign.LegIndex;
            Sprite(_assets.Node(NodeType.Castle), left, y - 2, 18, 18,
                taken ? Amber : here ? Color.White : new Color(110, 95, 80));
        }
    }

    // Locked palette (ASSET_MANIFEST): ink/muted text, amber highlight, ember/blood, panel + borders.
    private static readonly Color Ink = new(0xec, 0xe0, 0xcb);
    private static readonly Color Muted = new(0x9a, 0x84, 0x68);
    private static readonly Color Amber = new(0xd9, 0xa4, 0x41);
    private static readonly Color Blood = new(0xb2, 0x3b, 0x32);
    private static readonly Color Panel0 = new(0x1d, 0x15, 0x0e);
    private static readonly Color Border0 = new(0x5a, 0x46, 0x36);

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

    private void Text(SpriteFont font, string s, int x, int y, Color color) =>
        _spriteBatch.DrawString(font, s, new Vector2(x, y), color);

    private void Panel(int x, int y, int w, int h)
    {
        Rect(x, y, w, h, new Color(Panel0, 220));
        Border(x, y, w, h, Border0);
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
        var size = _assets.Mono.MeasureString(label);
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
}
