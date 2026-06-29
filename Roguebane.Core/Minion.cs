namespace Roguebane.Core;

// A minion occupies a BAY (not an action-bar slot) and reserves a stat to keep standing — the same
// reserve-on-the-body rule as a technique, so a drained stat (e.g. a smashed head starving INT)
// dismisses it via the cascade. While powered it auto-fires on whatever the caster is pressing.
// Minion VARIETY (beast/follower re-gating onto STR/INT/DEX/CON) is parked; the POC is INT-funded.
public sealed record Minion(string Id, Stat Stat, int Reserve, int Power);
