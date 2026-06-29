namespace Roguebane.Core;

public sealed record BuildSpec(string Name, IReadOnlyList<Technique> Activate);

public sealed record SimResult(BuildSpec Build, bool Won, int Ticks);

// Headless balance harness: run each build through a session to its end and rank them. Because
// the sim is deterministic, the ranking is stable — dominant strategies surface as the winners
// that clear in the fewest ticks.
public static class BalanceSim
{
    public static IReadOnlyList<SimResult> Run(
        IEnumerable<BuildSpec> builds,
        Func<Session> newSession,
        int tickCap = 5000)
    {
        var results = new List<SimResult>();
        foreach (var build in builds)
        {
            var session = newSession();
            foreach (var t in build.Activate) session.Toggle(t); // Session.Toggle arms auto-on (sim path)

            var ticks = 0;
            while (session.State == SessionState.Fighting && ticks < tickCap)
            {
                session.Tick();
                ticks++;
            }

            results.Add(new SimResult(build, session.State == SessionState.Won, ticks));
        }

        return results
            .OrderByDescending(r => r.Won)
            .ThenBy(r => r.Ticks)
            .ThenBy(r => r.Build.Name, StringComparer.Ordinal)
            .ToList();
    }
}
