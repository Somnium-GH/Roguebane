namespace Roguebane.Core;

// One combat grammar everywhere: anything a technique can hit — a foe, a castle, or the PLAYER — is a
// structured thing with an optional Frame of targetable parts and an HP life total. §8: a hit erodes the
// aimed part's stat (via Frame.Damage) AND takes HP (Damage) simultaneously, same power — the attacker
// (Caster.Hit) applies both; there is no part-vs-HP split here.
public interface ICombatTarget
{
    bool Down { get; }
    Body? Frame { get; }
    void Damage(int amount); // HP life total
}
