using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// FTL firing model: a Timered technique charges to ready then HOLDS — it discharges on command
// (Fire) or, with AUTO on, automatically on cadence. Aim picks which target a fired shot lands on.
public class CasterFiringTests
{
    private static Body Body()
    {
        var b = new Body();
        b.Add(new BodyPart("arm", Stat.Str, 6)); // no DEX => base cooldown
        b.Wield(Armory.Sword); // Jab consults it: power 4 * .5x = 2
        return b;
    }

    [Fact]
    public void ChargedNonAutoTechniqueHoldsUntilFired()
    {
        var foe = new Foe("dummy", 1000);
        var c = new Caster(Body(), foe);
        c.Activate(Techniques.Jab, auto: false);

        for (var i = 0; i < 200; i++) c.Step(); // long past its cooldown
        Assert.True(c.IsReady(Techniques.Jab)); // charged and holding
        Assert.Equal(1000, foe.Hp);             // never auto-fired

        Assert.True(c.Fire(Techniques.Jab));    // discharge on command
        Assert.True(foe.Hp < 1000);
        Assert.False(c.IsReady(Techniques.Jab)); // recharging again
    }

    [Fact]
    public void AutoTechniqueRepeatsWithoutCommand()
    {
        var foe = new Foe("dummy", 1000);
        var c = new Caster(Body(), foe);
        c.Activate(Techniques.Jab, auto: true);

        for (var i = 0; i < 200; i++) c.Step();
        Assert.True(foe.Hp < 1000); // fired on its own, repeatedly
    }

    [Fact]
    public void FiringBeforeReadyIsANoOp()
    {
        var foe = new Foe("dummy", 1000);
        var c = new Caster(Body(), foe);
        c.Activate(Techniques.Jab, auto: false);

        Assert.False(c.IsReady(Techniques.Jab)); // fresh, still charging
        Assert.False(c.Fire(Techniques.Jab));    // can't fire mid-charge
        Assert.Equal(1000, foe.Hp);
    }

    [Fact]
    public void PlayerDoctrineChargesToReadyWhileUntargetedThenFiresCleanlyOnceAimed()
    {
        // Debt watch item: "firing after a weapon charges while UNTARGETED misbehaves" (no repro found
        // yet). This pins the one Core-side path that could produce it: a player technique (requireAim)
        // reaching Countdown<=0 with no aim must HOLD indefinitely (not skip/duplicate its cooldown), and
        // the very next Step after being aimed must discharge exactly once with a clean cooldown reset.
        var foe = new Foe("dummy", 1000);
        var c = new Caster(Body(), foe, requireAim: true);
        c.Activate(Techniques.Jab, auto: true); // player techs are always Auto:true; requireAim holds fire

        for (var i = 0; i < 200; i++) c.Step(); // charges well past its cooldown, no aim yet
        Assert.True(c.IsReady(Techniques.Jab));  // ready...
        Assert.Equal(1000, foe.Hp);              // ...but untargeted -> never auto-fired

        c.Aim(Techniques.Jab, foe);
        c.Step(); // first tick after aiming
        Assert.True(foe.Hp < 1000);              // fires cleanly, exactly once
        Assert.False(c.IsReady(Techniques.Jab)); // cooldown reset, recharging again
    }

    [Fact]
    public void FireLandsOnTheTechniquesOwnAimNotTheDefaultFront()
    {
        var front = new Foe("front", 100);
        var flank = new Foe("flank", 100);
        var c = new Caster(Body(), front);
        c.Activate(Techniques.Jab, auto: false);
        c.Aim(Techniques.Jab, flank);

        for (var i = 0; i < 60; i++) c.Step(); // charge to ready
        Assert.True(c.Fire(Techniques.Jab));

        Assert.Equal(100, front.Hp);   // default front untouched
        Assert.True(flank.Hp < 100);   // the aimed flank took the hit
    }

    // Activation default refinement [LOCKED 2026-07-09, Doug]: a Timered technique's on/off state does
    // NOT carry into the next encounter -- RearmForEncounter must deactivate it outright (not just
    // rewind its charge), so the player starts every fight cold and must re-activate it explicitly.
    [Fact]
    public void RearmForEncounterDeactivatesATimeredTechniqueEntirely()
    {
        var foe = new Foe("dummy", 1000);
        var c = new Caster(Body(), foe);
        c.Activate(Techniques.Jab, auto: false);

        for (var i = 0; i < 200; i++) c.Step(); // charge well past ready
        Assert.True(c.IsReady(Techniques.Jab));

        c.RearmForEncounter();
        Assert.False(c.IsActive(Techniques.Jab)); // fully off, not just rewound -- must be re-activated
        Assert.False(c.IsReady(Techniques.Jab));
    }

    // ApplyEncounterDefaults [LOCKED 2026-07-09, Doug]: the one call site that actually arms the
    // Sustained "shield" default -- a fresh Caster and RearmForEncounter both leave everything off, so
    // this is what powers Brace on without the player having to toggle it, while leaving a Timered
    // attack like Jab untouched (cold, same as today).
    [Fact]
    public void ApplyEncounterDefaultsActivatesEquippedSustainedButLeavesTimeredAlone()
    {
        var body = new Body();
        body.Add(new BodyPart("arm", Stat.Str, 6));
        body.Add(new BodyPart("chest", Stat.Con, 8));
        body.Wield(Armory.Sword);
        var c = new Caster(body, new Foe("dummy", 100));

        c.ApplyEncounterDefaults(new[] { Techniques.Jab, Techniques.Brace });

        Assert.True(c.IsActive(Techniques.Brace));
        Assert.False(c.IsActive(Techniques.Jab));
    }

    // RE-ARM SCOPE (DESIGN_SPEC §7, LOCKED 2026-07-05): minions do NOT persist across back-to-back
    // encounters -- every fielded minion is dismissed at encounter end, full stop, so re-fielding one
    // next encounter re-pays Summons like any fresh summon (no carry-over discount).
    [Fact]
    public void RearmForEncounterDismissesEveryFieldedMinionAndFreesItsReservation()
    {
        var body = new Body();
        body.Add(new BodyPart("head", Stat.Int, 5)); // Skeleton reserves 2 Int
        var foe = new Foe("dummy", 100);
        var c = new Caster(body, foe, maxSummons: 5);
        Assert.True(c.Summon(Minions.Skeleton, minionCap: 1)); // Timer 25, Power 1
        var summonsBeforeRearm = c.SummonsLeft;

        for (var i = 0; i < Minions.Skeleton.Timer - 1; i++) c.Step(); // charged to 1 tick from firing
        c.RearmForEncounter();

        Assert.Equal(0, c.MinionCount); // dismissed, not just rewound
        Assert.True(body.Available(Stat.Int) >= 5); // the Int reservation was freed too

        for (var i = 0; i < 60; i++) c.Step(); // no minion fielded -- nothing can fire
        Assert.Equal(100, foe.Hp);

        Assert.True(c.Summon(Minions.Skeleton, minionCap: 1)); // must re-summon to fight on
        Assert.Equal(summonsBeforeRearm - 1, c.SummonsLeft); // re-pays Summons, no carry-over discount

        for (var i = 0; i < Minions.Skeleton.Timer; i++) c.Step();
        Assert.Equal(99, foe.Hp); // fires once re-fielded and its full timer has run
    }
}
