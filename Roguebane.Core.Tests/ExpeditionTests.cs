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
        // Activation default refinement [LOCKED 2026-07-09]: Timered techniques go cold on every
        // encounter rearm now, so the harness must re-toggle them each fight like a real player would --
        // filtered to inactive so an already-active Sustained default (Brace) is never double-toggled off.
        foreach (var t in exp.Equipment) if (!exp.IsActive(t)) exp.Toggle(t);
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

    // Activation default refinement [LOCKED 2026-07-09, Doug: "only shield auto-activates so that you
    // don't get hit the first time"]: entering ANY encounter (first included) auto-powers equipped
    // Sustained techniques but leaves Timered attacks cold until the player explicitly activates them.
    [Fact]
    public void EnteringACombatNodeAutoActivatesSustainedButNotTimeredTechniques()
    {
        var exp = Sessions.Expedition(); // nothing toggled by hand -- pure encounter-entry default
        Assert.True(exp.Enter("a2"));

        Assert.True(exp.IsActive(Techniques.Brace));  // Sustained: shield auto-on
        Assert.False(exp.IsActive(Techniques.Jab));   // Timered: still needs an explicit activation
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

    // FIXED 2026-07-10, Doug: "equipment should become enabled after combat" -- Cleared (right after a
    // win, before Redeploy) is out of combat exactly as much as Choosing, so the roster/gear mutation
    // gates must accept both, not Choosing alone.
    [Fact]
    public void RosterAndGearCanBeEditedWhileHoldingAtCleared()
    {
        var exp = FullLoadout();
        exp.Enter("a2");
        var guard = 0;
        while (exp.State == ExpeditionState.Fighting && guard++ < 10000) { AimAll(exp); exp.Tick(); }
        Assert.Equal(ExpeditionState.Cleared, exp.State);

        Assert.True(exp.UnequipTechnique(Techniques.Bandage));
        Assert.True(exp.EquipTechnique(Techniques.Bandage));
        Assert.True(exp.ReorderTechnique(Techniques.Bandage, 0));
        Assert.True(exp.UnequipWeapon(Armory.Dagger)); // DemoBody wields the Dagger already
        Assert.True(exp.EquipWeapon(Armory.Dagger));
    }

    // Bug fix (2026-07-04, Doug): a technique aimed at THIS fight's foe must not carry a stale lock
    // into the NEXT one — Redeploy() clears every equipped technique's aim.
    [Fact]
    public void RedeployClearsStaleAim()
    {
        var exp = FullLoadout();
        exp.Enter("a2");
        var foe = exp.Enemy!;
        exp.Aim(Techniques.Jab, foe);
        Assert.Same(foe, exp.AimOf(Techniques.Jab));

        var guard = 0;
        while (exp.State == ExpeditionState.Fighting && guard++ < 10000) { AimAll(exp); exp.Tick(); }
        exp.Redeploy();

        Assert.Null(exp.AimOf(Techniques.Jab));
    }

    // Same stale-lock hazard on the RETREAT path (an active fight broken off, not cleared).
    [Fact]
    public void RetreatClearsStaleAim()
    {
        var exp = Sessions.Expedition();
        exp.Toggle(Techniques.Jab);
        exp.Enter("a2");
        exp.Aim(Techniques.Jab, exp.Enemy!);
        Assert.NotNull(exp.AimOf(Techniques.Jab));

        exp.Retreat();

        Assert.Null(exp.AimOf(Techniques.Jab));
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


    // Bug-queue root-cause (2026-07-05 Doug note, "pager doesn't indicate page 2"): Seed(nodeId) is
    // a pure function of the node id STRING (no leg index / campaign salt), and Maps.StandardLegNodes
    // reuses the literal id "b" for the merchant on every leg. So node "b" rolls the SAME stock every
    // visit, forever: exactly 3 sections (weapons/armor/minions; techniques+runes both roll false at
    // this seed). SectionsPerPage is 3, so PageCount is always 1 at this node -- page 2 is mechanically
    // unreachable in live play, not a broken indicator (Pager/bind/click all verified correct by hand
    // and via a live RB_SMOKE screenshot).
    [Fact]
    public void MerchantNodeBNeverRollsMoreThanThreeSections()
    {
        var exp = FullLoadout();
        exp.Enter("a2"); FightToEnd(exp);
        exp.Enter("b");
        Assert.Empty(exp.OfferedTechniques);
        Assert.Empty(exp.OfferedMarks);
        Assert.NotEmpty(exp.OfferedWeapons);
        Assert.NotEmpty(exp.OfferedArmor);
        Assert.NotEmpty(exp.OfferedMinions);
    }

    // Fixed (2026-07-07, Doug's HIGH PRIORITY #1, global fix): Expedition.Seed(nodeId) now folds in
    // Campaign's leg index, so the SAME node id "b" on two different legs of the SAME campaign rolls
    // DIFFERENT stock -- no more identical merchant every leg/run. Two bare, leg-0 Expeditions (the old
    // test's setup) are still identical by design: that's within-leg reproducibility, not the bug.
    [Fact]
    public void MerchantNodeBRollsDifferentStockAcrossCampaignLegs()
    {
        var c = Sessions.NewCampaign();
        foreach (var t in Techniques.All) c.Toggle(t);
        c.SetAuto(true);

        void Step(string node)
        {
            c.Enter(node);
            // Activation default refinement [LOCKED 2026-07-09]: Timered techniques go cold on every
            // encounter rearm now, so re-toggle each fight like a real player would.
            foreach (var t in c.Current.Equipment) if (!c.IsActive(t)) c.Toggle(t);
            var guard = 0;
            while (c.Current.State == ExpeditionState.Fighting && guard++ < 10000)
            {
                if (c.Enemy is { } foe) foreach (var t in Techniques.All) if (c.IsActive(t)) c.Aim(t, foe);
                c.Tick();
            }
            c.Redeploy();
        }

        Step("a2");
        c.Enter("b"); // leg 0's merchant
        var (weapons0, armor0, minions0) = (c.Current.OfferedWeapons, c.Current.OfferedArmor, c.Current.OfferedMinions);

        Step("c1");
        Step("castle"); // wins leg 0 -> advances to leg 1
        Assert.Equal(1, c.LegIndex);

        Step("a2");
        c.Enter("b"); // leg 1's merchant -- same node id "b", different leg
        var (weapons1, armor1, minions1) = (c.Current.OfferedWeapons, c.Current.OfferedArmor, c.Current.OfferedMinions);

        Assert.False(
            weapons0.SequenceEqual(weapons1) &&
            armor0.SequenceEqual(armor1) &&
            minions0.SequenceEqual(minions1));
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
    private static Expedition MerchantAt(string nodeId, int maxSummons = -1, int minionCap = 0)
    {
        var body = Sessions.DemoBody();
        var caster = new Caster(body, maxCharge: Forge.MagicCapacity(body), requireAim: true,
            minionCap: minionCap, maxSummons: maxSummons);
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

    // §12/§9: Summons is the finite deploy resource (not a ware category) -- the merchant tops it up
    // same as Supplies/Charge (MerchantStocksSeededSuppliesAndCharge above), just gated by MaxSummons
    // instead of a section roll. No prior coverage existed for this buy path.
    [Fact]
    public void MerchantRefillsSummonsUpToTheCap()
    {
        var exp = MerchantAt("b", maxSummons: 3, minionCap: 1);
        Assert.True(exp.SummonMinion(Minions.Skeleton)); // spends 1 of 3
        Assert.Equal(2, exp.Summons);
        Assert.InRange(exp.SummonsStock, 1, 2);

        var stock = exp.SummonsStock;
        var gold = exp.Gold;
        Assert.True(exp.BuySummons());
        Assert.Equal(3, exp.Summons);
        Assert.Equal(gold - exp.SummonsPrice, exp.Gold);
        Assert.Equal(stock - 1, exp.SummonsStock);

        Assert.False(exp.BuySummons()); // already full -- refuses rather than overfill
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
        exp.UnequipWeapon(Armory.Sword); // DemoBody starts with both hands full -- free one first

        Assert.True(exp.EquipWeapon(Armory.Dagger));   // Choosing -> legal
        Assert.Contains(Armory.Dagger, exp.Player.Body.Hands);
        Assert.DoesNotContain(Armory.Dagger, exp.Stash.Weapons); // left the pack
    }

    // HIGH PRIORITY bug #1: the GEAR tab used to build its rows by concatenating Body + Stash live,
    // so equipping/unequipping (which MOVES a piece between them) reshuffled every other piece's
    // screen-slot index too, mis-routing clicks. Pins that the Expedition seeds a stable roster from
    // the starting kit at construction, and that it survives an equip/unequip round-trip untouched.
    [Fact]
    public void GearRosterSeedsFromTheStartingKitAndSurvivesAnEquipUnequipCycle()
    {
        var exp = FullLoadout(); // DemoBody wields Sword then Dagger, no armor
        var seeded = exp.Stash.WeaponRoster.ToList();
        Assert.Equal(new[] { Armory.Sword, Armory.Dagger }, seeded);
        Assert.Empty(exp.Stash.ArmorRoster);

        exp.Enter("a1"); FightToEnd(exp);
        exp.Enter("b");
        exp.UnequipWeapon(Armory.Sword); // Sword: Body -> pack
        Assert.Equal(seeded, exp.Stash.WeaponRoster); // roster order unchanged by the move

        Assert.True(exp.EquipWeapon(Armory.Sword)); // Sword: pack -> Body again
        Assert.Equal(seeded, exp.Stash.WeaponRoster); // still unchanged
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
        Assert.InRange(exp.Gold, 2, 4); // a skirmish pays 2-4 (randomized, was flat 3)
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

    // §6e technique roster: equip/unequip is the bar-sync mechanism (Equipment screen toggles
    // membership, the encounter bar just reads Equipment) — never attribute reservation, which is
    // Activate/Deactivate's job alone (the "Reservation timing" DESIGN_SPEC lock).
    private static Expedition CappedExpedition(int slots, params Technique[] starting)
    {
        var body = Sessions.DemoBody();
        var caster = new Caster(body, maxCharge: Forge.MagicCapacity(body), requireAim: true);
        return new Expedition(Forge.PlayerFighter(body), caster, starting, Maps.StandardLeg(),
            techniqueSlots: slots);
    }

    [Fact]
    public void EquippingATechniqueAddsItToTheBar()
    {
        var exp = CappedExpedition(2, Techniques.Jab);
        Assert.True(exp.EquipTechnique(Techniques.Cleave));
        Assert.Contains(Techniques.Cleave, exp.Equipment);
    }

    [Fact]
    public void EquippingAnAlreadyEquippedTechniqueIsANoOp()
    {
        var exp = CappedExpedition(2, Techniques.Jab);
        Assert.False(exp.EquipTechnique(Techniques.Jab));
        Assert.Single(exp.Equipment);
    }

    [Fact]
    public void EquipRefusesPastTheChassisKitCap()
    {
        var exp = CappedExpedition(1, Techniques.Jab); // Kit.Count-sized cap, already full
        Assert.False(exp.EquipTechnique(Techniques.Cleave));
        Assert.DoesNotContain(Techniques.Cleave, exp.Equipment);
    }

    [Fact]
    public void UnequippingATechniqueRemovesItFromTheBar()
    {
        var exp = CappedExpedition(2, Techniques.Jab, Techniques.Cleave);
        Assert.True(exp.UnequipTechnique(Techniques.Jab));
        Assert.DoesNotContain(Techniques.Jab, exp.Equipment);
        Assert.Contains(Techniques.Cleave, exp.Equipment); // unslot compacts left, no holes — not a wipe
    }

    [Fact]
    public void UnequippingAPoweredTechniqueDeactivatesItFirst()
    {
        var exp = CappedExpedition(2, Techniques.Jab, Techniques.Cleave);
        exp.Toggle(Techniques.Jab); // power it on
        Assert.True(exp.IsActive(Techniques.Jab));

        Assert.True(exp.UnequipTechnique(Techniques.Jab));
        Assert.False(exp.IsActive(Techniques.Jab)); // no dangling FSM reference to an unequipped card
    }

    [Fact]
    public void UnequippingAnUnequippedTechniqueIsANoOp()
    {
        var exp = CappedExpedition(2, Techniques.Jab);
        Assert.False(exp.UnequipTechnique(Techniques.Cleave));
    }

    [Fact]
    public void EquipAndUnequipAreOutOfCombatOnly()
    {
        var exp = CappedExpedition(2, Techniques.Jab);
        exp.Enter("a1"); // State -> Fighting
        Assert.False(exp.EquipTechnique(Techniques.Cleave));
        Assert.False(exp.UnequipTechnique(Techniques.Jab));
        Assert.Single(exp.Equipment); // untouched
    }

    [Fact]
    public void ReorderMovesATechniqueToTheGivenSlot()
    {
        var exp = CappedExpedition(3, Techniques.Jab, Techniques.Cleave, Techniques.Lunge);
        Assert.True(exp.ReorderTechnique(Techniques.Lunge, 0)); // drag the 3rd slot to the front
        Assert.Equal(
            new[] { Techniques.Lunge, Techniques.Jab, Techniques.Cleave },
            exp.Equipment);
    }

    [Fact]
    public void ReorderClampsAnOutOfRangeIndexToTheLastSlot()
    {
        var exp = CappedExpedition(3, Techniques.Jab, Techniques.Cleave, Techniques.Lunge);
        Assert.True(exp.ReorderTechnique(Techniques.Jab, 99));
        Assert.Equal(
            new[] { Techniques.Cleave, Techniques.Lunge, Techniques.Jab },
            exp.Equipment);
    }

    [Fact]
    public void ReorderOfAnUnequippedTechniqueIsANoOp()
    {
        var exp = CappedExpedition(3, Techniques.Jab, Techniques.Cleave);
        Assert.False(exp.ReorderTechnique(Techniques.Lunge, 0));
    }

    [Fact]
    public void ReorderIsOutOfCombatOnly()
    {
        var exp = CappedExpedition(3, Techniques.Jab, Techniques.Cleave);
        exp.Enter("a1"); // State -> Fighting
        Assert.False(exp.ReorderTechnique(Techniques.Cleave, 0));
    }

    [Fact]
    public void EquipTechniqueNeverReservesAttributes()
    {
        // "Reservation timing" DESIGN_SPEC lock: equipping a technique is pure roster membership —
        // reservation only ever happens on real in-combat Activate, never from being slotted.
        var exp = CappedExpedition(2, Techniques.Jab);
        var before = exp.Player.Body.Capacity(Stat.Str);
        Assert.True(exp.EquipTechnique(Techniques.Cleave));
        Assert.Equal(before, exp.Player.Body.Capacity(Stat.Str));
        Assert.False(exp.IsActive(Techniques.Cleave)); // slotted, not powered
    }
}
