using Roguebane.Core.Content;
using Roguebane.Core.Layout;

namespace Roguebane.Core.Tests;

// §12a worn-armor PART SELECTION (race-first, full-part sprites, CORRECTION #3). Generic + bare
// selection only — pins the candidate-key order the render-integration slice will one day draw
// from (that wiring is still an open OUR-side question, STATUS §17 #15 — not built here).
public class WornArmorBindingTests
{
    private static Body Humanoid(out BodyPart torso, out BodyPart legs)
    {
        var b = new Body();
        b.Add(new BodyPart("armL", Stat.Str, 4));
        b.Add(new BodyPart("armR", Stat.Str, 4));
        legs = new BodyPart("legs", Stat.Dex, 4);
        torso = new BodyPart("torso", Stat.Con, 6);
        b.Add(legs); b.Add(torso);
        return b;
    }

    [Fact]
    public void UnarmoredPartFallsStraightToBareHealthy()
    {
        var b = Humanoid(out _, out _);
        var keys = WornArmorBinding.SpriteKeys(b, "torso", "human");
        Assert.Equal(new[] { "sprites/gear/worn/human/chest/bare_healthy" }, keys);
    }

    [Fact]
    public void ArmoredPartLeadsWithTypeAndTierThenFallsToBare()
    {
        var b = Humanoid(out _, out _);
        b.Equip(Shops.Plate); // §6c Breastplate: Plate line -> "str", tier 1, CON/chest slot
        var keys = WornArmorBinding.SpriteKeys(b, "torso", "human");
        Assert.Equal(new[]
        {
            "sprites/gear/worn/human/chest/str_1_healthy",
            "sprites/gear/worn/human/chest/bare_healthy",
        }, keys);
    }

    [Fact]
    public void DamagedConditionAddsTheHealthyFallbackRungs()
    {
        var b = Humanoid(out var torso, out _);
        b.Equip(Shops.Plate);
        b.Damage(torso, 6); // Plate's own PartMitigation (2) eats part of the hit -> 2/6 left -> damaged
        var keys = WornArmorBinding.SpriteKeys(b, "torso", "human");
        Assert.Equal(new[]
        {
            "sprites/gear/worn/human/chest/str_1_damaged",
            "sprites/gear/worn/human/chest/str_1_healthy",
            "sprites/gear/worn/human/chest/bare_damaged",
            "sprites/gear/worn/human/chest/bare_healthy",
        }, keys);
    }

    [Fact]
    public void RaceTokenSelectsTheRaceFolder()
    {
        var b = Humanoid(out _, out _);
        b.Equip(Shops.Hide); // §6c Leather Leggings: Leather line -> "dex", tier 1, DEX/legs slot
        Assert.Equal("sprites/gear/worn/elf/legs/dex_1_healthy", WornArmorBinding.SpriteKeys(b, "legL", "elf")[0]);
        Assert.Equal("sprites/gear/worn/human/legs/dex_1_healthy", WornArmorBinding.SpriteKeys(b, "legR", "human")[0]);
    }

    [Fact]
    public void DisabledArmorFallsBackToBareSameAsFigureBinding()
    {
        // §6e: a worn piece whose governing attribute has collapsed sheds its render, same rule
        // FigureBinding.IsArmored already applies.
        var b = Humanoid(out _, out var legs);
        b.Equip(Shops.Hide);
        b.Damage(legs, 4); // DEX pool collapses -> Hide disabled
        var keys = WornArmorBinding.SpriteKeys(b, "legL", "human");
        Assert.Equal(new[]
        {
            "sprites/gear/worn/human/legs/bare_broken",
            "sprites/gear/worn/human/legs/bare_healthy",
        }, keys);
    }

    [Fact]
    public void BootsHasNoWornPartSlot()
    {
        var b = Humanoid(out _, out _);
        b.Equip(Shops.Hide);
        Assert.Empty(WornArmorBinding.SpriteKeys(b, "boots", "human"));
    }

    // CHUNK B item 3: THEMED art (B12) landed for all 7 cores across the 5 races -- the fallback
    // chain now offers the core's own line ahead of generic, falling through to generic then bare
    // when omitted or when that slot has no themed line at all (adept/summoner arms).
    [Fact]
    public void ThemeLeadsTheChainAheadOfGenericThenFallsToBare()
    {
        var b = Humanoid(out _, out _);
        b.Equip(Shops.Plate);
        var keys = WornArmorBinding.SpriteKeys(b, "torso", "human", "barbarian");
        Assert.Equal(new[]
        {
            "sprites/gear/worn/human/chest/barbarian/str_1_healthy",
            "sprites/gear/worn/human/chest/str_1_healthy",
            "sprites/gear/worn/human/chest/bare_healthy",
        }, keys);
    }

    [Fact]
    public void ThemeAddsItsOwnHealthyFallbackRungWhenDamaged()
    {
        var b = Humanoid(out var torso, out _);
        b.Equip(Shops.Plate);
        b.Damage(torso, 6); // -> damaged, same as DamagedConditionAddsTheHealthyFallbackRungs
        var keys = WornArmorBinding.SpriteKeys(b, "torso", "human", "barbarian");
        Assert.Equal(new[]
        {
            "sprites/gear/worn/human/chest/barbarian/str_1_damaged",
            "sprites/gear/worn/human/chest/barbarian/str_1_healthy",
            "sprites/gear/worn/human/chest/str_1_damaged",
            "sprites/gear/worn/human/chest/str_1_healthy",
            "sprites/gear/worn/human/chest/bare_damaged",
            "sprites/gear/worn/human/chest/bare_healthy",
        }, keys);
    }

    [Fact]
    public void OmittingThemeKeepsTheOldGenericOnlyChain()
    {
        var b = Humanoid(out _, out _);
        b.Equip(Shops.Plate);
        var keys = WornArmorBinding.SpriteKeys(b, "torso", "human");
        Assert.Equal(new[]
        {
            "sprites/gear/worn/human/chest/str_1_healthy",
            "sprites/gear/worn/human/chest/bare_healthy",
        }, keys);
    }

    [Fact]
    public void UnarmoredPartIgnoresThemeEntirely()
    {
        // No worn armor -> no armored rung at all, themed or generic -- straight to bare, same as
        // UnarmoredPartFallsStraightToBareHealthy.
        var b = Humanoid(out _, out _);
        var keys = WornArmorBinding.SpriteKeys(b, "torso", "human", "barbarian");
        Assert.Equal(new[] { "sprites/gear/worn/human/chest/bare_healthy" }, keys);
    }

    [Theory]
    [InlineData("dwarf")]
    [InlineData("elf")]
    [InlineData("half_giant")]
    [InlineData("halfling")]
    [InlineData("human")]
    public void EveryNewRaceResolvesTheSameThemedChainShapeAsHuman(string race)
    {
        // The 3 v6 races (dwarf/half_giant/halfling) plus the 2 original (human/elf) all shipped the
        // same mgcb worn tree (CHUNK B item 1) -- SpriteKeys has no race-specific branching, so this
        // pins that the race token alone drives the folder, same shape for all 5.
        var b = Humanoid(out _, out _);
        b.Equip(Shops.Plate);
        var keys = WornArmorBinding.SpriteKeys(b, "torso", race, "barbarian");
        Assert.Equal(new[]
        {
            $"sprites/gear/worn/{race}/chest/barbarian/str_1_healthy",
            $"sprites/gear/worn/{race}/chest/str_1_healthy",
            $"sprites/gear/worn/{race}/chest/bare_healthy",
        }, keys);
    }
}
