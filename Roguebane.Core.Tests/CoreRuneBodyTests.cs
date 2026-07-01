using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class CoreRuneBodyTests
{
    [Fact]
    public void CoreRuneMintsABodyWithPairedStatShares()
    {
        var body = CoreRunes.Grunt.NewBody();

        Assert.Equal(4, body.Capacity(Stat.Str)); // two arms, 2 + 2
        Assert.Equal(4, body.Capacity(Stat.Dex)); // two legs, 2 + 2
        Assert.Equal(3, body.Capacity(Stat.Int));
        Assert.Equal(4, body.Capacity(Stat.Con));
        Assert.Equal(6, body.Parts.Count);        // head, chest, arms x2, legs x2
    }

    [Fact]
    public void OddStatSharesSplitWithoutLoss()
    {
        // Adept STR 4 across two arms — and any odd total must still sum back exactly.
        var odd = new CoreRune("odd", new[]
        {
            new BodyPart("odd-arm-l", Stat.Str, 2),
            new BodyPart("odd-arm-r", Stat.Str, 3),
        }, RuneBudget: 0);

        Assert.Equal(5, odd.NewBody().Capacity(Stat.Str));
    }

    [Fact]
    public void TheSpecialistOutclassesTheGruntOnItsSignatureStat()
    {
        Assert.True(CoreRunes.Adept.NewBody().Capacity(Stat.Int)
            > CoreRunes.Grunt.NewBody().Capacity(Stat.Int));
    }

    [Fact]
    public void AMintedBodyGatesGearOnItsStats()
    {
        var grunt = CoreRunes.Grunt.NewBody(); // INT 3
        Assert.False(grunt.Activate(new Active("focus-tome", Stat.Int, 5))); // needs more INT than it has

        var adept = CoreRunes.Adept.NewBody(); // INT 10
        Assert.True(adept.Activate(new Active("focus-tome", Stat.Int, 5)));
    }
}
