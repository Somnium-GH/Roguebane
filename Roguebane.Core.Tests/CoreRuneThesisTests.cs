using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class CoreRuneThesisTests
{
    private static bool ClimbResonance(RuneLoadout runes) =>
        runes.TryTake(Paths.ResonanceI)
        && runes.TryTake(Paths.ResonanceII)
        && runes.TryTake(Paths.ResonantCore);

    [Fact]
    public void SpecialistJustAffordsItsOwnKeystone()
    {
        var runes = CoreRunes.Adept.NewLoadout();
        Assert.True(ClimbResonance(runes));
        Assert.True(runes.Has(Paths.ResonantCore));
        Assert.Equal(10, runes.Spent);   // tight budget 10, spent to the rune
        Assert.Equal(0, runes.Available);
    }

    [Fact]
    public void GruntCanClimbToTheSpecialistKeystoneAtARealCost()
    {
        var runes = CoreRunes.Grunt.NewLoadout();
        Assert.True(ClimbResonance(runes));
        Assert.True(runes.Has(Paths.ResonantCore));

        // CORE_RUNES.md v6: RuneDiscount retired to 0 (JoAT is now an attribute-cost effect, item 4,
        // not yet built) — full-price climb: 5 + (6-2) + (4-3) = 10, real, not free.
        Assert.Equal(10, runes.Spent);
        Assert.True(runes.Spent > 0);

        // "never built for it" is now a BUDGET gap, not a stat gap — attrs are race-only (§7). The
        // Grunt's edge is its fatter, cheaper budget, not a different body.
        Assert.True(CoreRunes.Grunt.RuneBudget > CoreRunes.Adept.RuneBudget);
    }

    [Fact]
    public void FatBudgetIsWhatEnablesTheExploit()
    {
        // Grunt's discount alone is not the trick — strip the budget and the climb dies.
        var starved = new RuneLoadout(budget: 3, runeDiscount: CoreRunes.Grunt.RuneDiscount);
        Assert.False(ClimbResonance(starved));
        Assert.False(starved.Has(Paths.ResonantCore));
    }
}
