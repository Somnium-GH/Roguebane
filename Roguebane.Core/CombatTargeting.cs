namespace Roguebane.Core;

// The combat targeting/firing FSM, lifted out of the MonoGame shell so it is headless-testable. It
// holds only the transient cursor — which action-bar module is currently PICKING a target — while all
// durable state (what is powered, aimed, AUTO) lives in the Expedition it drives. The render shell
// translates raw mouse/keys into these intents and reads Targeting back to draw the reticle/ring.
//
// The model (see STATUS): a module is powered by a card press; powering does NOT target. Pressing an
// already-powered card enters TARGETING and clears that module's target; in targeting, pressing a foe
// (optionally a limb) aims it and exits; right-press cancels (target stays cleared) or, on a card,
// unpowers it. AUTO is one global toggle. Nothing auto-targets or auto-fires.
public sealed class CombatTargeting
{
    public int Targeting { get; private set; } = -1;

    // A module is actively picking a target only while its card is still powered.
    public bool IsTargeting(Expedition e) =>
        Targeting >= 0 && Targeting < e.Equipment.Count && e.IsActive(e.Equipment[Targeting]);

    // Left-press a card: POWER an inactive module, or (on an active one) enter TARGETING and clear its
    // target. §8 target-side rules: neither a PASSIVE shield source nor a SELF technique (its aim
    // auto-picks on the caster — nothing to aim) enters the targeting FSM; pressing either one's active
    // card toggles it OFF, so a player can free the stat on a second left-press exactly the way every
    // other card responds (§6b). Only foe-targeted techniques enter TARGETING.
    public void CardPress(Expedition e, int i)
    {
        if (i < 0 || i >= e.Equipment.Count) return;
        var t = e.Equipment[i];
        if (!e.IsActive(t)) { e.Toggle(t); return; }
        if (t.IsPassive || t.Side == TargetSide.Self) { e.Toggle(t); return; }
        Targeting = i;
        e.ClearAim(t);
    }

    // Right-press a card: UNPOWER an active module (drops its target); leave targeting if it was this card.
    public void CardRightPress(Expedition e, int i)
    {
        if (i < 0 || i >= e.Equipment.Count) return;
        var t = e.Equipment[i];
        if (e.IsActive(t)) e.Toggle(t);
        if (Targeting == i) Targeting = -1;
    }

    // Left-press a foe (optionally a limb): aim the targeting module and exit targeting. No-op if not targeting.
    public void FoePress(Expedition e, ICombatTarget foe, BodyPart? part = null)
    {
        if (!IsTargeting(e)) return;
        var t = e.Equipment[Targeting];
        if (part is not null) e.Aim(t, foe, part); else e.Aim(t, foe);
        Targeting = -1;
    }

    // Right-press the battlefield: cancel targeting. The module's target stays cleared (no restore).
    public void CancelTargeting() => Targeting = -1;

    public void ToggleAuto(Expedition e) => e.SetAuto(!e.IsAuto()); // ONE global toggle

    // Keep the cursor valid between frames: a module deactivated elsewhere drops out of targeting.
    public void Sync(Expedition e) { if (!IsTargeting(e)) Targeting = -1; }
}
