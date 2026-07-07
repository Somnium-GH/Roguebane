using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// Headless mechanical-hook coverage for the Core Effects (CoreEffectKind, CORE_RUNES.md). Each
// effect's real hook lives in Body.cs (equip-time discounts, shared with the DisabledGear sustain
// cascade) or Caster.cs (reservation/discharge-time). Conscription is covered separately by
// CoreRuneRosterTests.SummonerConscriptionNeverSpendsSummonsFieldingMinions.
public class CoreEffectTests
{
    private static Body OneStat(Stat stat, int value)
    {
        var body = new Body();
        body.Add(new BodyPart("part", stat, value));
        return body;
    }

    // --- JackOfAllTrades (Grunt): flat -1 at every equip-time checkpoint, and DisabledGear must
    // reuse the same discounted number the equip gate used.
    [Fact]
    public void JackOfAllTradesDiscountsWieldByOne()
    {
        var sword = Armory.Longswords[0]; // Reserve 2, Str
        Assert.False(OneStat(Stat.Str, 1).Wield(sword));

        var body = OneStat(Stat.Str, 1);
        body.SetCoreEffect(CoreEffectKind.JackOfAllTrades);
        Assert.True(body.Wield(sword)); // 2 - 1 = 1
        Assert.True(body.HandItemUsable(0)); // DisabledGear must agree, not re-disable on the same number
    }

    [Fact]
    public void JackOfAllTradesDiscountsEquipRangedByOne()
    {
        var bow = Armory.Bow; // Reserve 2, Dex
        Assert.False(OneStat(Stat.Dex, 1).EquipRanged(bow));

        var body = OneStat(Stat.Dex, 1);
        body.SetCoreEffect(CoreEffectKind.JackOfAllTrades);
        Assert.True(body.EquipRanged(bow)); // 2 - 1 = 1
    }

    [Fact]
    public void JackOfAllTradesDiscountsArmorRequirementByOne()
    {
        var plate = ArmorLines.PlateArms[0]; // Str-governed, Requirement 2
        Assert.False(OneStat(Stat.Str, 1).Equip(plate));

        var body = OneStat(Stat.Str, 1);
        body.SetCoreEffect(CoreEffectKind.JackOfAllTrades);
        Assert.True(body.Equip(plate)); // 2 - 1 = 1
    }

    // --- Fortified (Warden): Plate's governing attribute reassigns Str -> Con, paid at -1/tier.
    [Fact]
    public void FortifiedReassignsPlateToConAndDiscountsByTier()
    {
        var chest = ArmorLines.PlateChest[0]; // normally Str-governed, Requirement 2
        Assert.False(OneStat(Stat.Con, 1).Equip(chest)); // no Str at all -> fails ungated

        var body = OneStat(Stat.Con, 1); // still no Str
        body.SetCoreEffect(CoreEffectKind.Fortified);
        Assert.True(body.Equip(chest)); // governed by Con now, 2 - tier(1) = 1

        Assert.True(body.ArmorSustained(chest));
        body.Damage(body.Parts.Single(), 3); // Plate's own 2-per-hit part-mitigation soaks 2 -> net 1, Con 1 -> 0
        Assert.False(body.ArmorSustained(chest)); // sheds via CON now, not the raw STR gate
    }

    // --- WarlordMight (Barbarian): 2H STR weapon -3 reserve; Plate armor -1 requirement, flat.
    [Fact]
    public void WarlordMightDiscountsTwoHandedStrWeaponByThree()
    {
        var claymore = Armory.Claymores[0]; // Reserve 5, Str, 2H
        Assert.False(OneStat(Stat.Str, 2).Wield(claymore));

        var body = OneStat(Stat.Str, 2);
        body.SetCoreEffect(CoreEffectKind.WarlordMight);
        Assert.True(body.Wield(claymore)); // 5 - 3 = 2
    }

    [Fact]
    public void WarlordMightDiscountsPlateRequirementByOneFlat()
    {
        var arms = ArmorLines.PlateArms[0]; // Requirement 2
        Assert.False(OneStat(Stat.Str, 1).Equip(arms));

        var body = OneStat(Stat.Str, 1);
        body.SetCoreEffect(CoreEffectKind.WarlordMight);
        Assert.True(body.Equip(arms)); // 2 - 1 = 1
    }

    // --- Resonance (Adept): a landed part-hit stacks a -2%/stack charge discount (cap 5), decaying
    // to 0 on a fresh encounter.
    [Fact]
    public void ResonanceStacksOnLandedHitsAndShortensCooldownThenDecaysOnRearm()
    {
        var caster = new Caster(OneStat(Stat.Int, 5), effect: CoreEffectKind.Resonance);
        var foe = new Foe("golem", 10_000, new Body());
        foe.Frame!.Add(new BodyPart("arm", Stat.Str, 50));
        var arm = foe.Frame!.Parts[0];

        caster.Activate(Techniques.Ember);
        caster.Aim(Techniques.Ember, foe, arm);
        var baseline = caster.StatusOf(Techniques.Ember).Cooldown;

        for (var i = 0; i < baseline * 8; i++) caster.Step(); // plenty of ticks to land several hits and cap at 5 stacks
        Assert.True(caster.StatusOf(Techniques.Ember).Cooldown < baseline);

        caster.RearmForEncounter();
        Assert.Equal(baseline, caster.StatusOf(Techniques.Ember).Cooldown); // decays fresh each encounter
    }

