namespace Roguebane.Core;

// A body part is the SOURCE of a stat (data). Paired parts (Arms x2, Legs x2) each carry a
// share — one arm holds half the body's STR. Capacity is the share at full health; the live
// contribution drops as the part takes damage. Damaging it subtracts that stat from the pool.
public sealed record BodyPart(string Id, Stat Stat, int Capacity);

// Equipment or an ability that RESERVES a stat while engaged. Reserve doubles as the requirement:
// you need that much of the stat free to wield it, and if the stat later falls under it, it drops.
public sealed record Active(string Id, Stat Stat, int Reserve);
