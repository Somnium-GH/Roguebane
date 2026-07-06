using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class ForgeTests
{
    [Fact]
    public void HeldRuneGrantsWidenTheMintedPool()
    {
        var race = Races.Human;
        var chassis = CoreRunes.Grunt;
        var baseCon = race.NewBody().Capacity(Stat.Con);

        var runes = chassis.NewLoadout();
        foreach (var rung in Paths.VesselLadder) Assert.True(runes.TryTake(rung));

        var minted = chassis.NewBody(race, runes);
        // Grunt's own +1 CON bonus, plus Hollow Vessel's +6 CON from the rune ladder.
        Assert.Equal(baseCon + chassis.ConBonus + 6, minted.Capacity(Stat.Con));
    }

    [Fact]
    public void UnallocatedRunesGrantNothing()
    {
        var race = Races.Human;
        var chassis = CoreRunes.Grunt;
        var runes = chassis.NewLoadout(); // nothing taken

        var minted = chassis.NewBody(race, runes);
        // No rune-granted CON, but the chassis's own stat bonus still applies.
        Assert.Equal(race.NewBody().Capacity(Stat.Con) + chassis.ConBonus, minted.Capacity(Stat.Con));
    }

    [Fact]
    public void AssembleThreadsCoreRuneRunesTechniquesAndRunIntoASession()
    {
        var race = Races.Human;
        var chassis = CoreRunes.Grunt;
        var runes = chassis.NewLoadout();
        foreach (var rung in Paths.VesselLadder) runes.TryTake(rung);

        var session = Forge.Assemble(race, chassis, runes, Techniques.All, Sieges.StandardRun());

        Assert.Equal(SessionState.Fighting, session.State);
        Assert.Equal(Techniques.All.Count, session.Equipment.Count);
        Assert.Equal(race.NewBody().Capacity(Stat.Con) + chassis.ConBonus + 6, session.Player.Body.Capacity(Stat.Con));
    }

    [Fact]
    public void AForgedSessionFightsTheRunToAResult()
    {
        var session = Sessions.Forged();
        foreach (var t in session.Equipment) session.Toggle(t);

        var ticks = 0;
        while (session.State == SessionState.Fighting && ticks < 5000)
        {
            session.Tick();
            ticks++;
        }

        // Deterministic flow reaches a terminal state — the whole pick->forge->siege path runs.
        Assert.NotEqual(SessionState.Fighting, session.State);
    }
}
