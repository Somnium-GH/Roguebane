namespace Roguebane.Core.Tests;

public class EntityTests
{
    private static Entity Body(int power, int focus = 0) => new(new AttributePool(
        new Dictionary<Attribute, int> { [Attribute.Power] = power, [Attribute.Focus] = focus }));

    private static Part Part(string id, int power, int focus = 0) => new(id,
        new Dictionary<Attribute, int> { [Attribute.Power] = power, [Attribute.Focus] = focus });

    [Fact]
    public void EnablingDrawsDemandFromPool()
    {
        var body = Body(10);
        var arm = Part("arm", power: 4);
        body.Add(arm);

        Assert.True(body.Enable(arm));
        Assert.True(body.IsEnabled(arm));
        Assert.Equal(6, body.Pool.Available(Attribute.Power));
    }

    [Fact]
    public void DisablingReturnsAttributesToPool()
    {
        var body = Body(10);
        var arm = Part("arm", power: 4);
        body.Add(arm);
        body.Enable(arm);

        body.Disable(arm);

        Assert.False(body.IsEnabled(arm));
        Assert.Equal(10, body.Pool.Available(Attribute.Power));
    }

    [Fact]
    public void DisablingFreesBudgetForAnotherPart()
    {
        var body = Body(10);
        var heavy = Part("heavy", power: 8);
        var other = Part("other", power: 6);
        body.Add(heavy);
        body.Add(other);
        body.Enable(heavy);

        Assert.False(body.Enable(other)); // only 2 left
        body.Disable(heavy);
        Assert.True(body.Enable(other));  // freed budget covers it
    }

    [Fact]
    public void EnableIsAtomicAcrossMultipleAttributes()
    {
        var body = Body(power: 10, focus: 2);
        var caster = Part("caster", power: 3, focus: 5); // focus demand exceeds capacity
        body.Add(caster);

        Assert.False(body.Enable(caster));
        Assert.Equal(10, body.Pool.Available(Attribute.Power)); // power not left claimed
        Assert.Equal(2, body.Pool.Available(Attribute.Focus));
    }

    [Fact]
    public void EnableIsIdempotent()
    {
        var body = Body(10);
        var arm = Part("arm", power: 4);
        body.Add(arm);

        Assert.True(body.Enable(arm));
        Assert.True(body.Enable(arm));
        Assert.Equal(6, body.Pool.Available(Attribute.Power)); // not drawn twice
    }
}
