namespace Roguebane.Core.Content;

// Thin wrappers over the shared catalog: a core's budget, action-bar size, minion capacity, stat bonus,
// starting kit, and Core Effect now live in design/systems/cores.json (the single source of truth, Doug
// LOCKED 2026-07-12), loaded by CoreRuneCatalog. Kept as named statics so every existing call site
// (CoreRunes.Grunt, CoreRunes.Roster) compiles unchanged. Do NOT re-inline values here — that's the
// three-way drift (CoreRunes.cs / core-kits.js / CORE_RUNES.md) this file was pulled out of; the JSON
// is authoritative (e.g. Summoner's Blast/Brace/Wooden-Shield/Skeleton-only/slots-4 kit comes straight
// from it, not from any hand-copy).
public static class CoreRunes
{
    public static readonly CoreRune Grunt = CoreRuneCatalog.Default.Cores["grunt"];
    public static readonly CoreRune Warden = CoreRuneCatalog.Default.Cores["warden"];
    public static readonly CoreRune Adept = CoreRuneCatalog.Default.Cores["adept"];
    public static readonly CoreRune Summoner = CoreRuneCatalog.Default.Cores["summoner"];
    public static readonly CoreRune Reaver = CoreRuneCatalog.Default.Cores["reaver"];
    public static readonly CoreRune Ranger = CoreRuneCatalog.Default.Cores["ranger"];
    public static readonly CoreRune Barbarian = CoreRuneCatalog.Default.Cores["barbarian"];

    // Roster order matches design/05's Choose-Your-Core line-up.
    public static readonly IReadOnlyList<CoreRune> Roster =
        new[] { Grunt, Warden, Adept, Summoner, Reaver, Ranger, Barbarian };
}
