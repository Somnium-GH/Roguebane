using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roguebane.Core.Layout;

// Typed view over Roguebane.Content/layout.json (the deterministic layout manifest
// authored by Claude Design). Pure data: parsing lives here so it stays headless-
// testable; the Game shell only feeds the raw file text in.
public sealed class LayoutManifest
{
    public Dictionary<string, Figure> Figures { get; init; } = new();
    public Dictionary<string, Gear> Gear { get; init; } = new();
    public Dictionary<string, Screen> Screens { get; init; } = new();
    public Style Style { get; init; } = new();
    public Dictionary<string, Template> Templates { get; init; } = new();

    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public static LayoutManifest Parse(string json)
        => JsonSerializer.Deserialize<LayoutManifest>(json, Opts)
           ?? throw new FormatException("layout.json deserialized to null");
}

public sealed class Figure
{
    public int[] Size { get; init; } = [];
    public int[] Pivot { get; init; } = [];
    public string[] Z { get; init; } = [];
    public Dictionary<string, Part> Parts { get; init; } = new();
    public Dictionary<string, int[]> Sockets { get; init; } = new();
    public Mount[] Mounts { get; init; } = [];
}

public sealed class Part { public int[] Rect { get; init; } = []; }

// A fill is EITHER a flat colour token (string) OR a gradient object ({type,from,to,dir}, §10). The
// converter accepts either so a Claude-Design manifest can carry gradient chrome without breaking parse.
[JsonConverter(typeof(FillConverter))]
public sealed class Fill
{
    public string? Token { get; init; } // the flat-token form (a style colour name)
    public string? Type { get; init; }  // "gradient" for the object form
    public string? From { get; init; }
    public string? To { get; init; }
    public string? Dir { get; init; }    // vertical | horizontal | diagonal
    public bool IsGradient => string.Equals(Type, "gradient", StringComparison.OrdinalIgnoreCase);
}

public sealed class FillConverter : JsonConverter<Fill>
{
    public override Fill Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
    {
        if (reader.TokenType == JsonTokenType.String)
            return new Fill { Token = reader.GetString() };
        using var doc = JsonDocument.ParseValue(ref reader);
        var e = doc.RootElement;
        string? S(string k) => e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() : null;
        return new Fill { Type = S("type"), From = S("from"), To = S("to"), Dir = S("dir") };
    }

    public override void Write(Utf8JsonWriter w, Fill v, JsonSerializerOptions o)
        => throw new NotSupportedException();
}

public sealed class Mount
{
    public string Gear { get; init; } = "";
    public string Socket { get; init; } = "";
}

public sealed class Gear { public int[] Pivot { get; init; } = []; }

public sealed class Screen
{
    public int[] DesignSize { get; init; } = [];
    public Element[] Elements { get; init; } = [];
}

public sealed class Element
{
    public string Id { get; init; } = "";
    public string Type { get; init; } = "";
    public string Anchor { get; init; } = "";
    public int[] Offset { get; init; } = [];
    public int[] Size { get; init; } = [];
    public int Z { get; init; }
    public string? Binds { get; init; }
    public string? Image { get; init; }
    public string? Color { get; init; }
    public Fill? Fill { get; init; }
    public string? Font { get; init; }
    public double? FontPx { get; init; }
    public Border? Border { get; init; }
    public Frame? Frame { get; init; }     // a nine-slice frame asset wrapping this element (§10)
    public Shadow? Shadow { get; init; }   // an engine-drawn drop shadow under this element (§10)
    public string? Content { get; init; } // a literal text element (no data binding)
    public Item? Item { get; init; }       // a repeated child: list (horizontal/vertical) or graph
    public string? ColorBind { get; init; } // a colour bound from live data (e.g. "preview.accent")
    public JsonElement States { get; init; } // interaction-state skins (button family asset map)
}

// An engine-drawn drop shadow (§10): the element silhouette offset by (Dx,Dy), softened by Blur, in the
// `Color` token at `Opacity`. `Token` names the style preset it derives from (informational).
public sealed class Shadow
{
    public string? Token { get; init; }
    public int Dx { get; init; }
    public int Dy { get; init; }
    public int Blur { get; init; }
    public string? Color { get; init; }
    public double Opacity { get; init; }
}

// A nine-slice frame (§10): a painted `asset` whose `slice` margins [L,T,R,B] fix the 4 corners and
// tile/stretch the 4 edges + centre, so ONE frame texture wraps any element size. `token` names the
// style.frames entry it derives from (informational).
public sealed class Frame
{
    public string? Token { get; init; }
    public string Asset { get; init; } = "";
    public int[] Slice { get; init; } = [];
    public string? Repeat { get; init; }            // v3: "tile" repeats edges/centre at native scale
    public bool CenterFill { get; init; } = true;   // v3: false leaves the frame's middle open
}

// How a container repeats a template: per bound datum (list) or per map/campaign node (graph). The
// consumer stamps Template at each position, laid out by Flow with Gap between Size-d cells.
public sealed class Item
{
    public string Template { get; init; } = "";
    public string Flow { get; init; } = ""; // horizontal | vertical | graph
    public int Gap { get; init; }
    public int[] Size { get; init; } = [];
}

public sealed class Border
{
    public string Color { get; init; } = "";
    public int W { get; init; }
    public string Style { get; init; } = "";
    public string[]? Sides { get; init; } // per-side borders, e.g. ["top"]; null/empty = all four
}

// A repeated UI card (techCard/poolRow/invCard/…): a fixed-size box of styled sub-parts whose
// rects are card-local. The shell stamps it at a screen position via CardTemplate. A template may
// instead be a SELF-STYLED LEAF (empty parts, its own binds/fill/border/states — e.g. a shield pip):
// the whole cell IS the visual.
public sealed class Template
{
    public int[] Size { get; init; } = [];
    public TemplatePart[] Parts { get; init; } = [];
    public string? Binds { get; init; }
    public string Color { get; init; } = "";
    public Fill? Fill { get; init; }
    public Border? Border { get; init; }
    public string Font { get; init; } = "";
    public double FontPx { get; init; }
    public JsonElement States { get; init; } // state key -> style overrides (fill/border/borderStyle)
}

public sealed class TemplatePart
{
    public int[] Rect { get; init; } = []; // card-local x,y,w,h
    public string Color { get; init; } = "";
    public string Font { get; init; } = "";
    public double FontPx { get; init; }
    public string Sample { get; init; } = ""; // placeholder text shown in the design mock
    public string? Image { get; init; }       // an IMAGE slot instead of text (e.g. a card's figure)
    public string? Binds { get; init; }        // which live datum fills this slot at render (vs the sample)
    public string? ImageBind { get; init; }    // an IMAGE slot resolved per datum: a Content path template
                                               // whose {bind} placeholders fill from the bound item (CD #15)
    public Fill? Fill { get; init; }           // part-level chrome (attr swatches, slot backgrounds)
    public Border? Border { get; init; }
    public string? ColorBind { get; init; }    // a colour bound from the stamped datum (e.g. "technique.attrColor")
}

public sealed class Style
{
    public Dictionary<string, string> Palette { get; init; } = new();
    public JsonElement Fonts { get; init; }
    public Dictionary<string, string> PartStates { get; init; } = new();
    public JsonElement Pip { get; init; }
    public Dictionary<string, Frame> Frames { get; init; } = new(); // the reusable nine-slice frame set
}
