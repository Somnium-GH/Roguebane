using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// STATUS.md "Quests" (2026-07-07, Doug: partially unblocked) -- the MECHANISM: a NodeType.Quest
// beacon is no fight, offers a two-step accept/decline prompt, resolves through the same
// Stash/CityMap hooks as combat loot, and only fires once per node. Content.Quests.Stub is an
// obvious placeholder (real catalog is Needs-Doug-and-CD) -- these tests pin the SHAPE, not the copy.
public class QuestTests
{
    [Fact]
    public void EnteringAQuestNodeIsNoFightAndOffersTheStubPrompt()
    {
        var exp = Sessions.Expedition();
        Assert.True(exp.Enter("quest"));
        Assert.Equal(ExpeditionState.Choosing, exp.State); // no Battle spun up
        Assert.Null(exp.Enemy);
        Assert.True(exp.AtQuest);
        Assert.Equal(Quests.Stub.Id, exp.CurrentQuest?.Id);
    }

    [Fact]
    public void AcceptingAppliesTheNegativeAndPositiveOutcomeAndResolvesTheNodeOnce()
    {
        var exp = Sessions.Expedition();
        exp.Enter("quest");
        var hp = exp.Player.Hp;
        var gold = exp.Gold;

        Assert.True(exp.AcceptQuest());
        Assert.Equal(hp - Quests.Stub.AcceptOutcome.Damage, exp.Player.Hp);
        Assert.Equal(gold + Quests.Stub.AcceptOutcome.Gold, exp.Gold);
        Assert.False(exp.AtQuest); // resolved -- no re-prompt on revisit

        // A second resolve attempt is a no-op, not a double payout.
        Assert.False(exp.AcceptQuest());
        Assert.Equal(hp - Quests.Stub.AcceptOutcome.Damage, exp.Player.Hp);
        Assert.Equal(gold + Quests.Stub.AcceptOutcome.Gold, exp.Gold);
    }

    [Fact]
    public void DecliningAppliesOnlyTheNegativeAloneOutcome()
    {
        var exp = Sessions.Expedition();
        exp.Enter("quest");
        var hp = exp.Player.Hp;
        var gold = exp.Gold;

        Assert.True(exp.DeclineQuest());
        Assert.Equal(hp - Quests.Stub.DeclineOutcome.Damage, exp.Player.Hp);
        Assert.Equal(gold, exp.Gold); // decline: negative-alone, no loot
        Assert.False(exp.AtQuest);
    }

    [Fact]
    public void AcceptOrDeclineOutsideAQuestNodeFails()
    {
        var exp = Sessions.Expedition();
        exp.Enter("a2"); // a Skirmish, not a Quest
        Assert.False(exp.AtQuest);
        Assert.False(exp.AcceptQuest());
        Assert.False(exp.DeclineQuest());
    }
}
