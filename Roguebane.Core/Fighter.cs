namespace Roguebane.Core;

// The player as a combat target. The player IS the socketed Body; on top of the part/stat layer
// sits a small HP life total (CON-scaled at mint; the lower-max-vs-pool question is parked). Taking
// a part hit erodes that part's stat through the Body — so the SAME cascade that sheds the player's
// gear when an arm is smashed runs against incoming damage — and overkill spills into HP. HP is
// only restored out of combat (shop / non-skirmish), never mid-fight.
public sealed class Fighter : ICombatTarget
{
    public Body Body { get; }
    public int MaxHp { get; }
    public int Hp { get; private set; }

    public Fighter(Body body, int maxHp)
    {
        if (maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(maxHp));
        Body = body;
        MaxHp = maxHp;
        Hp = maxHp;
    }

    public bool Down => Hp <= 0;
    public Body? Frame => Body;

    public void Damage(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Hp = Math.Max(0, Hp - amount);
    }

    public void DamagePart(BodyPart part, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var absorbed = Math.Min(Body.Contribution(part), amount);
        if (absorbed > 0) Body.Damage(part, absorbed); // routes through the reservation cascade
        var overkill = amount - absorbed;
        if (overkill > 0) Damage(overkill);
    }

    // Out-of-combat recovery only (§10): shop services or non-skirmish encounters call this.
    public void Heal(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Hp = Math.Min(MaxHp, Hp + amount);
    }
}
