#!/usr/bin/env python3
"""Fidelity diff: objective match score between a rendered screen shot and its design PNG.

Usage:
  python tools/fidelity_diff.py <shot.png> <design.png> [--map out.png] [--worst N]

Same-size images compare 1:1 with ZERO resampling (render shots at ref res via RB_SIZE);
mismatched sizes fall back to LANCZOS-resampling both into 960x540 design space. Either
way the compare runs on a 24x14 tile grid using per-tile mean color distance + structure
(edge-energy) distance. Prints an overall match % (tagged 1:1 vs resampled), and the N
worst tiles with their design-space rects so a pass can walk straight to the deltas.
Optionally writes a heatmap PNG (green=match .. red=delta) for eyeballing.

Exit code is always 0: the score is a MEASURE, the gate threshold lives with the caller
(STATUS records the per-screen baseline; a screen is design-done when its score clears the
agreed bar AND the coverage smoke is green).
"""
import argparse
import numpy as np
from PIL import Image

GRID_W, GRID_H = 24, 14        # tile grid (design space 960x540 -> 40x~39 px tiles)
DESIGN_W, DESIGN_H = 960, 540  # tile rects always REPORT in design space

def load_pair(shot_path, design_path):
    """Load both images at a common working size.

    Same size on both sides -> compare AT THAT SIZE, zero resampling (the P0-A contract:
    RB_SIZE-rendered shots vs 1920x1080 refs diff 1:1). Sizes differ -> legacy fallback,
    LANCZOS both into 960x540 design space (warps whatever doesn't match the design aspect
    — render at ref res instead whenever possible)."""
    a = Image.open(shot_path).convert("RGB")
    b = Image.open(design_path).convert("RGB")
    if a.size != b.size:
        a = a.resize((DESIGN_W, DESIGN_H), Image.LANCZOS)
        b = b.resize((DESIGN_W, DESIGN_H), Image.LANCZOS)
    return (np.asarray(a, dtype=np.float32) / 255.0,
            np.asarray(b, dtype=np.float32) / 255.0)

def edge_energy(a):
    gx = np.abs(np.diff(a.mean(axis=2), axis=1))
    gy = np.abs(np.diff(a.mean(axis=2), axis=0))
    e = np.zeros(a.shape[:2], dtype=np.float32)
    e[:, 1:] += gx
    e[1:, :] += gy
    return e

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("shot")
    ap.add_argument("design")
    ap.add_argument("--map", dest="map_out")
    ap.add_argument("--worst", type=int, default=8)
    args = ap.parse_args()

    shot, design = load_pair(args.shot, args.design)
    e_shot, e_design = edge_energy(shot), edge_energy(design)

    work_h, work_w = shot.shape[:2]
    th, tw = work_h // GRID_H, work_w // GRID_W
    # design-space factor for reported rects (1.0 when comparing in 960x540)
    kx, ky = DESIGN_W / work_w, DESIGN_H / work_h
    scores = np.zeros((GRID_H, GRID_W), dtype=np.float32)
    for ty in range(GRID_H):
        for tx in range(GRID_W):
            sl = np.s_[ty * th:(ty + 1) * th, tx * tw:(tx + 1) * tw]
            color_d = np.abs(shot[sl] - design[sl]).mean()
            edge_d = np.abs(e_shot[sl] - e_design[sl]).mean()
            # color dominates (palette/fill truth); edges catch structure/chrome/text presence
            scores[ty, tx] = 1.0 - min(1.0, 2.5 * color_d + 4.0 * edge_d)

    overall = float(scores.mean()) * 100.0
    mode = "1:1" if Image.open(args.shot).size == Image.open(args.design).size else "resampled"
    print(f"FIDELITY: {overall:.1f}% match [{mode} @ {work_w}x{work_h}] ({args.shot} vs {args.design})")

    flat = [(scores[ty, tx], tx, ty) for ty in range(GRID_H) for tx in range(GRID_W)]
    flat.sort()
    print("worst tiles (design-space rects):")
    for s, tx, ty in flat[:args.worst]:
        print(f"  {s * 100:5.1f}%  rect=({tx * tw * kx:.0f},{ty * th * ky:.0f},{tw * kx:.0f},{th * ky:.0f})")

    if args.map_out:
        hm = np.zeros((GRID_H, GRID_W, 3), dtype=np.uint8)
        hm[..., 0] = ((1.0 - scores) * 255).astype(np.uint8)  # red = delta
        hm[..., 1] = (scores * 255).astype(np.uint8)          # green = match
        Image.fromarray(hm, "RGB").resize((DESIGN_W, DESIGN_H), Image.NEAREST).save(args.map_out)
        print(f"delta map -> {args.map_out}")

if __name__ == "__main__":
    main()
