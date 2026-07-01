using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// The Phase 3 combat thesis, pinned end-to-end: under LIVE foe part-aim (§8), a defensive source keeps
// a build alive — an in-combat heal (§10) slows the part-strip, and a shield source (§6b) absorbs the
// strikes outright. Bespoke encounters (foePartAim on) so no live content / balance lock is touched;
// this documents WHY foe part-aim stays staged off until such sources sit in the kits (G1 reconcile).
public class PartAimMitigationTests
{
    private static Body PlayerBody()
    {
        var b = new Body();
        b.Add(new BodyPart("arm", Stat.Str, 6));
        b.Add(new BodyPart("head", Stat.Int, 4));
        b.Add(new BodyPart("legs", Stat.Dex, 4));
        b.Add(new BodyPart("chest", Stat.Con, 4));
        return b;
    }

    private static int TotalCapacity(Body b) => b.Parts.Sum(b.Contribution);

    // A tanky, SMART part-aiming foe: it keeps swinging at the player's largest live stat.
    private static Encounter PartAimingFoe() =>
        new("e", new[] { Foes.Armed("brute", 200, aim: FoeAim.Smart) }, structural: false, foePartAim: true);

    private static Body RunUnder(Technique? defense, ulong seed)
    {
        var body = PlayerBody();
        var caster = new Caster(body);
        if (defense is not null) caster.Activate(defense);
        var battle = new Battle(caster, PartAimingFoe(), new Fighter(body, maxHp: 40), seed);
        for (var i = 0; i < 400; i++) battle.Step();
        return body;
    }

    [Fact]
    public void AnInCombatHealSlowsThePartStrip()
    {
        var healed = RunUnder(Techniques.Bandage, seed: 5);
        var bare = RunUnder(null, seed: 5);
        Assert.True(TotalCapacity(healed) > TotalCapacity(bare),
            $"heal should leave more stat standing ({TotalCapacity(healed)} vs {TotalCapacity(bare)})");
    }

    [Fact]
    public void AShieldSourceAbsorbsThePartStrip()
    {
        var shielded = RunUnder(Techniques.Stoneskin, seed: 5);
        var bare = RunUnder(null, seed: 5);
        Assert.True(TotalCapacity(shielded) > TotalCapacity(bare),
            $"shield should leave more stat standing ({TotalCapacity(shielded)} vs {TotalCapacity(bare)})");
    }

    [Fact]
    public void BareBuildIsStrippedTheMost()
    {
        // The healless/shieldless build takes the full erosion — the intended penalty (§6b/§10).
        var bare = RunUnder(null, seed: 5);
        Assert.True(TotalCapacity(bare) < 18); // started at 6+4+4+4 = 18
    }
}
