namespace Roguebane.Core.Content;

// Sample Path content. Hollow Vessel is the rank-3 keystone: climbing the Vessel ladder
// refunds part of each rung, so reaching the keystone costs less than buying it outright.
public static class Paths
{
    public const string Vessel = "vessel";

    public static readonly Mark VesselI = new(Vessel, Rank: 1, Cost: 4, Refund: 2);
    public static readonly Mark VesselII = new(Vessel, Rank: 2, Cost: 6, Refund: 3);
    public static readonly Mark HollowVessel = new(Vessel, Rank: 3, Cost: 8, Refund: 0, Keystone: true);

    public static readonly IReadOnlyList<Mark> VesselLadder = new[] { VesselI, VesselII, HollowVessel };

    // The specialist's signature ladder. Its tight-budget owner can just afford the climb
    // to Resonant Core; a fat-budget outsider can reach it too, at a real cost.
    public const string Resonance = "resonance";

    public static readonly Mark ResonanceI = new(Resonance, Rank: 1, Cost: 5, Refund: 2);
    public static readonly Mark ResonanceII = new(Resonance, Rank: 2, Cost: 6, Refund: 3);
    public static readonly Mark ResonantCore = new(Resonance, Rank: 3, Cost: 4, Refund: 0, Keystone: true);

    public static readonly IReadOnlyList<Mark> ResonanceLadder = new[] { ResonanceI, ResonanceII, ResonantCore };
}
