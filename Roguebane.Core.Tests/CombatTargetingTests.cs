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
        exp.Aim(Card0(exp), exp.Foes[0]);       // give it a target
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
        ctrl.FoePress(exp, exp.Foes[0]);

        Assert.Same(exp.Foes[0], exp.AimOf(Card0(exp)));
        Assert.Equal(-1, ctrl.Targeting);       // aimed -> exit targeting
    }

    [Fact]
    public void FoePressWithALimbPartAimsThatLimb()
    {
        var (exp, ctrl) = Fighting();
        var head = exp.Foes[0].Frame!.Parts.First(p => p.Stat == Stat.Int);
        ctrl.CardPress(exp, 0);
        ctrl.CardPress(exp, 0);
        ctrl.FoePress(exp, exp.Foes[0], head);

        Assert.Same(head, exp.PartOf(Card0(exp)));
    }

    [Fact]
    public void FoePressDoesNothingWhenNotTargeting()
    {
        var (exp, ctrl) = Fighting();
        ctrl.CardPress(exp, 0);                 // powered but NOT targeting
        ctrl.FoePress(exp, exp.Foes[0]);
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
        exp.Aim(Card0(exp), exp.Foes[0]);
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
}
