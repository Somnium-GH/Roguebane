using System.Text.Json;
using System.Text.Json.Serialization;
using Roguebane.Core.Content;

namespace Roguebane.Core;

// The single source of truth for races + core-kit assembly: design/systems/cores.json (Doug LOCKED,
// 2026-07-12), ending the three-way drift between CoreRunes.cs / core-kits.js / CORE_RUNES.md. The JSON
// carries budget/actions/minionCap/statBonus/CoreEffect/starting-kit; the MECHANICAL catalogs
// (Techniques/Armory/ArmorLines/Minions) stay in C#, and the kit id strings resolve against them here.
// Any unresolvable id THROWS loudly — a typo can never silently drop a kit item from a build.
// Content.Races / Content.CoreRunes are thin wrappers over `Default`.
public sealed class CoreRuneCatalog
{
    public IReadOnlyDictionary<string, Race> Races { get; }
    public IReadOnlyDictionary<string, CoreRune> Cores { get; }

    private CoreRuneCatalog(Dictionary<string, Race> races, Dictionary<string, CoreRune> cores)
    {
        Races = races;
        Cores = cores;
    }

    // cores.json is embedded as a compiled resource (see Roguebane.Core.csproj) — no runtime file-path
    // fragility, still swappable in dev via Load(path).
    public static CoreRuneCatalog Default { get; } = LoadEmbedded();

    public static CoreRuneCatalog LoadEmbedded()
    {
        var asm = typeof(CoreRuneCatalog).Assembly;
        var name = asm.GetManifestResourceNames()
            .Single(n => n.EndsWith("cores.json", StringComparison.Ordinal));
        using var stream = asm.GetManifestResourceStream(name)
            ?? throw new InvalidDataException("cores.json resource missing from Roguebane.Core");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    public static CoreRuneCatalog Load(string path) => Parse(File.ReadAllText(path));

    public static CoreRuneCatalog Parse(string json)
    {
        var dto = JsonSerializer.Deserialize<CatalogDto>(json, JsonOpts)
            ?? throw new InvalidDataException("cores.json: empty or unparseable");

        // Id -> object lookups. Techniques resolve against the FULL superset (not the curated `.All`
        // default palette) plus the combat verbs that live in Armory — that covers every technique id
        // any kit references.
        var techs = Techniques.Full
            .Concat(new[] { Armory.Swing, Armory.Frenzy, Armory.Flurry, Armory.Shot, Armory.AimedShot })
            .ToDictionary(t => t.Id);
        var weapons = Armory.AllWeapons.ToDictionary(w => w.Id);
        var armor = ArmorLines.All.ToDictionary(a => a.Id);
        var minions = Minions.All.ToDictionary(m => m.Id);

        var races = dto.Races.ToDictionary(kv => kv.Key, kv =>
        {
            var r = kv.Value;
            return new Race(kv.Key, r.Str, r.Int, r.Dex, r.Con, r.Hp, r.Title ?? "", r.Tag ?? "", r.Blurb ?? "");
        });

        var cores = dto.Cores.ToDictionary(kv => kv.Key, kv =>
        {
            var c = kv.Value;
            var b = c.StatBonus ?? new StatBonusDto();
            var kit = c.Kit ?? new KitDto();
            return new CoreRune(
                kv.Key,
                RuneBudget: c.Budget,
                MinionCap: c.MinionCap,
                ActionSlots: c.ActionSlots,
                StrBonus: b.Str, IntBonus: b.Int, DexBonus: b.Dex, ConBonus: b.Con,
                DefaultEquipment: Resolve(kit.Techniques, techs, kv.Key, "techniques"),
                DefaultMinions: Resolve(kit.Minions, minions, kv.Key, "minions"),
                DefaultWeapons: Resolve(kit.Weapons, weapons, kv.Key, "weapons"),
                DefaultArmor: Resolve(kit.Armor, armor, kv.Key, "armor"),
                Archetype: c.Archetype ?? "",
                Flavor: c.Flavor ?? "",
                CoreEffectName: c.EffectName ?? "",
                CoreEffectDesc: c.EffectDesc ?? "",
                Effect: Enum.Parse<CoreEffectKind>(c.Effect),
                CoreEffectFreeSummons: c.EffectFreeSummons,
                Accent: c.Accent ?? "",
                Badge: c.Badge ?? "");
        });

        return new CoreRuneCatalog(races, cores);
    }

    private static T[] Resolve<T>(string[]? ids, IReadOnlyDictionary<string, T> map, string core, string kind) =>
        (ids ?? Array.Empty<string>()).Select(id =>
            map.TryGetValue(id, out var v) ? v
            : throw new InvalidDataException($"cores.json core '{core}' kit.{kind}: unknown id '{id}'")).ToArray();

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // DTOs mirror the JSON shape. Plain classes with settable properties + explicit JsonPropertyName —
    // NOT positional records, whose constructor-parameter name binding is the STJ quirk that silently
    // left these null. String fields stay nullable so a partial entry fails at id-resolution with a
    // clear message rather than a null-ref.
    private sealed class CatalogDto
    {
        [JsonPropertyName("races")] public Dictionary<string, RaceDto> Races { get; set; } = new();
        [JsonPropertyName("cores")] public Dictionary<string, CoreDto> Cores { get; set; } = new();
    }
    private sealed class RaceDto
    {
        [JsonPropertyName("str")] public int Str { get; set; }
        [JsonPropertyName("int")] public int Int { get; set; }
        [JsonPropertyName("dex")] public int Dex { get; set; }
        [JsonPropertyName("con")] public int Con { get; set; }
        [JsonPropertyName("hp")] public int Hp { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("tag")] public string? Tag { get; set; }
        [JsonPropertyName("blurb")] public string? Blurb { get; set; }
    }
    private sealed class StatBonusDto
    {
        [JsonPropertyName("str")] public int Str { get; set; }
        [JsonPropertyName("int")] public int Int { get; set; }
        [JsonPropertyName("dex")] public int Dex { get; set; }
        [JsonPropertyName("con")] public int Con { get; set; }
    }
    private sealed class KitDto
    {
        [JsonPropertyName("techniques")] public string[]? Techniques { get; set; }
        [JsonPropertyName("weapons")] public string[]? Weapons { get; set; }
        [JsonPropertyName("armor")] public string[]? Armor { get; set; }
        [JsonPropertyName("minions")] public string[]? Minions { get; set; }
    }
    private sealed class CoreDto
    {
        [JsonPropertyName("budget")] public int Budget { get; set; }
        [JsonPropertyName("actionSlots")] public int ActionSlots { get; set; }
        [JsonPropertyName("minionCap")] public int MinionCap { get; set; }
        [JsonPropertyName("statBonus")] public StatBonusDto? StatBonus { get; set; }
        [JsonPropertyName("effect")] public string Effect { get; set; } = "";
        [JsonPropertyName("effectName")] public string? EffectName { get; set; }
        [JsonPropertyName("effectDesc")] public string? EffectDesc { get; set; }
        [JsonPropertyName("effectFreeSummons")] public bool EffectFreeSummons { get; set; }
        [JsonPropertyName("archetype")] public string? Archetype { get; set; }
        [JsonPropertyName("flavor")] public string? Flavor { get; set; }
        [JsonPropertyName("badge")] public string? Badge { get; set; }
        [JsonPropertyName("accent")] public string? Accent { get; set; }
        [JsonPropertyName("kit")] public KitDto? Kit { get; set; }
    }
}
