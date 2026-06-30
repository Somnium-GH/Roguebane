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

    // Templates are heterogeneous (poolRow/techCard/attrBar/… each differ); kept raw
    // until the screen builder needs a typed shape. Reconcile when consumed.
    public Dictionary<string, JsonElement> Templates { get; init; } = new();

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

public sealed class Style
{
    public Dictionary<string, string> Palette { get; init; } = new();
    public JsonElement Fonts { get; init; }
    public JsonElement PartStates { get; init; }
    public JsonElement Pip { get; init; }
}
