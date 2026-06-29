namespace Roguebane.Core;

// One combat grammar everywhere: anything a technique can hit — a foe, a castle layer, or the
// PLAYER — is a structured thing with an optional Frame of targetable parts and an HP life total.
// A part-aimed hit erodes the part's stat first and only spills into HP once the part bottoms out
// (the §10 split); an unstructured target (Frame == null) just takes HP.
public interface ICombatTarget
{
    bool Down { get; }
    Body? Frame { get; }
    void Damage(int amount);                  // HP life total
    void DamagePart(BodyPart part, int amount); // localized: stat first, overkill to HP
}
