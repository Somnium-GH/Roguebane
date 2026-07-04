using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// §6c armor lines (blessed-initial numbers, structure canon): STR plate soaks the covered part's
// OWN damage per tier; DEX leather stacks global evade per worn piece with the §6 broken-leg hard
// zero; a piece's sustain rides its LINE's governing attribute (§6e disable), not its slot.
public class ArmorLineTests
{
    private static Body Humanoid(out BodyPart armL, out BodyPart armR, out BodyPart legL, out BodyPart legR)
    {
        var b = new Body();
        armL = new BodyPart("armL", Stat.Str, 3);
        armR = new BodyPart("armR", Stat.Str, 3);
        legL = new BodyPart("legL", Stat.Dex, 3);
        legR = new BodyPart("legR", Stat.Dex, 3);
        b.Add(armL); b.Add(armR); b.Add(legL); b.Add(legR);
        b.Add(new BodyPart("head", Stat.Int, 3));
        b.Add(new BodyPart("chest", Stat.Con, 4));
        return b;
    }

    [Fact]
    public void LaddersMatchTheCanonStructure()
    {
        Assert.Equal(10, ArmorLines.Ladders.Count); // 4 plate + 4 leather + 2 robe (no robe limbs)
        Assert.All(ArmorLines.Ladders, l => Assert.Equal(4, l.Count)); // four rungs each
        Assert.All(ArmorLines.All, a => Assert.InRange(a.Tier, 1, 4));
        // Names are canon spot-checks at the ladder ends, not a full pin.
        Assert.Equal("Iron Helm", ArmorLines.PlateHead[0].Name);
        Assert.Equal("Dwarven Steel Breastplate", ArmorLines.PlateChest[3].Name);
        Assert.Equal("Reinforced Leather", ArmorLines.LeatherChest[3].Name);
        Assert.Equal("Humming Circlet", ArmorLines.RobeHead[3].Name);
    }

    [Fact]
    public void PlateSoaksTheCoveredPartsDamagePerTier()
    {
        var b = Humanoid(out var armL, out _, out _, out _);
        b.Equip(ArmorLines.PlateArms[1]); // tier 2 -> -4 part damage
        b.Damage(armL, 4);
        Assert.Equal(3, b.Contribution(armL)); // fully soaked
        b.Damage(armL, 5);
        Assert.Equal(2, b.Contribution(armL)); // 5 - 4 = 1 lands
    }

    [Fact]
    public void PlateStopsSoakingWhenItsGoverningAttributeCollapses()
    {
        var b = Humanoid(out var armL, out var armR, out var legL, out _);
        b.Equip(ArmorLines.PlateLegs[1]); // Steel Greaves (req 4 <= STR 6), GOVERNED by STR
        b.Damage(armL, 9); b.Damage(armR, 9); // both arms break -> STR 0 -> plate disabled
        Assert.False(b.ArmorSustained(ArmorLines.PlateLegs[3]));
        b.Damage(legL, 2);
        Assert.Equal(1, b.Contribution(legL)); // no soak: the piece is red
    }

    [Fact]
    public void BrokenLegZeroesEvasionOutright()
    {
        var b = Humanoid(out _, out _, out var legL, out _);
        // Extra Dex headroom (not on legL/legR) so both pieces' cumulative reserve (4+4=8) sustains
        // (SUSTAIN MODEL shared pool) without perturbing legL's own Contribution math below.
        b.Add(new BodyPart("waist", Stat.Dex, 8));
        b.Equip(ArmorLines.LeatherChest[3]);
        b.Equip(ArmorLines.LeatherArms[3]);
        Assert.Equal(16, b.EvasionPercent()); // 2 pieces x 8%
        b.Damage(legL, 9); // one broken leg -> §6 hard override
        Assert.Equal(0, b.EvasionPercent());
    }

    [Fact]
    public void DisabledLeatherStopsStackingEvade()
    {
        var b = Humanoid(out _, out _, out var legL, out var legR);
        b.Equip(ArmorLines.LeatherHead[0]); // 2%
        Assert.Equal(2, b.EvasionPercent());
        // Damage (not break) both legs to DEX 0 is impossible without breaking one — instead
        // verify via sustain directly: leather governs on DEX.
        Assert.Equal(Stat.Dex, ArmorLines.LeatherHead[0].Governing);
        b.Damage(legL, 9); b.Damage(legR, 9);
        Assert.False(b.ArmorSustained(ArmorLines.LeatherHead[0]));
    }

    [Fact]
    public void RobeCarriesSpellDamagePerPiece()
    {
        Assert.All(ArmorLines.RobeChest.Concat(ArmorLines.RobeHead),
            a => Assert.Equal(2, a.SpellDamage)); // +2 per piece, 2-piece cap
        Assert.All(ArmorLines.PlateChest, a => Assert.Equal(0, a.SpellDamage));
    }

    [Fact]
    public void RobeBonusStacksToTwoPiecesAndDiesWithInt()
    {
        var b = Humanoid(out _, out _, out _, out _);
        Assert.Equal(0, b.SpellDamageBonus);
        b.Equip(ArmorLines.RobeChest[0]);
        Assert.Equal(2, b.SpellDamageBonus);
        b.Equip(ArmorLines.RobeHead[0]); // tier 1: within the head's 3-INT gate
        Assert.Equal(4, b.SpellDamageBonus); // two pieces = the cap
        var head = b.Parts.Single(p => p.Stat == Stat.Int);
        b.Damage(head, 9); // INT collapses -> the robe line is disabled
        Assert.Equal(0, b.SpellDamageBonus);
    }

    [Fact]
    public void RobeBuffsSpellHitsButNeverHeals()
    {
        // §6c: +spell DAMAGE only — an INT bolt lands harder in robes; the heal path is unbuffed.
        static int FoeDamage(bool robes)
        {
            var atk = new Body();
            atk.Add(new BodyPart("head", Stat.Int, 6));
            atk.Add(new BodyPart("chest", Stat.Con, 4));
            if (robes) { atk.Equip(ArmorLines.RobeChest[0]); atk.Equip(ArmorLines.RobeHead[0]); }
            var foe = new Foe("t", 100, new Body());
            var c = new Caster(atk, foe);
            c.Activate(new Technique("bolt", Stat.Int, 1, TechniqueKind.Sustained, 0, Power: 2));
            c.Step();
            return 100 - foe.Hp;
        }
        Assert.Equal(FoeDamage(false) + 4, FoeDamage(true));
    }
}
