#!/usr/bin/env python3
"""Fidelity diff: objective match score between a rendered screen shot and its design PNG.

Usage:
  python tools/fidelity_diff.py <shot.png> <design.png> [--elements rects.json]
      [--mask id,id,...] [--map out.png] [--worst N]

Same-size images compare 1:1 with ZERO resampling (render shots at ref res via RB_SIZE);
mismatched sizes fall back to LANCZOS-resampling both into 960x540 design space.

v2 (P0-A.3): pass --elements with the engine's rects sidecar (design-space rect per element
id, emitted next to every RB_MF=all shot) and the diff scores EACH ELEMENT'S CROP, printing
a RANKED per-element delta list (id, score, design rect) — the pixel walk becomes "fix the
top of the list". --mask names element ids whose regions are tolerated placeholder zones
(e.g. newgame stat digits pending the live tuning session): their pixels are neutralized
(ref copied over shot) BEFORE any scoring, so they can't depress the overall score, and
they're skipped in the ranked list.

The 24x14 tile grid remains as the whole-frame score + optional heatmap visual.

Exit code is always 0: the score is a MEASURE, the gate threshold lives with the caller.
"""
import argparse
import json
import numpy as np
from PIL import Image

GRID_W, GRID_H = 24, 14        # tile grid (design space 960x540 -> 40x~39 px tiles)
DESIGN_W, DESIGN_H = 960, 540  # element/tile rects are always design-space

def load_pair(shot_path, design_path):
    """Load both images at a common working size.

    Same size on both sides -> compare AT THAT SIZE, zero resampling (the P0-A contract:
    RB_SIZE-rendered shots vs 1920x1080 refs diff 1:1). Sizes differ -> legacy fallback,
    LANCZOS both into 960x540 design space (warps whatever doesn't match the design aspect
    — render at ref res instead whenever possible)."""
    a = Image.open(shot_path).convert("RGB")
    b = Image.open(design_path).convert("RGB")
    same = a.size == b.size
    if not same:
        a = a.resize((DESIGN_W, DESIGN_H), Image.LANCZOS)
        b = b.resize((DESIGN_W, DESIGN_H), Image.LANCZOS)
    return (np.asarray(a, dtype=np.float32) / 255.0,
            np.asarray(b, dtype=np.float32) / 255.0, same)

def edge_energy(a):
    gx = np.abs(np.diff(a.mean(axis=2), axis=1))
    gy = np.abs(np.diff(a.mean(axis=2), axis=0))
    e = np.zeros(a.shape[:2], dtype=np.float32)
    e[:, 1:] += gx
    e[1:, :] += gy
    return e

def region_score(shot, design, e_shot, e_design, sl):
    color_d = np.abs(shot[sl] - design[sl]).mean()
    edge_d = np.abs(e_shot[sl] - e_design[sl]).mean()
    # color dominates (palette/fill truth); edges catch structure/chrome/text presence
    return 1.0 - min(1.0, 2.5 * color_d + 4.0 * edge_d)

