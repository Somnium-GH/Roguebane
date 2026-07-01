namespace Roguebane.Core;

// End-to-end assembly, one composable flow: socket a chassis, fold in whatever its allocated runes
// grant, and compose the minted body with a technique equipment and a run into a ready-to-fight
// Session. The pick-chassis -> allocate-runes -> build-body -> run -> siege path lives here.
public static class Forge
{
    public static Session Assemble(
        Chassis chassis,
        RuneLoadout runes,
        IReadOnlyList<Technique> equipment,
        Run run)
    {
        var body = chassis.NewBody(runes);
        // Session is the legacy linear-run + balance-sim path (unattended): keep default-front auto-fire.
        // The interactive targeting FSM lives on the Expedition/Campaign mints below (requireAim).
        var caster = new Caster(body, run.Current.CurrentTarget, MagicCapacity(body));
        return new Session(PlayerFighter(body), caster, WithRuneGrants(equipment, runes), run);
    }

    // The same mint, dropped into the real map+combat loop instead of a linear run.
    public static Expedition Embark(
        Chassis chassis,
        RuneLoadout runes,
        IReadOnlyList<Technique> equipment,
        CityMap map)
    {
        var body = chassis.NewBody(runes);
        var caster = new Caster(body, maxCharge: MagicCapacity(body), requireAim: true, bayCap: chassis.Bays);
        SummonKit(caster, chassis, runes);
        return new Expedition(PlayerFighter(body), caster, WithRuneGrants(equipment, runes), map,
            figureId: chassis.Id);
    }

    // The same mint, marching a multi-leg campaign to the Capital instead of one leg.
    public static Campaign EmbarkCampaign(
        Chassis chassis,
        RuneLoadout runes,
        IReadOnlyList<Technique> equipment,
        IReadOnlyList<Func<CityMap>> legs)
    {
        var body = chassis.NewBody(runes);
        var caster = new Caster(body, maxCharge: MagicCapacity(body), requireAim: true, bayCap: chassis.Bays);
        SummonKit(caster, chassis, runes);
        return new Campaign(PlayerFighter(body), caster, WithRuneGrants(equipment, runes), legs,
            figureId: chassis.Id);
    }

    // Field the chassis's minion kit plus any rune-granted minions into its bays at assembly, so the
    // summoner archetype actually fights through its summons in a real run (capped by Bays; each pays
    // its own gate — INT reservation, charge, or free). Summon is idempotent, so a duplicate id no-ops.
    private static void SummonKit(Caster caster, Chassis chassis, RuneLoadout runes)
    {
        foreach (var minion in chassis.MinionKit.Concat(runes.GrantedMinions))
            caster.Summon(minion, chassis.Bays);
    }

    // Rune-granted techniques join the equipment (deduped) — a held keystone hands you a verb your
    // chassis never had.
    private static IReadOnlyList<Technique> WithRuneGrants(
        IReadOnlyList<Technique> equipment, RuneLoadout runes) =>
        equipment.Concat(runes.GrantedTechniques).GroupBy(t => t.Id).Select(g => g.First()).ToList();

    // The player's HP life total: a natural base plus a CON bonus (1 CON = 2 HP). Smashing the chest
    // drops CON, so MaxHp shrinks and current HP caps down — the locked CON->HP model.
    public static Fighter PlayerFighter(Body body) => Fighter.Scaled(body, baseHp: 8);

    // The magic resource pool scales with INT — the head funds spellcraft. (Name/tuning deferred.)
    public static int MagicCapacity(Body body) => body.Capacity(Stat.Int);
}
