namespace Roguebane.Core.Content;

// Minions are content. The POC bestiary is INT-funded (the Binder's summons); beast/follower types
// re-homed onto other stats are parked (see "Needs human").
public static class Minions
{
    // A raised skeleton: cheap to keep, a steady chip on the front.
    public static readonly Minion Skeleton = new("skeleton", Stat.Int, Reserve: 2, Power: 1);

    // A bound shade: costs more INT, hits harder.
    public static readonly Minion Shade = new("shade", Stat.Int, Reserve: 3, Power: 2);

    public static readonly IReadOnlyList<Minion> All = new[] { Skeleton, Shade };
}
