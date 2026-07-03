#!/usr/bin/env python3
"""Hand-shot normalizer (P0-A.6): run Doug's live window screenshots through the same
measurement pipeline as the gate's headless shots.

  python tools/normalize_shot.py <hand-shot.png> [-o out.png] [--design design/NN.png]
      [--trim-top N] [--client x,y,w,h]

Pipeline:
1. CLIENT CROP — auto-trims near-uniform margins (desktop, letterbox remnants, borders)
   by flooding inward from each edge while rows/columns stay low-variance. A visible OS
   title bar usually survives auto-trim (it has buttons/text): pass --trim-top with its
   height, or --client to name the client rect exactly.
2. ASPECT CHECK — §13 aspect-fill anchors HUD to REAL window edges, so only a ~16:9
   client compares cleanly against the 960x540-space refs. Off-aspect shots still
   normalize (with a printed warning) but edge-anchored elements will sit off-ref.
3. RESCALE to 1920x1080 (the reference resolution) and save.
4. Optionally chains fidelity_diff against a design ref so an eyeball report arrives
   with the same numbers a gate pass produces.

Exit 0 always — a normalizer, not a gate.
"""
import argparse
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image

REF_W, REF_H = 1920, 1080


def autotrim(im: np.ndarray) -> tuple[int, int, int, int]:
    """Trim near-uniform margins: walk inward while each row/col has tiny variance."""
    def uniform(v):  # a margin row/col: almost all pixels within a hair of its median
        med = np.median(v.reshape(-1, 3), axis=0)
        return (np.abs(v.astype(np.int16) - med).sum(axis=1) < 18).mean() > 0.98

    h, w = im.shape[:2]
    top, bottom, left, right = 0, h, 0, w
    while top < bottom - 1 and uniform(im[top]):
        top += 1
    while bottom > top + 1 and uniform(im[bottom - 1]):
        bottom -= 1
    while left < right - 1 and uniform(im[:, left]):
        left += 1
    while right > left + 1 and uniform(im[:, right - 1]):
        right -= 1
    return left, top, right - left, bottom - top


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("shot")
    ap.add_argument("-o", "--out")
    ap.add_argument("--design", help="chain fidelity_diff against this ref")
    ap.add_argument("--trim-top", type=int, default=0, help="OS title-bar height to drop first")
    ap.add_argument("--client", help="x,y,w,h client rect override (skips auto-trim)")
    args = ap.parse_args()

    im = Image.open(args.shot).convert("RGB")
    if args.trim_top:
        im = im.crop((0, args.trim_top, im.width, im.height))
    px = np.asarray(im)

    if args.client:
        x, y, w, h = (int(v) for v in args.client.split(","))
    else:
        x, y, w, h = autotrim(px)
    client = im.crop((x, y, x + w, y + h))
    print(f"client rect: ({x},{y},{w},{h}) of {im.width}x{im.height}")

    aspect = w / h
    if abs(aspect - 16 / 9) > 0.02:
        print(f"WARNING: client aspect {aspect:.3f} != 16:9 — §13 anchors HUD to real edges, "
              "so edge elements will sit off-ref after normalization")

    out = Path(args.out) if args.out else Path(args.shot).with_suffix(".norm.png")
    client.resize((REF_W, REF_H), Image.LANCZOS).save(out)
    print(f"normalized -> {out} ({REF_W}x{REF_H})")

    if args.design:
        r = subprocess.run([sys.executable, str(Path(__file__).parent / "fidelity_diff.py"),
                            str(out), args.design, "--worst", "8"],
                           capture_output=True, text=True)
        print(r.stdout.strip())


if __name__ == "__main__":
    main()
