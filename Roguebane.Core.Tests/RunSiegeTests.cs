using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class RunSiegeTests
{
    private static readonly IReadOnlyDictionary<Attribute, int> NoDemand = new Dictionary<Attribute, int>();

    private static Encounter Stronghold(int frontHp, int repairAmount, int repairEvery)
    {
        var wall = new Part("wall", NoDemand, PartRole.Generic, frontHp);
        var defenders = new Entity(new AttributePool(NoDemand));
        defenders.Add(wall);
        return new Encounter("hold", defenders, new[] { wall }, structural: true, repairAmount, repairEvery);
    }

    private static Caster Attacker(Encounter target, params Technique[] techniques)
    {
        var self = new Entity(new AttributePool(new Dictionary<Attribute, int>
        {
            [Attribute.Power] = 50, [Attribute.Focus] = 50, [Attribute.Vigor] = 50,
        }));
        var head = new Part("head", new Dictionary<Attribute, int> { [Attribute.Vigor] = 1 }, PartRole.Head, 5);
        self.Add(head);
        self.Enable(head);

        var caster = new Caster(self, target.Defenders, target.CurrentTarget!);
        foreach (var t in techniques) caster.Activate(t);
        return caster;
    }

    [Fact]
    public void ControlPointFocusesTheWeakestDefender()
    {
        var cp = Sieges.ControlPoint("cp", 12, 5); // index 0 is tougher
        var weak = cp.Parts[1];
        var tough = cp.Parts[0];

        Assert.Equal(weak, cp.CurrentTarget);
        cp.Defenders.Damage(weak, 5);
        Assert.Equal(tough, cp.CurrentTarget);
    }

    [Fact]
    public void CastleBreaksTheFrontLayerBeforeTheNext()
    {
        var castle = Sieges.Castle();
        var gate = castle.Parts[0];
        var wall = castle.Parts[1];

        Assert.Equal(gate, castle.CurrentTarget);
        castle.Defenders.Damage(gate, gate.MaxHealth);
        Assert.Equal(wall, castle.CurrentTarget); // strictly the next layer
    }

    [Fact]
    public void RalliedSupportStallsAWeakAttacker()
    {
        var hold = Stronghold(frontHp: 10, repairAmount: 3, repairEvery: 1); // +3/tick
        var battle = new Battle(Attacker(hold, Techniques.Drain), hold);     // -2/tick

        for (var i = 0; i < 100; i++) battle.Step();

        Assert.False(hold.Cleared);
        Assert.Equal(BattleOutcome.Ongoing, battle.Outcome);
    }

    [Fact]
    public void EnoughDamageOutRacesTheSupportStream()
    {
        var hold = Stronghold(frontHp: 10, repairAmount: 1, repairEvery: 1); // +1/tick
        var battle = new Battle(Attacker(hold, Techniques.Drain), hold);     // -2/tick

        for (var i = 0; i < 100 && battle.Outcome == BattleOutcome.Ongoing; i++) battle.Step();

        Assert.True(hold.Cleared);
        Assert.Equal(BattleOutcome.Cleared, battle.Outcome);
    }

    [Fact]
    public void FleeEndsTheBattleWithoutClearingIt()
    {
        var hold = Stronghold(frontHp: 50, repairAmount: 0, repairEvery: 0);
        var battle = new Battle(Attacker(hold, Techniques.Drain), hold);

        battle.Step();
        battle.Flee();
        battle.Step(); // no effect once fled

        Assert.Equal(BattleOutcome.Fled, battle.Outcome);
        Assert.False(hold.Cleared);
    }

    [Fact]
    public void RunAdvancesThroughControlPointsThenCompletesAtTheCastle()
    {
        var run = Sieges.StandardRun();

        Assert.False(run.TryAdvance()); // cp1 not cleared yet
        ClearAll(run.Current);
        Assert.True(run.TryAdvance());  // -> cp2

        ClearAll(run.Current);
        Assert.True(run.TryAdvance());  // -> castle
        Assert.True(run.OnFinalEncounter);

        Assert.False(run.Completed);
        ClearAll(run.Current);          // raze the castle
        Assert.True(run.Completed);
        Assert.False(run.TryAdvance());  // nothing past the castle
    }

    [Fact]
    public void SiegeIsDeterministic()
    {
        static int StepsToClear()
        {
            var hold = Stronghold(frontHp: 10, repairAmount: 1, repairEvery: 1);
            var battle = new Battle(Attacker(hold, Techniques.Drain), hold);
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

    private static void ClearAll(Encounter e)
    {
        foreach (var p in e.Parts) e.Defenders.Damage(p, p.MaxHealth);
    }
}
