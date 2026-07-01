using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// G7 (richer runes): a held keystone can grant non-extension effects — a technique or a minion the
// chassis never had — interpreted by the same data path as part grants.
public class RuneGrantsTests
{
    private static RuneLoadout Climbed(IReadOnlyList<Mark> ladder, int budget = 30)
    {
        var runes = new RuneLoadout(budget);
        foreach (var rung in ladder) Assert.True(runes.TryTake(rung));
        return runes;
    }

    [Fact]
    public void ATechniqueKeystoneExposesItsGrantedTechnique()
    {
        var runes = Climbed(Paths.TempestLadder);
        Assert.Contains(Paths.Maelstrom, runes.GrantedTechniques);
    }

    [Fact]
    public void AMinionKeystoneExposesItsGrantedMinion()
    {
        var runes = Climbed(Paths.ConclaveLadder);
        Assert.Contains(Minions.Shade, runes.GrantedMinions);
    }

    [Fact]
    public void GrantsAreEmptyBeforeTheKeystoneIsReached()
    {
        var runes = new RuneLoadout(30);
        runes.TryTake(Paths.TempestI);
        runes.TryTake(Paths.TempestII);
        Assert.Empty(runes.GrantedTechniques); // keystone not yet held
    }

    [Fact]
    public void ForgeFoldsRuneGrantedTechniquesIntoTheLoadout()
    {
        var chassis = CoreRunes.Grunt;
        var runes = chassis.NewLoadout();
        foreach (var rung in Paths.TempestLadder) runes.TryTake(rung);

        // Player picked only Jab; the Tempest keystone hands them Maelstrom on top.
        var session = Forge.Assemble(Races.Human, chassis, runes, new[] { Techniques.Jab }, Sieges.StandardRun());

        Assert.Contains(session.Equipment, t => t.Id == "jab");
        Assert.Contains(session.Equipment, t => t.Id == "maelstrom");
    }

    [Fact]
    public void ForgeDoesNotDuplicateAGrantedTechniqueAlreadyChosen()
    {
        var chassis = CoreRunes.Grunt;
        var runes = chassis.NewLoadout();
        foreach (var rung in Paths.TempestLadder) runes.TryTake(rung);

        var session = Forge.Assemble(Races.Human, chassis, runes, new[] { Paths.Maelstrom }, Sieges.StandardRun());

        Assert.Single(session.Equipment, t => t.Id == "maelstrom");
    }
}
