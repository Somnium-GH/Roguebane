namespace Roguebane.Core;

// An encounter target. HP is permanent within an encounter (the boss rule) — a foe only loses HP
// to incoming Power and recovers only by its own means. Multi-part foes that fight back are later
// work; for now a foe is a single HP pool.
public sealed class Foe
{
    public string Id { get; }
    public int MaxHp { get; }
    public int Hp { get; private set; }

    public Foe(string id, int hp)
    {
        Id = id;
        MaxHp = hp;
        Hp = hp;
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
