using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// CHUNK D item 2's fourth roster foe (FOES.md, Troll): the first content foe pairing REAL gear (Iron
// Axe, Swing-consulted -- FoeGearTests precedent) with a REAL self-mend that a Foe Effect modifies
// live (RegenerativeFlesh doubles Bandage, proven bare in TrollRegenerativeFleshTests). This file
// proves the two wire together correctly through actual Foes.Troll content and a real Battle, not a
// synthetic fixture.
public class FoeTrollTests
{
    private static Fighter Bystander() => new(new Body(), maxHp: 500); // survives; applies no pressure

    [Fact]
    public void TrollSwingDealsTheWieldedAxesActualPower()
    {
        var foe = Foes.Troll("troll");
        var player = Bystander();
        var battle = new Battle(new Caster(new Body()), new Encounter("e", foe, foePartAim: true), player);

        for (var i = 0; i < 75; i++) battle.Step(); // one Swing cooldown (Iron Axe timer 0.9x haste'd 80 -> 72)

        Assert.Equal(500 - Armory.Axes[0].Power, player.Hp); // exactly the wielded axe's own Power
    }

    [Fact]
    public void SmashingTrollsWeaponArmDropsTheAxeFromConsultedGear()
    {
        var foe = Foes.Troll("troll");
        var frame = foe.Frame!;
        Assert.NotEmpty(frame.Consulted(Armory.Swing)); // wielding, sane baseline

        frame.Damage(frame.Parts[0], 4); // arm STR 4 -> 0, below the Iron Axe's Reserve 1

        Assert.Empty(frame.Consulted(Armory.Swing)); // DisabledGear sheds it -- the same cascade a player gets
    }

    [Fact]
    public void TrollsBandageMendsDoubleThroughRegenerativeFleshInARealBattle()
    {
        var foe = Foes.Troll("troll");
        var frame = foe.Frame!;
        frame.Damage(frame.Parts[0], 2); // wound the arm so Bandage's mend has somewhere to land

        var player = Bystander();
        var battle = new Battle(new Caster(new Body()), new Encounter("e", foe, foePartAim: false), player);

        for (var i = 0; i < 80; i++) battle.Step(); // one Bandage cooldown (CON-reserved Timered, 8.0s)

        // Bandage's base Power is 1; RegenerativeFlesh doubles it to 2 (Caster.cs), same as the bare
        // fixture in TrollRegenerativeFleshTests -- this proves it fires through real Foes.Troll content.
        Assert.Equal(4 - 2 + 2, frame.Contribution(frame.Parts[0]));
    }

    [Fact]
    public void BreakingTrollsChestBelowBandagesReserveSilencesTheDoubledMendEntirely()
    {
        var foe = Foes.Troll("troll");
        var frame = foe.Frame!;
        frame.Damage(frame.Parts[0], 2); // wound to mend
        frame.Damage(frame.Parts[3], 3); // chest CON 4 -> 1, below Bandage's Reserve 2 -- "break the chest first"

        var player = Bystander();
        var battle = new Battle(new Caster(new Body()), new Encounter("e", foe, foePartAim: false), player);

        for (var i = 0; i < 80; i++) battle.Step();

        Assert.Equal(2, frame.Contribution(frame.Parts[0])); // still wounded -- the mend never fires
    }
}
