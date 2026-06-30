using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// Leather armor grants EVASION (a dodge chance), not flat protection — and the effect rides the part's
// condition. The dodge math + the seeded RNG already exist; these pin the leather content end to end.
public class ArmorEvasionTests
{
    [Fact]
    public void LeatherEvasionRidesThePartCondition()
    {
        var frame = new Body();
        var legs = new BodyPart("legs", Stat.Dex, 4);
        frame.Add(legs);
        frame.Equip(Shops.Hide); // leg leather, 25% — a whole-HP hit consults the legs (DEX)

        Assert.Equal(25, frame.EvasionPercent(null));
        frame.Damage(legs, 4);                       // break the legs
        Assert.Equal(0, frame.EvasionPercent(null)); // the dodge goes with the part
    }

    // Integration: over a fixed seed, a leather-legged defender dodges some of the incoming whole-HP
    // hits, so it ends with more HP than the same defender bare.
    [Fact]
    public void LeatherDodgesSomeIncomingHits()
    {
        static int Survives(bool leather)
        {
            var frame = new Body();
            frame.Add(new BodyPart("legs", Stat.Dex, 4));
            if (leather) frame.Equip(Shops.Hide);
            var foe = new Foe("target", 1000, frame);

            var atk = new Body();
            atk.Add(new BodyPart("head", Stat.Int, 6));
            var caster = new Caster(atk, foe);
            caster.UseRng(new Rng(99));
            caster.Activate(new Technique("bolt", Stat.Int, 1, TechniqueKind.Sustained, 0, Power: 2));

            for (var i = 0; i < 100; i++) caster.Step();
            return foe.Hp;
        }

        Assert.True(Survives(leather: true) > Survives(leather: false));
    }
}
