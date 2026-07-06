namespace Roguebane.Core;

// The Core Effect interpreter's dispatch key (RULES_SNAPSHOT.md "Cores" table). One enum drives
// every mechanical hook (Body's equip-time discounts, Caster's reservation/cooldown/charge hooks)
// so a core's identity lives in ONE place rather than scattered string checks on CoreEffectName.
public enum CoreEffectKind
{
    None,
    JackOfAllTrades, // Grunt: every attribute cost you pay is reduced by 1.
    Fortified,       // Warden: Plate armor is paid in CON at 1 less per tier.
    Resonance,       // Adept: a landed targeted spell reduces its own next charge time by 2%, stacking to 5.
    Conscription,    // Summoner: minions do not consume Summons when fielded (CoreEffectFreeSummons).
    Finesse,         // Reaver: techniques requiring two weapons cost 1 less to activate.
    FletcherLuck,    // Ranger: bow techniques have a 20% chance to consume no charge; bows cost 1 less per tier.
    WarlordMight,    // Barbarian: two-handed swords cost 3 less STR; STR plate costs 1 less STR per piece.
}
