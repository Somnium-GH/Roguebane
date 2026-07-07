namespace Roguebane.Core;

// One resolution branch of a Quest node (STATUS.md "Quests", 2026-07-07 Doug: partially unblocked
// -- build the mechanism now, real catalog is a separate content pass). A branch reads
// negative-alone (Damage>0, no loot) or negative+positive (Damage>0 AND loot together); loot uses
// the SAME vocabulary as node-clear spoils (LootDrop.Result) so a Quest resolves through
// Stash/CityMap exactly like combat does.
public sealed record QuestOutcome(string Text, int Damage = 0, int Gold = 0, bool Supplies = false,
    Weapon? Weapon = null, Armor? Armor = null, Technique? Technique = null, Mark? Mark = null,
    Minion? Summon = null);

// A two-step accept/decline prompt at a NodeType.Quest beacon. Only the DATA SHAPE is designed
// here -- narration, the real catalog, and map placement/frequency are Needs-Doug-and-CD (see
// Content.Quests.Stub for the one placeholder quest this shape currently carries).
public sealed record Quest(string Id, string Prompt, string AcceptText, string DeclineText,
    QuestOutcome AcceptOutcome, QuestOutcome DeclineOutcome);
