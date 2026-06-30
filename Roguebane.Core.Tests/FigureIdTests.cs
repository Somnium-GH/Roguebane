using Roguebane.Core;
using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// The chassis figure key must survive assembly so the shell renders the player with the right
// modular sprite set (and carry across a campaign's legs).
public class FigureIdTests
{
    [Fact]
    public void EmbarkCarriesTheChassisFigureId()
    {
        var chassis = Chassrium.Roster[0];
        var exp = Forge.Embark(chassis, chassis.NewLoadout(), chassis.Kit, Maps.StandardLeg());
        Assert.Equal(chassis.Id, exp.FigureId);
    }

    [Fact]
    public void CampaignLegsKeepTheFigureId()
    {
        var chassis = Chassrium.Roster[0];
        var camp = Forge.EmbarkCampaign(chassis, chassis.NewLoadout(), chassis.Kit, Maps.StandardLegs(2));
        Assert.Equal(chassis.Id, camp.Current.FigureId);
    }
}
