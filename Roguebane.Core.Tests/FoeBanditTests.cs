using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// CHUNK D item 2's seventh roster foe (FOES.md, Bandit): the first SHIELDED foe -- a real Brace
// (Sustained, CON) run by the foe's own offense Caster alongside Swing, proving a foe can hold its
// own shield pool the same way ShieldWiringTests proves it for a player. Chest CON raised 2->3 (Doug
// 2026-07-07, the Foe attribute-for-equipment rule) to fit Wooden Shield's 1 CON equip + Brace's 2 CON
// reserve EXACTLY -- no headroom, a deliberately tight fit, pinned here rather than assumed.
public class FoeBanditTests
{
    private static Fighter Bystander() => new(new Body(), maxHp: 500); // survives; applies no pressure

    [Fact]
    public void BanditSwingDealsTheWieldedAxesActualPower()
    {
        var foe = Foes.Bandit("bandit");
        var player = Bystander();
        var battle = new Battle(new Caster(new Body()), new Encounter("e", foe, foePartAim: true), player);

        for (var i = 0; i < 90; i++) battle.Step(); // comfortably past one Swing cooldown, short of two

        Assert.Equal(500 - Armory.Axes[0].Power, player.Hp); // exactly one wielded axe's Power
    }

    [Fact]
    public void SmashingBanditsWeaponArmDropsTheAxeFromConsultedGear()
    {
        var foe = Foes.Bandit("bandit");
        var frame = foe.Frame!;
        Assert.NotEmpty(frame.Consulted(Armory.Swing)); // wielding, sane baseline

        frame.Damage(frame.Parts[0], 3); // arm STR 3 -> 0, below the Iron Axe's Reserve 1

        Assert.Empty(frame.Consulted(Armory.Swing)); // DisabledGear sheds it -- the same cascade a player gets
    }

    [Fact]
    public void BanditsChestCarriesTheShieldAndBraceExactlyWithNoHeadroom()
    {
        var foe = Foes.Bandit("bandit");
        var frame = foe.Frame!;

        // Battle's ctor activates the whole arsenal (Brace included) before this reads Available --
        // build through a real Battle so the reservation is the one actually exercised in play.
        _ = new Battle(new Caster(new Body()), new Encounter("e", foe, foePartAim: false), Bystander());

        Assert.Equal(0, frame.Available(Stat.Con)); // Wooden Shield (1) + Brace (2) == chest CON 3, exact fit
        Assert.Equal(4, frame.ShieldPoints); // Brace's pool starts full
    }

    [Fact]
    public void BracesShieldPoolAbsorbsAHitOnTheBanditBeforeItsHpTakesDamage()
    {
        var foe = Foes.Bandit("bandit");
        var frame = foe.Frame!;
        _ = new Battle(new Caster(new Body()), new Encounter("e", foe, foePartAim: false), Bystander());
        Assert.Equal(4, frame.ShieldPoints);

        var attackerBody = new Body();
        attackerBody.Add(new BodyPart("player-arm", Stat.Str, 4));
        var strike = new Technique("strike", Stat.Str, 1, TechniqueKind.Sustained, 0, Power: 2);
        var attacker = new Caster(attackerBody, foe);
        attacker.Activate(strike);

        attacker.Step(); // power 2 -> shield absorbs, nothing reaches HP

        Assert.Equal(2, frame.ShieldPoints);
        Assert.Equal(foe.MaxHp, foe.Hp);
    }
}
