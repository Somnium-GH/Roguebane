namespace Roguebane.Core;

public enum CampaignState
{
    Redeploying, // a leg is underway
    Won,      // the Capital fell — the campaign is won
    Lost,     // a leg was lost
}

// The campaign spine: a march through several legs (cities) to the Capital. One body, caster,
// loadout and Stash carry the whole way — spoils and part damage persist between legs —
// but each leg brings a FRESH war party and a tougher castle. Win a leg to advance; win the last
// to take the Capital; lose any leg and the march is over.
public sealed class Campaign
{
    private readonly Fighter _player;
    private readonly Caster _caster;
    private readonly List<Technique> _loadout;
    private readonly IReadOnlyList<Func<CityMap>> _legs;
    private readonly Stash _stash;
    private readonly string _figureId;
    private readonly int _techniqueSlots;
    private int _legIndex;

    public Expedition Current { get; private set; }
    public CampaignState State { get; private set; } = CampaignState.Redeploying;

    public Campaign(
        Fighter player,
        Caster caster,
        IReadOnlyList<Technique> loadout,
        IReadOnlyList<Func<CityMap>> legs,
        Stash? stash = null,
        string figureId = "human_grunt",
        int techniqueSlots = int.MaxValue)
    {
        if (legs.Count == 0) throw new ArgumentException("a campaign needs at least one leg", nameof(legs));
        _player = player;
        _caster = caster;
        _loadout = loadout.ToList();
        _legs = legs;
        _stash = stash ?? new Stash();
        _figureId = figureId;
        _techniqueSlots = techniqueSlots;
        Current = NewLeg();
    }

    public int LegIndex => _legIndex;
    public int LegCount => _legs.Count;
    public Stash Stash => _stash;
    public bool OnFinalLeg => _legIndex == _legs.Count - 1;

    private Expedition NewLeg() =>
        new(_player, _caster, _loadout, _legs[_legIndex](), _stash, _figureId, _techniqueSlots);

    // Top-level passthroughs so a driver talks to the campaign, not the swapping leg underneath.
    public bool Enter(string nodeId)
    {
        if (State != CampaignState.Redeploying) return false;
        var ok = Current.Enter(nodeId); // can lose here (war party overrun on the jump)
        Advance();
        return ok;
    }

    public void Toggle(Technique technique) => Current.Toggle(technique);
    public bool IsActive(Technique technique) => Current.IsActive(technique);

    // Roster equip/unequip mirrored into the campaign-level loadout (not just the live leg) so a
    // mid-leg change survives NewLeg() rebuilding Current from _loadout when the party advances.
    public bool EquipTechnique(Technique technique)
    {
        if (!Current.EquipTechnique(technique)) return false;
        if (!_loadout.Contains(technique)) _loadout.Add(technique);
        return true;
    }

    public bool UnequipTechnique(Technique technique)
    {
        if (!Current.UnequipTechnique(technique)) return false;
        _loadout.Remove(technique);
        return true;
    }

    // Mirrors the reorder into _loadout too, else the position would revert on the next NewLeg().
    public bool ReorderTechnique(Technique technique, int newIndex)
    {
        if (!Current.ReorderTechnique(technique, newIndex)) return false;
        var i = _loadout.IndexOf(technique);
        if (i < 0) return true;
        _loadout.RemoveAt(i);
        _loadout.Insert(Math.Clamp(newIndex, 0, _loadout.Count - 1), technique);
        return true;
    }
    public void Redeploy() => Current.Redeploy(); // leave a cleared fight -> back to the chart

    // FTL targeting surface (delegates to the current leg's expedition).
    public Foe? Enemy => Current.Enemy;
    public void Aim(Technique technique, ICombatTarget target) => Current.Aim(technique, target);
    public void Aim(Technique technique, ICombatTarget target, BodyPart part) => Current.Aim(technique, target, part);
    public void ClearAim(Technique technique) => Current.ClearAim(technique);
    public bool Fire(Technique technique) => Current.Fire(technique);
    public void SetAuto(bool auto) => Current.SetAuto(auto); // ONE global toggle
    public bool IsAuto() => Current.IsAuto();
    public bool IsReady(Technique technique) => Current.IsReady(technique);
    public ICombatTarget? AimOf(Technique technique) => Current.AimOf(technique);

    public void Tick()
    {
        if (State != CampaignState.Redeploying) return;
        Current.Tick();
        Advance();
    }

    // After the current leg resolves, roll to the next city — or settle the campaign.
    private void Advance()
    {
        switch (Current.State)
        {
            case ExpeditionState.Lost:
                State = CampaignState.Lost;
                break;
            case ExpeditionState.Won when OnFinalLeg:
                State = CampaignState.Won;
                break;
            case ExpeditionState.Won:
                _legIndex++;
                _player.Heal(_player.MaxHp); // rest at the city: HP restored, parts persist
                Current = NewLeg();
                break;
        }
    }
}
