using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// The cores.json loader (Doug LOCKED 2026-07-12): design/systems/cores.json is our OWN design data, so
// pinning its literal resolved values is the point (unlike CD's regenerated layout.json). These assert
// the schema parses, every kit id resolves, and the resolved Race/CoreRune objects carry the JSON's
// values — the guard that keeps the single source of truth honest.
public class CoreRuneCatalogTests
{
    private static readonly CoreRuneCatalog Cat = CoreRuneCatalog.Default;

    [Fact]
    public void LoadsEveryRaceAndCore()
    {
        Assert.Equal(new[] { "dwarf", "elf", "half_giant", "halfling", "human" },
            Cat.Races.Keys.OrderBy(k => k).ToArray());
        Assert.Equal(new[] { "adept", "barbarian", "grunt", "ranger", "reaver", "summoner", "warden" },
            Cat.Cores.Keys.OrderBy(k => k).ToArray());
    }

    [Fact]
    public void ResolvedRaceValuesMatchTheJson()
    {
        var human = Cat.Races["human"];
        Assert.Equal((5, 5, 5, 5, 16), (human.Str, human.Int, human.Dex, human.Con, human.Hp));
        var elf = Cat.Races["elf"];
        Assert.Equal((4, 6, 4, 4, 13), (elf.Str, elf.Int, elf.Dex, elf.Con, elf.Hp));
        Assert.Equal("THE FOUNDER LINE", human.Tag);
    }

    [Fact]
    public void ResolvedGruntMatchesTheJson()
    {
        // STRUCTURAL invariants only (budget/slots/cap/bonus/effect) — the exact kit ids are a balance-
        // tuning surface (per CLAUDE.md, don't pin design-authored content that gets re-dropped; the
        // resolve/throw tests below cover id integrity). Assert instead that the kit is non-empty and the
        // action bar can hold it.
        var g = Cat.Cores["grunt"];
        Assert.Equal(20, g.RuneBudget);
        Assert.Equal(4, g.ActionSlots);
        Assert.Equal(2, g.MinionCap);
        Assert.Equal((1, 1, 1, 1), (g.StrBonus, g.IntBonus, g.DexBonus, g.ConBonus));
        Assert.Equal(CoreEffectKind.JackOfAllTrades, g.Effect);
        Assert.NotEmpty(g.Kit);
        Assert.True(g.Kit.Count <= g.ActionSlots, "kit must fit the action bar");
        Assert.NotEmpty(g.DefaultWeapons!);
    }

    [Fact]
    public void SummonerCarriesItsTargetKitFromTheJson()
    {
        // The rebuild lives ONLY in cores.json now: Blast + Brace in, actionSlots 4, Wooden Shield, and
        // Skeleton as the sole starting minion (Iron Golem dropped).
        var s = Cat.Cores["summoner"];
        Assert.Equal(4, s.ActionSlots);
        Assert.Equal(3, s.MinionCap);
        Assert.True(s.CoreEffectFreeSummons);
        Assert.Equal(new[] { "ember", "blast", "sacrifice", "brace" }, s.Kit.Select(t => t.Id).ToArray());
        Assert.Equal(new[] { "wand_adept", "shield_wooden" }, s.DefaultWeapons!.Select(w => w.Id).ToArray());
        Assert.Equal(new[] { "skeleton" }, s.DefaultMinions!.Select(m => m.Id).ToArray());
    }

    [Fact]
    public void EveryKitIdAcrossEveryCoreResolves()
    {
        // Reaching every resolved object without a throw IS the assertion — Parse throws loudly on an
        // unknown id, so this fails loudly if any kit references a stale/misspelled id.
        foreach (var core in Cat.Cores.Values)
        {
            Assert.All(core.Kit, t => Assert.False(string.IsNullOrEmpty(t.Id)));
            Assert.All(core.DefaultWeapons!, w => Assert.False(string.IsNullOrEmpty(w.Id)));
            Assert.All(core.DefaultArmor!, a => Assert.False(string.IsNullOrEmpty(a.Id)));
            Assert.All(core.DefaultMinions!, m => Assert.False(string.IsNullOrEmpty(m.Id)));
        }
    }

    [Fact]
    public void UnknownKitIdThrowsLoudly()
    {
        var bad = """
        { "races": {}, "cores": { "x": { "budget": 1, "actionSlots": 1, "minionCap": 0,
          "statBonus": {"str":0,"int":0,"dex":0,"con":0}, "effect": "JackOfAllTrades",
          "kit": { "techniques": ["not_a_real_technique"], "weapons": [], "armor": [], "minions": [] } } } }
        """;
        var ex = Assert.Throws<InvalidDataException>(() => CoreRuneCatalog.Parse(bad));
        Assert.Contains("not_a_real_technique", ex.Message);
    }
}
