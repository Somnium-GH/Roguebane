namespace Roguebane.Core;

// FOES.md "Foe-effect design rules": ONE effect per foe, built ONLY from designed mechanics (here:
// live part damage via Body.Damaged, same read Body.ArmorSustained/DisabledGear already use). Data
// (this enum) + the one interpreter site (Caster.Hit) -- CLAUDE.md's "one code path interprets
// data." Only the effects actually wired to a foe belong here; the rest of FOES.md's roster stays
// unbuilt (STATUS.md) rather than adding dead enum cases ahead of the content that needs them.
public enum FoeEffectKind
{
    None,
    Insubstantial, // Wraith: while its INT part is undamaged, a landed hit deals 1 less HP damage (min 1); breaks on the first hit that damages that part.
}
