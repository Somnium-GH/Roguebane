# Prompt — normalize the existing screens into asset format + structure
*Hand to the design/asset agent. Everything below the line is the prompt.*

---

You've already built the Roguebane prototype screens and read `design/DESIGN_SPEC.md` — **don't
rebuild.** Now get the assets those screens use into the **correct format** and a **correct,
full-implementation-ready structure.** Convert and reorganize what exists; don't start over.

**Do:**
- Organize every asset into a clear folder tree + naming convention, grouped by screen and type,
  with state/variant baked into names (e.g. `ui/button/{name}_{state}`, `sprite/body/{part}_{dmg0..3}`,
  `icon/attr/{str|int|dex|con}`). Structure for the **full** asset set so real art drops in later
  with zero restructuring; fit the MonoGame content pipeline.
- Export each asset in a sane format/size for that pipeline. Bind **asset → Core state** where it
  applies, so the render shell maps state to asset (honors the core/shell split).
- Produce a short `ASSET_MANIFEST`: *id · type · screen · drives-from state · size/format · variants
  · status (placeholder/final).*

**Dial back:**
- **Ease off the labeling.** The current screens over-label and highlight things (e.g. coloring a
  limb to show its purpose). That's **prototype scaffolding only** — keep it minimal, clearly
  separate, and strippable. It is NOT the art direction and must not bleed into any asset or string.
- **Art target = HIGH-fidelity retrofantasy EGA/VGA** — a crisp, high-resolution rendering of the
  oldschool 8-bit / EGA-VGA storybook look (Zeliard-style), as if that retro art were upscaled to HD
  while keeping its old-school soul: polished, decent quality, **NOT crude or low-res.** (Separate
  from the label/highlight scaffolding, which is not the style.) Current placeholders are fine while
  we nail format + structure — just don't mistake placeholder crudeness for the target look.

**Guardrail:** no third-party IP (FTL / Shadowbane / PoE) in any asset, filename, or string.

**Output:** the folder tree, the `ASSET_MANIFEST`, the reformatted/restructured assets, and a one-line
note of anything you treated as `[OPEN]`.
