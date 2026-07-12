namespace Roguebane.Core.Tests;

// CD_STATUS #41: each node exposes a backdrop Scene id that the shell resolves to bg/{scene}.png via
// the manifest imageBind. Only the DESIGNED mappings are wired (Camp, Castle); every other kind — and
// the still-undesigned Skirmish/Quest terrain + ResourceHold quarry/lumber split — falls back to the
// neutral combat field until a design lands. These asserts pin that contract so a stray mapping (or a
// premature terrain guess) reddens the build.
public class MapNodeSceneTests
{
    [Theory]
    [InlineData(NodeType.Camp, "enc_camp")]
    [InlineData(NodeType.Castle, "enc_city_gates")]
    [InlineData(NodeType.Skirmish, "combat_field")]
    [InlineData(NodeType.ResourceHold, "combat_field")]
    [InlineData(NodeType.Merchant, "combat_field")]
    [InlineData(NodeType.Quest, "combat_field")]
    [InlineData(NodeType.Unknown, "combat_field")]
    public void Scene_maps_only_designed_kinds(NodeType type, string expected)
        => Assert.Equal(expected, new MapNode("n", type).Scene);

    // Scene is a pure function of Type — no randomness, no per-instance state — so a node's backdrop is
    // stable across revisits. This is what lets us defer the flagged terrain-persistence question: it
    // simply cannot differ between two reads of the same node.
    [Fact]
    public void Scene_is_stable_across_reads()
    {
        var node = new MapNode("n", NodeType.Camp);
        var first = node.Scene;
        node.MarkVisited();
        Assert.Equal(first, node.Scene);
    }
}
