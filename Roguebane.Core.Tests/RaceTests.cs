using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// §7 race split (design/05): attrs + HP come from the Race ALONE; its body lays those attrs into the
// standard Head/Chest/Arms x2/Legs x2 anatomy. A CoreRune contributes no attrs.
public class RaceTests
{
    [Fact]
    public void RaceBodyCarriesItsAttrsAcrossTheStandardAnatomy()
    {
        var body = Races.Human.NewBody(); // 3/3/3/3

        Assert.Equal(6, body.Parts.Count);          // head, chest, 2 arms, 2 legs
        Assert.Equal(3, body.Capacity(Stat.Str));   // arms split 3
        Assert.Equal(3, body.Capacity(Stat.Int));
        Assert.Equal(3, body.Capacity(Stat.Dex));   // legs split 3
        Assert.Equal(3, body.Capacity(Stat.Con));
    }

    [Fact]
    public void OddAttrsSplitAcrossTheTwoLimbsWithoutLoss()
    {
        var body = Races.Elf.NewBody(); // dex 4 -> 2+2, str 2 -> 1+1
        Assert.Equal(4, body.Capacity(Stat.Dex));
        Assert.Equal(2, body.Capacity(Stat.Str));
    }

    [Fact]
    public void ElfIsDexLeaningAndFrailerThanHuman()
    {
        Assert.True(Races.Elf.Dex > Races.Human.Dex);
        Assert.True(Races.Elf.Hp < Races.Human.Hp);
        Assert.Equal(14, Races.Elf.Hp);
        Assert.Equal(20, Races.Human.Hp);
    }
}
