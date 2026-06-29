using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class CombatTickTests
{
    private static readonly Part Head =
        new("head", new Dictionary<Attribute, int> { [Attribute.Vigor] = 1 }, PartRole.Head, MaxHealth: 5);

    private static Entity Self(int power = 20, int focus = 20, int vigor = 20)
    {
        var e = new Entity(new AttributePool(new Dictionary<Attribute, int>
        {
            [Attribute.Power] = power,
            [Attribute.Focus] = focus,
            [Attribute.Vigor] = vigor,
        }));
        e.Add(Head);
        e.Enable(Head); // live head => can cast
        return e;
    }

    private static (Entity dummy, Part hide) Target(int hp = 1000)
    {
        var hide = new Part("hide", new Dictionary<Attribute, int>(), PartRole.Generic, MaxHealth: hp);
        var dummy = new Entity(new AttributePool(new Dictionary<Attribute, int>()));
        dummy.Add(hide);
        return (dummy, hide);
    }

    private static int DamageDealt(Entity target, Part part, int startHp) => startHp - target.Health(part);

    [Fact]
    public void TimeredFiresOnCooldown()
    {
        var self = Self();
        var (dummy, hide) = Target();
        var caster = new Caster(self, dummy, hide);

        caster.Activate(Techniques.Jab); // cd 2, power 3
        for (var i = 0; i < 4; i++) caster.Step();

        Assert.Equal(2 * 3, DamageDealt(dummy, hide, 1000)); // fires at tick 2 and 4
    }

    [Fact]
    public void SustainedOutputsEveryTick()
    {
        var self = Self();
        var (dummy, hide) = Target();
        var caster = new Caster(self, dummy, hide);

        caster.Activate(Techniques.Drain); // power 2 per tick
        for (var i = 0; i < 3; i++) caster.Step();

        Assert.Equal(3 * 2, DamageDealt(dummy, hide, 1000));
    }

    [Fact]
    public void ParallelByAllocation_SecondTechniqueBlockedWhenPoolIsExhausted()
    {
        var self = Self(focus: 4); // Drain(3) + Ember(2) = 5 focus needed, only 4 available
        var (dummy, hide) = Target();
        var caster = new Caster(self, dummy, hide);

        Assert.True(caster.Activate(Techniques.Drain));
        Assert.False(caster.Activate(Techniques.Ember));
        Assert.Equal(1, caster.ActiveCount);
    }

    [Fact]
    public void DeactivatingReturnsAllocationForAnother()
    {
        var self = Self(focus: 4);
        var (dummy, hide) = Target();
        var caster = new Caster(self, dummy, hide);

        caster.Activate(Techniques.Drain);
        caster.Deactivate(Techniques.Drain);
        Assert.True(caster.Activate(Techniques.Ember)); // freed focus now covers it
    }

    [Fact]
    public void HeadDisableSilencesCasting()
    {
        var self = Self();
        var (dummy, hide) = Target();
        var caster = new Caster(self, dummy, hide);
        caster.Activate(Techniques.Drain);

        self.Disable(Head); // head off

        Assert.False(caster.Activate(Techniques.Jab)); // cannot start a cast
        caster.Step();                                  // active casts silenced
        Assert.Equal(0, caster.ActiveCount);
        Assert.Equal(0, DamageDealt(dummy, hide, 1000)); // no output the tick it dropped
    }

    [Fact]
    public void DamageDestroysPartAndReturnsItsAllocation()
    {
        var self = Self();
        var arm = new Part("arm", new Dictionary<Attribute, int> { [Attribute.Power] = 5 }, MaxHealth: 4);
        self.Add(arm);
        Assert.True(self.Enable(arm));
        Assert.Equal(15, self.Pool.Available(Attribute.Power));

        self.Damage(arm, 4);

        Assert.True(self.IsDestroyed(arm));
        Assert.False(self.IsEnabled(arm));
        Assert.Equal(20, self.Pool.Available(Attribute.Power)); // allocation returned
        Assert.False(self.Enable(arm));                          // cannot re-power a destroyed part
    }

    [Fact]
    public void SimulationIsDeterministic_SameInputsSameOutcome()
    {
        static int Run()
        {
            var e = new Entity(new AttributePool(new Dictionary<Attribute, int>
            {
                [Attribute.Power] = 20, [Attribute.Focus] = 20, [Attribute.Vigor] = 20,
            }));
            var head = new Part("head", new Dictionary<Attribute, int> { [Attribute.Vigor] = 1 }, PartRole.Head, 5);
            e.Add(head);
            e.Enable(head);
            var hide = new Part("hide", new Dictionary<Attribute, int>(), MaxHealth: 100000);
            var dummy = new Entity(new AttributePool(new Dictionary<Attribute, int>()));
            dummy.Add(hide);

            var caster = new Caster(e, dummy, hide);
            caster.Activate(Techniques.Jab);
            caster.Activate(Techniques.Cleave);
            caster.Activate(Techniques.Drain);
            for (var i = 0; i < 50; i++) caster.Step();
            return 100000 - dummy.Health(hide);
        }

        Assert.Equal(Run(), Run());
    }
}
