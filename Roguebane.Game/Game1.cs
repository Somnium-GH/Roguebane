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
    private KeyboardState _prevKeys;

    // The leg under way is the campaign's current Expedition — most of the run screen reads it.
    private Expedition Exp => _campaign.Current;

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

        if (_smokeScreen is "combat" or "map") // march the real loop for the screenshot
        {
            _build.Toggle(Techniques.Jab);
            _build.Toggle(Techniques.Ember);
            _campaign = _build.March(Maps.StandardLegs(3));
            _screen = Screen.Run;
            if (_smokeScreen == "combat")
            {
                foreach (var t in Exp.Loadout) _campaign.Toggle(t);
                _campaign.Enter(Exp.Options[0].Id); // jump into the first fight (fresh)
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

        // March the campaign (gated on a chosen loadout; the footer mirrors this).
        if (Pressed(keys, Keys.Enter) && _build.Loadout.Count > 0)
        {
            _campaign = _build.March(Maps.StandardLegs(3));
            foreach (var t in Exp.Loadout) _campaign.Toggle(t); // arm the whole bar
            _screen = Screen.Run;
        }
    }

    private void UpdateRun(KeyboardState keys)
    {
        if (_campaign.State != CampaignState.Marching) return; // settled: hold the end overlay
        if (Exp.State == ExpeditionState.Fighting) UpdateCombat(keys);
        else UpdateChoosing(keys);
    }

    private void UpdateCombat(KeyboardState keys)
    {
        if (Pressed(keys, Keys.Space)) _paused = !_paused;
        if (Pressed(keys, Keys.F)) Exp.Flee();
        for (var i = 0; i < TechniqueKeys.Length && i < Exp.Loadout.Count; i++)
            if (Pressed(keys, TechniqueKeys[i]))
                _campaign.Toggle(Exp.Loadout[i]);

        if (!_paused && !_smoke) _campaign.Tick(); // smoke freezes the staged frame for the shot
    }

    // On the chart: number keys pick a charted jump; at a merchant, the shop verbs are live.
    private void UpdateChoosing(KeyboardState keys)
    {
        if (Exp.AtMerchant)
        {
            if (Pressed(keys, Keys.P)) Exp.BuyPotion();
            if (Pressed(keys, Keys.H)) Exp.BuyHeal();
            if (Pressed(keys, Keys.U)) Exp.UsePotion();
        }

        var options = Exp.Options;
        for (var i = 0; i < TechniqueKeys.Length && i < options.Count; i++)
            if (Pressed(keys, TechniqueKeys[i]))
            {
                _campaign.Enter(options[i].Id); // may win the leg and roll to the next city
                foreach (var t in Exp.Loadout)  // keep the bar armed into the next fight/leg
                    if (!_campaign.IsActive(t)) _campaign.Toggle(t);
                break;
            }
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
        }
        if (options.Count == 0) Text(_assets.Mono, "no charted jumps", x, y, Muted);
    }

    private void DrawMerchant(int x, int y)
    {
        Panel(x, y, 360, 150);
        Text(_assets.Display, "MERCHANT", x + 14, y + 10, Ink);
        Text(_assets.Mono, $"[P] buy potion (4)   potions {Exp.Potions}", x + 14, y + 48, Muted);
        Text(_assets.Mono, "[U] use potion  (repair parts)", x + 14, y + 72, Muted);
        Text(_assets.Mono, "[H] heal hp     (3)", x + 14, y + 96, Muted);
        Text(_assets.Mono, "pick a jump [1..] to march on", x + 14, y + 122, Amber);
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
        DrawPalette(320, 320);

        var ready = _build.Loadout.Count > 0;
        Panel(40, H - 40, W - 80, 32);
        Text(_assets.Mono, ready ? "ENTER  begin the march" : "pick at least one technique",
            56, H - 34, ready ? Amber : Muted);
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
            }
            Text(_assets.Mono, "Q W".Split(' ')[Math.Min(p, 1)], x - 18, top + 14, Muted);
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
            Border(left, y, 56, 56, selected ? Amber : Border0);
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
        for (var i = 0; i < Exp.Loadout.Count; i++)
        {
            var t = Exp.Loadout[i];
            var left = x + 12 + i * 84;
            var active = Exp.IsActive(t);
            Panel(left, y + 8, 76, 60);
            Sprite(_assets.Technique(t.Id), left + 14, y + 12, 48, 48, Color.White);
            Text(_assets.Mono, "[" + (i + 1) + "]", left + 6, y + 50, Muted);
            Text(_assets.Mono, t.Reserve.ToString(), left + 58, y + 50, StatColor(t.Stat));
            Border(left, y + 8, 76, 60, active ? Amber : Border0);
        }
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
