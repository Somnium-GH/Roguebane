namespace Roguebane.Core.Content;

// Weapons + the verbs that consult them. Weapons are stat-sticks (zero abilities); a consulting
// technique sources its cost and power from whatever is wielded. §6d ROSTER (locked 2026-07-03
// naming session; damage/req/timer = blessed initial): melee rides ONE material ladder
// (Iron → Steel → Mithral → Dwarven Steel); ranged + INT implements carry their own tier names.
// Bow/sling damage stays PLACEHOLDER-FLAGGED (§17 #9 — never dictated). The CON shield-object
// ladder's equip-gate resolved 2026-07-04 (§6c): 1 CON/tier — gating brace's shield-source off an
// equipped shield is a separate follow-up slice, not built here.
public static class Armory
{
    private static readonly string[] Materials = { "Iron", "Steel", "Mithral", "Dwarven Steel" };
    private static readonly string[] MaterialIds = { "iron", "steel", "mithral", "dwarven" };

    // A melee material ladder: 4 tiers of one silhouette. Ids follow the CD gear-catalog sprite
    // convention ({noun}_{material}) so sprites/gear/{w.Id} resolves with zero mapping.
    private static Weapon[] Melee(string noun, Stat stat, double timer, int dmgPerTier,
        int reqPerTier, int hands)
        => Enumerable.Range(1, 4).Select(t => new Weapon(
            Slug(noun) + "_" + MaterialIds[t - 1],
            stat, reqPerTier * t, dmgPerTier * t,
            Materials[t - 1] + " " + noun, t, timer, hands)).ToArray();

    // A named ladder (INT implements): explicit tier names, uniform per-tier gates. Ids follow the
    // CD gear-catalog sprite convention — "{slug}_{tier adjective, spaces squeezed}" (wand_adept,
    // staff_wooden, tome_oldworn).
    private static Weapon[] Named(string slug, Stat stat, WeaponKind kind, double timer,
        int dmgPerTier, int reqPerTier, int hands, params string[] names)
        => names.Select((n, i) => new Weapon(
            slug + "_" + n[..n.LastIndexOf(' ')].ToLowerInvariant().Replace(" ", "").Replace("'", ""),
            stat, reqPerTier * (i + 1), dmgPerTier * (i + 1),
            n, i + 1, timer, hands, kind)).ToArray();

    private static string Slug(string noun) => noun.ToLowerInvariant().Replace(" ", "");

    // STR melee: 1H Longsword/Axe/Mace, 2H Claymore/Battleaxe/Warhammer (§6d table).
    public static readonly IReadOnlyList<Weapon> Longswords = Melee("Longsword", Stat.Str, 1.0, 4, 2, 1);
    public static readonly IReadOnlyList<Weapon> Axes = Melee("Axe", Stat.Str, 0.9, 3, 1, 1);
    public static readonly IReadOnlyList<Weapon> Maces = Melee("Mace", Stat.Str, 1.1, 5, 3, 1);
    public static readonly IReadOnlyList<Weapon> Claymores = Melee("Claymore", Stat.Str, 1.3, 7, 5, 2);
    public static readonly IReadOnlyList<Weapon> Battleaxes = Melee("Battleaxe", Stat.Str, 1.2, 6, 4, 2);
    public static readonly IReadOnlyList<Weapon> Warhammers = Melee("Warhammer", Stat.Str, 1.4, 8, 5, 2);

    // DEX melee: 1H only — the bow is DEX's two-hander (§6d).
    public static readonly IReadOnlyList<Weapon> Daggers = Melee("Dagger", Stat.Dex, 0.6, 1, 1, 1);
    public static readonly IReadOnlyList<Weapon> Rapiers = Melee("Rapier", Stat.Dex, 0.7, 2, 2, 1);
    public static readonly IReadOnlyList<Weapon> ShortSwords = Melee("Short Sword", Stat.Dex, 0.8, 3, 3, 1);

    // RANGED slot. Bow: full shield bypass + Charge (§10); damage/tier OPEN §17 #9 — the flat
    // placeholder 2 stays FLAGGED. Sling: 1H, shield-compatible, same bypass+Charge, weaker —
    // damage/tier equally OPEN, flat placeholder 1 FLAGGED.
    public static readonly IReadOnlyList<Weapon> Bows = Enumerable.Range(1, 4).Select(t => new Weapon(
        t == 1 ? "bow" : "bow-" + t, Stat.Dex, 2 * t, Power: 2 /* PLACEHOLDER §17 #9 */,
        new[] { "Short Bow", "Long Bow", "Compound Bow", "Elven Bow" }[t - 1],
        t, 1.0, Hands: 2, WeaponKind.Bow)).ToArray();
    public static readonly IReadOnlyList<Weapon> Slings = Enumerable.Range(1, 4).Select(t => new Weapon(
        "sling_" + new[] { "shepherds", "braided", "sinew", "giantsbane" }[t - 1],
        Stat.Dex, t, Power: 1 /* PLACEHOLDER §17 #9 */,
        new[] { "Shepherd's Sling", "Braided Sling", "Sinew Sling", "Giantsbane Sling" }[t - 1],
        t, 1.0, Hands: 1, WeaponKind.Sling)).ToArray();

