namespace Roguebane.Core;

// A Mark is one rung on a Path's ladder. Content, not code: cost and refund are data.
// Climbing to a Mark overwrites the rung below it; Refund is the budget reclaimed when
// this Mark is the one being overwritten.
public sealed record Mark(
    string Path,
    int Rank,
    int Cost,
    int Refund,
    bool Keystone = false);
