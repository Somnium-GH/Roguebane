using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class ChassisThesisTests
{
    private static bool ClimbResonance(RuneLoadout runes) =>
        runes.TryTake(Paths.ResonanceI)
        && runes.TryTake(Paths.ResonanceII)
        && runes.TryTake(Paths.ResonantCore);

    [Fact]
    public void SpecialistJustAffordsItsOwnKeystone()
    {
        var runes = Chassrium.Adept.NewLoadout();
        Assert.True(ClimbResonance(runes));
        Assert.True(runes.Has(Paths.ResonantCore));
        Assert.Equal(10, runes.Spent);   // tight budget 10, spent to the rune
        Assert.Equal(0, runes.Available);
    }

    [Fact]
    public void GruntCanClimbToTheSpecialistKeystoneAtARealCost()
    {
        var runes = Chassrium.Grunt.NewLoadout();
        Assert.True(ClimbResonance(runes));
        Assert.True(runes.Has(Paths.ResonantCore));

        // cheap runes (discount 1): effective 4 + (5-2) + (3-3) = 7 — real, not free
        Assert.Equal(7, runes.Spent);
        Assert.True(runes.Spent > 0);

        // and the Grunt was never built for it: a feeble head vs the Adept's caster INT
        Assert.True(Chassrium.Grunt.NewBody().Capacity(Stat.Int)
            < Chassrium.Adept.NewBody().Capacity(Stat.Int));
    }

    [Fact]
    public void FatBudgetIsWhatEnablesTheExploit()
    {
        // Grunt's discount alone is not the trick — strip the budget and the climb dies.
        var starved = new RuneLoadout(budget: 3, runeDiscount: Chassrium.Grunt.RuneDiscount);
        Assert.False(ClimbResonance(starved));
        Assert.False(starved.Has(Paths.ResonantCore));
    }
}
