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
}
