#!/usr/bin/env python
"""Report Roguebane.Content assets NOT built by the game-side mgcb.

The game builds Roguebane.Game/Content/Content.mgcb (a hand-curated set that
/build's from ../../Roguebane.Content/...). When Claude Design drops new assets
into Roguebane.Content, the game mgcb must be synced or those assets render blank.
This catches the gap: run it after any CD asset drop.

Usage:  python tools/check_mgcb_coverage.py
Exit 1 (and prints the missing content keys) if the game mgcb is missing any.
"""
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CONTENT = os.path.join(ROOT, "Roguebane.Content")
MGCB = os.path.join(ROOT, "Roguebane.Game", "Content", "Content.mgcb")


def content_keys():
    keys = set()
    for dp, _, files in os.walk(CONTENT):
        for f in files:
            if f.endswith(".png"):
                rel = os.path.relpath(os.path.join(dp, f), CONTENT)
                keys.add(rel.replace(os.sep, "/")[:-4])
    return keys


def built_keys():
    keys = set()
    with open(MGCB, encoding="utf-8", errors="ignore") as fh:
        for line in fh:
            line = line.strip()
            if line.startswith("/build:"):
                keys.add(line.split(";")[-1].strip())
    return keys


def main():
    missing = sorted(content_keys() - built_keys())
    if not missing:
        print("mgcb coverage OK: every Roguebane.Content png is built by the game mgcb.")
        return 0
    print(f"{len(missing)} Roguebane.Content asset(s) MISSING from the game mgcb:")
    for m in missing:
        print("  ", m)
    print("\nAdd a #begin/.../build block for each, or sync the mgcb, so they don't render blank.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
