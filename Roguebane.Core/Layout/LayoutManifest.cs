using System.Text.Json;

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
    public string? Fill { get; init; }
    public string? Font { get; init; }
    public double? FontPx { get; init; }
    public Border? Border { get; init; }
}

public sealed class Border
{
    public string Color { get; init; } = "";
    public int W { get; init; }
    public string Style { get; init; } = "";
}

// A repeated UI card (techCard/poolRow/invCard/…): a fixed-size box of styled sub-parts whose
// rects are card-local. The shell stamps it at a screen position via CardTemplate.
public sealed class Template
{
    public int[] Size { get; init; } = [];
    public TemplatePart[] Parts { get; init; } = [];
}

public sealed class TemplatePart
{
    public int[] Rect { get; init; } = []; // card-local x,y,w,h
    public string Color { get; init; } = "";
    public string Font { get; init; } = "";
    public double FontPx { get; init; }
    public string Sample { get; init; } = ""; // which datum fills this slot (name/cost/desc/…)
    public string? Image { get; init; }       // an IMAGE slot instead of text (e.g. a card's figure)
}

public sealed class Style
{
    public Dictionary<string, string> Palette { get; init; } = new();
    public JsonElement Fonts { get; init; }
    public Dictionary<string, string> PartStates { get; init; } = new();
    public JsonElement Pip { get; init; }
}
