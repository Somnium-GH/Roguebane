namespace Roguebane.Core.Content;

// Sample Path content. Hollow Vessel is the rank-3 keystone: climbing the Vessel ladder
// refunds part of each rung, so reaching the keystone costs less than buying it outright.
public static class Paths
{
    public const string Vessel = "vessel";

    public static readonly Mark VesselI = new(Vessel, Rank: 1, Cost: 4, Refund: 2);
    public static readonly Mark VesselII = new(Vessel, Rank: 2, Cost: 6, Refund: 3);
    // Chassis-extending keystone: a Hollow Vessel sockets extra CON onto the body (the vessel core).
    public static readonly Mark HollowVessel = new(Vessel, Rank: 3, Cost: 8, Refund: 0, Keystone: true,
        Grants: new[] { new BodyPart("vessel-core", Stat.Con, 6) });

    public static readonly IReadOnlyList<Mark> VesselLadder = new[] { VesselI, VesselII, HollowVessel };

    // The specialist's signature ladder. Its tight-budget owner can just afford the climb
    // to Resonant Core; a fat-budget outsider can reach it too, at a real cost.
    public const string Resonance = "resonance";

    public static readonly Mark ResonanceI = new(Resonance, Rank: 1, Cost: 5, Refund: 2);
    public static readonly Mark ResonanceII = new(Resonance, Rank: 2, Cost: 6, Refund: 3);
    // The specialist keystone amplifies the head: a Resonant Core sockets extra INT (spell power).
    public static readonly Mark ResonantCore = new(Resonance, Rank: 3, Cost: 4, Refund: 0, Keystone: true,
        Grants: new[] { new BodyPart("resonant-core", Stat.Int, 4) });

    public static readonly IReadOnlyList<Mark> ResonanceLadder = new[] { ResonanceI, ResonanceII, ResonantCore };

    // A non-extension keystone: instead of widening the pool, the Tempest keystone GRANTS a verb the
    // chassis never had — a sustained INT storm. Demonstrates rune-granted techniques.
    public const string Tempest = "tempest";

    public static readonly Technique Maelstrom =
        new("maelstrom", Stat.Int, Reserve: 3, TechniqueKind.Sustained, Cooldown: 0, Power: 3);

    public static readonly Mark TempestI = new(Tempest, Rank: 1, Cost: 4, Refund: 2);
    public static readonly Mark TempestII = new(Tempest, Rank: 2, Cost: 5, Refund: 2);
    public static readonly Mark EyeOfTheStorm = new(Tempest, Rank: 3, Cost: 6, Refund: 0, Keystone: true,
        Techniques: new[] { Maelstrom });

    public static readonly IReadOnlyList<Mark> TempestLadder = new[] { TempestI, TempestII, EyeOfTheStorm };

    // Another non-extension keystone: the Conclave keystone GRANTS a minion type (the bound Shade).
    public const string Conclave = "conclave";

    public static readonly Mark ConclaveI = new(Conclave, Rank: 1, Cost: 4, Refund: 2);
    public static readonly Mark ConclaveII = new(Conclave, Rank: 2, Cost: 5, Refund: 2);
    public static readonly Mark BoundConclave = new(Conclave, Rank: 3, Cost: 6, Refund: 0, Keystone: true,
        Minions: new[] { Minions.Shade });

    public static readonly IReadOnlyList<Mark> ConclaveLadder = new[] { ConclaveI, ConclaveII, BoundConclave };
}
