using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// Item 3 (STATUS.md 2026-07-12, Doug: "defender limb damage does not seem to be occurring"): pins the
// LIVE skirmish/resource-hold content path end-to-end -- a REAL Sieges foe (drawn from the FOES.md T1
// pool, not a bespoke test Encounter) actually erodes a player PART under §8 foe part-aim. The existing
// PartAimMitigationTests prove the MECHANISM on a hand-authored Encounter; nothing proved the shipped
// Sieges factories wire foePartAim through to real damage. Player runs unopposed (empty caster) so the
// low-HP roster foe survives long enough to demonstrate its offense instead of dying first.
public class FoePartAimLandsTests
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

    // Run the given live encounter against an offenseless, high-HP player so the foe survives to swing.
    private static (int before, int after) RunUnopposed(Encounter enc, ulong seed)
    {
        var body = PlayerBody();
        var before = TotalCapacity(body);
        var battle = new Battle(new Caster(body), enc, new Fighter(body, maxHp: 500), seed);
        for (var i = 0; i < 600; i++) battle.Step();
        return (before, TotalCapacity(body));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void LiveSkirmishFoeErodesAPlayerPart(ulong seed)
    {
        var enc = Sieges.SkirmishPoint("node", seed);
        var (before, after) = RunUnopposed(enc, seed);
        Assert.True(after < before,
            $"{enc.Enemy!.Figure} skirmish (seed {seed}) left player capacity unchanged " +
            $"({after}/{before}) -- foe part-aim never landed on any limb");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void LiveResourceHoldFoeErodesAPlayerPart(ulong seed)
    {
        var enc = Sieges.ResourceHoldPoint("node", seed);
        var (before, after) = RunUnopposed(enc, seed);
        Assert.True(after < before,
            $"{enc.Enemy!.Figure} resource-hold (seed {seed}) left player capacity unchanged " +
            $"({after}/{before}) -- foe part-aim never landed on any limb");
    }
}
