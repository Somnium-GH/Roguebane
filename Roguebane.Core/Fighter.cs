namespace Roguebane.Core;

// The player as a combat target. The player IS the socketed Body; on top of the part/stat layer
// sits an HP life total. On the CON->HP model the max is a natural BASE plus a CON bonus (1 CON = 2
// HP): chest damage drops CON, so MaxHp shrinks and current HP caps down to it (a full-HP fighter
// taking a chest hit loses the bonus immediately, permanently — a chest repair never refunds it). §8:
// every hit erodes the aimed part's stat AND takes HP simultaneously — the SAME cascade that sheds gear
// runs against the part damage; there is no overkill spill. HP is only restored out of combat.
public sealed class Fighter : ICombatTarget
{
    private const int ConToHp = 2; // bonus HP per point of CON (may vary by chassis later)

    private readonly int _base;
    private readonly bool _conScaled;
    private int _hp;

    public Body Body { get; }

    // Fixed-max fighter (foes/tests): MaxHp is exactly the value given, no CON scaling.
    public Fighter(Body body, int maxHp)
    {
        if (maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(maxHp));
        Body = body;
        _base = maxHp;
        _conScaled = false;
        _hp = MaxHp;
    }

    private Fighter(Body body, int baseHp, bool conScaled)
    {
        Body = body;
        _base = baseHp;
        _conScaled = conScaled;
        _hp = MaxHp;
    }

    // The player: a natural base plus a CON-scaled bonus that shrinks as the chest is smashed.
    public static Fighter Scaled(Body body, int baseHp) => new(body, baseHp, conScaled: true);

    public int MaxHp => _conScaled ? _base + ConToHp * Body.Capacity(Stat.Con) : _base;
    public int Hp => Math.Min(_hp, MaxHp);

    public bool Down => Hp <= 0;
    public Body? Frame => Body;

    public void Damage(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        _hp = Math.Max(0, _hp - amount);
    }


    // Persist the cap so a later CON repair can't refund HP the fighter no longer had. Battle calls
    // this each tick after damage resolves.
    public void CapToMax() => _hp = Math.Min(_hp, MaxHp);

    // Out-of-combat recovery only (§10): shop services or non-skirmish encounters call this.
    public void Heal(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        _hp = Math.Min(MaxHp, _hp + amount);
    }
}
