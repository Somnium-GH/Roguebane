namespace Roguebane.Core.Tests;

// G1 (foe-fights-back): a structured, armed foe runs its own caster on the player; the player can
// lose; and smashing the foe's parts cascades its attacks off — the mirror of player degradation.
public class FoeOffenseTests
{
    private static Body PlayerBody(int con)
    {
        var body = new Body();
        body.Add(new BodyPart("chest", Stat.Con, con));
        return body;
    }

    private static Body FoeFrame(int str)
    {
        var frame = new Body();
        frame.Add(new BodyPart("foe-arm", Stat.Str, str));
        return frame;
    }

    private static Technique Strike(int power, int reserve = 1) =>
        new("strike", Stat.Str, reserve, TechniqueKind.Sustained, Cooldown: 0, Power: power);

    private static Encounter Solo(Foe foe) => new("e", new[] { foe }, structural: false);

    [Fact]
    public void AnArmedFoeChipsThePlayerHp()
    {
        var player = new Fighter(PlayerBody(4), maxHp: 10);
        var foe = new Foe("brute", 20, FoeFrame(3), new[] { Strike(2) });
        var battle = new Battle(new Caster(player.Body), Solo(foe), player);

        battle.Step();

        Assert.Equal(8, player.Hp); // 10 - 2
    }

    [Fact]
    public void ThePlayerLosesWhenHpHitsZero()
    {
        var player = new Fighter(PlayerBody(4), maxHp: 4);
        var foe = new Foe("brute", 20, FoeFrame(3), new[] { Strike(2) });
        var battle = new Battle(new Caster(player.Body), Solo(foe), player);

        battle.Step();           // 4 -> 2
        Assert.Equal(BattleOutcome.Ongoing, battle.Outcome);
        battle.Step();           // 2 -> 0
        Assert.Equal(BattleOutcome.Lost, battle.Outcome);
    }

    [Fact]
    public void SmashingTheFoesArmCascadesItsAttackOff()
    {
        // Foe arm carries STR 4; its strike reserves STR 2. Erode the arm below 2 and it falls off.
        var player = new Fighter(PlayerBody(4), maxHp: 20);
        var arm = new BodyPart("foe-arm", Stat.Str, 4);
        var frame = new Body();
        frame.Add(arm);
        var foe = new Foe("brute", 20, frame, new[] { Strike(3, reserve: 2) });

        var playerCaster = new Caster(player.Body, foe);
        var smash = new Technique("smash", Stat.Con, 1, TechniqueKind.Sustained, 0, Power: 2);
        playerCaster.Activate(smash);
        playerCaster.Aim(smash, foe, arm);
        var battle = new Battle(playerCaster, Solo(foe), player);

        // The smash reserves 1 CON, which now reads as a held block (1 off each HP hit), so the
        // foe's power-3 strike lands for 2.
        battle.Step(); // arm 4 -> 2, strike powered: 3 - 1 block -> player 18
        Assert.Equal(18, player.Hp);

        battle.Step(); // arm 2 -> 0, strike cascades off before it acts -> no damage
        Assert.Equal(18, player.Hp);

        battle.Step();
        Assert.Equal(18, player.Hp); // disarmed foe deals nothing further
    }

    [Fact]
    public void AnInertFoeNeverAttacks()
    {
        var player = new Fighter(PlayerBody(4), maxHp: 10);
        var foe = new Foe("dummy", 20); // no frame, no arsenal
        var battle = new Battle(new Caster(player.Body), Solo(foe), player);

        for (var i = 0; i < 5; i++) battle.Step();

        Assert.Equal(10, player.Hp);
    }
}
