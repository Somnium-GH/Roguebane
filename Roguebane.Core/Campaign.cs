namespace Roguebane.Core;

public enum CampaignState
{
    Marching, // a leg is underway
    Won,      // the Capital fell — the campaign is won
    Lost,     // a leg was lost
}

// The campaign spine: a march through several legs (cities) to the Capital. One body, caster,
// loadout and Stash carry the whole way — spoils, potions and part damage persist between legs —
// but each leg brings a FRESH war party and a tougher castle. Win a leg to advance; win the last
// to take the Capital; lose any leg and the march is over.
public sealed class Campaign
{
    private readonly Fighter _player;
    private readonly Caster _caster;
    private readonly IReadOnlyList<Technique> _loadout;
    private readonly IReadOnlyList<Func<RunMap>> _legs;
    private readonly Stash _stash;
    private int _legIndex;

    public Expedition Current { get; private set; }
    public CampaignState State { get; private set; } = CampaignState.Marching;

    public Campaign(
        Fighter player,
        Caster caster,
        IReadOnlyList<Technique> loadout,
        IReadOnlyList<Func<RunMap>> legs,
        Stash? stash = null)
    {
        if (legs.Count == 0) throw new ArgumentException("a campaign needs at least one leg", nameof(legs));
        _player = player;
        _caster = caster;
        _loadout = loadout;
        _legs = legs;
        _stash = stash ?? new Stash();
        Current = NewLeg();
    }

    public int LegIndex => _legIndex;
    public int LegCount => _legs.Count;
    public Stash Stash => _stash;
    public bool OnFinalLeg => _legIndex == _legs.Count - 1;

    private Expedition NewLeg() =>
        new(_player, _caster, _loadout, _legs[_legIndex](), _stash);

    // Top-level passthroughs so a driver talks to the campaign, not the swapping leg underneath.
    public bool Enter(string nodeId)
    {
        if (State != CampaignState.Marching) return false;
        var ok = Current.Enter(nodeId); // can lose here (war party overrun on the jump)
        Advance();
        return ok;
    }

    public void Toggle(Technique technique) => Current.Toggle(technique);
    public bool IsActive(Technique technique) => Current.IsActive(technique);

    // FTL targeting surface (delegates to the current leg's expedition).
    public IReadOnlyList<Foe> Foes => Current.Foes;
    public void Aim(Technique technique, ICombatTarget target) => Current.Aim(technique, target);
    public void ClearAim(Technique technique) => Current.ClearAim(technique);
    public bool Fire(Technique technique) => Current.Fire(technique);
    public void SetAuto(bool auto) => Current.SetAuto(auto); // ONE global toggle
    public bool IsAuto() => Current.IsAuto();
    public bool IsReady(Technique technique) => Current.IsReady(technique);
    public ICombatTarget? AimOf(Technique technique) => Current.AimOf(technique);

    public void Tick()
    {
        if (State != CampaignState.Marching) return;
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
                _player.Heal(_player.MaxHp); // rest at the city: HP restored, parts persist (use potions)
                Current = NewLeg();
                break;
        }
    }
}
