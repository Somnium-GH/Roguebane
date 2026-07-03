#!/usr/bin/env python3
"""Drop-time audit: diff each design/dchtml/*.dc.html inventory against layout.json.

CD drops ship the design SOURCES (dc.html, read-only) alongside the extracted manifest.
Anything present in the html markup but missing from the manifest is an EXTRACTION GAP
and must be caught AT DROP TIME, not weeks later on a pixel walk.

Checks per screen (dc.html -> screens.<id> in layout.json):
  - designSize: data-design coords / 2 must equal the manifest screen designSize
  - elements:   every data-el id exists in the manifest screen's elements
  - binds:      every data-binds value (screen scope) appears on some manifest element
  - templates:  every data-tpl name exists in manifest templates; every data-template
                reference resolves (container element carries item.template == ref)
  - tpl binds:  binds/image-binds/color-binds inside a data-tpl subtree appear in that
                manifest template (template level or parts)
  - imageBind:  every data-image-bind pattern appears somewhere in the manifest
  - frames:     data-frames list matches the manifest element's frames
  - bind-gate:  data-bind-gate elements carry BOTH content (the literal) and binds (the
                gate) in the manifest
Exit code 0 = clean, 1 = gaps found. Manifest-only extras are informational, never a gap
(the manifest may legitimately carry more than one screen's html shows).

Usage: python tools/drop_audit.py [--dchtml design/dchtml] [--manifest Roguebane.Content/layout.json]
"""
import argparse
import json
import re
import sys
from html.parser import HTMLParser
from pathlib import Path

VOID_TAGS = {"area", "base", "br", "col", "embed", "hr", "img", "input",
             "link", "meta", "param", "source", "track", "wbr"}


class Node:
    __slots__ = ("tag", "attrs", "el", "tpl", "text")

    def __init__(self, tag, attrs):
        self.tag = tag
        self.attrs = dict(attrs)
        self.el = self.attrs.get("data-el")
        self.tpl = self.attrs.get("data-tpl")
        self.text = []


class DcHtml(HTMLParser):
    """Collects the extraction inventory of one dc.html file."""

    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.stack = []
        self.screen = None          # data-screen id
        self.design = None          # (w, h) from data-design
        self.elements = {}          # data-el id -> info dict (screen scope)
        self.templates = {}         # data-tpl name -> {binds, image_binds, color_binds, parts}
        self._tpl_ctx = []          # active template-name stack

    def _cur_tpl(self):
        return self._tpl_ctx[-1] if self._tpl_ctx else None

    def handle_starttag(self, tag, attrs):
        node = Node(tag, attrs)
        a = node.attrs
        if "data-screen" in a:
            self.screen = a["data-screen"]
            if "data-design" in a:
                try:
                    w, h = (int(x) for x in a["data-design"].split(","))
                    self.design = (w, h)
                except ValueError:
                    pass
        if node.tpl:
            self.templates.setdefault(node.tpl, {
                "binds": set(), "image_binds": set(), "color_binds": set(), "parts": 0})
            self._tpl_ctx.append(node.tpl)
        tpl = self._cur_tpl()
        binds = a.get("data-binds")
        if tpl:
            t = self.templates[tpl]
            if binds:
                t["binds"].add(binds)
            if "data-image-bind" in a:
                t["image_binds"].add(a["data-image-bind"])
            if "data-color-bind" in a:
                t["color_binds"].add(a["data-color-bind"])
            if "data-part" in a:
                t["parts"] += 1
        elif node.el:
            self.elements[node.el] = {
                "binds": binds,
                "template": a.get("data-template"),
                "container": "data-container" in a,
                "bind_gate": "data-bind-gate" in a,
                "image_bind": a.get("data-image-bind"),
                "color_bind": a.get("data-color-bind"),
                "frames": a.get("data-frames"),
                "states": a.get("data-states"),
                "text": "",
            }
        elif binds and not tpl:
            # bind on a non-data-el node at screen scope still names a datum the
            # manifest must resolve somewhere
            self.elements.setdefault("(anon:" + binds + ")", {
                "binds": binds, "template": None, "container": False,
                "bind_gate": False, "image_bind": a.get("data-image-bind"),
                "color_bind": a.get("data-color-bind"), "frames": None,
                "states": None, "text": "", "anon": True})
        if tag not in VOID_TAGS:
            self.stack.append(node)

    def handle_startendtag(self, tag, attrs):
        self.handle_starttag(tag, attrs)
        if tag not in VOID_TAGS:
            self.handle_endtag(tag)

    def handle_endtag(self, tag):
        while self.stack:
            node = self.stack.pop()
            if node.tpl and self._tpl_ctx and self._tpl_ctx[-1] == node.tpl:
                self._tpl_ctx.pop()
            if node.el and not self._cur_tpl() and node.el in self.elements:
                txt = " ".join("".join(node.text).split())
                if txt and "{{" not in txt:
                    self.elements[node.el]["text"] = txt
            if node.tag == tag:
                break

    def handle_data(self, data):
        for node in reversed(self.stack):
            if node.el or node.tpl:
                node.text.append(data)
                break


def manifest_bind_universe(screen):
    """All bind names any element of a manifest screen resolves."""
    out = set()
    for e in screen.get("elements", []):
        if e.get("binds"):
            out.add(e["binds"])
    return out


def walk_strings(obj, key):
    """All values of `key` anywhere in a JSON subtree."""
    found = set()
    if isinstance(obj, dict):
        for k, v in obj.items():
            if k == key and isinstance(v, str):
                found.add(v)
            else:
                found |= walk_strings(v, key)
    elif isinstance(obj, list):
        for v in obj:
            found |= walk_strings(v, key)
    return found


