using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// The combat targeting FSM, driven headless (the shell only feeds it raw presses). Asserts the input
// -> state spec: powering doesn't target; pressing a powered card enters targeting + clears its aim;
// a foe press aims (a limb press part-aims) and exits; right-press unpowers/cancels; AUTO is global.
public class CombatTargetingTests
{
    private static (Expedition, CombatTargeting) Fighting()
    {
        var exp = Sessions.Expedition();
        exp.Enter("a2"); // a skirmish so there are foes to aim at
        return (exp, new CombatTargeting());
    }

    private static Technique Card0(Expedition e) => e.Equipment[0];

    [Fact]
    public void PoweringDoesNotEnterTargeting()
    {
        var (exp, ctrl) = Fighting();
        ctrl.CardPress(exp, 0);                 // inactive -> POWER only
        Assert.True(exp.IsActive(Card0(exp)));
        Assert.Equal(-1, ctrl.Targeting);       // powering never targets
        Assert.Null(exp.AimOf(Card0(exp)));
    }

    [Fact]
    public void PressingAPoweredCardEntersTargetingAndClearsItsAim()
    {
        var (exp, ctrl) = Fighting();
        ctrl.CardPress(exp, 0);                 // power
        exp.Aim(Card0(exp), exp.Enemy!);       // give it a target
        Assert.NotNull(exp.AimOf(Card0(exp)));

        ctrl.CardPress(exp, 0);                 // active -> enter TARGETING
        Assert.Equal(0, ctrl.Targeting);
        Assert.True(ctrl.IsTargeting(exp));
        Assert.Null(exp.AimOf(Card0(exp)));     // entering targeting cleared the target
    }

    [Fact]
    public void FoePressAimsTheTargetingModuleAndExits()
    {
        var (exp, ctrl) = Fighting();
        ctrl.CardPress(exp, 0);
        ctrl.CardPress(exp, 0);                 // enter targeting
        ctrl.FoePress(exp, exp.Enemy!);

        Assert.Same(exp.Enemy!, exp.AimOf(Card0(exp)));
        Assert.Equal(-1, ctrl.Targeting);       // aimed -> exit targeting
    }

    [Fact]
    public void FoePressWithALimbPartAimsThatLimb()
    {
        var (exp, ctrl) = Fighting();
        var head = exp.Enemy!.Frame!.Parts.First(p => p.Stat == Stat.Int);
        ctrl.CardPress(exp, 0);
        ctrl.CardPress(exp, 0);
        ctrl.FoePress(exp, exp.Enemy!, head);

        Assert.Same(head, exp.PartOf(Card0(exp)));
    }

    [Fact]
    public void FoePressDoesNothingWhenNotTargeting()
    {
        var (exp, ctrl) = Fighting();
        ctrl.CardPress(exp, 0);                 // powered but NOT targeting
        ctrl.FoePress(exp, exp.Enemy!);
        Assert.Null(exp.AimOf(Card0(exp)));     // ignored
    }

    [Fact]
    public void CardRightPressUnpowersAndLeavesTargeting()
    {
        var (exp, ctrl) = Fighting();
        ctrl.CardPress(exp, 0);
        ctrl.CardPress(exp, 0);                 // targeting card 0
        Assert.Equal(0, ctrl.Targeting);

        ctrl.CardRightPress(exp, 0);            // unpower
        Assert.False(exp.IsActive(Card0(exp)));
        Assert.Equal(-1, ctrl.Targeting);
    }

    [Fact]
    public void CancelTargetingLeavesTheTargetCleared()
    {
        var (exp, ctrl) = Fighting();
        ctrl.CardPress(exp, 0);
        exp.Aim(Card0(exp), exp.Enemy!);
        ctrl.CardPress(exp, 0);                 // enter targeting (clears aim)
        ctrl.CancelTargeting();

        Assert.Equal(-1, ctrl.Targeting);
        Assert.True(exp.IsActive(Card0(exp)));  // still powered
        Assert.Null(exp.AimOf(Card0(exp)));     // target stays cleared (no restore)
    }

