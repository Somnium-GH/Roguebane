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

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;

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
        _session = Sessions.Demo();
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

        if (Pressed(keys, Keys.Space)) _session.TogglePause();
        if (Pressed(keys, Keys.F)) _session.Flee();
        for (var i = 0; i < TechniqueKeys.Length; i++)
            if (Pressed(keys, TechniqueKeys[i]))
                _session.Toggle(_session.Loadout[i]);

        _prevKeys = keys;

        _session.Tick();
        base.Update(gameTime);
    }

    private bool Pressed(KeyboardState keys, Keys key) => keys.IsKeyDown(key) && _prevKeys.IsKeyUp(key);

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 18, 24));
        _spriteBatch.Begin();

        DrawPlayerPool(16, 16);
        DrawEncounter(16, 120);
        DrawLoadout(16, 360);
        DrawStateOverlay();

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawPlayerPool(int x, int y)
    {
        var pool = _session.Player.Pool;
        var attrs = new[]
        {
            (Attribute.Power, new Color(220, 90, 70)),
            (Attribute.Focus, new Color(80, 150, 230)),
            (Attribute.Vigor, new Color(120, 200, 120)),
        };
        for (var i = 0; i < attrs.Length; i++)
        {
            var (a, color) = attrs[i];
            var cap = pool.Capacity(a);
            var used = cap - pool.Available(a);
            var top = y + i * 28;
            Rect(x, top, 240, 22, new Color(40, 40, 48));      // capacity track
            if (cap > 0) Rect(x, top, 240 * used / cap, 22, color); // allocated portion
        }
    }

    private void DrawEncounter(int x, int y)
    {
        var encounter = _session.Run.Current;
        var defenders = encounter.Defenders;
        for (var i = 0; i < encounter.Parts.Count; i++)
        {
            var part = encounter.Parts[i];
            var top = y + i * 40;
            var isTarget = ReferenceEquals(encounter.CurrentTarget, part);
            Rect(x, top, 360, 32, new Color(40, 40, 48));
            if (part.MaxHealth > 0)
            {
                var frac = (float)defenders.Health(part) / part.MaxHealth;
                var color = defenders.IsDestroyed(part) ? new Color(60, 60, 60)
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
