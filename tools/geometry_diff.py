#!/usr/bin/env python3
"""Geometry diff (M0.4): judge LAYOUT with numbers — pixels judge art, geometry judges layout.

  python tools/geometry_diff.py <Screen.dc.html> <shot.rects.json> <shot.textgeom.json> [--el id]

Per data-el element in the dc.html SOURCE (the authored truth), reports a numeric row against
what the manifest resolved and what the engine actually drew:

  pos-delta / size-delta   authored box (absolute-positioned styles, /2 into design space)
                           vs the manifest-resolved rect from the rects sidecar
  fontPx / family          authored font-size/2 + family vs the engine's drawn record
  string                   authored literal text vs the drawn string (bound els skip content)

Elements without absolute positioning (flex children) report authored font/text checks only.
Immune to antialiasing by construction. Exit 0 always — the table is a MEASURE.
"""
import argparse
import json
import re
import sys
from html.parser import HTMLParser

VOID = {"br", "img", "hr", "input", "meta", "link", "span"}  # span NOT void; keep real set
VOID = {"br", "img", "hr", "input", "meta", "link"}


class Src(HTMLParser):
    """Collects per data-el authored geometry from the dc.html source."""

    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.stack = []          # (tag, el_or_None)
        self.cur = None          # innermost data-el id
        self.els = {}            # id -> dict
        self.screen = None

    @staticmethod
    def style_of(attrs):
        return dict(a for a in attrs if a[0] == "style") .get("style", "")

    def handle_starttag(self, tag, attrs):
        a = dict(attrs)
        if "data-screen" in a:
            self.screen = a["data-screen"]
        el = a.get("data-el")
        style = a.get("style", "")

        def px(prop):
            m = re.search(prop + r"\s*:\s*(-?[\d.]+)px", style)
            return float(m.group(1)) / 2 if m else None  # 1920-space -> design

        if el:
            self.els[el] = {
                "box": [px("left"), px("top"), px("width"), px("height")]
                       if "position:absolute" in style.replace(" ", "") else None,
                "fontPx": px("font-size"),
                "family": ("mono" if "Mono" in style else
                           "display" if "IM Fell" in style else None),
                "text": "",
                "spans": [],   # inner-span styling (fontPx, family) — flattening detector
            }
            self.cur = el
        elif self.cur and tag == "span":
            fs = px("font-size")
            fam = ("mono" if "Mono" in style else "display" if "IM Fell" in style else None)
            if fs or fam:
                self.els[self.cur]["spans"].append({"fontPx": fs, "family": fam})
        if tag not in VOID:
            self.stack.append((tag, el))

    def handle_endtag(self, tag):
        while self.stack:
            t, el = self.stack.pop()
            if el and el == self.cur:
                self.cur = next((e for _, e in reversed(self.stack) if e), None)
            if t == tag:
                break

    def handle_data(self, data):
        if self.cur and "{{" not in data:
            t = " ".join(data.split())
            if t:
                self.els[self.cur]["text"] = (self.els[self.cur]["text"] + " " + t).strip()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("dchtml")
    ap.add_argument("rects")
    ap.add_argument("textgeom")
    ap.add_argument("--el", help="single element filter")
    args = ap.parse_args()

    src = Src()
    src.feed(open(args.dchtml, encoding="utf-8").read())
    raw = json.loads(open(args.rects, encoding="utf-8").read())
    rects = {k: (v["rect"] if isinstance(v, dict) else v) for k, v in raw.items()}
    fonts = {k: v for k, v in raw.items() if isinstance(v, dict)}
    drawn = {}
    for t in json.loads(open(args.textgeom, encoding="utf-8").read()):
        drawn.setdefault(t["el"], []).append(t)

    print(f"GEOMETRY {src.screen}: authored(dc.html/2) vs manifest vs drawn — design px")
    print(f"{'element':22} {'pos-d':>9} {'size-d':>9} {'fontPx a/m':>11} {'fam a/d':>9}  notes")
    issues = 0
    for eid, a in sorted(src.els.items()):
        if args.el and eid != args.el:
            continue
        notes = []
        pos_d = size_d = ""
        if a["box"] and all(v is not None for v in a["box"]) and eid in rects:
            m = rects[eid]
            dx, dy = m[0] - a["box"][0], m[1] - a["box"][1]
            dw, dh = m[2] - a["box"][2], m[3] - a["box"][3]
            pos_d = f"{dx:+.0f},{dy:+.0f}"
            size_d = f"{dw:+.0f},{dh:+.0f}"
            if abs(dx) > 1 or abs(dy) > 1:
                notes.append("POS")
            if abs(dw) > 1 or abs(dh) > 1:
                notes.append("SIZE")
        man_font = fonts.get(eid, {}).get("fontPx")
        fpx = ""
        if a["fontPx"] and man_font:
            fpx = f"{a['fontPx']:.1f}/{man_font:g}"
            if abs(a["fontPx"] - float(man_font)) > 0.6:
                notes.append("FONTPX")
        dfam = drawn.get(eid, [{}])[0].get("font", "")
        fam = ""
        if a["family"]:
            fam = f"{a['family'][:4]}/{dfam[:4] or '-'}"
            if dfam and a["family"] != dfam:
                notes.append("FAMILY")
        if a["text"] and eid in drawn:
            got = " ".join(d["text"] for d in drawn[eid])
            if a["text"].lower() not in got.lower() and got.lower() not in a["text"].lower():
                notes.append(f"STRING(a='{a['text'][:24]}' d='{got[:24]}')")
        elif a["text"] and eid in rects and eid not in drawn:
            notes.append(f"NO-TEXT-DRAWN(a='{a['text'][:24]}')")
        if a["spans"] and len({(s.get('fontPx'), s.get('family')) for s in a["spans"]}) > 1:
            notes.append(f"FLATTENED({len(a['spans'])} spans)")
        if notes:
            issues += 1
        print(f"{eid:22} {pos_d:>9} {size_d:>9} {fpx:>11} {fam:>9}  {' '.join(notes)}")
    print(f"\ngeometry_diff: {issues} element(s) with findings")


if __name__ == "__main__":
    main()
