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
    public void SupportAutoFiresOnTheEnemyIntermittently()
    {
        // amount 3 every 2 ticks, no boss restore, an idle caster: only rallied support lands.
        var foe = new Foe("boss", 100);
        var encounter = new Encounter("siege", foe, supportAmount: 3, supportEvery: 2);
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
    public void SupportDropsTheEnemyThenStops()
    {
        var foe = new Foe("boss", 3);
        var encounter = new Encounter("siege", foe, supportAmount: 3, supportEvery: 1);
        var battle = new Battle(IdleCaster(), encounter);

        battle.Step(); // 3 onto the one enemy -> down
        Assert.True(foe.Down);
        Assert.Equal(BattleOutcome.Cleared, battle.Outcome); // nothing left to fire on
    }

    [Fact]
    public void SupportChipsTheEnemyForThePlayer()
    {
        // Rallied support fires FOR the player: 2/tick on a 10-HP foe clears it even from an idle caster.
        var foe = new Foe("wall", 10);
        var encounter = new Encounter("siege", foe, supportAmount: 2, supportEvery: 1);
        var battle = new Battle(IdleCaster(), encounter);

        for (var i = 0; i < 100 && battle.Outcome == BattleOutcome.Ongoing; i++) battle.Step();

        Assert.Equal(BattleOutcome.Cleared, battle.Outcome);
    }
}
