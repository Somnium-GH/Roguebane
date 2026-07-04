namespace Roguebane.Core;

// Moves gear between the Stash pack and the Body, honoring the body's own gates (a weapon needs a free
// hand + the stat to lift it; armor is one piece per part-group). Pure coordination — the rules live on
// the Body; this only keeps the pack and the body in sync so a piece is never in both places at once.
public static class Gearing
{
    // Wield a carried weapon. Fails (leaving it in the pack) if it isn't carried or the body can't lift
    // it (no free hand / not enough stat). On success it leaves the pack and goes onto the body.
    public static bool EquipWeapon(Stash pack, Body body, Weapon weapon)
    {
        if (!pack.HasWeapon(weapon)) return false;
        if (!body.Wield(weapon)) return false; // gated by hands + stat capacity
        pack.RemoveWeapon(weapon);
        return true;
    }

    // Return a wielded weapon to the pack.
    public static bool UnequipWeapon(Stash pack, Body body, Weapon weapon)
    {
        if (!body.Hands.Contains(weapon)) return false;
        body.Unwield(weapon);
        pack.AddWeapon(weapon);
        return true;
    }

    // Wear a carried armor piece — gated by the body's own §6c requirement check. One piece per
    // slot: any piece it displaces returns to the pack.
    public static bool EquipArmor(Stash pack, Body body, Armor piece)
    {
        if (!pack.HasArmor(piece)) return false;
        var displaced = body.ArmorOn(piece.Slot);
        if (!body.Equip(piece)) return false; // requirement unmet -> stays in the pack
        pack.RemoveArmor(piece);
        if (displaced is not null) pack.AddArmor(displaced);
        return true;
    }

    // Take the worn piece off a part-group and return it to the pack.
    public static bool UnequipArmor(Stash pack, Body body, Stat group)
    {
        var worn = body.ArmorOn(group);
        if (worn is null) return false;
        body.Unequip(group);
        pack.AddArmor(worn);
        return true;
    }
}