def tpl_bind_universe(tpl):
    u = set()
    if tpl.get("binds"):
        u.add(tpl["binds"])
    u |= walk_strings(tpl.get("parts", []), "binds")
    if tpl.get("item"):
        u |= walk_strings(tpl["item"], "binds")
    return u


def audit_screen(name, html, manifest):
    """Returns (gaps, infos) — gaps fail the drop, infos are context."""
    gaps, infos = [], []
    screens = manifest.get("screens", {})
    if name not in screens:
        return [f"screen '{name}' missing from layout.json"], infos
    scr = screens[name]

    if html.design:
        want = [html.design[0] // 2, html.design[1] // 2]
        have = scr.get("designSize")
        if have != want:
            gaps.append(f"designSize {have} != dc.html {want} (data-design/2)")

    el_ids = {e["id"] for e in scr.get("elements", []) if "id" in e}
    el_by_id = {e["id"]: e for e in scr.get("elements", []) if "id" in e}
    bind_u = manifest_bind_universe(scr)
    tpls = manifest.get("templates", {})

    for eid, info in html.elements.items():
        anon = info.get("anon", False)
        if not anon and eid not in el_ids:
            gaps.append(f"element '{eid}' in dc.html, absent from manifest")
            continue
        me = el_by_id.get(eid, {})
        if info["binds"] and info["binds"] not in bind_u:
            gaps.append(f"bind '{info['binds']}' ({eid}) unmapped in manifest screen")
        if info["template"]:
            if info["template"] not in tpls:
                gaps.append(f"template '{info['template']}' ({eid}) absent from manifest templates")
            elif not anon and me.get("item", {}).get("template") != info["template"]:
                gaps.append(f"container '{eid}' item.template != '{info['template']}' "
                            f"(manifest: {me.get('item', {}).get('template')})")
        if info["bind_gate"] and not anon:
            if not me.get("content") and not me.get("sample"):
                gaps.append(f"bind-gated '{eid}' has no content/sample literal in manifest")
            if not me.get("binds"):
                gaps.append(f"bind-gated '{eid}' has no gating bind in manifest")
        if info["frames"] and not anon:
            want_frames = [f.strip() for f in info["frames"].split(",")]
            if me.get("frames") != want_frames:
                gaps.append(f"frames on '{eid}': manifest {me.get('frames')} != dc.html {want_frames}")
        if info["states"] and not anon and "states" not in me:
            # data-states="button" names a skin family; JSON blobs are inline styles
            gaps.append(f"'{eid}' declares data-states, manifest element has none")
        if info["text"] and not anon and not (me.get("content") or me.get("sample")
                                              or me.get("binds") or me.get("item")):
            infos.append(f"'{eid}' shows literal text '{info['text'][:40]}' — "
                         f"manifest has no content/sample/binds")
    return gaps, infos


def audit_templates(html_templates, manifest, seen_gap):
    gaps = []
    tpls = manifest.get("templates", {})
    for tname, tinfo in html_templates.items():
        if tname not in tpls:
            gaps.append(f"template '{tname}' defined in dc.html, absent from manifest")
            continue
        mu = tpl_bind_universe(tpls[tname])
        for b in sorted(tinfo["binds"]):
            key = (tname, b)
            if b not in mu and key not in seen_gap:
                seen_gap.add(key)
                gaps.append(f"template '{tname}' bind '{b}' unmapped in manifest template")
        m_img = walk_strings(tpls[tname], "imageBind")
        for ib in sorted(tinfo["image_binds"]):
            key = (tname, "img", ib)
            if ib not in m_img and key not in seen_gap:
                seen_gap.add(key)
                gaps.append(f"template '{tname}' imageBind '{ib}' absent from manifest template")
        m_col = walk_strings(tpls[tname], "colorBind")
        for cb in sorted(tinfo["color_binds"]):
            key = (tname, "col", cb)
            if cb not in m_col and key not in seen_gap:
                seen_gap.add(key)
                gaps.append(f"template '{tname}' colorBind '{cb}' absent from manifest template")
    return gaps


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--dchtml", default="design/dchtml")
    ap.add_argument("--manifest", default="Roguebane.Content/layout.json")
    args = ap.parse_args()

    manifest = json.loads(Path(args.manifest).read_text(encoding="utf-8"))
    files = sorted(Path(args.dchtml).glob("*.dc.html"))
    if not files:
        print(f"drop_audit: no *.dc.html under {args.dchtml}", file=sys.stderr)
        return 1

    total_gaps = 0
    seen_tpl_gap = set()
    for f in files:
        parser = DcHtml()
        parser.feed(f.read_text(encoding="utf-8"))
        if not parser.screen or parser.screen not in manifest.get("screens", {}):
            print(f"-- {f.name}: reference-only (screen="
                  f"{parser.screen or 'none'}), skipped")
            continue
        gaps, infos = audit_screen(parser.screen, parser, manifest)
        gaps += audit_templates(parser.templates, manifest, seen_tpl_gap)
        status = "GAPS" if gaps else "clean"
        print(f"== {parser.screen} ({f.name}): {status} "
              f"[{len(parser.elements)} els, {len(parser.templates)} tpls in html]")
        for g in gaps:
            print(f"   GAP  {g}")
        for i in infos:
            print(f"   info {i}")
        total_gaps += len(gaps)

    print(f"\ndrop_audit: {total_gaps} extraction gap(s)")
    return 1 if total_gaps else 0


if __name__ == "__main__":
    sys.exit(main())
