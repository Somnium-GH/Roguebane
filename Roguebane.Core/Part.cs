namespace Roguebane.Core;

// A part is content, not code: its identity and attribute demand are data. Runtime
// enabled/disabled state lives on the Entity, not here.
public sealed record Part(string Id, IReadOnlyDictionary<Attribute, int> Demand);
