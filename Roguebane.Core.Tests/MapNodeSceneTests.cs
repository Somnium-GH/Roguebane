namespace Roguebane.Core.Tests;

// CD_STATUS #41 + terrain rule LOCKED by Doug (2026-07-12): each node exposes a Scene(seed) the shell
// resolves to bg/{scene}.png. Camp/Castle are fixed; ResourceHold picks quarry-vs-lumber; every other
// Encounter-routed kind picks from the general terrain pool. The pick is a deterministic function of
// the caller's per-node STABLE seed, so a node keeps ONE backdrop across revisits. These asserts pin
// the pool membership, the fixed cases, and determinism so a stray mapping reddens the build.
public class MapNodeSceneTests
{
    private static readonly ulong[] Seeds = { 0ul, 1ul, 2ul, 7ul, 42ul, 12345ul, ulong.MaxValue };

    [Theory]
    [InlineData(NodeType.Camp, "enc_camp")]
    [InlineData(NodeType.Castle, "enc_city_gates")]
    public void Fixed_kinds_ignore_the_seed(NodeType type, string expected)
    {
        var node = new MapNode("n", type);
        Assert.All(Seeds, s => Assert.Equal(expected, node.Scene(s)));
    }

    [Fact]
    public void ResourceHold_picks_only_quarry_or_lumber()
    {
        var node = new MapNode("n", NodeType.ResourceHold);
        Assert.All(Seeds, s => Assert.Contains(node.Scene(s), new[] { "enc_quarry", "enc_lumber" }));
    }

    [Theory]
    [InlineData(NodeType.Skirmish)]
    [InlineData(NodeType.Quest)]
    public void General_kinds_pick_from_the_terrain_pool(NodeType type)
    {
        var node = new MapNode("n", type);
        var pool = new[] { "enc_forest", "enc_mountain", "enc_river", "enc_meadow" };
        Assert.All(Seeds, s => Assert.Contains(node.Scene(s), pool));
    }

    // Both ResourceHold variants and every terrain must actually be reachable — a pick that can only
    // ever return one option would silently strand the other shipped PNGs (the very gap this replaced).
    [Fact]
    public void Every_variant_is_reachable_across_seeds()
    {
        var res = new MapNode("r", NodeType.ResourceHold);
        var terr = new MapNode("t", NodeType.Skirmish);
        var seenRes = new HashSet<string>();
        var seenTerr = new HashSet<string>();
        for (ulong s = 0; s < 200; s++) { seenRes.Add(res.Scene(s)); seenTerr.Add(terr.Scene(s)); }
        Assert.Equal(new[] { "enc_lumber", "enc_quarry" }, seenRes.OrderBy(x => x).ToArray());
        Assert.Equal(new[] { "enc_forest", "enc_meadow", "enc_mountain", "enc_river" },
            seenTerr.OrderBy(x => x).ToArray());
    }

    // Determinism (CLAUDE.md fixed-timestep rule): same seed always yields the same backdrop, so a node
    // is visually stable across revisits and the sim stays reproducible.
    [Fact]
    public void Scene_is_deterministic_for_a_given_seed()
    {
        var node = new MapNode("n", NodeType.ResourceHold);
        Assert.All(Seeds, s => Assert.Equal(node.Scene(s), node.Scene(s)));
    }
}
