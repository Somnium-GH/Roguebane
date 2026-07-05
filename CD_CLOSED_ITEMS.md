# CD closed items — engine-side confirmations for Claude Design

**Purpose.** Claude Design's dev-loop memory (`DEV_LOOP_MEMORY`) tracks features CD shipped in the
manifest/assets that were "engine pending" — waiting on the game engine to consume them. This file is the
REVERSE of `outputs/CLAUDE_DESIGN_issues.md`: when the engine (the loop) SHIPS one of those items, it is
recorded HERE so CD, reading the repo, can CONFIRM-TO-CLOSE and clear the item from its own dev-memory.
CD's memory tracks what CD shipped, not what the engine caught up on — so those entries go stale silently;
this file is how the engine tells CD "you can let that one go."

**Protocol (loop, per pass — see `.claude/loop.md` step 5).**
- When a slice implements the engine consumer for something CD's dev-memory opened as engine-pending, ADD an
  entry: CD dev-memory item # (if known) · what shipped · `file:symbol` evidence · date.
- This is a CONFIRMATION log for CD, not a work queue. CD clears its dev-memory on seeing the entry.
- Entries are DURABLE (unlike the outbox, which holds open items only) — they are the audit trail CD reads
  across drops. Git history is the deeper archive.

## Closed (engine shipped — CD may clear these from dev-memory)

**2026-07-05 — audited CD dev-memory against the actual engine. These were listed engine-pending but have
already SHIPPED engine-side:**
- **#20 Merchant screen consumer** — real manifest-driven Merchant screen: click-to-buy tiles, pagination,
  buy/leave wired, purchases land in expedition inventory. The old popover stopgap is gone.
  `Game1.ManifestRenderer.cs::MerchantSections()` / `WareRects()`; merchant input in `Game1.cs`.
- **#17 `states` resolution + draw** — per-element/template interaction states resolved AND drawn
  (pickerCard / actionCard / invCard / loadoutCard families, incl. equipped/disabled/equippable/locked +
  hover/active/idle). `Game1.ManifestRenderer.cs::InvCardState()` + family resolution.
- **#18 per-side borders (`border.sides`)** — non-uniform borders drawn from the manifest `sides` array
  (left-edge accents, top-borders, dividers). `Game1.Canvas.cs::Border(..., string[]? sides)`,
  `LayoutManifest.Border.Sides`.
- **#21 shield-pool pip instancing + wrap** — N pips instanced from the live `ShieldPool` count; row wraps
  on width overflow. `Game1.ManifestRenderer.cs::ListData()` (`ShieldPool.points`) + `ListLayout.Cells()`.
- **#29 `core.label` datum** — engine exposes the "CORE GRUNT" label form, distinct from `core.name`.
  `Game1.ManifestRenderer.cs::ResolveScreenBind()` case `core.label`.
- **#4 list pagination (part of)** — data-driven list paging shipped: merchant 3-per-page pager +
  `ListLayout` overflow wrap. `Game1.ManifestRenderer.cs` `_merchantPage` / `SectionsPerPage`. (A generic
  per-item `PER_PAGE` manifest field is still not a thing — paging is per-screen today.)

**Still engine-pending (NOT closed — listed so CD does NOT over-clear):**
- **#35 recursive `parent`-box resolution** — the `parent` field is brand new; the engine consumer
  (`ScreenLayout.Resolve`) is being built. Closes when parented children place correctly.
- **#30 glow/pulse primitive** — no engine draw of `pulse`/`glow`/`style.pulse` yet (only the hardcoded
  targeting-reticle frame cycle).
- **#32 worn-armor part DRAW** — `WornArmorBinding.SpriteKeys()` resolves keys but is not yet wired into the
  figure draw; back-mounts unrendered.
