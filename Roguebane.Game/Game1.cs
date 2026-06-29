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
    private Session _session = null!;
    private KeyboardState _prevKeys;

    // Smoke mode (RB_SMOKE=1): load, drive to RB_SCREEN, render, optionally save RB_SHOT, exit. Lets
    // the headless loop verify the pipeline builds, every asset binds, AND the screen renders without
    // a human at the window — a saved PNG is the visual receipt.
    private readonly bool _smoke = Environment.GetEnvironmentVariable("RB_SMOKE") == "1";
    private readonly string? _shotPath = Environment.GetEnvironmentVariable("RB_SHOT");
    private readonly string? _smokeScreen = Environment.GetEnvironmentVariable("RB_SCREEN");
    private int _frames;

    private const int W = 960, H = 540; // 2x the design's 480x270 native; integer-scaled HD pixel

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true; // the Core tick advances once per fixed Update — deterministic
        _graphics.PreferredBackBufferWidth = W;
        _graphics.PreferredBackBufferHeight = H;
    }

    protected override void Initialize()
    {
        _build = Sessions.NewBuild(); // start on the build screen; Launch threads into the siege

        if (_smokeScreen == "combat") // jump straight into a fight and tick it into a live state
        {
            _build.Toggle(Techniques.Jab);
            _build.Toggle(Techniques.Ember);
            _session = _build.Launch(Sieges.StandardRun());
            for (var i = 0; i < _session.Loadout.Count; i++) _session.Toggle(_session.Loadout[i]);
            for (var i = 0; i < 6; i++) _session.Tick();
            _screen = Screen.Run;
        }

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _assets = new AssetRegistry(Content);
    }

    protected override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();
        if (keys.IsKeyDown(Keys.Escape)) Exit();

        if (_screen == Screen.Build) UpdateBuild(keys);
        else UpdateRun(keys);

        _prevKeys = keys;
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

        // No actions, no run: Launch is gated on a chosen loadout (the readiness bar mirrors this).
        if (Pressed(keys, Keys.Enter) && _build.Loadout.Count > 0)
        {
            _session = _build.Launch(Sieges.StandardRun());
            _screen = Screen.Run;
        }
    }

    private void UpdateRun(KeyboardState keys)
    {
        if (Pressed(keys, Keys.Space)) _session.TogglePause();
        if (Pressed(keys, Keys.F)) _session.Flee();
        for (var i = 0; i < TechniqueKeys.Length && i < _session.Loadout.Count; i++)
            if (Pressed(keys, TechniqueKeys[i]))
                _session.Toggle(_session.Loadout[i]);

        _session.Tick();
    }

    private bool Pressed(KeyboardState keys, Keys key) => keys.IsKeyDown(key) && _prevKeys.IsKeyUp(key);

    private static readonly (Stat Stat, Color Color)[] StatColors =
    {
        (Stat.Str, new Color(220, 90, 70)),
        (Stat.Int, new Color(80, 150, 230)),
        (Stat.Dex, new Color(120, 200, 120)),
        (Stat.Con, new Color(200, 180, 90)),
    };

    protected override void Draw(GameTime gameTime)
    {
        // When capturing, paint into an offscreen target so the exact frame can be saved to PNG.
        var shooting = _smoke && _shotPath is not null;
        RenderTarget2D? target = shooting ? new RenderTarget2D(GraphicsDevice, W, H) : null;
        GraphicsDevice.SetRenderTarget(target);

        GraphicsDevice.Clear(new Color(0x17, 0x11, 0x0b)); // panel-dark base from the locked palette
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp); // HD pixel: crisp integer edges

        if (_screen == Screen.Build) DrawBuildScreen();
        else DrawRunScreen();

        _spriteBatch.End();

        if (target is not null)
        {
            GraphicsDevice.SetRenderTarget(null);
            using var fs = System.IO.File.Create(_shotPath!);
            target.SaveAsPng(fs, W, H);
            target.Dispose();
        }

        base.Draw(gameTime);

        if (_smoke && ++_frames >= 1) SmokeReportAndExit();
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

    // Combat screen (design/01): backdrop, the run strip up top, you on the left (part composite +
    // HP + attribute pips), the foe on the right, the action bar along the bottom. All read from the
    // live Session; the shell only paints it.
    private void DrawRunScreen()
    {
        Stretch(_assets.Background("combat_field"), 0, 0, W, H);
        Panel(0, 0, W, 40);
        Text(_assets.Display, "SIEGE", 16, 8, Ink);
        DrawNodeStrip(180, 8);

        DrawFighter(40, 90);
        DrawFoes(560, 90);
        DrawActionBar(40, H - 92);
        DrawStateOverlay();
    }

    // The run strip: a row of node icons, current node ringed amber, cleared nodes dimmed.
    private void DrawNodeStrip(int x, int y)
    {
        var run = _session.Run;
        for (var i = 0; i < run.Nodes.Count; i++)
        {
            var node = run.Nodes[i];
            var left = x + i * 30;
            var icon = node.Structural ? _assets.Node(NodeType.Castle) : _assets.Node(NodeType.Skirmish);
            var tint = node.Cleared ? new Color(90, 80, 70) : Color.White;
            Sprite(icon, left, y, 24, 24, tint);
            if (i == run.Index) Border(left - 1, y - 1, 26, 26, Amber);
        }
    }

    // Stat bars sized against a reference max (~20 is huge). showReserved overlays the bright,
    // currently-reserved portion on top of the dim capacity bar.
    private void DrawPool(Body body, int x, int y, bool showReserved)
    {
        const int max = 20, fullW = 240;
        for (var i = 0; i < StatColors.Length; i++)
        {
            var (s, color) = StatColors[i];
            var cap = body.Capacity(s);
            var top = y + i * 26;
            var capW = Math.Min(fullW, fullW * cap / max);
            Rect(x, top, fullW, 20, new Color(40, 40, 48));                 // track
            Rect(x, top, capW, 20, new Color(color, 110)); // capacity (dim)
            if (showReserved && cap > 0)
            {
                var used = cap - body.Available(s);
                Rect(x, top, capW * used / cap, 20, color);                 // reserved (bright)
            }
        }
    }

    // The build screen: pick a chassis, climb the rune ladders, choose techniques, then Launch.
    private void DrawBuildScreen()
    {
        DrawChassisSelector(16, 16);
        DrawBudget(16, 56);
        DrawLadders(16, 92);
        DrawPool(_build.Preview(), 16, 200, showReserved: false); // previewed body, runes folded in
        DrawPalette(16, 320);

        // Launch readiness: a bottom bar lights when a loadout is chosen (Enter to start the run).
        var ready = _build.Loadout.Count > 0;
        Rect(16, 400, 240, 12, ready ? new Color(90, 200, 160) : new Color(50, 50, 58));
    }

    private void DrawChassisSelector(int x, int y)
    {
        for (var i = 0; i < _build.ChassisCount; i++)
        {
            var left = x + i * 64;
            var selected = i == _build.ChassisIndex;
            Rect(left, y, 56, 28, selected ? new Color(230, 200, 90) : new Color(50, 50, 58));
        }
    }

    private void DrawBudget(int x, int y)
    {
        var runes = _build.Runes;
        const int fullW = 240;
        Rect(x, y, fullW, 16, new Color(40, 40, 48));
        if (runes.Budget > 0)
            Rect(x, y, fullW * runes.Spent / runes.Budget, 16, new Color(150, 120, 210)); // spent
    }

    private void DrawLadders(int x, int y)
    {
        for (var p = 0; p < _build.Paths.Count; p++)
        {
            var ladder = _build.Paths[p];
            if (ladder.Count == 0) continue;
            var held = _build.Runes.CurrentRank(ladder[0].Path);
            var top = y + p * 36;
            for (var r = 0; r < ladder.Count; r++)
            {
                var left = x + r * 36;
                var filled = r < held;
                var keystone = ladder[r].Keystone;
                var color = filled
                    ? keystone ? new Color(230, 160, 90) : new Color(150, 120, 210)
                    : new Color(50, 50, 58);
                Rect(left, top, 28, 28, color);
                if (keystone) Border(left, top, 28, 28, new Color(230, 200, 90));
            }
        }
    }

    private void DrawPalette(int x, int y)
    {
        for (var i = 0; i < _build.Palette.Count; i++)
        {
            var technique = _build.Palette[i];
            var left = x + i * 56;
            Rect(left, y, 48, 48, new Color(40, 40, 48));
            if (_build.IsSelected(technique)) Rect(left + 4, y + 4, 40, 40, new Color(90, 200, 160));
            else Border(left + 4, y + 4, 40, 40, new Color(90, 90, 100));
        }
    }

    // Player side: a part composite (each limb's sprite chosen by its condition), the HP life total,
    // and the attribute-pool pip widget below.
    private void DrawFighter(int x, int y)
    {
        var body = _session.Player.Body;
        Panel(x, y, 220, 360);
        Text(_assets.Mono, "YOU", x + 12, y + 8, Muted);
        DrawHumanoid(body, x + 110, y + 70, 2);

        var hp = _session.Player;
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
        var encounter = _session.Run.Current;
        for (var i = 0; i < encounter.Foes.Count; i++)
        {
            var foe = encounter.Foes[i];
            var top = y + i * 150;
            var isTarget = ReferenceEquals(encounter.CurrentTarget, foe);
            var tint = foe.Down ? new Color(70, 60, 55) : Color.White;
            Sprite(_assets.Texture("sprites/char/ogre"), x, top, 144, 156, tint);
            if (isTarget && !foe.Down) Sprite(_assets.Reticle("focus"), x + 24, top, 96, 96, Amber);
            DrawBar(x, top + 156, 144, _assets.Resource("hp"), foe.Hp, foe.MaxHp, Blood);
        }
    }

    // The action bar: one card per loadout technique — icon, mono stat cost, active ring.
    private void DrawActionBar(int x, int y)
    {
        Panel(x, y, W - 80, 76);
        for (var i = 0; i < _session.Loadout.Count; i++)
        {
            var t = _session.Loadout[i];
            var left = x + 12 + i * 84;
            var active = _session.IsActive(t);
            Panel(left, y + 8, 76, 60);
            Sprite(_assets.Technique(t.Id), left + 14, y + 12, 48, 48, Color.White);
            Text(_assets.Mono, "[" + (i + 1) + "]", left + 6, y + 50, Muted);
            Text(_assets.Mono, t.Reserve.ToString(), left + 58, y + 50, StatColor(t.Stat));
            Border(left, y + 8, 76, 60, active ? Amber : Border0);
        }
    }

    private void DrawStateOverlay()
    {
        (Color tint, string label)? overlay = _session.State switch
        {
            SessionState.Won => (new Color(40, 120, 60, 110), "VICTORY"),
            SessionState.Lost => (new Color(120, 40, 40, 140), "OVERRUN"),
            SessionState.Fled => (new Color(120, 90, 40, 110), "FLED"),
            _ => _session.Paused ? (new Color(0, 0, 0, 120), "PAUSED") : ((Color, string)?)null,
        };
        if (overlay is { } o)
        {
            Rect(0, 0, W, H, o.tint);
            Text(_assets.Display, o.label, W / 2 - 60, H / 2 - 12, Ink);
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
