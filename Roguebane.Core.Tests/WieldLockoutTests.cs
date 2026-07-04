using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// §6d/§6 broken-limb hard override: a broken ARM removes its hand slot outright — the held weapon
// stops answering techniques regardless of which stat it gates (a DEX bow is exactly as arm-gated
// as a STR sword). Hand 0 rides the dominant armR (second Str part), hand 1 the armL.
public class WieldLockoutTests
{
    private static Body Armed(out BodyPart armL, out BodyPart armR)
    {
        var b = new Body();
        armL = new BodyPart("armL", Stat.Str, 3);
        armR = new BodyPart("armR", Stat.Str, 3);
        b.Add(armL); b.Add(armR);
        b.Add(new BodyPart("legL", Stat.Dex, 3));
        b.Add(new BodyPart("legR", Stat.Dex, 3));
        return b;
    }

    [Fact]
    public void ABrokenArmSilencesTheBowWhateverItsStat()
    {
        // §6d: a BOW needs BOTH arms — break either and Shot stops answering, though the bow
        // stays ASSIGNED in its ranged slot (§6e). DEX is untouched throughout.
        var b = Armed(out _, out var armR);
        Assert.True(b.EquipRanged(Armory.Bow));
        Assert.Single(b.Consulted(Armory.Shot));

        b.Damage(armR, 9);
        Assert.Equal(Armory.Bow, b.Ranged);     // still assigned
        Assert.False(b.RangedUsable);
        Assert.Empty(b.Consulted(Armory.Shot)); // but it no longer answers
    }

    [Fact]
    public void ASlingNeedsOnlyOneThrowingArm()
    {
        var b = Armed(out var armL, out var armR);
        Assert.True(b.EquipRanged(Armory.Slings[0]));
        b.Damage(armL, 9); // one arm gone -> the 1H sling still throws
        Assert.True(b.RangedUsable);
        b.Damage(armR, 9); // both gone -> it can't
        Assert.False(b.RangedUsable);
    }

    [Fact]
    public void TheOtherHandKeepsAnswering()
    {
        var b = Armed(out var armL, out _);
        Assert.True(b.Wield(Armory.Maces[0])); // hand 0 (STR 6 >= req 3)
        Assert.True(b.Wield(Armory.Dagger));   // hand 1
        b.Damage(armL, 9);                      // break the OFF arm -> hand 1 gone, hand 0 intact
        Assert.True(b.HandUsable(0));
        Assert.False(b.HandUsable(1));
        Assert.Single(b.Consulted(Armory.Swing)); // the mace in hand 0 still answers
    }

    [Fact]
    public void WeaponTimerScalesTheChargeTimerAndDualWieldAverages()
    {
        // §6d: the consulting weapon's timer multiplies the technique's CHARGE timer
        // (<1.0 = faster); dual-wield averages both. Self-contained techniques never scale.
        // Arms-only body: zero DEX keeps haste out of the arithmetic.
        var b = new Body();
        b.Add(new BodyPart("armL", Stat.Str, 3));
        b.Add(new BodyPart("armR", Stat.Str, 3));
        var fast = new Weapon("w-fast", Stat.Str, 1, 1, Timer: 0.6);
        var slow = new Weapon("w-slow", Stat.Str, 1, 1, Timer: 1.4);
        var c = new Caster(b, new Foe("f", 100));

        var primary = new Technique("p", Stat.Str, 0, TechniqueKind.Timered, Cooldown: 10,
            Power: 0, Consults: WeaponUse.Primary);
        var dual = new Technique("d", Stat.Str, 0, TechniqueKind.Timered, Cooldown: 10,
            Power: 0, Consults: WeaponUse.Both);
        var self = new Technique("s", Stat.Str, 1, TechniqueKind.Timered, Cooldown: 10, Power: 1);

        Assert.True(b.Wield(fast));
        Assert.Equal(6, c.EffectiveCooldown(primary));  // 10 x 0.6
        Assert.True(b.Wield(slow));
        Assert.Equal(10, c.EffectiveCooldown(dual));    // 10 x avg(0.6, 1.4) = 10
        Assert.Equal(10, c.EffectiveCooldown(self));    // self-contained: untouched
    }

    [Fact]
    public void BodiesWithoutArmsDoNotGate()
    {
        var b = new Body();
        b.Add(new BodyPart("legs", Stat.Dex, 4));
        Assert.True(b.HandUsable(0));
        Assert.True(b.HandUsable(1));
    }
}
