using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class RunSiegeTests
{
    private static Encounter Stronghold(int frontHp) => new("hold", new Foe("wall", frontHp));

    private static Caster Attacker(params Technique[] techniques)
    {
        var body = new Body();
        body.Add(new BodyPart("arms", Stat.Str, 20));
        body.Add(new BodyPart("head", Stat.Int, 20));
        var caster = new Caster(body);
        foreach (var t in techniques) caster.Activate(t);
        return caster;
    }

    private static readonly Technique Drain =
        new("drain", Stat.Int, 2, TechniqueKind.Sustained, Cooldown: 0, Power: 2);

    [Fact]
    public void AnEncounterIsOneEnemy()
    {
        // Single-foe canon: a control point folds its old layers into ONE foe; it IS the target until down.
        var cp = Sieges.ControlPoint("cp", 12, 5); // -> one foe, hp 17
        var foe = cp.Enemy!;
        Assert.Same(foe, cp.Enemy);

        foe.Damage(foe.MaxHp);
        Assert.True(cp.Enemy!.Down); // downed -> nothing live to aim at
        Assert.True(cp.Cleared);
    }

    [Fact]
    public void TheCastleIsAnArmedMendingBoss()
    {
        // §8: no free restore -- the castle is a STRUCTURED, armed boss whose Arsenal carries a real
        // heal technique (run by its own offense caster) alongside its strike.
        var boss = Sieges.Castle().Enemy!;
        Assert.NotNull(boss.Frame);                         // structured (part-aimable)
        Assert.Contains(boss.Arsenal, t => t.Heals);        // a real mend technique, not a free tick
        Assert.Contains(boss.Arsenal, t => t.Power > 0);    // and a strike
        Assert.False(boss.Down);
    }

    [Fact]
    public void EnoughDamageClearsAHold()
    {
        var hold = Stronghold(frontHp: 10);
        var battle = new Battle(Attacker(Drain), hold); // -2/tick

        for (var i = 0; i < 100 && battle.Outcome == BattleOutcome.Ongoing; i++) battle.Step();

        Assert.True(hold.Cleared);
        Assert.Equal(BattleOutcome.Cleared, battle.Outcome);
    }

    [Fact]
    public void RetreatEndsTheBattleWithoutClearingIt()
    {
        var hold = Stronghold(frontHp: 50);
        var battle = new Battle(Attacker(Drain), hold);

        battle.Step();
        battle.Retreat();
        battle.Step();

        Assert.Equal(BattleOutcome.Fled, battle.Outcome);
        Assert.False(hold.Cleared);
    }

    [Fact]
    public void RunAdvancesThroughControlPointsThenCompletesAtTheCastle()
    {
        var run = Sieges.StandardRun();

        Assert.False(run.TryAdvance());
        ClearAll(run.Current);
        Assert.True(run.TryAdvance());

        ClearAll(run.Current);
        Assert.True(run.TryAdvance());
        Assert.True(run.OnFinalEncounter);

        Assert.False(run.Completed);
        ClearAll(run.Current);
        Assert.True(run.Completed);
        Assert.False(run.TryAdvance());
    }

    [Fact]
    public void SiegeIsDeterministic()
    {
        static int StepsToClear()
        {
            var hold = Stronghold(frontHp: 10);
            var battle = new Battle(Attacker(Drain), hold);
            var steps = 0;
            while (battle.Outcome == BattleOutcome.Ongoing && steps < 1000)
            {
                battle.Step();
                steps++;
            }
            return steps;
        }

        Assert.Equal(StepsToClear(), StepsToClear());
    }

    private static void ClearAll(Encounter e) => e.Enemy.Damage(e.Enemy.MaxHp);
}