    [Fact]
    public void ToggleAutoFlipsTheGlobalSwitch()
    {
        var (exp, ctrl) = Fighting();
        Assert.False(exp.IsAuto());
        ctrl.ToggleAuto(exp);
        Assert.True(exp.IsAuto());
        ctrl.ToggleAuto(exp);
        Assert.False(exp.IsAuto());
    }

    [Fact]
    public void SyncDropsTargetingWhenTheModuleIsGone()
    {
        var (exp, ctrl) = Fighting();
        ctrl.CardPress(exp, 0);
        ctrl.CardPress(exp, 0);                 // targeting card 0
        exp.Toggle(Card0(exp));                 // deactivated by some other path
        ctrl.Sync(exp);
        Assert.Equal(-1, ctrl.Targeting);
    }

    // §8 target-side rules: SELF techniques and PASSIVE shield sources never enter the targeting FSM.
    private static int IndexOf(Expedition e, Technique t)
    {
        for (var i = 0; i < e.Equipment.Count; i++) if (e.Equipment[i].Id == t.Id) return i;
        return -1;
    }

    // CORRECTED RULE (STATUS.md 2026-07-12, Doug): left-click NEVER disables anything — only right-click
    // does, uniformly across every technique kind. So a second LEFT-press on an active Self technique is
    // a no-op (stays active, never enters the FSM); RIGHT-press is the sole deactivate path.
    [Fact]
    public void LeftPressOnActiveSelfTechniqueIsANoOp_RightPressDeactivates()
    {
        var (exp, ctrl) = Fighting();
        var ix = IndexOf(exp, Techniques.Bandage);
        Assert.True(ix >= 0); // the seeded kit fields the heal
        Assert.Equal(TargetSide.Self, exp.Equipment[ix].Side);
        ctrl.CardPress(exp, ix);                // power ON (left-click powers, never disables)
        Assert.True(exp.IsActive(exp.Equipment[ix]));
        ctrl.CardPress(exp, ix);                // active self-tech -> NO-OP: stays active, never the FSM
        Assert.True(exp.IsActive(exp.Equipment[ix]));
        Assert.Equal(-1, ctrl.Targeting);
        ctrl.CardRightPress(exp, ix);           // right-click is the ONLY disable path
        Assert.False(exp.IsActive(exp.Equipment[ix]));
    }

    // CORRECTED RULE (STATUS.md 2026-07-12, Doug): the pre-existing passive left-click-toggle-off was
    // ALSO wrong — left-click never disables. A left-press on an active passive shield is a no-op; only
    // right-click frees the stat. (A left-press on an INACTIVE card still powers it ON — that's not a disable.)
    [Fact]
    public void LeftPressOnActiveShieldSourceIsANoOp_RightPressDeactivates()
    {
        var (exp, ctrl) = Fighting();
        var ix = IndexOf(exp, Techniques.Brace);
        Assert.True(ix >= 0);
        Assert.True(Techniques.Brace.IsPassive);
        // Activation default refinement [LOCKED 2026-07-09]: Sustained techniques auto-power on
        // encounter entry, so Brace is already active by the time Fighting() returns.
        Assert.True(exp.IsActive(exp.Equipment[ix]));
        ctrl.CardPress(exp, ix);                // active passive -> NO-OP: stays active, never the FSM
        Assert.True(exp.IsActive(exp.Equipment[ix]));
        Assert.Equal(-1, ctrl.Targeting);
        ctrl.CardRightPress(exp, ix);           // right-click is the ONLY disable path
        Assert.False(exp.IsActive(exp.Equipment[ix]));
        ctrl.CardPress(exp, ix);                // now inactive -> left-click powers back ON (never the FSM)
        Assert.True(exp.IsActive(exp.Equipment[ix]));
        Assert.Equal(-1, ctrl.Targeting);
    }
}
