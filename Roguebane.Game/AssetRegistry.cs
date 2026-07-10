using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Roguebane.Core;

namespace Roguebane.Game;

// The Core-id -> asset-path binding layer (manifest's "Core/shell split"): the shell maps simulation
// state to a safe asset name and asks for the texture; it never decides rules. Lookups are tolerant
// — a missing asset returns null so a half-authored content set never crashes the render shell.
public sealed class AssetRegistry
{
    private readonly ContentManager _content;
    private readonly Dictionary<string, Texture2D?> _cache = new();

    public SpriteFont Display { get; }
    public SpriteFont Mono { get; }

    public AssetRegistry(ContentManager content)
    {
        _content = content;
        Display = content.Load<SpriteFont>("display");
        Mono = content.Load<SpriteFont>("mono");
    }

    // Tolerant load: the pipeline throws ContentLoadException for an unbuilt asset; we cache the
    // null so the shell can draw a fallback rather than die on a gap in the content set.
    public Texture2D? Texture(string path)
    {
        if (_cache.TryGetValue(path, out var cached)) return cached;
        Texture2D? tex;
        try { tex = _content.Load<Texture2D>(path); }
        catch (ContentLoadException) { tex = null; }
        _cache[path] = tex;
        return tex;
    }

    public Texture2D? Node(NodeType type) => Texture("icons/node/" + NodeName[type]);
    public Texture2D? Camp => Texture("icons/node/camp");
    public Texture2D? Attr(Stat stat) => Texture("icons/attr/" + AttrName[stat]);
    public Texture2D? Resource(string id) => Texture("icons/resource/" + id);
    public Texture2D? Rune(string tier) => Texture("icons/rune/" + tier);
    public Texture2D? Background(string id) => Texture("bg/" + id);
    public Texture2D? CoreRuneFigure(string id) => Texture("sprites/char/chassis/" + id);
    public Texture2D? Minion(string id) => Texture("sprites/minion/" + id);

    // Technique id -> glyph where one is authored; otherwise a neutral fallback so the bar always fills.
    public Texture2D? Technique(string id) =>
        Texture("icons/technique/" + (TechniqueName.GetValueOrDefault(id) ?? "swing"));

    public Texture2D? Pip(string state) => Texture("ui/pip/pip_" + state);
    public Texture2D? Reticle(string role) => Texture("ui/reticle/" + role);
    public Texture2D? Button(string state) => Texture("ui/button/button_" + state);

    // The node-type -> icon token, shared with imageBind path templates ("icons/node/{node.type}").
    public static string NodeToken(NodeType type) => NodeName[type];

    private static readonly Dictionary<NodeType, string> NodeName = new()
    {
        [NodeType.Camp] = "camp",
        [NodeType.Skirmish] = "skirmish", // dedicated icon landed 2026-07-01 (stopgap "?" retired)
        [NodeType.ResourceHold] = "resource",
        [NodeType.Merchant] = "merchant",
        [NodeType.Quest] = "quest", // 2026-07-09 Doug crash fix: missing entry threw KeyNotFoundException
        [NodeType.Unknown] = "unknown",
        [NodeType.Castle] = "castle",
    };

    private static readonly Dictionary<Stat, string> AttrName = new()
    {
        [Stat.Str] = "strength",
        [Stat.Int] = "intellect",
        [Stat.Dex] = "dexterity",
        [Stat.Con] = "constitution",
    };

    // Only ids with a matching glyph; the rest fall back to swing (see Technique). ember reads as a
    // firebolt, cleave as a frenzy sweep. (Glyph set: brace/disarm/firebolt/frenzy/swing.)
    private static readonly Dictionary<string, string> TechniqueName = new()
    {
        ["brace"] = "brace",
        ["ember"] = "firebolt",
        ["cleave"] = "frenzy",
    };
}
