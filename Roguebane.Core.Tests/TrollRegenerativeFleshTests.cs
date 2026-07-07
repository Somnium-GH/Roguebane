namespace Roguebane.Core.Tests;

// CHUNK D item 2's fourth Foe Effect (FOES.md, Troll): unlike Insubstantial/Brittle/Stoneform, which
// all key off the DEFENDER (`target` in Hit/Discharge), Regenerative Flesh keys off the ATTACKER's
// (healer's) own identity -- its mend doubles the base part-points. Neither Hit nor Discharge
// previously had any way to know "am I a foe, and what's my effect" -- Caster gained a `foeEffect`
// constructor parameter for this (mirrors the existing player-side `effect` param for CoreEffectKind),
// set by Battle when it builds a foe's offense caster from `foe.Effect`. No separate "chest still
// whole" gate is needed: the mend is CON-reserved (Bandage/Suture), so FOES.md's "break the chest
// first" is already the EXISTING reservation cascade -- breaking a Troll's chest below the mend's
// Reserve silences the technique outright, the same mechanism any other consulted-stat break uses.
// Proven via a bare fixture, not Foes.Troll (that foe's content isn't built yet -- this proof only
// needs the effect's own vocabulary, same precedent as Brittle/Stoneform).
public class TrollRegenerativeFleshTests
{
    private static Body SelfBody(int chestCapacity, out BodyPart arm, out BodyPart chest)
    {
        var body = new Body();
        arm = new BodyPart("arm", Stat.Str, 6);
        chest = new BodyPart("chest", Stat.Con, chestCapacity);
        body.Add(arm);
        body.Add(chest);
        return body;
    }

    private static Technique Bandage(int power) =>
        new("bandage", Stat.Con, 2, TechniqueKind.Timered, Cooldown: 80, Power: power, Heals: true);

    [Fact]
    public void MendDoublesTheBasePartPoints()
    {
        var body = SelfBody(4, out var arm, out _);
        body.Damage(arm, 4); // wound to mend
        var caster = new Caster(body, foeEffect: FoeEffectKind.RegenerativeFlesh);
        var tech = Bandage(power: 1);
        caster.Activate(tech);

        for (var i = 0; i < 80; i++) caster.Step(); // Timered: charges down, auto-discharges on the last tick

        Assert.Equal(6 - 4 + 2, body.Contribution(arm)); // base Power 1 doubled to 2
    }

    [Fact]
    public void ANonTrollFoeNeverDoubles()
    {
        var body = SelfBody(4, out var arm, out _);
        body.Damage(arm, 4);
        var caster = new Caster(body, foeEffect: FoeEffectKind.None);
        var tech = Bandage(power: 1);
        caster.Activate(tech);

        for (var i = 0; i < 80; i++) caster.Step();

        Assert.Equal(6 - 4 + 1, body.Contribution(arm)); // no effect -- base heal only
    }

    [Fact]
    public void BreakingItsChestBelowTheMendsReserveSilencesTheDoubledMendEntirely()
    {
        var body = SelfBody(4, out var arm, out var chest);
        body.Damage(arm, 4);
        body.Damage(chest, 3); // CON capacity drops to 1, below Bandage's Reserve of 2
        var caster = new Caster(body, foeEffect: FoeEffectKind.RegenerativeFlesh);
        var tech = Bandage(power: 1);

        Assert.False(caster.Activate(tech)); // the existing reservation cascade -- not new gate logic
        Assert.Equal(6 - 4, body.Contribution(arm)); // nothing mended -- the mend never fires at all
    }
}
