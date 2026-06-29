namespace Roguebane.Core;

// One stat, one body part. STR=Arms, INT=Head, DEX=Legs, CON=Chest. WIS folded into INT,
// CHA dropped. Integer-only and low-scale (~20 is huge) so combat math stays deterministic.
public enum Stat
{
    Str,
    Int,
    Dex,
    Con,
}
