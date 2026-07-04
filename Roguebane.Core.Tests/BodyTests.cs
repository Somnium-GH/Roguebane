namespace Roguebane.Core.Tests;

public class BodyTests
{
    // STR 8 (two arms of 4), DEX 6 (two legs of 3), INT 5 (head), CON 6 (chest).
    private static Body Build(out BodyPart leftArm, out BodyPart rightArm)
    {
        leftArm = new BodyPart("arm-l", Stat.Str, 4);
        rightArm = new BodyPart("arm-r", Stat.Str, 4);
        var body = new Body();
        body.Add(leftArm);
        body.Add(rightArm);
        body.Add(new BodyPart("leg-l", Stat.Dex, 3));
        body.Add(new BodyPart("leg-r", Stat.Dex, 3));
        body.Add(new BodyPart("head", Stat.Int, 5));
        body.Add(new BodyPart("chest", Stat.Con, 6));
        return body;
    }

    [Fact]
    public void CapacityIsTheSumOfPairedShares()
    {
        var body = Build(out _, out _);
        Assert.Equal(8, body.Capacity(Stat.Str));
        Assert.Equal(6, body.Capacity(Stat.Dex));
        Assert.Equal(5, body.Capacity(Stat.Int));
        Assert.Equal(6, body.Capacity(Stat.Con));
    }

    [Fact]
    public void DamageSubtractsThatPartsStatGradedAtLowScale()
    {
        var body = Build(out var leftArm, out _);
        body.Damage(leftArm, 2); // 1-3 scale
        Assert.Equal(6, body.Capacity(Stat.Str));
        Assert.Equal(2, body.Contribution(leftArm));
    }

    [Fact]
    public void LosingAnArmRemovesItsWholeStrShare()
    {
        var body = Build(out var leftArm, out _);
        body.Damage(leftArm, 99);
        Assert.Equal(0, body.Contribution(leftArm));
        Assert.Equal(4, body.Capacity(Stat.Str)); // only the right arm remains
    }

    [Fact]
    public void RepairRestoresPartUpToItsCapacity()
    {
        var body = Build(out var leftArm, out _);
        body.Damage(leftArm, 3);
        body.Repair(leftArm, 2);
        Assert.Equal(3, body.Contribution(leftArm));
        body.Repair(leftArm, 99);
        Assert.Equal(4, body.Contribution(leftArm)); // capped at the part's share
    }

    [Fact]
    public void CannotEngageGearWithoutEnoughStat()
    {
        var body = Build(out _, out _);
        var greatPlate = new Active("great-plate", Stat.Str, 9); // STR is only 8
        Assert.False(body.Activate(greatPlate));
        Assert.False(body.IsActive(greatPlate));
    }

    [Fact]
    public void SmashingAnArmDropsTheGearItCouldNoLongerCarry()
    {
        var body = Build(out var leftArm, out _);
        var plate = new Active("plate", Stat.Str, 6);
        var grip = new Active("grip", Stat.Str, 2);
        Assert.True(body.Activate(plate)); // reserved 6 of 8
        Assert.True(body.Activate(grip));  // reserved 8 of 8

        body.Damage(leftArm, 2); // STR 8 -> 6, reserved 8 > 6

        Assert.False(body.IsActive(grip)); // newest sheds first
        Assert.True(body.IsActive(plate)); // 6 still fits

        body.Damage(leftArm, 99); // STR 6 -> 4
        Assert.False(body.IsActive(plate)); // now the torso is exposed
    }


    [Fact]
    public void AttackPowerIsStrPlusAQuarterDex()
    {
        var body = Build(out _, out _);
        Assert.Equal(8 + 6 / 4, body.AttackPower); // 8 + 1 = 9, integer quarter-units
    }

    // Reservation timing [DESIGN_SPEC lock 2026-07-04]: a technique left active from a finished
    // fight (nothing deactivates it between encounters) must NOT leak into the Equipment screen's
    // gear-usability read — only the real (combat-time) checks see TechReserved.
    [Fact]
    public void GearOnlyChecksIgnoreLingeringTechniqueReservation()
    {
        var body = Build(out _, out _); // STR 8
        // Wield gates on raw Capacity only (SUSTAIN MODEL: equip-time gate is separate from ongoing
        // sustain) — so activate the technique FIRST, then wield a piece the shared pool can't cover.
        var stance = new Active("stance", Stat.Str, 5);
        Assert.True(body.Activate(stance));
        Assert.True(body.Wield(new Weapon("sword", Stat.Str, 6, Power: 1))); // 5 + 6 = 11 > STR 8

        Assert.False(body.HandItemUsable(0));       // real check: sword sheds under the tech's draw
        Assert.True(body.HandItemGearOnlyUsable(0)); // Equipment screen: sword alone fits STR 8
    }

    [Fact]
    public void RangedGearOnlyUsableIgnoresLingeringTechniqueReservation()
    {
        var body = new Body();
        body.Add(new BodyPart("leg-l", Stat.Dex, 4));
        body.Add(new BodyPart("leg-r", Stat.Dex, 4));
        var stance = new Active("stance", Stat.Dex, 5);
        Assert.True(body.Activate(stance));
        Assert.True(body.EquipRanged(new Weapon("bow", Stat.Dex, 7, Power: 1, Kind: WeaponKind.Bow))); // 5 + 7 = 12 > DEX 8

        Assert.False(body.RangedUsable);
        Assert.True(body.RangedGearOnlyUsable);
    }

    [Fact]
    public void ArmorGearOnlySustainedIgnoresLingeringTechniqueReservation()
    {
        var body = Build(out _, out _); // STR 8
        var plate = Content.ArmorLines.PlateLegs[1]; // Str-governed, Requirement 4
        var stance = new Active("stance", Stat.Str, 5);
        Assert.True(body.Activate(stance));
        Assert.True(body.Equip(plate)); // 5 + 4 = 9 > STR 8

        Assert.False(body.ArmorSustained(plate));
        Assert.True(body.ArmorGearOnlySustained(plate));
    }
}