    // --- Finesse (Reaver): a Both-consulting dual-wield technique's OWN reservation costs 1 less.
    [Fact]
    public void FinesseDiscountsDualWieldTechniqueReservationByOne()
    {
        Body TwinDaggerBody()
        {
            var b = new Body();
            b.Add(new BodyPart("arm", Stat.Str, 10));
            b.Add(new BodyPart("leg", Stat.Dex, 10));
            b.Wield(Armory.Daggers[0]);
            b.Wield(Armory.Daggers[0]);
            return b;
        }

        var plain = TwinDaggerBody();
        var plainCaster = new Caster(plain);
        Assert.True(plainCaster.Activate(Armory.Frenzy));
        Assert.Equal(10 - 3, plain.Available(Stat.Str)); // Frenzy's full Reserve 3, undiscounted

        var finesse = TwinDaggerBody();
        var finesseCaster = new Caster(finesse, effect: CoreEffectKind.Finesse);
        Assert.True(finesseCaster.Activate(Armory.Frenzy));
        Assert.Equal(10 - 2, finesse.Available(Stat.Str)); // Reserve 3 - Finesse's -1
    }

    // --- FletcherLuck (Ranger): bow reserve -1/tier, plus a 20% chance to skip a pierce's Charge
    // spend entirely. maxCharge:0 makes discharge possible ONLY on a lucky roll, isolating the
    // mechanic deterministically for a fixed seed.
    [Fact]
    public void FletcherLuckDiscountsBowReserveByTier()
    {
        var bow = Armory.Bows[1]; // tier 2, Reserve 4
        Assert.False(OneStat(Stat.Dex, 2).EquipRanged(bow));

        var body = OneStat(Stat.Dex, 2);
        body.SetCoreEffect(CoreEffectKind.FletcherLuck);
        Assert.True(body.EquipRanged(bow)); // 4 - tier(2) = 2
    }

    // --- Display-facing regression (2026-07-06 loop): the invItems weapon-card badge must read this
    // SAME discounted number, not Weapon.Reserve raw, or the card lies about what the gate charges.
    [Fact]
    public void EffectiveWeaponReserveIsPubliclyReadableForCardDisplayAndMatchesTheEquipGate()
    {
        var claymore = Armory.Claymores[0]; // Reserve 5, Str, 2H
        var plain = new Body();
        Assert.Equal(claymore.Reserve, plain.EffectiveWeaponReserve(claymore)); // no effect -> raw

        var warlord = new Body();
        warlord.SetCoreEffect(CoreEffectKind.WarlordMight);
        Assert.Equal(claymore.Reserve - 3, warlord.EffectiveWeaponReserve(claymore));
    }

    // --- Display-facing regression (2026-07-07 loop): the invItems technique-card badge must read
    // this SAME number Caster.Reservation would charge at Activate() time, not Technique.Reserve raw —
    // the pre-run Equipment screen has no live Caster (needs a combat target) to ask directly.
    [Fact]
    public void EffectiveTechniqueReserveIsPubliclyReadableForCardDisplayAndMatchesCasterReservation()
    {
        var plain = new Body();
        Assert.Equal(Armory.Flurry.Reserve, plain.EffectiveTechniqueReserve(Armory.Flurry)); // no effect -> raw

        var finesse = new Body();
        finesse.SetCoreEffect(CoreEffectKind.Finesse);
        Assert.Equal(Armory.Flurry.Reserve - 1, finesse.EffectiveTechniqueReserve(Armory.Flurry));

        var jack = new Body();
        jack.SetCoreEffect(CoreEffectKind.JackOfAllTrades);
        Assert.Equal(Armory.Flurry.Reserve - 1, jack.EffectiveTechniqueReserve(Armory.Flurry));

        // Primary-consult (e.g. AimedShot): the one weapon it swings already reserves as gear, so the
        // technique itself always reserves 0 regardless of its raw Reserve or any active core effect.
        Assert.Equal(0, plain.EffectiveTechniqueReserve(Armory.AimedShot));
        Assert.Equal(0, jack.EffectiveTechniqueReserve(Armory.AimedShot));
    }

    [Fact]
    public void FletcherLuckSkipsChargeOnALuckyRollWhileABaselineCasterNeverFiresDry()
    {
        (Caster caster, Foe foe) Build(CoreEffectKind effect)
        {
            var body = new Body();
            body.Add(new BodyPart("leg", Stat.Dex, 4));
            body.EquipRanged(Armory.Bow);
            var foe = new Foe("dummy", 10_000);
            var caster = new Caster(body, foe, maxCharge: 0, effect: effect);
            caster.UseRng(new Rng(12345));
            Assert.True(caster.Activate(Armory.Shot));
            return (caster, foe);
        }

        var (noneCaster, noneFoe) = Build(CoreEffectKind.None);
        var (luckCaster, luckFoe) = Build(CoreEffectKind.FletcherLuck);
        for (var i = 0; i < 300; i++)
        {
            noneCaster.Step();
            luckCaster.Step();
        }

        Assert.Equal(10_000, noneFoe.Hp); // dry Charge always holds without the effect
        Assert.True(luckFoe.Hp < 10_000); // the 20% roll eventually fires for free
    }
}
