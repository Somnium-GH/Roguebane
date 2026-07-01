using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class ForgeTests
{
    [Fact]
    public void HeldRuneGrantsWidenTheMintedPool()
    {
        var chassis = Chassrium.Grunt;
        var baseCon = chassis.NewBody().Capacity(Stat.Con);

        var runes = chassis.NewLoadout();
        foreach (var rung in Paths.VesselLadder) Assert.True(runes.TryTake(rung));

        var minted = chassis.NewBody(runes);
        // Hollow Vessel sockets +6 CON onto the chassis.
        Assert.Equal(baseCon + 6, minted.Capacity(Stat.Con));
    }

    [Fact]
    public void UnallocatedRunesGrantNothing()
    {
        var chassis = Chassrium.Grunt;
        var runes = chassis.NewLoadout(); // nothing taken

        var minted = chassis.NewBody(runes);
        Assert.Equal(chassis.NewBody().Capacity(Stat.Con), minted.Capacity(Stat.Con));
    }

    [Fact]
    public void AssembleThreadsChassisRunesTechniquesAndRunIntoASession()
    {
        var chassis = Chassrium.Grunt;
        var runes = chassis.NewLoadout();
        foreach (var rung in Paths.VesselLadder) runes.TryTake(rung);

        var session = Forge.Assemble(chassis, runes, Techniques.All, Sieges.StandardRun());

        Assert.Equal(SessionState.Fighting, session.State);
        Assert.Equal(Techniques.All.Count, session.Equipment.Count);
        Assert.Equal(chassis.NewBody().Capacity(Stat.Con) + 6, session.Player.Body.Capacity(Stat.Con));
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
