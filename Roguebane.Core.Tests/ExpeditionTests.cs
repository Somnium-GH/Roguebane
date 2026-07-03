using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// G4/G5 integration: the Expedition is the real loop — pick a node, fight what you land on, race the
// war party to the castle. Combat, the map, supplies and the clock compose into one win/lose result.
public class ExpeditionTests
{
    // Unattended drive-to-completion harness. The player now requires an explicit aim to fire (no
    // front fallback), so the harness re-aims every active technique at the first standing foe each
    // tick — the stand-in for a human keeping targets pointed — and turns AUTO on so a target persists.
    private static Expedition FullLoadout()
    {
        var exp = Sessions.Expedition();
        foreach (var t in exp.Equipment) exp.Toggle(t);
        exp.SetAuto(true); // global AUTO on so the re-aimed targets persist
        return exp;
    }

    private static void AimAll(Expedition exp)
    {
        var foe = exp.Enemy;
        if (foe is null) return;
        foreach (var t in exp.Equipment) if (exp.IsActive(t)) exp.Aim(t, foe);
    }

    // Fight to completion, then REDEPLOY back to the chart (a cleared node now holds at Cleared until
    // the player redeploys). The one test that checks the Cleared state inlines the fight instead.
    private static void FightToEnd(Expedition exp)
    {
        var guard = 0;
        while (exp.State == ExpeditionState.Fighting && guard++ < 10000) { AimAll(exp); exp.Tick(); }
        exp.Redeploy();
    }

    // FSM pin: powering a technique reserves + charges it but does NOT target. With no aim it holds at
    // the ready and fires nothing — nothing auto-targets or falls back to a front.
    [Fact]
    public void PoweringATechniqueDoesNotFireUntilTargeted()
    {
        var exp = Sessions.Expedition();
        exp.Toggle(Techniques.Jab);          // power only — no target
        Assert.False(exp.IsAuto()); // AUTO off by default

        exp.Enter("a2");                     // a skirmish; foe HP only ever moves by the player's hand
        var foe = exp.Enemy!;
        var hp = foe.Hp;

        for (var i = 0; i < 200; i++) exp.Tick(); // long past Jab's cooldown
        Assert.True(exp.IsReady(Techniques.Jab)); // charged and holding
        Assert.Equal(hp, foe.Hp);                 // untargeted => never fired
    }

    // FSM pin: targeting a powered technique IS the trigger — once charged + aimed it fires (no fire
    // button). Aiming never flips AUTO on.
    [Fact]
    public void TargetingAPoweredTechniqueFiresAtIt()
    {
        var exp = Sessions.Expedition();
        exp.Toggle(Techniques.Jab);
        exp.Enter("a2");
        var foe = exp.Enemy!;
        var hp = foe.Hp;

        exp.Aim(Techniques.Jab, foe);
        Assert.False(exp.IsAuto()); // aiming left AUTO untouched

        for (var i = 0; i < 120; i++) exp.Tick(); // charge to ready -> fires at the aim
        Assert.True(foe.Hp < hp);                 // the target drove the shot
    }

