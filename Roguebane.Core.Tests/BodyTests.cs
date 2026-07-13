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

    // Damaged() is the UI's third pip tier (bug #4): capacity lost to injury, distinct from both
    // free (Available) and spoken-for-but-intact (Reserved).
    [Fact]
    public void DamagedIsZeroWhenNothingIsHurt()
    {
        var body = Build(out _, out _);
        Assert.Equal(0, body.Damaged(Stat.Str));
    }

    [Fact]
    public void DamagedReflectsExactCapacityLostAndClearsOnRepair()
    {
        var body = Build(out var leftArm, out _);
        body.Damage(leftArm, 2); // STR 8 -> 6
        Assert.Equal(2, body.Damaged(Stat.Str));
        Assert.Equal(6, body.Capacity(Stat.Str));

        body.Repair(leftArm, 2); // fully healed
        Assert.Equal(0, body.Damaged(Stat.Str));
        Assert.Equal(8, body.Capacity(Stat.Str));
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
        // Wield's equip-time gate is gear-only (blind to TechReserved, cumulative only with other
        // EQUIPPED gear) -- so activate the technique FIRST, then wield a piece the shared pool
        // can't cover; with no other gear yet equipped the wield still succeeds gear-only.
        var stance = new Active("stance", Stat.Str, 5);
        Assert.True(body.Activate(stance));
        Assert.True(body.Wield(new Weapon("sword", Stat.Str, 6, Power: 1))); // 5 + 6 = 11 > STR 8

        Assert.False(body.HandItemUsable(0));       // real check: sword sheds under the tech's draw
        Assert.True(body.HandItemGearOnlyUsable(0)); // Equipment screen: sword alone fits STR 8
    }

    [Fact]
    public void DamageShedsArmorBeforeAWeaponOnTheSameStatPool()
    {
        // Doug item 7: when a stat pool is short from part damage, ARMOR sheds before the weapon so a
        // hurt fighter keeps its offense. The weapon here has the HIGHER reserve, so the OLD reserve-
        // first cascade would have dropped the weapon — this test fails without the armor-first partition.
        var body = new Body();
        var arm = new BodyPart("arm", Stat.Str, 9);
        body.Add(arm);
        var blade = new Weapon("blade", Stat.Str, 5, Power: 1);            // reserve 5 (the higher demand)
        var plate = new Armor("plate", "Plate", Stat.Str, ArmorLine.Plate, 2); // STR-governed, requirement 4
        Assert.True(body.Wield(blade));
        Assert.True(body.Equip(plate));
        Assert.True(body.HandItemGearOnlyUsable(0));     // full health: STR 9 covers 5 + 4 exactly
        Assert.True(body.ArmorGearOnlySustained(plate));

        // Damage 5, of which the sustained plate soaks its own PartMitigation (2*T2 = 4), nets 1 to the
        // arm -> STR 9 -> 8. Now 5 + 4 no longer fits, exactly one item must shed.
        body.Damage(arm, 5);
        Assert.Equal(8, body.Capacity(Stat.Str)); // guard the setup: pool really dropped to 8
        Assert.True(body.HandItemGearOnlyUsable(0), "the weapon must be KEPT — armor sheds first (item 7)");
        Assert.False(body.ArmorGearOnlySustained(plate), "the armor is the piece that sheds");
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

    // Equip-time over-reservation gap fix [DESIGN_SPEC §7 "Reservation timing", STATUS 2026-07-05,
    // fixed 2026-07-06 loop]: equipping must be refused OUTRIGHT once it would exceed cumulative
    // gear reservation on a stat -- not silently accepted and left for DisabledGear to degrade
    // afterward. Symmetric with how Activate already refuses instead of degrading.
    [Fact]
    public void WieldRefusesOutrightOnceOtherGearHasFilledThePool()
    {
        var body = Build(out _, out _); // STR 8
        Assert.True(body.Wield(new Weapon("first", Stat.Str, 8, Power: 1))); // fills the pool exactly
        Assert.False(body.Wield(new Weapon("second", Stat.Str, 1, Power: 1))); // any more is refused
        Assert.Empty(body.Hands.Where(w => w.Id == "second")); // never assigned, unlike the old cascade-and-degrade
    }

    [Fact]
    public void EquipArmorRefusesOutrightOnceOtherGearHasFilledThePool()
    {
        var body = Build(out _, out _); // STR 8
        Assert.True(body.Wield(new Weapon("sword", Stat.Str, 8, Power: 1))); // fills the pool exactly
        var plate = Content.ArmorLines.PlateLegs[0]; // Str-governed, Requirement 2
        Assert.False(body.Equip(plate)); // refused at the click, not equipped-then-disabled
        Assert.Null(body.ArmorOn(Stat.Str));
    }

    // §17 #16 DISABLE CASCADE, exhaustive ranking coverage (thesis-adjacent economy math, per the
    // §6e lock: "highest-requirement-first, ties last-equipped-first, cheapest-first recovery").
    // Equip-time now gates cumulatively against other EQUIPPED gear on the same stat (2026-07-06
    // loop, DESIGN_SPEC §7 "Reservation timing" fix), so the cascade can no longer be triggered by
    // stacking gear past the pool AT equip time -- these tests start with enough headroom for every
    // piece to equip, then shrink the pool via Damage to force the SAME cascade ranking. DisabledGear
    // itself is untouched and still recomputes live off current Contribution.
    [Fact]
    public void DisableCascadeShedsHighestRequirementFirstAndRecoversCheapestFirst()
    {
        var arm = new BodyPart("arm", Stat.Str, 12); // headroom for all three to equip cumulatively
        var body = new Body();
        body.Add(arm);

        // Slots deliberately avoid Stat.Str (the arm's own stat): Plate's part-mitigation soak
        // (§6c, worn.PartMitigation) only applies when an armor piece's SLOT matches the damaged
        // part's stat, and would otherwise silently blunt the Damage() calls below.
        var cheap = new Armor("low", "Low", Stat.Con, ArmorLine.Plate, 1);  // Requirement 2
        var mid = new Armor("mid", "Mid", Stat.Dex, ArmorLine.Plate, 2);    // Requirement 4
        var costly = new Armor("top", "Top", Stat.Int, ArmorLine.Plate, 3); // Requirement 6
        Assert.True(body.Equip(cheap));  // 2 <= 12
        Assert.True(body.Equip(mid));    // 2 + 4 = 6 <= 12
        Assert.True(body.Equip(costly)); // 2 + 4 + 6 = 12 <= 12, exactly fits

        body.Damage(arm, 6); // pool 12 -> 6: combined demand 12 > 6, only the priciest piece sheds
        Assert.False(body.ArmorSustained(costly));
        Assert.True(body.ArmorSustained(mid));
        Assert.True(body.ArmorSustained(cheap));

        body.Damage(arm, 3); // pool 6 -> 3: now even 2 + 4 (=6) overshoots, mid sheds too
        Assert.False(body.ArmorSustained(costly));
        Assert.False(body.ArmorSustained(mid));
        Assert.True(body.ArmorSustained(cheap)); // the cheapest piece is the last one standing

        body.Repair(arm, 3); // pool back to 6: recovery re-enables cheapest-first with no new state
        Assert.False(body.ArmorSustained(costly)); // still the one piece that can't fit
        Assert.True(body.ArmorSustained(mid));
        Assert.True(body.ArmorSustained(cheap));
    }

    [Fact]
    public void DisableCascadeTiesBreakLastEquippedFirst()
    {
        var armL = new BodyPart("arm-l", Stat.Str, 5);
        var armR = new BodyPart("arm-r", Stat.Str, 5);
        var body = new Body();
        body.Add(armL);
        body.Add(armR); // STR 10 -- headroom for both weapons to wield cumulatively

        var first = new Weapon("first", Stat.Str, 5, Power: 1);
        var second = new Weapon("second", Stat.Str, 5, Power: 1);
        Assert.True(body.Wield(first));  // seq 1, 5 <= 10
        Assert.True(body.Wield(second)); // seq 2, 5 + 5 = 10 <= 10, exactly fits

        body.Damage(armL, 2); // STR 10 -> 8: sum 10 > 8, equal reserves -- tie-break kicks in
        Assert.False(body.HandItemUsable(1)); // last-equipped sheds first on a tie
        Assert.True(body.HandItemUsable(0));
    }

    [Fact]
    public void DisableCascadeRanksAcrossHandRangedAndArmorTogether()
    {
        var armL = new BodyPart("arm-l", Stat.Str, 5);
        var armR = new BodyPart("arm-r", Stat.Str, 5);
        var body = new Body();
        body.Add(armL);
        body.Add(armR); // STR 10 -- headroom for all three to equip cumulatively

        var hand = new Weapon("sword", Stat.Str, 3, Power: 1);
        var ranged = new Weapon("great-bow", Stat.Str, 3, Power: 1, Kind: WeaponKind.Bow);
        // Slot deliberately not Stat.Str (the damaged arm's own stat): Plate's part-mitigation soak
        // only applies when an armor piece's SLOT matches the damaged part's stat and would
        // otherwise blunt the Damage() call below even though this piece is Str-GOVERNED.
        var armor = new Armor("heavy", "Heavy", Stat.Con, ArmorLine.Plate, 2); // Requirement 4
        Assert.True(body.Wield(hand));           // seq 1, reserve 3
        Assert.True(body.EquipRanged(ranged));   // seq 2, reserve 3
        Assert.True(body.Equip(armor));          // seq 3, requirement 4 -- 3+3+4=10 <= 10, exactly fits

        body.Damage(armL, 2); // STR 10 -> 8: combined demand 10 > 8
        // The armor sheds on MAGNITUDE alone despite being equipped last -- recency only breaks
        // ties, it never overrides a higher requirement.
        Assert.False(body.ArmorSustained(armor));
        Assert.True(body.HandItemUsable(0));
        Assert.True(body.RangedUsable);
    }
}
