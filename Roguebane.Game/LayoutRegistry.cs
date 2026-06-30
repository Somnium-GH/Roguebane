using System;
using System.IO;
using Roguebane.Core.Layout;

namespace Roguebane.Game;

// Loads the deterministic layout manifest (Roguebane.Content/layout.json, copied to the
// output Content dir at build) and hands the shell the typed Core model. Tolerant: a
// missing or malformed manifest yields Manifest == null so the shell can fall back to
// legacy hard-offset drawing rather than crash on a content gap.
public sealed class LayoutRegistry
{
    public LayoutManifest? Manifest { get; }
    public string? LoadError { get; }

    public LayoutRegistry()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Content", "layout.json");
        try
        {
            Manifest = LayoutManifest.Parse(File.ReadAllText(path));
        }
        catch (Exception e) when (e is IOException or FormatException or System.Text.Json.JsonException)
        {
            LoadError = $"{path}: {e.Message}";
        }
    }
}
