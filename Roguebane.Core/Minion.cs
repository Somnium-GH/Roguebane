namespace Roguebane.Core;

// How a minion is paid for, as DATA so chassis express their theme:
//  - Stat:    reserves a stat to keep standing (default, INT-funded) — a drained stat dismisses it
//             via the cascade. The bread-and-butter summon.
//  - None:    ungated — a chassis-granted loyal ally (a knight's retinue) that costs no reservation
//             and never cascades off.
//  - AltCost: paid once at summon from the finite charge resource instead of reserving a stat (a
//             caster that spends magic to call an ally) — no ongoing reservation, no cascade.
public enum MinionGate { Stat, None, AltCost }

// A minion occupies a BAY (not an action-bar slot). Its GATE decides what summoning costs; while
// powered it auto-fires on whatever the caster is pressing. AltCost is the charge spent when
// Gate == AltCost. Minion VARIETY (re-gating onto STR/DEX/CON) rides on this Gate field now.
public sealed record Minion(
    string Id, Stat Stat, int Reserve, int Power, MinionGate Gate = MinionGate.Stat, int AltCost = 0);
