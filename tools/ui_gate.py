#!/usr/bin/env python3
"""UI regression gate: one command that proves the screens still render and still match.

  python tools/ui_gate.py [--update] [--build-dir DIR]

Pipeline:
  1. dotnet build Roguebane.Game into a scratch dir (a running game locks the default output).
  2. Run the driven all-screen smoke (RB_SCREEN=encounter RB_MF=all RB_SMOKE=1): the engine itself
     fails the run on a backdrop-blank screen or a chrome-carrying element that paints nothing.
  3. Parse SMOKE BINDS per screen and compare resolved counts against tools/ui_baseline.json —
     a count DROP means a bind went dead; fail. (A rise is progress: rerun with --update.)
  4. Score every screen's shot against its design PNG with fidelity_diff and compare against the
     baseline — a drop past the tolerance fails. (Scores are placeholder-data-depressed; the
     baseline tracks the CURRENT floor, not the pixel-perfect bar.)

--update rewrites the baseline from this run (use after a slice that legitimately improves things).
Exit 0 = no regression. Nonzero = the printed reasons.
"""
import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
BASELINE = ROOT / "tools" / "ui_baseline.json"
DESIGNS = {
    "encounter": "01-encounter", "equipment": "02-equipment", "citymap": "03-citymap",
    "campaignmap": "04-campaignmap", "newgame": "05-newgame", "merchant": "07-merchant",
}
FIDELITY_TOLERANCE = 2.0  # points a score may dip before failing (antialias/scene jitter head-room)
# Tolerated placeholder zones (Doug 2026-07-03): design/05 v2 stat blocks are NOT adopted — the build
# keeps its values until the live tuning session, so newgame's stat-digit regions are masked out of
# the diff. Element-rect granularity; race/core-card digits live inside card templates and are
# accepted as a known depression until per-part masking is warranted.
# M0.3 (Doug 2026-07-03 late): the ONLY approved mask is the stat-digit zones (tuning session
# pending). The identity-block mask was removed — the DRIVE aligns to the ref state (Grunt) instead.
# Any new mask requires a STATUS-logged human approval FIRST.
MASKS = {
    "newgame": "previewAttrRow,previewHpTile,previewLayoutRow,previewBudgetTile,"
               "previewActionsTile,previewBaysTile",
}


