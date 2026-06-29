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

    private Screen _screen = Screen.Build;
    private BuildSession _build = null!;
    private Session _session = null!;
    private KeyboardState _prevKeys;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true; // the Core tick advances once per fixed Update — deterministic
    }

    protected override void Initialize()
    {
        _build = Sessions.NewBuild(); // start on the build screen; Launch threads into the siege
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
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
        GraphicsDevice.Clear(new Color(18, 18, 24));
        _spriteBatch.Begin();

        if (_screen == Screen.Build) DrawBuildScreen();
        else DrawRunScreen();

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawRunScreen()
    {
        DrawNodeMap(16, 16);
        DrawPool(_session.Player, 16, 56, showReserved: true);
        DrawEncounter(16, 180);
        DrawLoadout(16, 400);
        DrawStateOverlay();
    }

    // The run-map: one cell per encounter, left to right. Cleared nodes dim, the current node lights
    // gold, the castle (structural) sits wider — the player reads their position in the run at a glance.
    private void DrawNodeMap(int x, int y)
    {
        var run = _session.Run;
        for (var i = 0; i < run.Nodes.Count; i++)
        {
            var node = run.Nodes[i];
            var left = x + i * 64;
            var w = node.Structural ? 56 : 40;
            var color = node.Cleared ? new Color(60, 80, 60)
                : i == run.Index ? new Color(230, 200, 90)
                : new Color(90, 90, 110);
            Rect(left, y, w, 24, color);
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

    private void DrawEncounter(int x, int y)
    {
        var encounter = _session.Run.Current;
        for (var i = 0; i < encounter.Foes.Count; i++)
        {
            var foe = encounter.Foes[i];
            var top = y + i * 40;
            var isTarget = ReferenceEquals(encounter.CurrentTarget, foe);
            Rect(x, top, 360, 32, new Color(40, 40, 48));
            if (foe.MaxHp > 0)
            {
                var frac = (float)foe.Hp / foe.MaxHp;
                var color = foe.Down ? new Color(60, 60, 60)
                    : isTarget ? new Color(230, 200, 90) : new Color(170, 70, 70);
                Rect(x, top, (int)(360 * frac), 32, color);
            }
        }
    }

    private void DrawLoadout(int x, int y)
    {
        for (var i = 0; i < _session.Loadout.Count; i++)
        {
            var technique = _session.Loadout[i];
            var left = x + i * 56;
            var active = _session.IsActive(technique);
            Rect(left, y, 48, 48, new Color(40, 40, 48));
            if (active) Rect(left + 4, y + 4, 40, 40, new Color(90, 200, 160));
            else Border(left + 4, y + 4, 40, 40, new Color(90, 90, 100));
        }
    }

    private void DrawStateOverlay()
    {
        Color? tint = _session.State switch
        {
            SessionState.Won => new Color(40, 120, 60, 90),
            SessionState.Fled => new Color(120, 90, 40, 90),
            _ => _session.Paused ? new Color(0, 0, 0, 120) : null,
        };
        if (tint is { } c)
            Rect(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, c);
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
