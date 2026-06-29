namespace Roguebane.Core.Tests;

// G2a: plate armor blunts stat-damage to its part-group and rides the part's condition.
public class ArmorTests
{
    private static (Body, BodyPart) ArmsBody(int str)
    {
        var body = new Body();
        var arm = new BodyPart("arm", Stat.Str, str);
        body.Add(arm);
        return (body, arm);
    }

    [Fact]
    public void PlateSubtractsFlatProtectionFromStatDamage()
    {
        var (body, arm) = ArmsBody(6);
        body.Equip(new Armor("pauldron", Stat.Str, ArmorKind.Plate, 2));

        var overkill = body.AbsorbPartHit(arm, 3); // 3 - 2 protection = 1 erosion

        Assert.Equal(5, body.Contribution(arm));
        Assert.Equal(0, overkill);
    }

    [Fact]
    public void ProtectionRidesThePartConditionAndIsGoneOnceItBreaks()
    {
        var (body, arm) = ArmsBody(2);
        body.Equip(new Armor("pauldron", Stat.Str, ArmorKind.Plate, 2));

        body.AbsorbPartHit(arm, 3); // 3 - 2 = 1 erosion -> arm 2 -> 1
        Assert.Equal(1, body.Contribution(arm));
        body.AbsorbPartHit(arm, 3); // still protected (part stands): 1 erosion -> arm 1 -> 0
        Assert.Equal(0, body.Contribution(arm));

        var overkill = body.AbsorbPartHit(arm, 3); // part down -> no protection -> all 3 to overkill
        Assert.Equal(3, overkill);
    }

    [Fact]
    public void OverkillIsWhatExceedsArmorPlusTheRemainingPart()
    {
        var (body, arm) = ArmsBody(2);
        body.Equip(new Armor("pauldron", Stat.Str, ArmorKind.Plate, 1));

        var overkill = body.AbsorbPartHit(arm, 5); // 5 - 1 = 4 effective; arm absorbs 2; 2 overkill
        Assert.Equal(0, body.Contribution(arm));
        Assert.Equal(2, overkill);
    }

    [Fact]
    public void LeatherAndSpellWardGiveNoFlatProtectionYet()
    {
        var (body, arm) = ArmsBody(6);
        body.Equip(new Armor("jerkin", Stat.Str, ArmorKind.Leather, 3));

        Assert.Equal(0, body.Protection(arm)); // evasion path deferred
        body.AbsorbPartHit(arm, 3);
        Assert.Equal(3, body.Contribution(arm)); // full erosion, unprotected
    }

    [Fact]
    public void ArmorMitigatesIncomingFoeDamageToThePlayer()
    {
        var (body, _) = ArmsBody(6);
        body.Equip(new Armor("pauldron", Stat.Str, ArmorKind.Plate, 2));
        var player = new Fighter(body, maxHp: 10);
        var arm = body.Parts[0];

        player.DamagePart(arm, 3); // 1 erosion, no HP spill
        Assert.Equal(5, body.Contribution(arm));
        Assert.Equal(10, player.Hp);
    }
}
