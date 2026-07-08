using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// G2: weapons are stat-sticks; techniques CONSULT them for cost and power; losing the arm drops them.
public class WeaponTests
{
    private static (Body, BodyPart, BodyPart) ArmsBody(int strEach)
    {
        var body = new Body();
        var l = new BodyPart("arm-l", Stat.Str, strEach);
        var r = new BodyPart("arm-r", Stat.Str, strEach);
        body.Add(l);
        body.Add(r);
        return (body, l, r);
    }

    [Fact]
    public void WieldingGatesOnStatCapacity()
    {
        var (weak, _, _) = ArmsBody(1);   // 2 STR total
        Assert.False(weak.Wield(Armory.Maces[0])); // Iron Mace needs 3, can't lift it

        var (strong, _, _) = ArmsBody(3); // 6 STR total
        Assert.True(strong.Wield(Armory.Maces[0]));
    }

    [Fact]
    public void TwoHandsIsTheLimit()
    {
        var (body, _, _) = ArmsBody(4); // 8 STR
        Assert.True(body.Wield(Armory.Sword));
        Assert.True(body.Wield(Armory.Axe));
        Assert.False(body.Wield(Armory.Dagger)); // no third hand
    }

    [Fact]
    public void ASwingConsultsThePrimaryWeaponForPowerAndReserve()
    {
        var (body, _, _) = ArmsBody(3); // 6 STR
        body.Wield(Armory.Sword);       // Iron Longsword: power 4, reserve 2
        var foe = new Foe("front", 100);
        var caster = new Caster(body, foe);

        Assert.True(caster.Activate(Armory.Swing));
        Assert.Equal(2, body.Reserved(Stat.Str)); // sword's reserve, not the technique's 0

        for (var i = 0; i < 80; i++) caster.Step(); // timered cd 80 (0 DEX haste, sword timer 1.0)
        Assert.Equal(96, foe.Hp);      // sword power 4
    }

    [Fact]
    public void JabReservesAdditivelyOnTopOfThePrimaryWeaponsOwnReserve()
    {
        // Reservation-timing fix (2026-07-07, Doug bug report -- RULES_SNAPSHOT "Reservation / combat
        // model"): a Consults==Primary technique's OWN Reserve field used to be zeroed by
        // Caster.ResolveReservation/Body.EffectiveTechniqueReserve as a special case. Equipment reserves
        // at equip time and techniques reserve SEPARATELY and ADDITIVELY on activation -- no exception
        // for weapon-consulting verbs. Iron Longsword (Reserve 2) + Jab (Reserve 1) must reserve 3, not 2.
        var (body, _, _) = ArmsBody(3); // 6 STR
        body.Wield(Armory.Sword);       // Iron Longsword: reserve 2
        var foe = new Foe("front", 100);
        var caster = new Caster(body, foe);

        Assert.True(caster.Activate(Techniques.Jab));
        Assert.Equal(3, body.Reserved(Stat.Str)); // sword's 2 + Jab's own 1, additive
    }

    [Fact]
    public void WithoutAWeaponAConsultingTechniqueCannotActivate()
    {
        var (body, _, _) = ArmsBody(6);
        var caster = new Caster(body, new Foe("f", 10));
        Assert.False(caster.Activate(Armory.Swing)); // nothing to swing
    }

    [Fact]
    public void FrenzyConsultsBothWeaponsSummingTheirReserveAndPower()
    {
        var (body, _, _) = ArmsBody(5); // 10 STR
        body.Wield(Armory.Sword);       // Iron Longsword r2 p4
        body.Wield(Armory.Axe);         // Iron Axe r1 p3
        var foe = new Foe("front", 100);
        var caster = new Caster(body, foe);

        Assert.True(caster.Activate(Armory.Frenzy));
        Assert.Equal(6, body.Reserved(Stat.Str)); // 2 + 1 weapon reserves, + Frenzy's own reserve of 3

        for (var i = 0; i < 76; i++) caster.Step(); // cd 80, 0 haste (no DEX), Sword/Axe avg timer 0.95 -> 76
        Assert.Equal(93, foe.Hp);                  // 4 + 3 = 7
    }

    [Fact]
    public void SmashingAnArmDisablesAWeaponButItStaysAssigned()
    {
        // §6 [LOCKED]: below its threshold the weapon does NOT leave the slot — it reads
        // DISABLED, stops answering, and re-activates when the attribute heals.
        var (body, l, r) = ArmsBody(2); // 4 STR, the Iron Mace needs 3
        body.Wield(Armory.Maces[0]);
        Assert.Single(body.Hands);

        body.Damage(l, 2); // STR 4 -> 2, below the mace's threshold (arm damaged, not broken)
        Assert.Single(body.Hands);                 // still assigned
        Assert.False(body.HandItemUsable(0));
        Assert.Empty(body.Consulted(Armory.Swing)); // but it no longer answers

        body.Repair(l, 2); // the attribute heals -> the mace re-activates by itself
        Assert.True(body.HandItemUsable(0));
        Assert.Single(body.Consulted(Armory.Swing));
    }
}