    // FSM: dismissing the target (right-click) keeps the technique active + AUTO off, just drops the aim.
    [Fact]
    public void DismissingTargetKeepsTheTechniqueActiveAndAutoOff()
    {
        var exp = Sessions.Expedition();
        exp.Toggle(Techniques.Jab);
        exp.Enter("a2");
        exp.Aim(Techniques.Jab, exp.Enemy!);

        exp.ClearAim(Techniques.Jab);
        Assert.True(exp.IsActive(Techniques.Jab));   // still active
        Assert.False(exp.IsAuto());    // AUTO untouched
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
    public void TheBattleExposesItsEncounterForRendering()
    {
        var exp = FullLoadout();
        exp.Enter("a2");
        Assert.NotNull(exp.Battle!.Encounter);
        Assert.NotNull(exp.Battle.Encounter.Enemy); // the shell paints these
    }

    [Fact]
    public void ClearingASkirmishHoldsAtClearedUntilRedeploy()
    {
        var exp = FullLoadout();
        exp.Enter("a2");
        var guard = 0;
        while (exp.State == ExpeditionState.Fighting && guard++ < 10000) { AimAll(exp); exp.Tick(); }
        Assert.Equal(ExpeditionState.Cleared, exp.State); // no silent auto-return to the map

        Assert.False(exp.Enter("b")); // can't jump until redeployed
        exp.Redeploy();
        Assert.Equal(ExpeditionState.Choosing, exp.State); // now back on the chart
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
    public void MerchantSellsOneHpBuysAndAPremiumFullHeal()
    {
        // §12 (2026-07-02): a 1-HP buy at the per-HP price, and a FULL repair at a premium.
        var exp = FullLoadout();
        exp.Enter("a2"); FightToEnd(exp); // earns spoils
        exp.Player.Damage(3);             // carry a wound in
        exp.Enter("b");

        var price = exp.HealPricePerHp;
        Assert.InRange(price, 1, 2);                 // §10: 1 HP per randomized, loot-bounded cost
        var before = exp.Player.Hp;
        var gold = exp.Gold;
        Assert.True(exp.BuyHeal());
        Assert.Equal(before + 1, exp.Player.Hp);     // exactly one HP per buy
        Assert.Equal(gold - price, exp.Gold);

        var missing = exp.Player.MaxHp - exp.Player.Hp;
        Assert.Equal(missing * (price + 1), exp.FullHealPrice); // premium over the per-HP path
        if (exp.Gold >= exp.FullHealPrice)
        {
            gold = exp.Gold;
            var full = exp.FullHealPrice;
            Assert.True(exp.BuyFullHeal());
            Assert.Equal(exp.Player.MaxHp, exp.Player.Hp);
            Assert.Equal(gold - full, exp.Gold);
        }
    }

    [Fact]
    public void MerchantStocksSeededSuppliesAndCharge()
    {
        // §12 resource stock: small seeded quantities, stable per node; buying tops the resource up
        // (capped) and consumes the stock.
        var exp = FullLoadout();
        exp.Enter("a2"); FightToEnd(exp);
        exp.Enter("b");
        Assert.Equal(exp.SuppliesStock, exp.SuppliesStock); // seeded => stable
        Assert.InRange(exp.SuppliesStock, 1, 3);
        Assert.InRange(exp.ChargeStock, 1, 2);

        var stock = exp.SuppliesStock;
        var supplies = exp.Map.Supplies;
        if (exp.Gold >= exp.SuppliesPrice && supplies < exp.Map.MaxSupplies)
        {
            Assert.True(exp.BuySupplies());
            Assert.Equal(supplies + 1, exp.Map.Supplies);
            Assert.Equal(stock - 1, exp.SuppliesStock);
        }
        // Charge starts full in this loadout, so a top-up must refuse rather than overfill.
        if (exp.Charge >= exp.MaxCharge) Assert.False(exp.BuyCharge());
    }

    [Fact]
    public void MerchantHealPriceIsStablePerNode()
    {
        var exp = FullLoadout();
        exp.Enter("a2"); FightToEnd(exp);
        exp.Enter("b");
        Assert.Equal(exp.HealPricePerHp, exp.HealPricePerHp); // same merchant => same price, reproducible
    }


    [Fact]
    public void TheMerchantSellsAWeaponIntoTheStashPack()
    {
        var exp = FullLoadout();
        exp.Enter("a1"); FightToEnd(exp); // a hold pays 4 gold
        exp.Enter("b");
        Assert.True(exp.AtMerchant);

        var gold = exp.Gold;
        Assert.True(exp.BuyWeapon(Armory.Dagger)); // price reserve(1)+power(1) = 2
        Assert.Equal(gold - 2, exp.Gold);
        Assert.Contains(Armory.Dagger, exp.Stash.Weapons);
        Assert.DoesNotContain(Armory.Dagger, exp.OfferedWeapons); // sold out of the stock
    }

    [Fact]
    public void TheMerchantSellsArmorIntoTheStashPack()
    {
        var exp = FullLoadout();
        exp.Enter("a1"); FightToEnd(exp);
        exp.Enter("b");

        Assert.True(exp.BuyArmor(Shops.Plate)); // price value(2)+2 = 4
        Assert.Contains(Shops.Plate, exp.Stash.Armor);
    }

    // §12 receiving (LOCKED 2026-07-03): every ware category buys into the RUN inventory —
    // technique -> palette pool, minion -> minion inventory, rune -> rune bag. Slotting stays
    // the Equipment screen's job. Category presence per node is the seeded roll's business, so
    // the technique/rune tests ride nodes whose rolls stock them ("m2" / "mk20" — deterministic).
    private static Expedition MerchantAt(string nodeId)
    {
        var body = Sessions.DemoBody();
        var caster = new Caster(body, maxCharge: Forge.MagicCapacity(body), requireAim: true);
        var nodes = new[]
        {
            new MapNode("camp", NodeType.Camp, nodeId),
            new MapNode(nodeId, NodeType.Merchant, "castle"),
            new MapNode("castle", NodeType.Castle),
        };
        var map = new CityMap(nodes, "camp", supplies: 8, marchLength: 9);
        var exp = new Expedition(Forge.PlayerFighter(body), caster, Techniques.All, map);
        exp.Stash.AddGold(50);
        exp.Enter(nodeId);
        Assert.True(exp.AtMerchant);
        return exp;
    }

    [Fact]
    public void TheMerchantSellsATechniqueIntoTheRunInventory()
    {
        var exp = MerchantAt("m2"); // this node's seeded roll stocks the technique section
        Assert.NotEmpty(exp.OfferedTechniques);
        var pick = exp.OfferedTechniques[0];
        var gold = exp.Gold;

        Assert.True(exp.BuyTechnique(pick));
        Assert.Contains(pick, exp.Stash.Techniques);          // landed in the palette pool
        Assert.DoesNotContain(pick, exp.OfferedTechniques);   // cleared from the shelf
        Assert.Equal(gold - Expedition.Price(pick), exp.Gold);
    }

    [Fact]
    public void TheMerchantSellsAMinionIntoTheRunInventory()
    {
        var exp = MerchantAt("b"); // minions section stocks on this roll
        Assert.NotEmpty(exp.OfferedMinions);
        var pick = exp.OfferedMinions[0];

        Assert.True(exp.BuyMinion(pick));
        Assert.Contains(pick, exp.Stash.Minions);
        Assert.DoesNotContain(pick, exp.OfferedMinions);
    }

    [Fact]
    public void TheMerchantSellsARuneIntoTheBag()
    {
        var exp = MerchantAt("mk20"); // this node's seeded roll stocks the rune section
        Assert.NotEmpty(exp.OfferedMarks);
        var pick = exp.OfferedMarks[0];

        Assert.True(exp.BuyMark(pick));
        Assert.Contains(pick, exp.Stash.Marks);
        Assert.DoesNotContain(pick, exp.OfferedMarks);
    }

    [Fact]
    public void WarePurchasesRejectWhenGoldRunsShort()
    {
        var exp = MerchantAt("b");
        while (exp.Gold > 0) exp.Stash.TrySpend(1); // broke
        Assert.NotEmpty(exp.OfferedMinions);

        Assert.False(exp.BuyMinion(exp.OfferedMinions[0]));
        Assert.Empty(exp.Stash.Minions);
    }

    [Fact]
    public void BoughtGearEquipsOntoTheBodyOutOfCombat()
    {
        var exp = FullLoadout();
        exp.Enter("a1"); FightToEnd(exp); // gold
        exp.Enter("b"); exp.BuyWeapon(Armory.Dagger); // into the pack

        Assert.True(exp.EquipWeapon(Armory.Dagger));   // Choosing -> legal
        Assert.Contains(Armory.Dagger, exp.Player.Body.Hands);
        Assert.DoesNotContain(Armory.Dagger, exp.Stash.Weapons); // left the pack
    }

    [Fact]
    public void GearCannotBeEquippedMidFight()
    {
        var exp = FullLoadout();
        exp.Enter("a1"); FightToEnd(exp);
        exp.Enter("b"); exp.BuyWeapon(Armory.Dagger);
        exp.Enter("c2"); // a fight starts
        Assert.Equal(ExpeditionState.Fighting, exp.State);
        Assert.False(exp.EquipWeapon(Armory.Dagger)); // sealed during combat
        Assert.True(exp.Stash.HasWeapon(Armory.Dagger));
    }

    [Fact]
    public void GearCannotBeBoughtAwayFromAMerchant()
    {
        var exp = FullLoadout();
        exp.Enter("a1"); // a fight, not a merchant
        Assert.False(exp.BuyWeapon(Armory.Dagger));
        Assert.False(exp.BuyArmor(Shops.Plate));
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
        Assert.Equal(CityMapOutcome.CastleCracked, exp.Map.Outcome);
    }

    // Bank-on-CLEAR: landing a resource-hold banks nothing until its fight is won — fleeing it banks none.
    [Fact]
    public void AHoldBanksOnlyOnceItsFightIsCleared()
    {
        var exp = FullLoadout();
        exp.Enter("a1"); // a resource-hold fight
        Assert.Equal(ExpeditionState.Fighting, exp.State);
        Assert.Equal(0, exp.Map.SupportBank); // not banked on arrival
        FightToEnd(exp);
        Assert.Equal(1, exp.Map.SupportBank); // banked on clear

        var fled = FullLoadout();
        fled.Enter("a1");
        fled.Retreat();
        Assert.Equal(0, fled.Map.SupportBank); // abandoned mid-fight -> nothing banked
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
