using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// §7 race split (design/05): attrs + HP come from the Race ALONE; its body lays those attrs into the
// standard Head/Chest/Arms x2/Legs x2 anatomy. A CoreRune contributes no attrs. Assertions reference the
// Race's own values (not literals) so the placeholder numbers can be TUNED without reddening the build.
public class RaceTests
{
    [Fact]
    public void RaceBodyCarriesItsAttrsAcrossTheStandardAnatomy()
    {
        var r = Races.Human;
        var body = r.NewBody();

        Assert.Equal(6, body.Parts.Count);                 // head, chest, 2 arms, 2 legs
        Assert.Equal(r.Str, body.Capacity(Stat.Str));      // arms split STR, no loss
        Assert.Equal(r.Int, body.Capacity(Stat.Int));
        Assert.Equal(r.Dex, body.Capacity(Stat.Dex));      // legs split DEX, no loss
        Assert.Equal(r.Con, body.Capacity(Stat.Con));
    }

    [Fact]
    public void OddAttrsSplitAcrossTheTwoLimbsWithoutLoss()
    {
        // Whatever the numbers, the two-limb split must sum back exactly (odd totals included).
        var odd = new Race("odd", Str: 3, Int: 2, Dex: 5, Con: 2, Hp: 10);
        var body = odd.NewBody();
        Assert.Equal(3, body.Capacity(Stat.Str)); // 1 + 2
        Assert.Equal(5, body.Capacity(Stat.Dex)); // 2 + 3
    }

    [Fact]
    public void ElfIsIntLeaningAndFrailerThanHuman()
    {
        Assert.True(Races.Elf.Int > Races.Human.Int);  // the deep-minded caster
        Assert.True(Races.Elf.Con < Races.Human.Con);  // the frailer body
        Assert.True(Races.Elf.Hp < Races.Human.Hp);
    }

    [Fact]
    public void DwarfAndHalfGiantHpMatchDougsConfirmedSwap()
    {
        // Doug (2026-07-05, STATUS.md HIGH PRIORITY #1): the pre-swap numbers had this backwards.
        // Pinning the confirmed placeholder values verbatim (not re-derived) so a future edit can't
        // silently drift back.
        Assert.Equal(17, Races.Dwarf.Hp);
        Assert.Equal(20, Races.HalfGiant.Hp);
        Assert.Equal(13, Races.Halfling.Hp); // unchanged
    }
}
