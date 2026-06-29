namespace Roguebane.Core;

// Rallied support: player-allied, banked, and undamageable — an intermittent auto-fire that lands
// on whatever the player is pressing. It is a stream, not a unit, so nothing can kill it.
public sealed class Support
{
    private readonly int _amount;
    private readonly int _every;
    private int _tick;

    public Support(int amount, int every)
    {
        _amount = amount;
        _every = every;
    }

    public int Fire()
    {
        _tick++;
        return _amount > 0 && _every > 0 && _tick % _every == 0 ? _amount : 0;
    }
}