def run(cmd, **kw):
    return subprocess.run(cmd, capture_output=True, text=True, **kw)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--update", action="store_true", help="rewrite the baseline from this run")
    ap.add_argument("--build-dir", default=str(ROOT / ".ui-gate-build"))
    args = ap.parse_args()

    # P0-A.1 reference contract guard: every design ref must be EXACTLY 960K x 540K (integer K).
    # A ref at any other size means the export pipeline regressed — every score taken against it
    # would be warped, so hard-fail before spending a build.
    print("== reference contract ==")
    from PIL import Image
    bad_refs = []
    for png in sorted((ROOT / "design").glob("[0-9][0-9]-*.png")):
        if png.name.startswith("00-assets"):
            continue  # asset SHEETS, not screen refs — the 960K x 540K contract is screens-only
        w, h = Image.open(png).size
        k = w / 960
        if w % 960 or h % 540 or w // 960 != h // 540 or k < 1:
            bad_refs.append(f"{png.name}: {w}x{h} (not 960K x 540K)")
    if bad_refs:
        print("REFERENCE CONTRACT FAILED:")
        for b in bad_refs:
            print("  - " + b)
        return 2
    print(f"  all design/NN refs are 960K x 540K")

    build_dir = Path(args.build_dir)
    print("== build ==")
    r = run(["dotnet", "build", str(ROOT / "Roguebane.Game"), "-v", "q", "-nologo", "-o", str(build_dir)])
    errors = [l for l in (r.stdout + r.stderr).splitlines() if re.search(r"error (CS|MSB|MGCB)", l)]
    if errors:
        print("\n".join(errors[:10]))
        return 2

    # Two driven passes so every screen is validated in its RICHEST reachable state: the encounter
    # drive (mid castle fight) covers encounter/equipment/campaignmap/newgame; the citymap drive
    # stops AT THE MERCHANT, so merchant + citymap validate with live stock/gauges instead of empty
    # state-gated lists. Per-screen numbers are taken from the pass that owns the screen.
    import os
    failures = []
    binds, fidelity, textgeom = {}, {}, {}
    passes = [("encounter", ("encounter", "equipment", "campaignmap", "newgame")),
              ("citymap", ("citymap", "merchant"))]
    for drive, owns in passes:
        print(f"== driven all-screen smoke (RB_SCREEN={drive}) ==")
        shot = build_dir / f"gate-{drive}.png"
        # RB_SIZE pins shots at reference resolution -> the fidelity diff runs 1:1, zero resampling
        env = {"RB_SCREEN": drive, "RB_MF": "all", "RB_SMOKE": "1", "RB_SHOT": str(shot),
               "RB_SIZE": "1920x1080"}
        r = run([str(build_dir / "Roguebane.Game.exe")], cwd=str(build_dir), env={**os.environ, **env})
        print(r.stdout.strip())
        if r.returncode != 0:
            failures.append(f"{drive} drive: smoke exit {r.returncode} (blank screen/element — see above)")
        for m in re.finditer(r"SMOKE BINDS: (\w+) resolved=(\d+)/(\d+)", r.stdout):
            if m.group(1) in owns:
                binds[m.group(1)] = int(m.group(2))
        for m in re.finditer(r"SMOKE TEXTGEOM: (\w+) overflow=(\d+) collide=(\d+)(?: truncated=(\d+))?", r.stdout):
            if m.group(1) in owns:
                textgeom[m.group(1)] = [int(m.group(2)), int(m.group(3)), int(m.group(4) or 0)]

    print("== fidelity ==")
    for screen, design in DESIGNS.items():
        drive = next(d for d, owns in passes if screen in owns)
        shot_png = build_dir / f"gate-{drive}.{screen}.png"
        if not shot_png.exists():
            failures.append(f"{screen}: no shot rendered")
            continue
        cmd = [sys.executable, str(ROOT / "tools" / "fidelity_diff.py"),
               str(shot_png), str(ROOT / "design" / f"{design}.png"), "--worst", "5"]
        rects_json = shot_png.parent / (shot_png.stem + ".rects.json")
        if rects_json.exists():
            cmd += ["--elements", str(rects_json)]
        if screen in MASKS:
            cmd += ["--mask", MASKS[screen]]
        fr = run(cmd)
        m = re.search(r"FIDELITY: ([\d.]+)%", fr.stdout)
        if not m:
            failures.append(f"{screen}: fidelity_diff produced no score")
            continue
        fidelity[screen] = float(m.group(1))
        worst = [l.strip() for l in fr.stdout.splitlines() if l.strip().startswith("ELEM ")]
        print(f"  {screen}: {fidelity[screen]:.1f}%" + (f"  worst: {'; '.join(worst[:5])}" if worst else ""))

    current = {"binds_resolved": binds, "fidelity": fidelity, "textgeom": textgeom}
    if args.update or not BASELINE.exists():
        BASELINE.write_text(json.dumps(current, indent=2) + "\n")
        print(f"baseline written -> {BASELINE}")
        return 0 if not failures else 1

    base = json.loads(BASELINE.read_text())
    for screen, count in base.get("binds_resolved", {}).items():
        got = binds.get(screen, 0)
        if got < count:
            failures.append(f"{screen}: bind resolution dropped {count} -> {got} (a bind went dead)")
    for screen, score in base.get("fidelity", {}).items():
        got = fidelity.get(screen, 0.0)
        if got < score - FIDELITY_TOLERANCE:
            failures.append(f"{screen}: fidelity dropped {score:.1f} -> {got:.1f}")
    # P0-A.5: zero NEW text overflows/collisions — counts may only go DOWN vs the baseline.
    for screen, counts in base.get("textgeom", {}).items():
        b_over, b_coll = counts[0], counts[1]
        got = textgeom.get(screen, [0, 0, 0])
        if got[0] > b_over:
            failures.append(f"{screen}: text OVERFLOWS rose {b_over} -> {got[0]}")
        if got[1] > b_coll:
            failures.append(f"{screen}: text COLLISIONS rose {b_coll} -> {got[1]}")
    # M1 detector gap: TRUNCATED text (invisible words — the wrap clamp dropped content) is pinned
    # ZERO, absolute. No baseline ride: any truncation anywhere fails the gate outright.
    for screen, counts in textgeom.items():
        if len(counts) > 2 and counts[2] > 0:
            failures.append(f"{screen}: {counts[2]} TRUNCATED text element(s) — invisible words (pinned zero)")

    if failures:
        print("\nGATE FAILED:")
        for f in failures:
            print("  - " + f)
        return 1
    print("\nGATE OK (no regression vs baseline)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
