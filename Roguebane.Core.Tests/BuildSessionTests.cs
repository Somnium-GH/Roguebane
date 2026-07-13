using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class BuildSessionTests
{
    private static BuildSession New() => Sessions.NewBuild();

    [Fact]
    public void RosterExposesTheWholeLineUpForTheCoreGrid()
    {
        var build = New();
        Assert.Equal(CoreRunes.Roster.Count, build.Roster.Count);
        Assert.Same(build.CoreRune, build.Roster[build.CoreRuneIndex]);
    }

    [Fact]
    public void CyclingCoreRuneWrapsAndResetsRunes()
    {
        var build = New();
        var roster = CoreRunes.Roster;
        Assert.Same(roster[0], build.CoreRune);

        build.CycleCoreRune(1);
        Assert.Same(roster[1], build.CoreRune);
        Assert.Equal(roster[1].RuneBudget, build.Runes.Available); // fresh budget

        build.CycleCoreRune(-1); // back to the first
        Assert.Same(roster[0], build.CoreRune);

        build.CycleCoreRune(-1); // wraps to the last
        Assert.Same(roster[^1], build.CoreRune);
    }

    [Fact]
    public void CyclingRaceSwapsBodyAttrsAndKeepsTheCoreBudget()
    {
        var build = New();
        var races = Races.Roster;
        Assert.Same(races[0], build.Race);               // Human first
        var budgetBefore = build.Runes.Available;
        var conBefore = build.Preview().Capacity(Stat.Con);

        build.CycleRace(1);
        Assert.Same(races[1], build.Race);               // Elf
        Assert.Equal(build.Runes.Available, budgetBefore);        // core budget untouched by a race swap
        Assert.NotEqual(conBefore, build.Preview().Capacity(Stat.Con)); // Elf con 2 vs Human con 3

        build.CycleRace(-1); // wraps back to Human
        Assert.Same(races[0], build.Race);
    }

    [Fact]
    public void ClimbingAPathSpendsBudgetAndStopsAtTheKeystone()
    {
        var build = New(); // Grunt: budget 24, discount 1
        Assert.True(build.Climb(Paths.VesselLadder));  // VesselI
        Assert.True(build.Climb(Paths.VesselLadder));  // VesselII
        Assert.True(build.Climb(Paths.VesselLadder));  // Hollow Vessel
        Assert.False(build.Climb(Paths.VesselLadder)); // ladder exhausted

        Assert.True(build.Runes.Has(Paths.HollowVessel));
    }

    [Fact]
    public void PreviewFoldsRuneGrantsIntoTheBody()
    {
        var build = New();
        var baseCon = build.Preview().Capacity(Stat.Con);
        foreach (var _ in Paths.VesselLadder) build.Climb(Paths.VesselLadder);

        Assert.Equal(baseCon + 6, build.Preview().Capacity(Stat.Con));
    }

    [Fact]
    public void EquipmentOnlyEverIncludesPaletteTechniques()
    {
        var build = New(); // Grunt kit (cores.json): jab, shot, brace, bandage — Siphon/Lunge aren't in it
        build.Toggle(Techniques.Siphon);
        build.Toggle(Techniques.Lunge);

        Assert.True(build.IsSelected(Techniques.Jab)); // kit
        // off-palette toggles never surface in Equipment — the inventory can't offer more than the
        // current core's kit plus whatever the runes taken so far grant.
        Assert.DoesNotContain("siphon", build.Equipment.Select(t => t.Id));
        Assert.DoesNotContain("lunge", build.Equipment.Select(t => t.Id));
        Assert.Equal(build.CoreRune.Kit.Select(t => t.Id), build.Equipment.Select(t => t.Id));

        build.Toggle(Techniques.Jab); // a kit item can still be dropped
        Assert.False(build.IsSelected(Techniques.Jab));
        Assert.DoesNotContain("jab", build.Equipment.Select(t => t.Id));
    }

    [Fact]
    public void PaletteIsScopedToTheCurrentCoresKitForEveryChassis()
    {
        var build = New();
        for (var i = 0; i < build.CoreRuneCount; i++)
        {
            // no rune grants taken yet, so the palette is exactly the chassis kit — never the whole roster
            Assert.Equal(build.CoreRune.Kit.Count, build.Palette.Count);
            build.CycleCoreRune(1);
        }
    }

    [Fact]
    public void ClimbingAGrantKeystoneAddsItsTechniqueToThePalette()
    {
        var build = new BuildSession(Races.Roster, CoreRunes.Roster, new[] { Paths.TempestLadder });
        var baseCount = build.Palette.Count;

        foreach (var _ in Paths.TempestLadder) build.Climb(Paths.TempestLadder);

        Assert.Equal(baseCount + 1, build.Palette.Count);
        Assert.Contains(build.Palette, t => t.Id == "maelstrom");
    }

    [Fact]
    public void LaunchMintsTheChosenBodyIntoARun()
    {
        var build = New();
        var kitSize = build.CoreRune.Kit.Count; // Grunt's kit, whatever the current balance tune is
        build.Climb(Paths.VesselLadder);
        build.Toggle(Techniques.Jab); // drop a kit item

        var run = Sieges.StandardRun();
        var session = build.Launch(run);

        Assert.Equal(SessionState.Fighting, session.State);
        Assert.Equal(kitSize - 1, session.Equipment.Count); // kit minus the dropped jab
        Assert.Equal(3, session.Run.Nodes.Count); // cp1, cp2, castle
    }

    [Fact]
    public void EmbarkDropsTheChosenBodyIntoTheRealLoop()
    {
        var build = New();

        var exp = build.Embark(Maps.StandardLeg(autoResolveCastle: false));

        Assert.Equal(ExpeditionState.Choosing, exp.State);
        Assert.Contains(exp.Equipment, t => t.Id == "jab"); // shipped in the kit, no pick needed
        Assert.Equal("camp", exp.Map.CurrentId);
    }

    [Fact]
    public void RedeploySendsTheChosenBodyDownTheCampaignSpine()
    {
        var build = New();

        var campaign = build.Redeploy(Maps.StandardLegs(3));

        Assert.Equal(CampaignState.Redeploying, campaign.State);
        Assert.Equal(3, campaign.LegCount);
        Assert.Equal(0, campaign.LegIndex);
        Assert.Contains(campaign.Current.Equipment, t => t.Id == "jab"); // kit
    }

    // The launch gate is gone: every chassis ships a non-empty fixed kit, so the bar is never empty
    // and Launch is never blocked.
    [Fact]
    public void EveryCoreRuneShipsANonEmptyKitThatLandsInTheLoadout()
    {
        var build = New();
        for (var i = 0; i < build.CoreRuneCount; i++)
        {
            Assert.NotEmpty(build.CoreRune.Kit);
            Assert.NotEmpty(build.Equipment); // seeded, no manual pick
            build.CycleCoreRune(1);
        }
    }

    // Data invariant (2026-07-06 loop, Debt item): the action bar's ActionSlots must never be smaller
    // than the starting Kit — every display/lock site that reads "how many technique slots" (NewGame
    // preview, Equipment slotLabel/core.stats, the inventory lock threshold) was undersizing itself by
    // reading Kit.Count instead of ActionSlots on the 5/7 cores where a rune-granted technique needs
    // room the kit alone doesn't reserve. Pins the invariant those fixes now depend on.
    [Fact]
    public void EveryCoreRunesActionSlotsCoverItsOwnStartingKit()
    {
        foreach (var core in CoreRunes.Roster)
            Assert.True(core.ActionSlots >= core.Kit.Count,
                $"{core.Id}: ActionSlots {core.ActionSlots} < Kit.Count {core.Kit.Count}");
    }

    [Fact]
    public void CyclingCoreRuneReseedsTheKit()
    {
        // The Equipment reseeds to whatever the CURRENT core's kit is (asserted against the core itself
        // so a balance-tune of the kits doesn't redden this behavioral test).
        var build = New(); // Grunt
        Assert.Equal(build.CoreRune.Kit.Select(t => t.Id), build.Equipment.Select(t => t.Id));

        build.CycleCoreRune(1); // Warden
        Assert.Equal(build.CoreRune.Kit.Select(t => t.Id), build.Equipment.Select(t => t.Id));
    }
}
