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
    public void ArmedFoesChipAPassivePlayer()
    {
        var player = Fighter.Scaled(PlayerBody(), baseHp: 8); // MaxHp 16
        var enc = Sieges.ArmedPoint("cp", 100, 100);          // tanky so they keep swinging
        var battle = new Battle(new Caster(player.Body), enc, player);

        for (var i = 0; i < 200; i++) battle.Step(); // player has no techniques -> only takes hits
        Assert.True(player.Hp < player.MaxHp);
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
}
