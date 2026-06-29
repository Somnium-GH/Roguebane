using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// G4/G5 integration: the Expedition is the real loop — pick a node, fight what you land on, race the
// war party to the castle. Combat, the map, supplies and the clock compose into one win/lose result.
public class ExpeditionTests
{
    private static Expedition FullLoadout()
    {
        var exp = Sessions.Expedition();
        foreach (var t in exp.Loadout) exp.Toggle(t);
        return exp;
    }

    private static void FightToEnd(Expedition exp)
    {
        var guard = 0;
        while (exp.State == ExpeditionState.Fighting && guard++ < 10000) exp.Tick();
    }

    [Fact]
    public void EnteringACombatNodeStartsAFight()
    {
        var exp = FullLoadout();
        Assert.Equal(ExpeditionState.Choosing, exp.State);

        Assert.True(exp.Enter("a2")); // a skirmish
        Assert.Equal(ExpeditionState.Fighting, exp.State);
        Assert.NotNull(exp.Battle);
    }

    [Fact]
    public void ClearingASkirmishReturnsToChoosing()
    {
        var exp = FullLoadout();
        exp.Enter("a2");
        FightToEnd(exp);
        Assert.Equal(ExpeditionState.Choosing, exp.State);
    }

    [Fact]
    public void AMerchantIsAStopNotAFight()
    {
        var exp = FullLoadout();
        exp.Enter("a2");
        FightToEnd(exp);

        exp.Enter("b"); // the merchant
        Assert.Equal(ExpeditionState.Choosing, exp.State); // no fight
        Assert.True(exp.AtMerchant);
    }

    [Fact]
    public void MerchantHpServiceCostsGoldAndTopsUp()
    {
        var exp = FullLoadout();
        exp.Enter("a2"); FightToEnd(exp); // earns spoils
        exp.Player.Damage(2);             // carry a wound in
        exp.Enter("b");

        Assert.True(exp.Gold >= 3);
        var gold = exp.Gold;
        Assert.True(exp.BuyHeal());
        Assert.Equal(exp.Player.MaxHp, exp.Player.Hp);
        Assert.Equal(gold - 3, exp.Gold);
    }

    [Fact]
    public void PotionsAreBoughtAtMerchantsAndRepairPartsOutOfCombat()
    {
        var exp = FullLoadout();
        exp.Enter("a1"); FightToEnd(exp); // bank + spoils
        // wound a part directly to observe repair
        var arm = exp.Player.Body.Parts.First(p => p.Stat == Stat.Str);
        exp.Player.Body.Damage(arm, 2);
        var hurt = exp.Player.Body.Contribution(arm);

        exp.Enter("b");
        Assert.True(exp.BuyPotion());
        Assert.Equal(1, exp.Potions);

        Assert.True(exp.UsePotion()); // out of combat (Choosing)
        Assert.Equal(0, exp.Potions);
        Assert.True(exp.Player.Body.Contribution(arm) > hurt); // part repaired
    }

    [Fact]
    public void PotionsCannotBeUsedMidFight()
    {
        var exp = FullLoadout();
        exp.Enter("a1"); FightToEnd(exp);
        exp.Enter("b"); exp.BuyPotion();
        exp.Enter("c2"); // a fight starts
        Assert.Equal(ExpeditionState.Fighting, exp.State);
        Assert.False(exp.UsePotion()); // sealed during combat
    }

    [Fact]
    public void ClearingNodesAwardsSpoils()
    {
        var exp = FullLoadout();
        Assert.Equal(0, exp.Gold);
        exp.Enter("a2"); FightToEnd(exp);
        Assert.Equal(3, exp.Gold); // a skirmish pays 3
    }

    [Fact]
    public void CrackingTheCastleWinsTheLeg()
    {
        var exp = FullLoadout();
        // camp -> a1 (hold, banks support) -> b (merchant) -> c2 (hold) -> castle
        exp.Enter("a1"); FightToEnd(exp);
        exp.Enter("b");                     // merchant heal, no fight
        exp.Enter("c2"); FightToEnd(exp);
        Assert.Equal(ExpeditionState.Choosing, exp.State);

        exp.Enter("castle");
        Assert.Equal(ExpeditionState.Fighting, exp.State);
        FightToEnd(exp);

        Assert.Equal(ExpeditionState.Won, exp.State);
        Assert.Equal(RunMapOutcome.CastleCracked, exp.Map.Outcome);
    }

    [Fact]
    public void BankedHoldsFeedTheCastleSupport()
    {
        var exp = FullLoadout();
        exp.Enter("a1"); FightToEnd(exp); // bank 1
        exp.Enter("b");
        exp.Enter("c2"); FightToEnd(exp); // bank 2
        Assert.Equal(2, exp.Map.SupportBank);
    }

    [Fact]
    public void TheRunIsDeterministic()
    {
        static ExpeditionState Play()
        {
            var exp = FullLoadout();
            exp.Enter("a1"); FightToEnd(exp);
            exp.Enter("b");
            exp.Enter("c2"); FightToEnd(exp);
            exp.Enter("castle"); FightToEnd(exp);
            return exp.State;
        }
        Assert.Equal(Play(), Play());
    }
}
