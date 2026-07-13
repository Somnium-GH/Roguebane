namespace Roguebane.Core.Content;

// Thin wrappers over the shared catalog: race attrs/HP/copy now live in design/systems/cores.json (the
// single source of truth, Doug LOCKED 2026-07-12), loaded by CoreRuneCatalog. Kept as named statics so
// every existing call site (Races.Human, Races.Roster) compiles unchanged. Do NOT re-inline values here
// — that's the three-way drift this file was pulled out of.
public static class Races
{
    public static readonly Race Human = CoreRuneCatalog.Default.Races["human"];
    public static readonly Race Elf = CoreRuneCatalog.Default.Races["elf"];
    public static readonly Race Dwarf = CoreRuneCatalog.Default.Races["dwarf"];
    public static readonly Race Halfling = CoreRuneCatalog.Default.Races["halfling"];
    public static readonly Race HalfGiant = CoreRuneCatalog.Default.Races["half_giant"];

    // Roster order matches design/05's Race column.
    public static readonly IReadOnlyList<Race> Roster = new[] { Human, Elf, Dwarf, Halfling, HalfGiant };
}
