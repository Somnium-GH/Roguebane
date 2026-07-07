namespace Roguebane.Core;

// An encounter target. HP is a permanent life total within an encounter (the boss rule) — a foe
// only loses HP to incoming Power and recovers only by its own means.
//
// A foe is also a STRUCTURED thing with targetable parts (one combat grammar everywhere): an
// optional Frame carries the same Body-of-parts the player has. §8: a part-aimed hit erodes that
// part's stat AND takes HP simultaneously (same power, no part-vs-HP split, no overkill path). A foe
// with no Frame is an unstructured HP pool (control-point fodder).
public sealed class Foe : ICombatTarget
{
    public string Id { get; }
    public int MaxHp { get; }
    public int Hp { get; private set; }
    public Body? Frame { get; }

    // The foe's offense, powered by its own Frame (a structured foe fights back). Empty = inert
    // target. A foe needs a Frame to power an Arsenal — the same reserve-on-parts rule as the player.
    public IReadOnlyList<Technique> Arsenal { get; }

    // The manifest figure to render this foe with (creature figure key, e.g. "ogre"). Distinct from
    // Id, which is an encounter-role tag ("gate", "ogre-0"). The shell composes the figure from it.
    public string Figure { get; }

    // Which player limb this foe goes for (§8 personality, data). Inert/unaimed foes default RANDOM.
    public FoeAim Aim { get; }

    // The foe's ONE Foe Effect (FOES.md design rule), data-only — Caster.Hit/Discharge is the interpreter.
    public FoeEffectKind Effect { get; }

    // One-shot latch for effects that fire only once per foe (Brittle) — effects that read LIVE part
    // state instead (Insubstantial) never touch this.
    public bool EffectTriggered { get; private set; }
    public void TriggerEffect() => EffectTriggered = true;

    public Foe(string id, int hp, Body? frame = null, IReadOnlyList<Technique>? arsenal = null,
        string figure = "ogre", FoeAim aim = FoeAim.Random, FoeEffectKind effect = FoeEffectKind.None)
    {
        Id = id;
        MaxHp = hp;
        Hp = hp;
        Frame = frame;
        Arsenal = arsenal ?? Array.Empty<Technique>();
        Figure = figure;
        Aim = aim;
        Effect = effect;
    }

    public bool Down => Hp <= 0;

    public void Damage(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Hp = Math.Max(0, Hp - amount);
    }

    public void Restore(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Hp = Math.Min(MaxHp, Hp + amount);
    }
}