def design_rect_to_slice(rect, work_w, work_h):
    """Design-space [x,y,w,h] -> numpy slice at the working resolution, clamped."""
    fx, fy = work_w / DESIGN_W, work_h / DESIGN_H
    x0 = max(0, int(rect[0] * fx)); y0 = max(0, int(rect[1] * fy))
    x1 = min(work_w, int((rect[0] + rect[2]) * fx)); y1 = min(work_h, int((rect[1] + rect[3]) * fy))
    if x1 <= x0 or y1 <= y0:
        return None
    return np.s_[y0:y1, x0:x1]

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("shot")
    ap.add_argument("design")
    ap.add_argument("--elements", help="engine rects sidecar (design-space rect per element id)")
    ap.add_argument("--mask", default="", help="comma-separated element ids to neutralize (placeholder zones)")
    ap.add_argument("--map", dest="map_out")
    ap.add_argument("--worst", type=int, default=8)
    args = ap.parse_args()

    shot, design, same_size = load_pair(args.shot, args.design)
    work_h, work_w = shot.shape[:2]

    rects = {}
    if args.elements:
        raw = json.loads(open(args.elements, encoding="utf-8").read())
        # sidecar v1 = {id: [x,y,w,h]}; v2 = {id: {rect, fontPx, borderW, type}}
        rects = {k: (v["rect"] if isinstance(v, dict) else v) for k, v in raw.items()}
    masked = [m for m in args.mask.split(",") if m]
    for mid in masked:
        if mid in rects:
            sl = design_rect_to_slice(rects[mid], work_w, work_h)
            if sl is not None:
                shot[sl] = design[sl]  # neutral: zero delta in tolerated placeholder zones

    e_shot, e_design = edge_energy(shot), edge_energy(design)

    th, tw = work_h // GRID_H, work_w // GRID_W
    kx, ky = DESIGN_W / work_w, DESIGN_H / work_h
    scores = np.zeros((GRID_H, GRID_W), dtype=np.float32)
    for ty in range(GRID_H):
        for tx in range(GRID_W):
            sl = np.s_[ty * th:(ty + 1) * th, tx * tw:(tx + 1) * tw]
            scores[ty, tx] = region_score(shot, design, e_shot, e_design, sl)

    overall = float(scores.mean()) * 100.0
    mode = "1:1" if same_size else "resampled"
    mask_note = f" mask=[{','.join(masked)}]" if masked else ""
    print(f"FIDELITY: {overall:.1f}% match [{mode} @ {work_w}x{work_h}]{mask_note} ({args.shot} vs {args.design})")

    if rects:
        # M0.1 (Doug 2026-07-03 late): NO smoothing — the blur made the tool blind to the 2-3px
        # class. ALIGNMENT SEARCH instead: score each element crop at integer shifts within +-3
        # design px, keep the best, and REPORT THE SHIFT — a real offset becomes a number to fix.
        shift_px = 3
        ranked = []
        for eid, rect in rects.items():
            if eid in masked:
                continue
            base = design_rect_to_slice(rect, work_w, work_h)
            if base is None:
                continue
            fx, fy = work_w / DESIGN_W, work_h / DESIGN_H
            best, bdx, bdy = -1.0, 0, 0
            for dy in range(-shift_px, shift_px + 1):
                for dx in range(-shift_px, shift_px + 1):
                    sh = design_rect_to_slice([rect[0] + dx, rect[1] + dy, rect[2], rect[3]],
                                              work_w, work_h)
                    if sh is None:
                        continue
                    ds = design_rect_to_slice(rect, work_w, work_h)
                    # shot sampled at the shifted window, design at the authored window
                    a = shot[sh]; b = design[ds]
                    hh = min(a.shape[0], b.shape[0]); ww = min(a.shape[1], b.shape[1])
                    if hh < 1 or ww < 1:
                        continue
                    color_d = np.abs(a[:hh, :ww] - b[:hh, :ww]).mean()
                    ea = e_shot[sh]; eb = e_design[ds]
                    edge_d = np.abs(ea[:hh, :ww] - eb[:hh, :ww]).mean()
                    s = 1.0 - min(1.0, 2.5 * color_d + 4.0 * edge_d)
                    if s > best:
                        best, bdx, bdy = s, dx, dy
            ranked.append((best, bdx, bdy, eid, rect))
        ranked.sort()
        n = args.worst if args.worst else len(ranked)
        print(f"ELEMENTS: {len(ranked)} scored (unblurred, best of +-{shift_px}px alignment), worst first:")
        for s, dx, dy, eid, r in ranked[:n]:
            off = f" shift=({dx:+d},{dy:+d})px" if (dx, dy) != (0, 0) else ""
            print(f"  ELEM {s * 100:5.1f}%  {eid}  rect=({r[0]},{r[1]},{r[2]},{r[3]}){off}")
    else:
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
