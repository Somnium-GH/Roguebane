using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// CHUNK D item 4 (STATUS.md DoD): headless economy asserts per built roster foe (Wraith, Ogre --
// the only two with real gear/effects wired so far; grows as Skeleton/Gargoyle/Troll/Bandit clear
// their Needs-Doug authoring blocks, same growing-fixture pattern as EncounterTableRosterTests).
// FOES.md's "Balance envelope" section is the spec under test: T1 foe HP 8-16, offense 0.2-0.4 DPS;
// player T1 kit HP 20, DPS 0.4-0.7. "Campaign still winnable for all 35 combos" is already proven
// elsewhere with full mutual combat (CoreCampaignTests.EveryRaceAndCoreWinsTheCampaignWithPartAimPlay,
// now running through this same roster pool since CHUNK D item 3) -- not re-proven here.
public class FoeEconomyTests
{
    private static Fighter Bystander(int hp = 1000) => new(new Body(), maxHp: hp); // absorbs hits, applies no pressure

    // Empirical DPS: run a defenseless bystander against the foe's own offense for a long, multi-cycle
    // window and divide total damage by elapsed seconds -- the same technique FoeGearTests uses for a
    // single cycle, extended for a stable average (10 ticks/sec clock).
    private static double MeasuredDps(Foe foe, int ticks = 3000)
    {
        var player = Bystander();
        var battle = new Battle(new Caster(new Body()), new Encounter("e", foe, foePartAim: true), player);
        for (var i = 0; i < ticks; i++) battle.Step();
        return (1000 - player.Hp) / (ticks / 10.0);
    }

    [Fact]
    public void WraithHpFallsInsideFoesMdsT1Band()
    {
        Assert.InRange(Foes.Wraith("w").MaxHp, 8, 16);
    }

    [Fact]
    public void OgreHpFallsInsideFoesMdsT1Band()
    {
        Assert.InRange(Foes.Ogre("o").MaxHp, 8, 16);
    }

    [Fact]
    public void WraithsEmberDpsFallsInsideFoesMdsT1OffenseBand()
    {
        // FOES.md: "T1 foe ... offense 0.2-0.4 DPS (1-2 dmg per 4-6 s)" -- Ember (1 dmg/~2.9s haste'd) lands here.
        Assert.InRange(MeasuredDps(Foes.Wraith("w")), 0.2, 0.4);
    }

    [Fact]
    public void OgresSwingDpsIsPinnedAboveFoesMdsT1BandNeedsDougReview()
    {
        // FOES.md quotes Ogre's Swing at "~0.35 DPS" (its own T1 band is 0.2-0.4), but Iron Mace's
        // actual Power(5)/EffectiveCooldown(~8.4s haste'd) measures ~0.595 DPS here -- inside T2's
        // 0.4-0.6 band instead, not T1's. This is a genuine spec/engine mismatch from the earlier
        // weapon-consult wiring pass (STATUS.md chunkd-ogre-weapon-consult-gear-wiring), not something
        // to silently retune (which weapon tier/Power a T1 Ogre should wield is Doug's spreadsheet
        // call, flagged in STATUS.md) -- this pins the CURRENT measured value as a regression guard so
        // a future change is deliberate, not a silent drift.
        Assert.InRange(MeasuredDps(Foes.Ogre("o")), 0.55, 0.65);
    }

    [Fact]
    public void SmashingTheWraithsHeadSilencesEmberJustLikeAPlayersBrokenPartSilencesASpell()
    {
        // "lose enough of the stat and the body sheds it" (Technique.cs) applies to foes identically --
        // Ember reserves 1 INT; zeroing the head's INT below that drops it, same cascade as a player's.
        var foe = Foes.Wraith("wraith"); // parts: [0] arm [1] head (Int 4) [2] legs [3] chest
        foe.Frame!.Damage(foe.Frame!.Parts[1], 4); // Int 4 -> 0, below Ember's Reserve 1

        var player = Bystander();
        var battle = new Battle(new Caster(new Body()), new Encounter("e", foe, foePartAim: true), player);
        for (var i = 0; i < 600; i++) battle.Step();

        Assert.Equal(1000, player.Hp); // no INT left to reserve Ember -- it never fires
    }

    // A representative T1 player kit per FOES.md's envelope: HP 20, one Timered strike pinned at the
    // band's midpoint DPS (2 dmg / 3.6s = 0.556, inside the declared 0.4-0.7). foePartAim:false so the
    // foe never counter-attacks (Battle.Step clears its aim when part-aim is off) -- this isolates the
    // kill-time question from part-erosion RNG; full mutual combat across all 35 race/core combos is
    // proven elsewhere (CoreCampaignTests). aimPart null = plain HP aim (fine for Ogre, no Foe Effect
    // gates its incoming damage); Wraith needs its HEAD aimed -- FOES.md's own "aim the HEAD" lesson --
    // or Insubstantial keeps taxing every hit 1 HP, which isn't the intended kill line.
    private static (Fighter player, Foe foe, Battle battle) T1KillTimeRig(Foe foe, BodyPart? aimPart)
    {
        var body = new Body();
        body.Add(new BodyPart("arm", Stat.Str, 5));
        var player = new Fighter(body, maxHp: 20);
        var caster = new Caster(body, foe);
        var strike = new Technique("t1-strike", Stat.Str, 1, TechniqueKind.Timered, Cooldown: 36, Power: 2);
        caster.Activate(strike);
        if (aimPart is not null) caster.Aim(strike, foe, aimPart); else caster.Aim(strike, foe);
        var battle = new Battle(caster, new Encounter("e", foe, foePartAim: false), player);
        return (player, foe, battle);
    }

    private static int TicksToKill(Battle battle, Foe foe)
    {
        var ticks = 0;
        while (!foe.Down && ticks++ < 1000) battle.Step();
        Assert.True(foe.Down, "foe never died within the guard window");
        return ticks;
    }

    [Fact]
    public void WraithsKillTimeAgainstAT1PlayerKitFallsInsideFoesMdsEnvelope()
    {
        var wraith = Foes.Wraith("w");
        var (_, foe, battle) = T1KillTimeRig(wraith, wraith.Frame!.Parts[1]); // head -- breaks Insubstantial
        var seconds = TicksToKill(battle, foe) / 10.0;

        // Envelope-implied bound: foe HP / player DPS band edges (0.4-0.7).
        Assert.InRange(seconds, foe.MaxHp / 0.7, foe.MaxHp / 0.4);
    }

    [Fact]
    public void OgresKillTimeAgainstAT1PlayerKitFallsInsideFoesMdsEnvelope()
    {
        var (_, foe, battle) = T1KillTimeRig(Foes.Ogre("o"), aimPart: null);
        var seconds = TicksToKill(battle, foe) / 10.0;

        Assert.InRange(seconds, foe.MaxHp / 0.7, foe.MaxHp / 0.4);
    }
}
