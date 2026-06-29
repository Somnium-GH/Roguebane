namespace Roguebane.Core;

// The persistent run economy — gold and carried consumables. Lives above a single Expedition so it
// carries across the legs of a campaign.
public sealed class Stash
{
    public int Gold { get; private set; }
    public int Potions { get; private set; }

    public Stash(int gold = 0, int potions = 0)
    {
        Gold = gold;
        Potions = potions;
    }

    public void AddGold(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Gold += amount;
    }

    public bool TrySpend(int cost)
    {
        if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
        if (Gold < cost) return false;
        Gold -= cost;
        return true;
    }

    public void AddPotion() => Potions++;

    public bool TryUsePotion()
    {
        if (Potions == 0) return false;
        Potions--;
        return true;
    }
}
