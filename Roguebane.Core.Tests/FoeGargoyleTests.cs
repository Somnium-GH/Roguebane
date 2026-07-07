using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// CHUNK D item 2's fifth roster foe (FOES.md, Gargoyle): pairs REAL gear (Iron Axe wielded as the
// "stone fists," Jab-consulted -- FoeGearTests/FoeTrollTests precedent) with a live-read Foe Effect
// (Stoneform, proven bare in GargoyleStoneformTests) instead of a reservation-gated one like Troll's
// RegenerativeFlesh. This file proves both wire together through actual Foes.Gargoyle content and a
// real Battle/Caster, not a synthetic fixture.
public class FoeGargoyleTests
{
    private static Fighter Bystander() => new(new Body(), maxHp: 500); // survives; applies no pressure

    [Fact]
    public void JabDealsHalfTheWieldedAxesPowerRoundedAwayFromZero()
    {
        var foe = Foes.Gargoyle("gargoyle");
        var player = Bystander();
        var battle = new Battle(new Caster(new Body()), new Encounter("e", foe, foePartAim: true), player);

        // EffectiveCooldown: base 40 * (100 - 2% haste from legs DEX 1) / 100 = 39.2, * axe timer 0.9 = 35.28 -> 35.
        for (var i = 0; i < 35; i++) battle.Step();

        // Jab: Power 0 + round(Iron Axe Power 3 * DamageMult 0.5, AwayFromZero) = 2.
        Assert.Equal(500 - 2, player.Hp);
    }

    [Fact]
    public void SmashingGargoylesFistArmDropsTheAxeFromConsultedGear()
    {
        var foe = Foes.Gargoyle("gargoyle");
        var frame = foe.Frame!;
        Assert.NotEmpty(frame.Consulted(Techniques.Jab)); // wielding, sane baseline

        frame.Damage(frame.Parts[0], 3); // arm STR 3 -> 0, below the Iron Axe's Reserve 1

        Assert.Empty(frame.Consulted(Techniques.Jab)); // DisabledGear sheds it -- the same cascade a player gets
    }

    private static Technique Smash(int power) =>
        new("smash", Stat.Str, 1, TechniqueKind.Sustained, Cooldown: 0, Power: power);

    private static Caster AimedAt(Foe foe, BodyPart part, int power)
    {
        var body = new Body();
        body.Add(new BodyPart("player-arm", Stat.Str, 12));
        var caster = new Caster(body, foe);
        var tech = Smash(power);
        caster.Activate(tech);
        caster.Aim(tech, foe, part);
        return caster;
    }

    [Fact]
    public void StoneformDiscountsPartDamageThroughRealFoesGargoyleContentWhileTheChestHolds()
    {
        var foe = Foes.Gargoyle("gargoyle");
        var arm = foe.Frame!.Parts[0]; // STR arm, capacity 3 -- big enough that the discount stays visible

        var caster = AimedAt(foe, arm, power: 2);
        caster.Step();

        Assert.Equal(3 - 1, foe.Frame!.Contribution(arm)); // power 2, discounted to 1 (min-1 floor)
        Assert.Equal(12 - 2, foe.Hp); // HP lands full power regardless (Stoneform is part-only)
    }

    [Fact]
    public void BreakingGargoylesChestFirstRemovesStoneformsDiscountEntirely()
    {
        var foe = Foes.Gargoyle("gargoyle");
        var chest = foe.Frame!.Parts[3];
        var arm = foe.Frame!.Parts[0];
        foe.Frame!.Damage(chest, 4); // break the chest before the fight starts (capacity 4)

        var caster = AimedAt(foe, arm, power: 2);
        caster.Step();

        Assert.Equal(3 - 2, foe.Frame!.Contribution(arm)); // full power now -- the effect is gone for good
    }
}
