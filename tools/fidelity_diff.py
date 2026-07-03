#!/usr/bin/env python3
"""Fidelity diff: objective match score between a rendered screen shot and its design PNG.

Usage:
  python tools/fidelity_diff.py <shot.png> <design.png> [--map out.png] [--worst N]

Both images are resampled to a common working size (the design's aspect at 960x540 design
space), then compared on a 24x14 tile grid (40x~39 design-px tiles) using per-tile mean
color distance + structure (edge-energy) distance. Prints an overall match %, and the N
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
WORK_W, WORK_H = 960, 540      # compare in design space

def load(path):
    im = Image.open(path).convert("RGB").resize((WORK_W, WORK_H), Image.LANCZOS)
    return np.asarray(im, dtype=np.float32) / 255.0

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

    shot, design = load(args.shot), load(args.design)
    e_shot, e_design = edge_energy(shot), edge_energy(design)

    th, tw = WORK_H // GRID_H, WORK_W // GRID_W
    scores = np.zeros((GRID_H, GRID_W), dtype=np.float32)
    for ty in range(GRID_H):
        for tx in range(GRID_W):
            sl = np.s_[ty * th:(ty + 1) * th, tx * tw:(tx + 1) * tw]
            color_d = np.abs(shot[sl] - design[sl]).mean()
            edge_d = np.abs(e_shot[sl] - e_design[sl]).mean()
            # color dominates (palette/fill truth); edges catch structure/chrome/text presence
            scores[ty, tx] = 1.0 - min(1.0, 2.5 * color_d + 4.0 * edge_d)

    overall = float(scores.mean()) * 100.0
    print(f"FIDELITY: {overall:.1f}% match ({args.shot} vs {args.design})")

    flat = [(scores[ty, tx], tx, ty) for ty in range(GRID_H) for tx in range(GRID_W)]
    flat.sort()
    print("worst tiles (design-space rects):")
    for s, tx, ty in flat[:args.worst]:
        print(f"  {s * 100:5.1f}%  rect=({tx * tw},{ty * th},{tw},{th})")

    if args.map_out:
        hm = np.zeros((GRID_H, GRID_W, 3), dtype=np.uint8)
        hm[..., 0] = ((1.0 - scores) * 255).astype(np.uint8)  # red = delta
        hm[..., 1] = (scores * 255).astype(np.uint8)          # green = match
        Image.fromarray(hm, "RGB").resize((WORK_W, WORK_H), Image.NEAREST).save(args.map_out)
        print(f"delta map -> {args.map_out}")

if __name__ == "__main__":
    main()
