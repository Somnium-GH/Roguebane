# Claude Design payload — post-drop reconcile, 2026-07-03 pm
_The 2026-07-03 drop (payload #11–#18) was REVIEWED against the repo. Verdict: essentially everything
landed. This file now = confirm-to-close for CD's dev-memory + the few residuals. Prior items #1–#18
are superseded by this reconcile._

## Confirm-to-close (verified LANDED in-repo — CD can clear these from its dev loop)
- #11 reference contract: all `design/NN` refs exactly 1920×1080. ✓
- #12 targeting redesign: 01+08 v2 (hotkey chips, number aim-tag stack, no boxes, pulse frames
  focus_p0–p2, red aiming cursor). ✓
- #13 merchant extents/fit + #15 button/chip content (beginBtn, pager ◀▶, HELD, LEAVE) + #14 per-core
  role chips + #16 technique-icon imageBinds + youAreHere/doomTitle homed + #17 Elf blurb. ✓
- #18 dc.html + scripts shipped (`design/dchtml/` incl. proto/ + DROP_AUDIT.md). ✓
- Older family: card/tile/ware/tech chrome parts, pip templates everywhere, equipment backdrop +
  ✕ CLOSE, citymap ex-overlays homed (castlePanel/fortRows, campaignStrip, packChips, equipmentBtn),
  z normalized to ONE paint-ordinal convention, doomEta bind-gated. ✓ (2026-07-02 payload #1–#10
  all closed by this drop except the two notes below.)

## Residuals (small)
1. **"Repo docs to update in the same PR" didn't arrive** — LAYOUT_CONTRACT/DESIGN_SPEC updates listed
   in your DROP_AUDIT §"Repo docs". The ENGINE side is folding them from DROP_AUDIT (no action needed
   if that was the intent — just don't expect a separate doc drop from us to bounce back).
2. **`reference/screens/*` never landed** — listed in DROP_AUDIT contents; not needed repo-side if
   design/*.png stay the canon; confirm it's CD-internal.
3. **`target_tag.png` deletion is being done repo-side** (both content dirs + game mgcb) per your
   "delete in repo" note — closing the loop here so it isn't re-shipped in a future drop.
4. **Encounter extraction gaps (found by the new `tools/drop_audit.py` at drop time):** Encounter.dc.html
   binds `ShieldPool.count` (the "n/m" text inside the heroShield header) and `ShieldPool.regenPct`
   (the fill inside heroShieldRegen) on anonymous spans, but neither datum reached layout.json —
   the manifest carries only `ShieldPool` / `ShieldPool.points` / `ShieldPool.regen`. The engine
   already renders both live (SHIELD n/m header + regen fill), so this is manifest-completeness
   only: next drop, extract those two spans as bound elements/parts.

## FYI locks from Doug (2026-07-03 pm) — already folded into DESIGN_SPEC
- Core Effect roster from design/05 v2 ADOPTED AS CANON (incl. Called Shot rename); mechanics get a
  dedicated effect-model pass later.
- design/05 v2 STAT BLOCKS **not** adopted — Doug wants a live tuning session first; if a future 05
  re-render can read stats from a handed sample set, ask him for the tuned numbers then.
- Merchant receiving locked: click-to-buy tiles for ALL ware categories; purchases land in inventory
  (technique→palette, minion→minion inventory, rune→rune bag); slotting stays on Equipment.
- §13 aspect-fill is being built engine-side (letterbox dies).
