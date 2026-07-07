namespace Roguebane.Core.Content;

// STATUS.md "Quests" (2026-07-07, Doug: partially unblocked) -- ONE placeholder quest proves the
// mechanism (NodeType.Quest, accept/decline, loot-vocabulary outcomes) while the real narration
// and catalog stay Needs-Doug-and-CD. All text below is an OBVIOUS placeholder, not shipped copy.
public static class Quests
{
    public static readonly Quest Stub = new(
        Id: "stub-quest",
        Prompt: "[PLACEHOLDER QUEST] A stranger flags you down with a job. Take the risk?",
        AcceptText: "[PLACEHOLDER] Take the job",
        DeclineText: "[PLACEHOLDER] Walk away",
        AcceptOutcome: new QuestOutcome( // negative+positive: risk damage, reward gold
            Text: "[PLACEHOLDER] It went sideways, but you got paid.",
            Damage: 4, Gold: 5),
        DeclineOutcome: new QuestOutcome( // negative-alone: no reward, small opportunity cost
            Text: "[PLACEHOLDER] You walk away. The delay costs you.",
            Damage: 1));
}
