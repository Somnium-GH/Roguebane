using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class SessionTests
{
    [Fact]
    public void PauseFreezesTheTick()
    {
        var s = Sessions.Demo();
        s.Toggle(Techniques.Siphon);
        s.TogglePause();

        var before = s.Battle.Outcome;
        for (var i = 0; i < 10; i++) s.Tick();

        Assert.True(s.Paused);
        Assert.Equal(before, s.Battle.Outcome);
    }

    [Fact]
    public void ToggleActivatesAndDeactivatesTechniques()
    {
        var s = Sessions.Demo();
        Assert.False(s.IsActive(Techniques.Cleave));
        s.Toggle(Techniques.Cleave);
        Assert.True(s.IsActive(Techniques.Cleave));
        s.Toggle(Techniques.Cleave);
        Assert.False(s.IsActive(Techniques.Cleave));
    }

    [Fact]
    public void RetreatEndsTheSession()
    {
        var s = Sessions.Demo();
        s.Retreat();
        Assert.Equal(SessionState.Fled, s.State);

        for (var i = 0; i < 5; i++) s.Tick(); // inert once fled
        Assert.Equal(SessionState.Fled, s.State);
    }

    [Fact]
    public void AFullLoadoutClearsTheRunAndWins()
    {
        var s = Sessions.Demo();
        foreach (var t in s.Equipment) s.Toggle(t);

        var steps = 0;
        while (s.State == SessionState.Fighting && steps < 5000)
        {
            s.Tick();
            steps++;
        }

        Assert.Equal(SessionState.Won, s.State);
        Assert.True(s.Run.Completed);
    }

    [Fact]
    public void SessionIsDeterministic()
    {
        static int RunToWin()
        {
            var s = Sessions.Demo();
            foreach (var t in s.Equipment) s.Toggle(t);
            var steps = 0;
            while (s.State == SessionState.Fighting && steps < 5000)
            {
                s.Tick();
                steps++;
            }
            return steps;
        }

        Assert.Equal(RunToWin(), RunToWin());
    }
}
