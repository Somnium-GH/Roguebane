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
    public void AMerchantHealsWithoutAFight()
    {
        var exp = FullLoadout();
        exp.Enter("a2");
        FightToEnd(exp);              // damage may have been taken
        exp.Player.Damage(3);        // simulate carrying a wound to the merchant
        var wounded = exp.Player.Hp;

        exp.Enter("b");              // the merchant
        Assert.Equal(ExpeditionState.Choosing, exp.State); // no fight
        Assert.True(exp.Player.Hp >= wounded);
        Assert.Equal(exp.Player.MaxHp, exp.Player.Hp);     // HP service tops up
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
