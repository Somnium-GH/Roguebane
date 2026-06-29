namespace Roguebane.Core.Tests;

public class SupportTests
{
    private static Caster IdleCaster() => new(MakeBody()); // no techniques => only support acts

    private static Body MakeBody()
    {
        var body = new Body();
        body.Add(new BodyPart("arms", Stat.Str, 10));
        return body;
    }

    [Fact]
    public void SupportAutoFiresOnTheFrontIntermittently()
    {
        // amount 3 every 2 ticks, no boss restore, an idle caster: only rallied support lands.
        var foe = new Foe("gate", 100);
        var encounter = new Encounter("siege", new[] { foe }, structural: true,
            supportAmount: 3, supportEvery: 2);
        var battle = new Battle(IdleCaster(), encounter);

        battle.Step(); // tick 1, no fire
        Assert.Equal(100, foe.Hp);
        battle.Step(); // tick 2, fires 3
        Assert.Equal(97, foe.Hp);
        battle.Step(); // tick 3
        Assert.Equal(97, foe.Hp);
        battle.Step(); // tick 4, fires 3
        Assert.Equal(94, foe.Hp);
    }

    [Fact]
    public void SupportHitsTheCurrentFrontAndAdvancesWithIt()
    {
        var gate = new Foe("gate", 3);
        var keep = new Foe("keep", 100);
        var encounter = new Encounter("siege", new[] { gate, keep }, structural: true,
            supportAmount: 3, supportEvery: 1);
        var battle = new Battle(IdleCaster(), encounter);

        battle.Step(); // 3 onto gate -> gate down
        Assert.True(gate.Down);
        battle.Step(); // now lands on the keep
        Assert.Equal(97, keep.Hp);
    }

    [Fact]
    public void SupportRacesAgainstBossRestoreNotForIt()
    {
        // support 2/tick vs boss restore 1/tick: net player gain, the front falls without the
        // player lifting a finger — support helps the player, the inverse of the old code.
        var foe = new Foe("wall", 10);
        var encounter = new Encounter("siege", new[] { foe }, structural: true,
            restoreAmount: 1, restoreEvery: 1, supportAmount: 2, supportEvery: 1);
        var battle = new Battle(IdleCaster(), encounter);

        for (var i = 0; i < 100 && battle.Outcome == BattleOutcome.Ongoing; i++) battle.Step();

        Assert.Equal(BattleOutcome.Cleared, battle.Outcome);
    }
}
