namespace Roguebane.Core.Tests;

// G1 (foe-fights-back): a structured, armed foe runs its own caster on the player; it erodes the
// player's PARTS (§8, not raw HP — HP only via part-overkill); the player can lose; and smashing the
// foe's parts cascades its attacks off — the mirror of player degradation.
public class FoeOffenseTests
{
    // A single-part body: every personality picks the lone standing part, so these stay deterministic.
    private static Body ChestOnly(int con)
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

    private static Encounter Solo(Foe foe) => new("e", foe, foePartAim: true);

    [Fact]
    public void AnArmedFoeErodesThePlayersPart()
    {
        var player = new Fighter(ChestOnly(4), maxHp: 10);
        var foe = new Foe("brute", 20, FoeFrame(3), new[] { Strike(2) });
        var battle = new Battle(new Caster(player.Body), Solo(foe), player);

        battle.Step();

        // §8: the one hit erodes the aimed part AND takes HP, simultaneously.
        Assert.Equal(2, player.Body.Capacity(Stat.Con)); // chest 4 -> 2 (part)
        Assert.Equal(8, player.Hp);                      // and 2 HP, same hit
    }

    [Fact]
    public void EveryFoeHitTakesPartAndHpUntilTheFighterFalls()
    {
        var player = new Fighter(ChestOnly(1), maxHp: 3);
        var foe = new Foe("brute", 20, FoeFrame(3), new[] { Strike(2) });
        var battle = new Battle(new Caster(player.Body), Solo(foe), player);

        battle.Step();           // §8: chest 1 -> 0 AND HP 3 -> 1 (no overkill maths)
        Assert.Equal(0, player.Body.Capacity(Stat.Con));
        Assert.Equal(1, player.Hp);
        Assert.Equal(BattleOutcome.Ongoing, battle.Outcome);

        battle.Step();           // chest gone -> no standing part -> the hit lands on HP: 1 -> 0 (lost)
        Assert.Equal(BattleOutcome.Lost, battle.Outcome);
    }

    [Fact]
    public void SmashingTheFoesArmCascadesItsAttackOff()
    {
        // Foe arm carries STR 4; its strike reserves STR 2. Erode the arm below 2 and it falls off.
        var player = new Fighter(ChestOnly(4), maxHp: 20);
        var arm = new BodyPart("foe-arm", Stat.Str, 4);
        var frame = new Body();
        frame.Add(arm);
        var foe = new Foe("brute", 20, frame, new[] { Strike(3, reserve: 2) });

        var playerCaster = new Caster(player.Body, foe);
        var smash = new Technique("smash", Stat.Con, 1, TechniqueKind.Sustained, 0, Power: 2);
        playerCaster.Activate(smash);
        playerCaster.Aim(smash, foe, arm);
        var battle = new Battle(playerCaster, Solo(foe), player);

        battle.Step(); // player smashes arm 4 -> 2 (strike still powered); foe's strike takes chest 4 -> 1 AND HP 20 -> 17
        Assert.Equal(1, player.Body.Capacity(Stat.Con));

        battle.Step(); // arm 2 -> 0 before it acts -> strike cascades off, no player hit this tick
        Assert.Equal(1, player.Body.Capacity(Stat.Con));

        battle.Step();
        Assert.Equal(1, player.Body.Capacity(Stat.Con)); // disarmed foe erodes nothing further
        Assert.Equal(17, player.Hp);                     // only the one landed strike hit HP (§8: part+HP)
    }

    [Fact]
    public void AnInertFoeNeverAttacks()
    {
        var player = new Fighter(ChestOnly(4), maxHp: 10);
        var foe = new Foe("dummy", 20); // no frame, no arsenal
        var battle = new Battle(new Caster(player.Body), Solo(foe), player);

        for (var i = 0; i < 5; i++) battle.Step();

        Assert.Equal(10, player.Hp);
        Assert.Equal(4, player.Body.Capacity(Stat.Con));
    }
}
