#!/usr/bin/env python3
"""Numeric render probes (P0-A.4): measure the SHOT's pixels against the manifest's numbers.

  python tools/probes.py <shot.png> <rects.sidecar.json> [--worst N]

Two probes, both reported per element as NUMBERS (kills "looks overscaled" arguments):

- TEXT-HEIGHT: for text elements with an authored fontPx, the tight ink bbox inside the
  element rect gives the drawn glyph height in design px. Reported as drawn px and the
  drawn/authored ratio (cap-height ~0.7em means a ratio near 0.7-1.1 is healthy; ~1.5-2.0
  is the "reads oversized" class Doug flagged).
- BORDER-STROKE: for elements with an authored border width, sample the rendered stroke
  thickness at the midpoint of each edge (consecutive non-background px running inward).
  Reported as measured px vs authored px (both design-space).

Needs the sidecar v2 (rect + fontPx + borderW + type) emitted next to every gate shot.
Exit code is always 0 — the numbers are a MEASURE; thresholds live with the caller.
"""
import argparse
import json
import numpy as np
from PIL import Image

DESIGN_W, DESIGN_H = 960, 540


def ink_mask(px, bg_sample):
    """Pixels that differ noticeably from the local background estimate."""
    diff = np.abs(px.astype(np.int16) - bg_sample.astype(np.int16)).sum(axis=2)
    return diff > 60


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("shot")
    ap.add_argument("sidecar")
    ap.add_argument("--worst", type=int, default=10)
    args = ap.parse_args()

    im = np.asarray(Image.open(args.shot).convert("RGB"))
    h, w = im.shape[:2]
    kx, ky = w / DESIGN_W, h / DESIGN_H
    meta = json.loads(open(args.sidecar, encoding="utf-8").read())

    text_rows, border_rows = [], []
    for eid, m in meta.items():
        if not isinstance(m, dict):
            continue  # v1 sidecar carries no style numbers
        x, y, rw, rh = m["rect"]
        px0, py0 = int(x * kx), int(y * ky)
        px1, py1 = min(w, int((x + rw) * kx)), min(h, int((y + rh) * ky))
        if px1 - px0 < 2 or py1 - py0 < 2:
            continue
        crop = im[py0:py1, px0:px1]

        font_px = float(m.get("fontPx") or 0)
        if m.get("type") == "text" and font_px > 0 and not m.get("skinned"):
            # background estimate: the crop's median color (labels sit on flat chrome)
            bg = np.median(crop.reshape(-1, 3), axis=0)
            mask = ink_mask(crop, bg)
            ys = np.where(mask.any(axis=1))[0]
            if len(ys) > 2:  # enough ink to be a drawn label
                drawn = (ys[-1] - ys[0] + 1) / ky  # design px
                text_rows.append((drawn / font_px, drawn, font_px, eid))

        bw = int(m.get("borderW") or 0)
        if bw > 0 and rw > 8 and rh > 8:
            # stroke thickness = the ink run inward from each edge midpoint (median of 4 edges)
            bg = np.median(crop.reshape(-1, 3), axis=0)
            mask = ink_mask(crop, bg)
            col = mask[:, mask.shape[1] // 2]
            row = mask[mask.shape[0] // 2, :]
            def run(v):
                n = 0
                for b in v:
                    if b: n += 1
                    else: break
                return n
            # per-side borders leave 0-runs on unbordered edges: take the LARGEST stroke seen,
            # capped at 6x authored so a filled rect can't masquerade as a border.
            runs = [run(col), run(col[::-1]), run(row), run(row[::-1])]
            measured = max(runs) / ky
            if 0 < measured <= bw * 6:
                border_rows.append((measured / bw, measured, bw, eid))

    text_rows.sort(reverse=True)
    print("TEXT-HEIGHT (drawn/authored, worst-oversize first):")
    for ratio, drawn, authored, eid in text_rows[:args.worst]:
        print(f"  {ratio:5.2f}x  drawn={drawn:5.1f}px authored={authored:4.1f}px  {eid}")

    border_rows.sort(reverse=True)
    print("BORDER-STROKE (measured/authored, worst-oversize first):")
    for ratio, measured, authored, eid in border_rows[:args.worst]:
        print(f"  {ratio:5.2f}x  measured={measured:4.1f}px authored={authored}px  {eid}")


if __name__ == "__main__":
    main()
