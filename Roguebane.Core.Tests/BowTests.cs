using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// The BOW (charge #4): a DEX stat-stick whose consulting verb Armory.Shot IGNORES shields and spends
// Charge per loose (§6b). Power/cost come from the wielded bow. The Ranger core ships it.
public class BowTests
{
    private static Body Archer()
    {
        var body = new Body();
        body.Add(new BodyPart("leg-l", Stat.Dex, 2)); // DEX to wield the bow (reserve 2)
        body.Add(new BodyPart("leg-r", Stat.Dex, 2));
        body.Add(new BodyPart("head", Stat.Int, 3));  // INT funds the charge pool
        return body;
    }

    [Fact]
    public void ShotLoosesTheBowThroughShieldsForCharge()
    {
        var defBody = new Body();
        defBody.Add(new BodyPart("head", Stat.Int, 4));
        defBody.Add(new BodyPart("chest", Stat.Con, 6));
        var defender = new Fighter(defBody, maxHp: 20);
        new Caster(defBody).Activate(Techniques.Stoneskin); // 3-layer shield
        Assert.Equal(3, defBody.ShieldPoints);

        var body = Archer();
        body.Add(new BodyPart("arm-l", Stat.Str, 2)); // §6d: a bow needs both arms usable
        body.Add(new BodyPart("arm-r", Stat.Str, 2));
        Assert.True(body.EquipRanged(Armory.Bow));           // the RANGED slot (DEX 4 >= reserve 2)

        var atk = new Caster(body, defender, maxCharge: 2);
        atk.Activate(Armory.Shot);
        atk.Aim(Armory.Shot, defender);
        for (var i = 0; i < 160; i++) atk.Step();            // cd 80, 8% haste (4 DEX) -> 74/loose,
                                                              // fires twice then the charge runs dry

        Assert.Equal(3, defBody.ShieldPoints);               // shields IGNORED (piercing)
        Assert.Equal(0, atk.Charge);                         // both charges spent (1 per loose)
        Assert.Equal(20 - 2 * 2, defender.Hp);               // two looses, bow power 2 each, straight to HP
    }

    [Fact]
    public void RangerShipsAndWieldsTheBow()
    {
        var exp = Forge.Embark(Races.Human, CoreRunes.Ranger, CoreRunes.Ranger.NewLoadout(),
            CoreRunes.Ranger.Kit, Maps.StandardLeg());

        Assert.Equal("bow", exp.Player.Body.Ranged?.Id); // bow mounts the RANGED slot at assembly
        Assert.Contains(CoreRunes.Ranger.Kit, t => t.Id == "shot"); // the Shot verb is on the bar
        Assert.True(Armory.Shot.ShieldPiercing);                    // and it pierces
    }
}
