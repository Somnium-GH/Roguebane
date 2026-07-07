namespace Roguebane.Core.Tests;

// CHUNK D item 1's second Foe Effect (FOES.md, Skeleton): the first hit that breaks a foe's STR
// (arm) part refunds the ATTACKER's own Timered cooldown -- a player-side reward, not a foe-side
// read like Insubstantial. Proven via a bare fixture, not Foes.Skeleton (see STATUS.md's Needs-Doug
// note: FOES.md pairs Skeleton with Jab, a STR-line verb, over an Iron Dagger, a DEX weapon --
// Caster.Consulted only matches a technique's own Stat, so Jab can never consult a dagger; that's a
// content-spec conflict for Doug, not an engine gap, and this proof doesn't need it resolved).
public class BrittleEffectTests
{
    private static Body CasterBody()
    {
        var body = new Body();
        body.Add(new BodyPart("arm", Stat.Str, 12));
        return body;
    }

    private const int CooldownTicks = 40;

    private static Technique Smash(int power) =>
        new("smash", Stat.Str, 1, TechniqueKind.Timered, Cooldown: CooldownTicks, Power: power);

    // Timered techniques start on cooldown (CasterFiringTests' pattern) -- auto:false so it doesn't
    // discharge unaimed while charging, then Step it to ready before the test fires it deliberately.
    private static Caster Charged(Foe foe, Technique tech)
    {
        var caster = new Caster(CasterBody(), foe);
        caster.Activate(tech, auto: false);
        for (var i = 0; i < CooldownTicks; i++) caster.Step();
        return caster;
    }

    [Fact]
    public void BreakingTheArmRefundsTheAimedTechniquesCooldownImmediately()
    {
        var frame = new Body();
        frame.Add(new BodyPart("foe-arm", Stat.Str, 2));
        var foe = new Foe("brittle-test", 8, frame, effect: FoeEffectKind.Brittle);
        var tech = Smash(power: 2); // exactly breaks the 2-capacity arm
        var caster = Charged(foe, tech);
        caster.Aim(tech, foe, frame.Parts[0]);

        Assert.True(caster.Fire(tech));

        Assert.True(foe.EffectTriggered);
        Assert.True(caster.IsReady(tech)); // refunded -- ready now, not on the normal 40-tick cooldown
    }

    [Fact]
    public void ANonBreakingHitGetsNoRefund()
    {
        var frame = new Body();
        frame.Add(new BodyPart("foe-arm", Stat.Str, 4));
        var foe = new Foe("brittle-partial", 8, frame, effect: FoeEffectKind.Brittle);
        var tech = Smash(power: 2); // arm at 2/4 after this -- still standing
        var caster = Charged(foe, tech);
        caster.Aim(tech, foe, frame.Parts[0]);

        Assert.True(caster.Fire(tech));

        Assert.False(foe.EffectTriggered);
        Assert.False(caster.IsReady(tech)); // normal cooldown -- nothing to refund yet
    }

    [Fact]
    public void ItOnlyRefundsTheFirstArmBreakNotASecondOne()
    {
        var frame = new Body();
        frame.Add(new BodyPart("foe-arm1", Stat.Str, 2));
        frame.Add(new BodyPart("foe-arm2", Stat.Str, 2));
        var foe = new Foe("brittle-two-arms", 8, frame, effect: FoeEffectKind.Brittle);
        var tech = Smash(power: 2);
        var caster = Charged(foe, tech);

        caster.Aim(tech, foe, frame.Parts[0]);
        Assert.True(caster.Fire(tech)); // breaks arm1 -- refund
        Assert.True(caster.IsReady(tech));

        caster.Aim(tech, foe, frame.Parts[1]);
        Assert.True(caster.Fire(tech)); // breaks arm2 -- already triggered, normal cooldown applies
        Assert.False(caster.IsReady(tech));
    }
}
