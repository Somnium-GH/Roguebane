namespace Roguebane.Core;

// How a minion is paid for, as DATA so chassis express their theme:
//  - Stat:    reserves a stat to keep standing (default, INT-funded) — a drained stat dismisses it
//             via the cascade. The bread-and-butter summon.
//  - None:    ungated — a chassis-granted loyal ally (a knight's retinue) that costs no reservation
//             and never cascades off.
//  - AltCost: paid once at summon from a DESIGNED cost (HP or a stat, §9) instead of reserving a stat
//             — NOT Charge (Charge is the shield-pierce resource now). No alt-cost minion is authored
//             yet, so the summon is currently un-costed; wire the HP/stat spend when one ships.
public enum MinionGate { Stat, None, AltCost }

// A minion occupies a BAY (not an action-bar slot). Its GATE decides what summoning costs; while
// powered it fires on its own TIMER (ticks between discharges, same unit as a Technique's Cooldown —
// §9, 2026-07-04) instead of every combat tick. AltCost holds the (designed, non-Charge) alt cost
// amount. Minion VARIETY (re-gating onto STR/DEX/CON) rides on the Gate field.
public sealed record Minion(
    string Id, Stat Stat, int Reserve, int Power, int Timer,
    MinionGate Gate = MinionGate.Stat, int AltCost = 0,
    string Desc = "") // DISPLAY-ONLY card copy (design/01); {power}/{timer} resolve from data at render.
{
    public string DescText => Desc.Replace("{power}", Power.ToString()).Replace("{timer}", Timer.ToString());
}