    // INT implements (§6d): wand = 1H shield-SUBTRACTION hand item (resolution is its own slice);
    // staff = 2H plain blockable melee, 2 INT/t per WEAPONS.md's table; charm/tome = pure-bonus
    // offhands (+1 FLAT minion/spell damage per tier — WEAPONS.md rescale 2026-07-12; staff's own
    // +1/tier SPELL bonus is doc-canon but not yet wired), Power 0.
    // Wand/staff Timer (1.0 here) is WEAPONS.md's own Open/TBD ("wand / staff timer multipliers") —
    // placeholder, not a locked number.
    // Req corrected 2026-07-05 (WEAPONS.md): 2->1 INT/tier — the old value put Summoner's kit one
    // INT over the model's demand (8).
    public static readonly IReadOnlyList<Weapon> Wands = Named("wand", Stat.Int, WeaponKind.Wand,
        1.0, 2, 1, 1, "Adept Wand", "Twisted Wand", "Gemstone Wand", "Glowing Wand");
    public static readonly IReadOnlyList<Weapon> Staffs = Named("staff", Stat.Int, WeaponKind.Staff,
        1.0, 2, 2, 2, "Wooden Staff", "Twisted Staff", "Ornate Staff", "Humming Staff");
    public static readonly IReadOnlyList<Weapon> Charms = Named("charm", Stat.Int, WeaponKind.Charm,
        1.0, 0, 1, 1, "Wooden Charm", "Bone Charm", "Ornate Charm", "Humming Charm");
    public static readonly IReadOnlyList<Weapon> Tomes = Named("tome", Stat.Int, WeaponKind.Tome,
        1.0, 0, 1, 1, "Old Worn Tome", "Leather Tome", "Ornate Tome", "Glowing Tome");

    // CON shield OBJECT (§6c): 1H off-hand item, Power 0 (blocks nothing on its own — the shield
    // pool it feeds is a technique's job, §6b), gate 1 CON/tier (resolved 2026-07-04).
    public static readonly IReadOnlyList<Weapon> Shields = Named("shield", Stat.Con, WeaponKind.Shield,
        1.0, 0, 1, 1, "Wooden Shield", "Iron Buckler", "Kite Shield", "Tower Shield");

    public static readonly IReadOnlyList<IReadOnlyList<Weapon>> Ladders = new[]
    {
        Longswords, Axes, Maces, Claymores, Battleaxes, Warhammers,
        Daggers, Rapiers, ShortSwords, Bows, Slings, Wands, Staffs, Charms, Tomes, Shields,
    };

    public static IEnumerable<Weapon> AllWeapons => Ladders.SelectMany(l => l);

    // Legacy handles: the tier-1 pieces existing kits/stock/tests reference by field.
    public static readonly Weapon Sword = Longswords[0];
    public static readonly Weapon Axe = Axes[0];
    public static readonly Weapon Dagger = Daggers[0];
    public static readonly Weapon Bow = Bows[0];

    // Cooldowns are in COMBAT TICKS at the fixed 10 ticks/sec clock; 80 = the canon's base-speed
    // anchor (8.0s, CORE_RUNES.md/TECHNIQUES.md) for every speed x1.0 verb below.
    // Swing consults STR only (its own weapon ladders are all melee STR bar the Reaver's daggers,
    // which use Frenzy/Flurry instead). Frenzy/Flurry are stat-flexible (LOCKED 2026-07-05,
    // TECHNIQUES.md/CORE_RUNES.md): AltStat: Stat.Dex lets Reaver's twin daggers consult them too —
    // the earlier frenzy_dex/flurry_dex clone-pair plan is RETIRED in favor of this single verb.
    // Reserve on a Consults!=None technique is inert (Caster.Reservation zeroes it) — the WEAPON's own
    // reserve is what's tracked; kept at the TECHNIQUES.md-quoted value for future §11 Finesse display.
    public static readonly Technique Swing =
        new("swing", Stat.Str, Reserve: 0, TechniqueKind.Timered, Cooldown: 80, Power: 0, Consults: WeaponUse.Primary,
            Desc: "A basic strike with your main-hand weapon.");

    public static readonly Technique Frenzy =
        new("frenzy", Stat.Str, Reserve: 3, TechniqueKind.Timered, Cooldown: 80, Power: 0, DamageMult: 1.0,
            Consults: WeaponUse.Both, AltStat: Stat.Dex,
            Desc: "A dual-wield assault with both blades at once. Needs two weapons; paid in STR or DEX by what you wield.");

    public static readonly Technique Flurry =
        new("flurry", Stat.Str, Reserve: 2, TechniqueKind.Timered, Cooldown: 40, Power: 0, DamageMult: 0.5,
            Consults: WeaponUse.Both, AltStat: Stat.Dex,
            Desc: "A fast dual-wield flurry, both blades at half power. Needs two weapons; paid in STR or DEX.");

    // Shot looses the primary DEX weapon (the BOW): it BYPASSES the shield pool and spends 1 Charge per
    // loose (§6b Charge = the shield-pierce resource); dry => it holds. Power/cost come from the bow.
    // Kept as legacy content (superseded by AimedShot below in every v6 kit) — inert but harmless.
    public static readonly Technique Shot =
        new("shot", Stat.Dex, Reserve: 0, TechniqueKind.Timered, Cooldown: 80, Power: 0,
            Consults: WeaponUse.Primary, ChargeCost: 1, ShieldPiercing: true,
            Desc: "Loose your bow: bypasses the foe's shield and spends 1 charge. Dry, it holds.");

    // The Marksman's real bow verb (CORE_RUNES.md/TECHNIQUES.md): 2.0x the bow's power, slow (160
    // ticks = 16s), 1 Reserve, DEX-gated, spends+bypasses on Charge same as Shot.
    public static readonly Technique AimedShot =
        new("aimed_shot", Stat.Dex, Reserve: 2, TechniqueKind.Timered, Cooldown: 160, Power: 0, DamageMult: 2.0,
            Consults: WeaponUse.Primary, ChargeCost: 1, ShieldPiercing: true,
            Desc: "A slow, heavy bow shot for double power; bypasses shields and spends 1 charge.");

    public static readonly IReadOnlyList<Weapon> All = new[] { Sword, Axe, Dagger, Bow };
}
