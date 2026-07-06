using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// CON->HP: a CON-scaled fighter's MaxHp is a natural base plus 2 HP per CON. Smashing the chest
// drops CON, shrinking MaxHp and capping current HP down to it. The base comes from the RACE (§7).
public class ConHpTests
{
    [Fact]
    public void AssembledPlayerHpIsTheRaceBasePlusConBonus()
    {
        // §7: the RACE supplies the HP BASE; CON adds the bonus on top (2/CON). Reference the race values
        // so a numbers tune doesn't redden the test.
        var human = Forge.Assemble(Races.Human, CoreRunes.Grunt, CoreRunes.Grunt.NewLoadout(),
            CoreRunes.Grunt.Kit, Sieges.StandardRun());
        Assert.Equal(Races.Human.Hp + 2 * (Races.Human.Con + CoreRunes.Grunt.ConBonus), human.Player.MaxHp);

        // The frailer Elf ends up lower.
        var elf = Forge.Assemble(Races.Elf, CoreRunes.Grunt, CoreRunes.Grunt.NewLoadout(),
            CoreRunes.Grunt.Kit, Sieges.StandardRun());
        Assert.Equal(Races.Elf.Hp + 2 * (Races.Elf.Con + CoreRunes.Grunt.ConBonus), elf.Player.MaxHp);
        Assert.True(elf.Player.MaxHp < human.Player.MaxHp);
    }

    private static (Body body, BodyPart chest) BodyWithChest(int con)
    {
        var body = new Body();
        var chest = new BodyPart("chest", Stat.Con, con);
        body.Add(chest);
        return (body, chest);
    }

    [Fact]
    public void MaxHpIsBasePlusTwoPerCon()
    {
        var (body, _) = BodyWithChest(5);
        var f = Fighter.Scaled(body, baseHp: 8);
        Assert.Equal(18, f.MaxHp); // 8 + 2*5
        Assert.Equal(18, f.Hp);
    }

    [Fact]
    public void ChestDamageShrinksMaxHpAndCapsCurrent()
    {
        var (body, chest) = BodyWithChest(5);
        var f = Fighter.Scaled(body, baseHp: 8); // MaxHp 18, full

        body.Damage(chest, 3); // CON 5 -> 2
        f.CapToMax();
        Assert.Equal(12, f.MaxHp); // 8 + 2*2
        Assert.Equal(12, f.Hp);    // full pool capped down to the new max
    }

    [Fact]
    public void RepairingConDoesNotRefundHpAlreadyLost()
    {
        var (body, chest) = BodyWithChest(5);
        var f = Fighter.Scaled(body, baseHp: 8); // 18

        body.Damage(chest, 3); // -> MaxHp 12
        f.CapToMax();          // Hp 12 persisted
        body.Repair(chest, 3); // CON back to 5 -> MaxHp 18 again
        f.CapToMax();
        Assert.Equal(18, f.MaxHp);
        Assert.Equal(12, f.Hp); // no free refund of the lost 6
    }

    [Fact]
    public void FixedMaxFighterIgnoresCon()
    {
        var (body, chest) = BodyWithChest(5);
        var f = new Fighter(body, maxHp: 10); // fixed, no scaling
        body.Damage(chest, 5);
        f.CapToMax();
        Assert.Equal(10, f.MaxHp);
    }
}
