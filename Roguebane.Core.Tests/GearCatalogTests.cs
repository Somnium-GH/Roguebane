using System.Text.Json;
using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// The engine roster and the CD gear catalog share ONE identity: a piece's Id IS its sprite/catalog
// key (sprites/gear/{Id}). Pins the alignment so a roster rename or a catalog re-drop that breaks
// the join fails here, not as an invisible unarmed render. Bows are the known art gap (no bow
// sprites shipped — logged to CD), so they are exempt until that batch lands.
public class GearCatalogTests
{
    private static HashSet<string> CatalogIds()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var c = Path.Combine(dir, "Roguebane.Content", "gear_catalog.json");
            if (File.Exists(c))
                return JsonDocument.Parse(File.ReadAllText(c)).RootElement
                    .EnumerateArray().Select(e => e.GetProperty("id").GetString()!).ToHashSet();
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException("gear_catalog.json");
    }

    [Fact]
    public void EveryWeaponIdResolvesInTheCatalogExceptTheBowGap()
    {
        var ids = CatalogIds();
        var missing = Armory.AllWeapons
            .Where(w => w.Kind != Roguebane.Core.WeaponKind.Bow) // known CD art gap
            .Where(w => !ids.Contains(w.Id)).Select(w => w.Id).ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public void EveryArmorIdResolvesInTheCatalog()
    {
        var ids = CatalogIds();
        var missing = ArmorLines.All.Where(a => !ids.Contains(a.Id)).Select(a => a.Id).ToList();
        Assert.Empty(missing);
    }
}
