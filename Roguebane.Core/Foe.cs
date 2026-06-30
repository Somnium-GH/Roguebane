namespace Roguebane.Core;

// An encounter target. HP is a permanent life total within an encounter (the boss rule) — a foe
// only loses HP to incoming Power and recovers only by its own means.
//
// A foe is also a STRUCTURED thing with targetable parts (one combat grammar everywhere): an
// optional Frame carries the same Body-of-parts the player has. A part-aimed hit erodes that
// part's stat first (localized degradation) and only spills into HP once the part bottoms out
// (the §10 split: stat damage to the targeted part; HP from overkill). A foe with no Frame is an
// unstructured HP pool (control-point fodder).
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

    public Foe(string id, int hp, Body? frame = null, IReadOnlyList<Technique>? arsenal = null,
        string figure = "ogre")
    {
        Id = id;
        MaxHp = hp;
        Hp = hp;
        Frame = frame;
        Arsenal = arsenal ?? Array.Empty<Technique>();
        Figure = figure;
    }

    public bool Down => Hp <= 0;

    public void Damage(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Hp = Math.Max(0, Hp - amount);
    }

    // Localized damage: erode the targeted part's stat first; overkill (or any hit once the part
    // is already gone) spills into the HP life total.
    public void DamagePart(BodyPart part, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Frame is null) { Damage(amount); return; }

        var overkill = Frame.AbsorbPartHit(part, amount); // armor blunts, the part erodes
        if (overkill > 0) Damage(overkill);
    }

    public void Restore(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Hp = Math.Min(MaxHp, Hp + amount);
    }
}
