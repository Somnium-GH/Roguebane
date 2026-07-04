using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// §6c leather grants EVASION (a global dodge stacking per worn sustained piece), never flat
// protection — and a broken leg hard-zeroes it (§6). The dodge math + the seeded RNG already
// exist; these pin the leather content end to end.
public class ArmorEvasionTests
{
    [Fact]
    public void LeatherEvasionRidesItsGoverningAttribute()
    {
        var frame = new Body();
        var legs = new BodyPart("legs", Stat.Dex, 4);
        frame.Add(legs);
        frame.Equip(Shops.Hide); // §6c Leather Leggings, tier 1 -> 2% evade

        Assert.Equal(2, frame.EvasionPercent());
        frame.Damage(legs, 4);                   // break the legs
        Assert.Equal(0, frame.EvasionPercent()); // §6 hard zero + the piece's DEX collapsed
    }

    // Integration: over a fixed seed, a leather-wearing defender dodges some of the incoming
    // whole-HP hits, so it ends with more HP than the same defender bare. A full tier-4 set
    // (4 x 8% = 32%) keeps the dodge count meaningful over 100 swings.
    [Fact]
    public void LeatherDodgesSomeIncomingHits()
    {
        static int Survives(bool leather)
        {
            var frame = new Body();
            frame.Add(new BodyPart("legs", Stat.Dex, 4));
            frame.Add(new BodyPart("chest", Stat.Con, 4));
            frame.Add(new BodyPart("head", Stat.Int, 4));
            frame.Add(new BodyPart("arms", Stat.Str, 4));
            if (leather)
                foreach (var l in new[] { ArmorLines.LeatherHead, ArmorLines.LeatherChest,
                    ArmorLines.LeatherArms, ArmorLines.LeatherLegs })
                    frame.Equip(l[3]);
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
