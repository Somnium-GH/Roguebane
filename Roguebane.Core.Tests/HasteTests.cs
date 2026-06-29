using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// DEX = action speed: it shortens technique cooldowns a modest % per point, capped so it stays
// non-OP. Cooldowns are in ticks at the 10/sec combat clock.
public class HasteTests
{
    private static Caster CasterWithDex(int dex)
    {
        var b = new Body();
        b.Add(new BodyPart("leg-l", Stat.Dex, dex)); // all DEX in one part for the test
        b.Add(new BodyPart("arm", Stat.Str, 4));     // enough STR to wield Jab
        return new Caster(b);
    }

    [Fact]
    public void NoDexLeavesTheBaseCooldown()
    {
        Assert.Equal(50, CasterWithDex(0).EffectiveCooldown(Techniques.Jab));
    }

    [Fact]
    public void DexShortensTheCooldown()
    {
        // 10 DEX -> 20% haste -> 50 * 80/100 = 40.
        Assert.Equal(40, CasterWithDex(10).EffectiveCooldown(Techniques.Jab));
    }

    [Fact]
    public void HasteIsCappedNonOp()
    {
        // 20 DEX -> capped at 28% -> 50 * 72/100 = 36; more DEX cannot beat the cap.
        Assert.Equal(36, CasterWithDex(20).EffectiveCooldown(Techniques.Jab));
        Assert.Equal(36, CasterWithDex(99).EffectiveCooldown(Techniques.Jab));
    }

    [Fact]
    public void HastedCasterFiresMoreOftenOverTime()
    {
        var dummy = new Foe("dummy", 100000); // big HP so it never dies and never down
        Caster Arm(int dex)
        {
            var c = CasterWithDex(dex);
            c.Retarget(dummy);
            c.Activate(Techniques.Jab);
            return c;
        }

        var slow = Arm(0);
        var fastFoe = new Foe("dummy2", 100000);
        var fast = CasterWithDex(20);
        fast.Retarget(fastFoe);
        fast.Activate(Techniques.Jab);

        for (var i = 0; i < 300; i++) { slow.Step(); fast.Step(); }
        Assert.True(fastFoe.MaxHp - fastFoe.Hp > dummy.MaxHp - dummy.Hp); // haste => more hits landed
    }
}
