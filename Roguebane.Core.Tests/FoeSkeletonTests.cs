using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// CHUNK D item 2's sixth roster foe (FOES.md, Skeleton): the roster's ONE deliberate weapon/technique
// stat mismatch (Iron Dagger is Stat.Dex; Jab is Stat.Str), left exactly as FOES.md specifies (Doug
// 2026-07-07). Corrects the ruling's own technical premise: Caster.Activate's
// `Consulted(technique).Count == 0 => return false` gate (Caster.cs) fires for ANY Consults != None
// technique before Body.Activate ever runs -- proven generically by
// WeaponTests.WithoutAWeaponAConsultingTechniqueCannotActivate, and pinned here through real
// Foes.Skeleton content: Jab never activates at all (zero offense), not "activates for 0 weapon-scaled
// damage" as the ruling assumed.
public class FoeSkeletonTests
{
    private static Fighter Bystander() => new(new Body(), maxHp: 500); // survives; applies no pressure

    [Fact]
    public void JabNeverActivatesOnTheSkeletonBecauseTheDaggerIsTheWrongStatWeapon()
    {
        var foe = Foes.Skeleton("skeleton");
        var caster = new Caster(new Body(), foe);

        Assert.Empty(foe.Frame!.Consulted(Techniques.Jab)); // Dagger is Dex; Jab consults Str -- nothing to swing
        Assert.False(caster.Activate(Techniques.Jab));
    }

    [Fact]
    public void TheSkeletonDealsZeroDamageAcrossAFullBattleSinceItsOnlyArsenalTechniqueNeverArms()
    {
        var foe = Foes.Skeleton("skeleton");
        var player = Bystander();
        var battle = new Battle(new Caster(new Body()), new Encounter("e", foe, foePartAim: true), player);

        for (var i = 0; i < 500; i++) battle.Step(); // Battle's ctor already tried and failed to activate Jab

        Assert.Equal(500, player.Hp); // no activation ever happened -- the Skeleton lands zero hits, not weak ones
    }

    [Fact]
    public void BreakingTheSkeletonsArmTriggersBrittleThroughRealFoesSkeletonContent()
    {
        var foe = Foes.Skeleton("skeleton");
        var frame = foe.Frame!;
        var arm = frame.Parts[0]; // Str 2

        var attackerBody = new Body();
        attackerBody.Add(new BodyPart("player-arm", Stat.Str, 12));
        var attacker = new Caster(attackerBody, foe);
        var smash = new Technique("smash", Stat.Str, 1, TechniqueKind.Timered, Cooldown: 40, Power: 2);
        attacker.Activate(smash, auto: false);
        for (var i = 0; i < 40; i++) attacker.Step(); // charge to ready
        attacker.Aim(smash, foe, arm);

        Assert.True(attacker.Fire(smash)); // exactly breaks the 2-capacity arm

        Assert.True(foe.EffectTriggered);
        Assert.True(attacker.IsReady(smash)); // Brittle's refund -- ready again immediately, not on cooldown
    }
}
