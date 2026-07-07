namespace Roguebane.Core;

// Encounter-clear rewards beyond spoils gold (STATUS.md "Loot backlog", 2026-07-07 Doug-unblocked):
// four INDEPENDENT rolls per cleared node -- gear/technique/rune (rare), supplies (frequent), and
// summons (uncommon) -- matching Doug's "also"/"and" phrasing (separate chances, not a shared slot).
// Percentages and the gear-kind split are placeholder-blessed, not final; tune whenever a real
// economy pass happens (same convention as merchant pricing/MerchantStock's section weights).
public static class LootDrop
{
    public const int GearChancePercent = 8;
    public const int SuppliesChancePercent = 35;
    public const int SummonsChancePercent = 20;
    public const int SuppliesAmount = 1; // placeholder: one jump's worth per drop

    public sealed record Result(Weapon? Weapon, Armor? Armor, Technique? Technique, Mark? Mark,
        bool Supplies, Minion? Summon);

    // rng is caller-owned so a node-clear's gold spoils and loot rolls share one deterministic
    // stream off the same seed (same reproducibility convention as the rest of Expedition).
    public static Result Roll(Rng rng, IReadOnlyList<Weapon> weaponPool, IReadOnlyList<Armor> armorPool,
        IReadOnlyList<Technique> techniquePool, IReadOnlyList<Mark> markPool, IReadOnlyList<Minion> minionPool)
    {
        Weapon? weapon = null;
        Armor? armor = null;
        Technique? technique = null;
        Mark? mark = null;
        if (rng.Chance(GearChancePercent))
        {
            // one of four equally-weighted gear kinds -- no design weighting given, placeholder split
            switch (rng.Next(4))
            {
                case 0 when weaponPool.Count > 0: weapon = weaponPool[rng.Next(weaponPool.Count)]; break;
                case 1 when armorPool.Count > 0: armor = armorPool[rng.Next(armorPool.Count)]; break;
                case 2 when techniquePool.Count > 0: technique = techniquePool[rng.Next(techniquePool.Count)]; break;
                case 3 when markPool.Count > 0: mark = markPool[rng.Next(markPool.Count)]; break;
            }
        }

        var supplies = rng.Chance(SuppliesChancePercent);

        Minion? summon = rng.Chance(SummonsChancePercent) && minionPool.Count > 0
            ? minionPool[rng.Next(minionPool.Count)]
            : null;

        return new Result(weapon, armor, technique, mark, supplies, summon);
    }
}
