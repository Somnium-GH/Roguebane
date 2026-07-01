using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// Live-run foes are armed: they carry a Frame + Arsenal and chip the player. Threat stays low
// (runs winnable), but combat is genuinely two-sided.
public class FoeArmingTests
{
    private static Body PlayerBody()
    {
        var b = new Body();
        b.Add(new BodyPart("arm", Stat.Str, 4));
        b.Add(new BodyPart("leg", Stat.Dex, 4));
        b.Add(new BodyPart("head", Stat.Int, 4));
        b.Add(new BodyPart("chest", Stat.Con, 4));
        return b;
    }

    [Fact]
    public void ArmedFoesCarryAFrameAndArsenal()
    {
        var enc = Sieges.ArmedPoint("cp", 6);
        var foe = enc.Foes[0];
        Assert.NotNull(foe.Frame);
        Assert.NotEmpty(foe.Arsenal);
    }

    [Fact]
    public void ArmedFoesErodeAPassivePlayersParts()
    {
        // §8 foe part-aim is LIVE on skirmishes: foes erode the player's PARTS (the live stat pool
        // shrinks), not restorable HP. A passive, healless player just gets stripped.
        var player = Fighter.Scaled(PlayerBody(), baseHp: 8);
        var before = player.Body.Parts.Sum(p => player.Body.Contribution(p));
        var enc = Sieges.ArmedPoint("cp", 100, 100); // tanky so they keep swinging
        var battle = new Battle(new Caster(player.Body), enc, player);

        for (var i = 0; i < 200; i++) battle.Step(); // no techniques, no heal -> only takes hits
        Assert.True(player.Body.Parts.Sum(p => player.Body.Contribution(p)) < before);
    }

    [Fact]
    public void SmashingAFoesArmCascadesItsStrikeOff()
    {
        var enc = Sieges.ArmedPoint("cp", 100);
        var foe = enc.Foes[0];
        Assert.NotNull(foe.Frame);
        // Erode the arm below the strike's reserve -> the foe can no longer power it.
        foe.Frame!.Damage(foe.Frame.Parts[0], 4);
        Assert.Equal(0, foe.Frame.Capacity(Stat.Str));
    }

    // G1: a content foe carries several distinct parts so the player can aim limb-by-limb.
    [Fact]
    public void ArmedFoesExposeDistinctTargetableParts()
    {
        var foe = Sieges.ArmedPoint("cp", 10).Foes[0];
        var stats = foe.Frame!.Parts.Select(p => p.Stat).ToHashSet();
        Assert.Contains(Stat.Str, stats);
        Assert.Contains(Stat.Int, stats);
        Assert.True(foe.Frame.Parts.Count >= 3); // real choices to pick from
    }

    // Part-aim at a content foe erodes the chosen part's stat (not its HP, until the part bottoms out).
    [Fact]
    public void PartAimErodesTheChosenFoePart()
    {
        var foe = Sieges.ArmedPoint("cp", 10).Foes[0];
        var head = foe.Frame!.Parts.First(p => p.Stat == Stat.Int); // capacity 2

        var body = new Body();
        body.Add(new BodyPart("arm", Stat.Str, 6));
        var c = new Caster(body, null, requireAim: true);
        c.Activate(Techniques.Jab, auto: true);
        c.Aim(Techniques.Jab, foe, head); // PART aim

        for (var i = 0; i < 60; i++) c.Step(); // one Jab (Power 2) lands on the head
        Assert.Equal(0, foe.Frame.Contribution(head)); // head stat eroded to 0
        Assert.Equal(10, foe.Hp);                       // fully absorbed -> HP untouched
    }
}
