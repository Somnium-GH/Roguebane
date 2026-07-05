// Roguebane — SCREEN EXTRACTOR (the screen analog of roster_gen.js).
// Per LAYOUT_CONTRACT §9: the .dc.html RENDERS each screen, so the browser has
// already computed every element's rect/text/font/colour. This walks the live DOM
// and PROJECTS it into layout.json `screens.<id>` + `templates` — never hand-authored.
//
// Deterministic: same dc.html -> same DOM -> same computed layout -> same manifest.
//
// HOW IT RUNS (mechanical, no transcription):
//   1. Open the screen's dc.html in a browser (the same run that screenshots it).
//   2. eval:  eval(await readFile('style_tokens.js'));        // -> window.RB_STYLE
//             eval(await readFile('proto/screen_extract.js')); // -> window.RB_extractScreen
//             return RB_extractScreen(document.querySelector('[data-screen]'), RB_STYLE);
//   3. Merge the returned {screen, templates} into Content/layout.json.
//
// INSTRUMENTATION the dc.html carries (only intent the DOM can't infer):
//   data-screen="encounter" data-design="1920,1080"   (root)
//   data-el="id"            meaningful node            -> screens.<id>.elements[]
//   data-anchor="BottomLeft" screen edge it sticks to (default TopLeft)
//   data-z — DEPRECATED (2026-07-03, payload #1 "z conventions are mixed"). The extractor now IGNORES
//     data-z and emits z = the element's PAINT ORDINAL derived from the live DOM stacking order
//     (CSS z-index chain + document order). ONE convention: ascending z = paint order, back → front;
//     z:0 is the backmost element (the *.scene backdrop where one exists). Children always rank
//     after their container, so a container's fill can never overpaint its leaves.
//   data-binds="Body.pool"   Core state it reads (else textContent becomes a literal)
//   data-type="figure|icon"  non-panel element kind
//   data-container + data-template="card" + data-flow + data-cols  dynamic list (§7)
//   data-tpl="card"          the repeated item root (template mini-layout source)
//   data-shadow="panel"      §10 engine-drawn drop shadow — resolves via style.shadows.<token>
//   data-frame="panel"       §10 nine-slice ornate frame  — resolves via style.frames.<token>
//   data-states="pickerCard" per-state styling — a bare name resolves via style.interactionStates.<name>
//     (emitted as {family:<name>,…}); an inline {…} JSON literal emits verbatim (tokens, not hex).
//     DESIGN payload v5 §D: chips/rows/tabs/toggles declare their visual states, not just text.
//   data-image-bind="icons/node/{node.type}"  ASSET-SELECTION bind — the engine blits the named
//     Content PNG (path template resolved against the bound item's fields) INSTEAD of re-drawing the
//     element's own border/fill/glyph. Kills the ASCII-spritefont fallback problem: a map node's ⚔/◉
//     never goes through the font — the token PNG (which has the glyph baked in) is the render.
//     (data-binds ALSO works on any node inside a data-tpl item, incl. value-only nodes with no
//     text/icon of their own, e.g. a charge-bar fill — it forces that sub-part into the template.)
//   data-frames="ui/reticle/focus_p0,ui/reticle/focus_p1,…"  AUTHORED ANIMATION FRAMES — the engine
//     cycles the listed Content PNGs on its fixed tick (DESIGN_SPEC §8 focus-reticle pulse). Frame 0
//     is the canonical/frozen frame (identical to the element's static image).
//   data-bind-gate           the element's data-binds is a COMMAND / VISIBILITY GATE, not a text
//     source (HELD chip, BEGIN button, pager arrows, LEAVE): its rendered text is emitted as literal
//     `content` (the engine's label) while `binds` stays the show/enable/command wire. Without this,
//     bound text emits as `sample` and the engine's unresolved-content-suppresses rule blanks it.
//   data-part                force-capture a chrome-only node inside a data-tpl item (card header
//     divider bands, charge-bar troughs, stat-tile boxes): no text, no icon, no binds — just its
//     fill/border box, so card chrome survives extraction (payload #4/#13). A NON-EMPTY value NAMES
//     the part (ADDENDUM A3 2026-07-03), and named parts also work on [data-el] ELEMENTS: the
//     element emits `parts` (element-relative rects, real fonts) so value/label spans stop
//     flattening into one text run (previewBudgetTile et al).

(function (root) {

  // ---------- colour helpers: rendered rgb() -> nearest palette TOKEN ----------
  function parseRGB(s) {
    if (!s) return null;
    var m = s.match(/rgba?\(([^)]+)\)/i);
    if (!m) return null;
    var p = m[1].split(',').map(function (x) { return parseFloat(x.trim()); });
    if (p.length >= 4 && p[3] === 0) return null;            // fully transparent
    return { r: p[0], g: p[1], b: p[2], a: p.length >= 4 ? p[3] : 1 };
  }
  function hexToRGB(h) {
    h = h.replace('#', '');
    if (h.length === 3) h = h[0] + h[0] + h[1] + h[1] + h[2] + h[2];
    return { r: parseInt(h.slice(0, 2), 16), g: parseInt(h.slice(2, 4), 16), b: parseInt(h.slice(4, 6), 16) };
  }
  function nearestToken(rgb, palette) {
    if (!rgb) return null;
    var best = null, bestD = 1e9;
    for (var k in palette) {
      var c = hexToRGB(palette[k]);
      var d = (c.r - rgb.r) * (c.r - rgb.r) + (c.g - rgb.g) * (c.g - rgb.g) + (c.b - rgb.b) * (c.b - rgb.b);
      if (d < bestD) { bestD = d; best = k; }
    }
    // only bind to a token if it's a real match (< ~26/channel); else null (gradient/image)
    return bestD <= 2100 ? best : null;
  }
  function fontRole(ff) { return /jetbrains|mono/i.test(ff) ? 'mono' : 'display'; }

  // split a CSS function's argument list on TOP-LEVEL commas only (rgba(...) internals have commas
  // too, so a naive .split(',') breaks a gradient's colour-stop list — DEV_LOOP_MEMORY #7).
  function splitTopLevel(s) {
    var parts = [], depth = 0, cur = '';
    for (var i = 0; i < s.length; i++) {
      var ch = s[i];
      if (ch === '(') depth++;
      if (ch === ')') depth--;
      if (ch === ',' && depth === 0) { parts.push(cur); cur = ''; } else cur += ch;
    }
    if (cur.trim()) parts.push(cur);
    return parts.map(function (p) { return p.trim(); });
  }
  // LAYOUT_CONTRACT §10: `fill` may be a gradient spec {type,from,to,dir}. Resolve a CSS
  // `linear-gradient(...)` background-image to the nearest palette tokens for its first/last colour
  // stop + a coarse direction bucket, so screens that use a real gradient (topBar, preview panel, core
  // rune-card header, …) stop falling through as no-fill (DEV_LOOP_MEMORY #7 — gradient capture path).
  // radial-gradient (decorative full-screen backdrops) is intentionally NOT resolved — LAYOUT_CONTRACT
  // §10 only specifies vertical|horizontal|diagonal linear fills; radial backdrops aren't data-el'd.
  function parseGradient(bgImage, palette) {
    var m = /linear-gradient\(([\s\S]+)\)/.exec(bgImage || '');
    if (!m) return null;
    var args = splitTopLevel(m[1]);
    if (!args.length) return null;
    var dir = 'vertical', head = args[0];
    if (/deg\s*$/.test(head)) {
      var deg = ((parseFloat(head) % 360) + 360) % 360;
      if (deg > 60 && deg < 120) dir = 'horizontal';
      else if (deg > 240 && deg < 300) dir = 'horizontal';
      else if (deg < 30 || deg > 330 || (deg > 150 && deg < 210)) dir = 'vertical';
      else dir = 'diagonal';
      args = args.slice(1);
    } else if (/^to\s/.test(head)) {
      if (/left|right/.test(head) && /top|bottom/.test(head)) dir = 'diagonal';
      else if (/left|right/.test(head)) dir = 'horizontal';
      else dir = 'vertical';
      args = args.slice(1);
    }
    var stops = args.map(function (a) { return a.replace(/\s+[\d.]+%\s*$/, '').trim(); }).filter(Boolean);
    if (stops.length < 2) return null;
    function stopRGB(s) { return /^#/.test(s) ? hexToRGB(s) : parseRGB(s); }
    var from = nearestToken(stopRGB(stops[0]), palette);
    var to = nearestToken(stopRGB(stops[stops.length - 1]), palette);
    if (!from || !to) return null;
    return { type: 'gradient', from: from, to: to, dir: dir };
  }

  // ---------- main ----------
  function RB_extractScreen(rootEl, STYLE) {
    var palette = STYLE.palette;
    var dd = (rootEl.getAttribute('data-design') || '1920,1080').split(',').map(Number);
    var nativeW = dd[0], nativeH = dd[1];
    var rootRect = rootEl.getBoundingClientRect();
    var px2native = nativeW / rootRect.width;        // undo any preview scaling
    var DS = 2;                                       // native px -> design-space (960x540)
    var designW = Math.round(nativeW / DS), designH = Math.round(nativeH / DS);

    // a rect in design-space, relative to the screen root
    function dRect(el) {
      var r = el.getBoundingClientRect();
      return {
        x: Math.round((r.left - rootRect.left) * px2native / DS),
        y: Math.round((r.top - rootRect.top) * px2native / DS),
        w: Math.round(r.width * px2native / DS),
        h: Math.round(r.height * px2native / DS),
      };
    }
    // anchor offset relative to a CONTAINER box (design-space rect). For a top-level element the box
    // is the whole screen {x:0,y:0,w:designW,h:designH}; for a nested element it is the parent
    // [data-el]'s design rect — so a grouped child's offset is PARENT-RELATIVE and the whole group
    // reflows as ONE unit. This is the LAYOUT_CONTRACT §3 NO-ABSOLUTE-POSITIONING invariant
    // (2026-07-05): an element's position is a pure function of (anchor, offset, parent box, screen
    // size) and NEVER its raw 1920×1080 pixel coordinate. Negative offset = inset from the box's
    // right/bottom edge.
    function anchorOffsetBox(anchor, rc, box) {
      var a = anchor || 'TopLeft', ox, oy;
      if (/Left/.test(a)) ox = rc.x - box.x;
      else if (/Right/.test(a)) ox = (rc.x + rc.w) - (box.x + box.w);
      else ox = Math.round((rc.x + rc.w / 2) - (box.x + box.w / 2));   // Top/Bottom/Center
      if (/Top/.test(a)) oy = rc.y - box.y;
      else if (/Bottom/.test(a)) oy = (rc.y + rc.h) - (box.y + box.h);
      else oy = Math.round((rc.y + rc.h / 2) - (box.y + box.h / 2));   // Left/Right/Center
      return [ox, oy];
    }
    var SCREEN_BOX = { x: 0, y: 0, w: designW, h: designH };
    // does el contain a deeper [data-el]? (then it's a panel, not a leaf text node)
    function hasElDescendant(el) { return !!el.querySelector('[data-el]'); }

    function visual(el) {
      var cs = getComputedStyle(el);
      var out = {};
      var colTok = nearestToken(parseRGB(cs.color), palette);
      if (colTok) out.color = colTok;
      var fillTok = nearestToken(parseRGB(cs.backgroundColor), palette);
      if (fillTok) out.fill = fillTok;
      else { var grad = parseGradient(cs.backgroundImage, palette); if (grad) out.fill = grad; }
      // borders per SIDE (payload v5 §A: accent left-edges, single-edge band borders — the old
      // top-only read dropped previewApex's border-left and every column divider). `transparent`
      // sides are border-image carriers (data-frame), NOT visual borders — skipped.
      var bSides = [], bNames = ['Top', 'Right', 'Bottom', 'Left'];
      bNames.forEach(function (sd) {
        var bw = parseFloat(cs['border' + sd + 'Width']) || 0;
        if (bw <= 0) return;
        var bRGB = parseRGB(cs['border' + sd + 'Color']);
        if (!bRGB) return;
        bSides.push({ side: sd.toLowerCase(), color: nearestToken(bRGB, palette),
                      w: Math.max(1, Math.round(bw / DS)), style: cs['border' + sd + 'Style'] });
      });
      if (bSides.length === 4 && bSides.every(function (b) { return b.color === bSides[0].color && b.w === bSides[0].w && b.style === bSides[0].style; })) {
        out.border = { color: bSides[0].color, w: bSides[0].w, style: bSides[0].style };
      } else if (bSides.length) {
        out.border = { color: bSides[0].color, w: bSides[0].w, style: bSides[0].style,
                       sides: bSides.map(function (b) { return b.side; }) };
      }
      if (parseFloat(cs.fontSize)) { out.font = fontRole(cs.fontFamily); out.fontPx = Math.round(parseFloat(cs.fontSize) / DS * 100) / 100; }
      // §3/§8 `align` (payload v5 §B: never emitted before — every text part rendered LEFT).
      // Only non-default alignments are emitted; absence = left.
      if (/center/.test(cs.textAlign)) out.align = 'center';
      else if (/right|end/.test(cs.textAlign)) out.align = 'right';
      if (/url\(/.test(cs.backgroundImage) && /Content\//.test(cs.backgroundImage)) {
        out.image = cs.backgroundImage.replace(/^.*url\(["']?/, '').replace(/["']?\).*$/, '').replace(/^.*?(Content\/)/, '$1');
      }
      // <img src="Content/..."> — plain <img> tags carry no background-image, so check src directly
      // (fixes DEV_LOOP_MEMORY #3: the extractor used to be blind to <img>-based sprites).
      if (el.tagName === 'IMG') {
        var src = el.getAttribute('src') || '';
        if (/Content\//.test(src)) out.image = src.replace(/^.*?(Content\/)/, '$1');
      }
      applyShadowFrame(el, out);
      return out;
    }

    // §10 fidelity primitives: data-shadow="<token>" / data-frame="<token>" resolve through the shared
    // style tables into a concrete spec so the ENGINE draws the shadow / nine-patches the frame — never
    // baked into a PNG. Any element (or template item root) may carry either attribute.
    function applyShadowFrame(el, out) {
      var st = el.getAttribute('data-shadow');
      if (st && STYLE.shadows && STYLE.shadows[st]) out.shadow = Object.assign({ token: st }, STYLE.shadows[st]);
      var ft = el.getAttribute('data-frame');
      if (ft && STYLE.frames && STYLE.frames[ft]) out.frame = Object.assign({ token: ft }, STYLE.frames[ft]);
      var ib = el.getAttribute('data-image-bind');
      if (ib) out.imageBind = ib;   // engine blits Content/<resolved path>.png instead of font/vector re-draw
      // authored animation frames (2026-07-03 payload #12c): the engine cycles these Content PNGs on
      // its fixed tick. Frame 0 must equal the element's static/frozen image.
      var frs = el.getAttribute('data-frames');
      if (frs) out.frames = frs.split(',').map(function (f) { return f.trim(); }).filter(Boolean);
      // colorBind (approved 2026-07-02) — the COLOR analog of imageBind: the bound datum supplies the
      // element's accent colour (applied to the captured colour/fill/border wherever the design used
      // it), so per-core / per-attribute accents stop rendering as the first exemplar's token.
      var cb = el.getAttribute('data-color-bind');
      if (cb) out.colorBind = cb;
      // per-state styling (payload v5 §D): bare name -> style.interactionStates family reference;
      // inline JSON literal -> verbatim spec (palette tokens, never hex).
      var sts = el.getAttribute('data-states');
      if (sts) {
        if (sts.charAt(0) === '{') { try { out.states = JSON.parse(sts); } catch (e) { /* bad JSON: skip, never crash the extract */ } }
        else if (STYLE.interactionStates && STYLE.interactionStates[sts]) out.states = Object.assign({ family: sts }, STYLE.interactionStates[sts]);
      }
    }

    // container PADDING (payload #9 "citymap legend rows overlap their panel's top edge (item pad)"):
    // list items stamp inside the container's CONTENT box, so the engine needs the authored padding.
    // Emitted as item.pad = [top,right,bottom,left] design px when any side is non-zero.
    function padOf(cs) {
      var p = ['Top', 'Right', 'Bottom', 'Left'].map(function (sd) { return Math.round((parseFloat(cs['padding' + sd]) || 0) / DS); });
      return (p[0] || p[1] || p[2] || p[3]) ? p : null;
    }

    // capture a template's mini-layout from its first rendered item
    function captureTemplate(containerEl, name, templates) {
      if (templates[name]) return;
      var item = containerEl.querySelector('[data-tpl="' + name + '"]');
      if (!item) return;
      var itemRect = item.getBoundingClientRect();
      var base = { left: itemRect.left, top: itemRect.top };
      var size = [Math.round(itemRect.width * px2native / DS), Math.round(itemRect.height * px2native / DS)];
      var tpl = { size: size, parts: [] };
      var itemBinds = item.getAttribute('data-binds');
      if (itemBinds) tpl.binds = itemBinds;
      // ENVELOPE chrome (payload v5 §A — the single biggest gap): the template item's own
      // fill/border/gradient/image/shadow/frame/states, so cards stop rendering chrome-less.
      // visual() also applies data-shadow/data-frame/data-states via applyShadowFrame.
      Object.assign(tpl, visual(item));
      // sub-elements = descendants that carry direct text, an icon image, OR an explicit data-binds
      // (§9 of LAYOUT_CONTRACT + "author per-part binds" — a bound value-only node, e.g. a charge-bar
      // fill with no text, must still be captured so the game can stamp it from live data).
      // Nodes living inside a NESTED [data-container] are skipped here — they belong to that nested
      // list's own template, captured below (merchant category sections, rune-bag groups).
      item.querySelectorAll('*').forEach(function (n) {
        var nestHost = n.closest('[data-container]');
        if (nestHost && nestHost !== containerEl && item.contains(nestHost)) return;
        var directText = '';
        n.childNodes.forEach(function (c) { if (c.nodeType === 3) directText += c.nodeValue; });
        directText = directText.trim();
        var cs = getComputedStyle(n);
        var hasIcon = (/url\(/.test(cs.backgroundImage) && /Content\//.test(cs.backgroundImage)) ||
                      (n.tagName === 'IMG' && /Content\//.test(n.getAttribute('src') || ''));
        var subBinds = n.getAttribute('data-binds');
        var forced = n.hasAttribute('data-part');   // chrome-only node force-captured (payload #4/#13)
        if (!directText && !hasIcon && !subBinds && !forced) return;
        var r = n.getBoundingClientRect();
        var s = {
          rect: [Math.round((r.left - base.left) * px2native / DS), Math.round((r.top - base.top) * px2native / DS),
                 Math.round(r.width * px2native / DS), Math.round(r.height * px2native / DS)],
        };
        Object.assign(s, visual(n));
        // data-bind-gate on a BOUND node: bind = command/visibility gate, text = literal label ->
        // content. Everything else keeps the sample path (dedupeParts folds the {{ }} inner-text
        // twins by sample — see DEV_LOOP #8 note below).
        if (directText) { if (subBinds && n.hasAttribute('data-bind-gate')) s.content = directText.slice(0, 200); else s.sample = directText.slice(0, 200); }
        if (subBinds) s.binds = subBinds;
        // ADDENDUM A3 (2026-07-03): NAMED parts — a non-empty data-part value names the sub-part so
        // the engine + tools/drop_audit.py can track span-level fidelity, not just presence.
        var pName = n.getAttribute('data-part');
        if (pName) s.part = pName;
        tpl.parts.push(s);
      });
      // NESTED dynamic list inside a template item (§7 applied recursively): a template may carry a
      // [data-container][data-template] region — emitted as a `list` part (rect + flow/gap/item size)
      // with its own template captured, so the engine can stamp per-item repeats INSIDE a stamped item
      // (a shop section's ware cards, a rune group's rune rows).
      item.querySelectorAll('[data-container][data-template]').forEach(function (sub) {
        var subName = sub.getAttribute('data-template');
        var sr = sub.getBoundingClientRect();
        var scs = getComputedStyle(sub);
        var part = {
          rect: [Math.round((sr.left - base.left) * px2native / DS), Math.round((sr.top - base.top) * px2native / DS),
                 Math.round(sr.width * px2native / DS), Math.round(sr.height * px2native / DS)],
          list: { template: subName, flow: sub.getAttribute('data-flow') || 'vertical',
                  gap: Math.round((parseFloat(scs.gap) || parseFloat(scs.columnGap) || 0) / DS) },
        };
        var spad = padOf(scs);
        if (spad) part.list.pad = spad;
        if (sub.getAttribute('data-cols')) part.list.cols = parseInt(sub.getAttribute('data-cols'), 10);
        var sfi = sub.querySelector('[data-tpl="' + subName + '"]');
        if (sfi) { var sir = sfi.getBoundingClientRect(); part.list.size = [Math.round(sir.width * px2native / DS), Math.round(sir.height * px2native / DS)]; }
        var sb = sub.getAttribute('data-binds');
        if (sb) part.binds = sb;
        tpl.parts.push(part);
        captureTemplate(sub, subName, templates);
      });
      tpl.parts = dedupeParts(tpl.parts);
      templates[name] = tpl;
    }

    // DEV_LOOP_MEMORY #8: the templating runtime wraps an interpolated {{ }} hole in its own inner
    // node, so a `data-binds` wrapper's literal value is never a DIRECT text child — it lands one
    // level deeper. That inner text node has no `data-binds` of its own, so the sub-part scan captures
    // it a SECOND time (rect ~inside the wrapper's rect) as a sample-only twin of the wrapper's
    // binds-only entry. De-dupe by CONTAINMENT (not exact-rect match — the inner text bbox is tighter
    // than the padded wrapper box): fold a sample-only, non-visual (no image/shadow/frame of its own)
    // part into the smallest binds-only part whose rect contains it, and drop the duplicate. Parts that
    // carry their OWN image/shadow/frame are never folded — those are genuinely distinct sub-parts
    // (e.g. an <img> sprite nested in a bound panel), not a duplicate of the wrapper.
    function dedupeParts(parts) {
      var keep = [];
      parts.forEach(function (p, i) {
        if (p.binds) { keep.push(p); return; }
        if (p.image || p.shadow || p.frame) { keep.push(p); return; }
        var host = null, hostArea = Infinity;
        parts.forEach(function (q, j) {
          if (j === i || !q.binds || q.sample) return;
          var within = p.rect[0] >= q.rect[0] - 2 && p.rect[1] >= q.rect[1] - 2 &&
                       p.rect[0] + p.rect[2] <= q.rect[0] + q.rect[2] + 2 && p.rect[1] + p.rect[3] <= q.rect[1] + q.rect[3] + 2;
          if (!within) return;
          var area = q.rect[2] * q.rect[3];
          if (area < hostArea) { hostArea = area; host = q; }
        });
        if (host && p.sample) { host.sample = p.sample; return; }
        keep.push(p);
      });
      return keep;
    }

    var elements = [], templates = {};
    // ---- z NORMALIZATION (payload #1): ignore data-z; derive each element's PAINT ORDINAL from the
    // live DOM stacking order. Key = per-ancestor-level [effective z-index, sibling index] from the
    // screen root down; lexicographic compare, shorter (ancestor) key first — so children always paint
    // after their container and CSS z-index wins over document order at each level.
    function stackKey(el) {
      var key = [], n = el;
      while (n && n !== rootEl) {
        var cs = getComputedStyle(n);
        var z = (cs.position !== 'static' && cs.zIndex !== 'auto') ? (parseInt(cs.zIndex, 10) || 0) : 0;
        var idx = 0, s = n;
        while ((s = s.previousElementSibling)) idx++;
        key.unshift([z, idx]);
        n = n.parentElement;
      }
      return key;
    }
    function cmpKey(a, b) {
      var L = Math.max(a.length, b.length);
      for (var i = 0; i < L; i++) {
        var x = a[i] || [0, -1], y = b[i] || [0, -1];
        if (x[0] !== y[0]) return x[0] - y[0];
        if (x[1] !== y[1]) return x[1] - y[1];
      }
      return 0;
    }
    var ranked = [];
    rootEl.querySelectorAll('[data-el]').forEach(function (el) { ranked.push({ el: el, key: stackKey(el) }); });
    ranked.sort(function (a, b) { return cmpKey(a.key, b.key); });
    var zRank = new Map();
    ranked.forEach(function (r, i) { zRank.set(r.el, i); });

    rootEl.querySelectorAll('[data-el]').forEach(function (el) {
      var rc = dRect(el);
      var anchor = el.getAttribute('data-anchor') || 'TopLeft';
      // PARENT-RELATIVE offsets (§3 no-absolute-positioning, 2026-07-05): if this element nests inside
      // another [data-el], its offset is measured against that PARENT's design box (so the whole group
      // reflows as one unit) and it emits a `parent` reference. Otherwise it anchors to the screen.
      var parentEl = el.parentElement ? el.parentElement.closest('[data-el]') : null;
      var box = parentEl ? dRect(parentEl) : SCREEN_BOX;
      var e = {
        id: el.getAttribute('data-el'),
        type: el.getAttribute('data-type') || (el.hasAttribute('data-container') ? 'list' : (hasElDescendant(el) ? 'panel' : 'text')),
        anchor: anchor,
        offset: anchorOffsetBox(anchor, rc, box),
        size: [rc.w, rc.h],
        z: zRank.get(el),
      };
      if (parentEl) e.parent = parentEl.getAttribute('data-el');
      var binds = el.getAttribute('data-binds');
      if (binds) e.binds = binds;
      Object.assign(e, visual(el));   // visual() also applies data-shadow/data-frame (applyShadowFrame)
      // dynamic list (§7): container + item template + flow
      if (el.hasAttribute('data-container')) {
        var tpl = el.getAttribute('data-template');
        var firstItem = el.querySelector('[data-tpl="' + tpl + '"]');
        var cs = getComputedStyle(el);
        e.item = {
          template: tpl,
          flow: el.getAttribute('data-flow') || 'vertical',
          gap: Math.round((parseFloat(cs.gap) || parseFloat(cs.columnGap) || 0) / DS),
        };
        if (el.getAttribute('data-cols')) e.item.cols = parseInt(el.getAttribute('data-cols'), 10);
        var epad = padOf(cs);
        if (epad) e.item.pad = epad;
        if (firstItem) { var ir = firstItem.getBoundingClientRect(); e.item.size = [Math.round(ir.width * px2native / DS), Math.round(ir.height * px2native / DS)]; }
        captureTemplate(el, tpl, templates);
      }
      // leaf text: an UNBOUND literal emits `content`; a BOUND leaf emits its rendered text as
      // `sample` (payload v5 §C). Cap raised 80→200 (2026-07-02: real item/technique descriptions
      // are sometimes-long content — do not clip them).
      if (!el.hasAttribute('data-container') && (e.type === 'text' || e.type === 'button')) {
        var t = (el.textContent || '').trim().replace(/\s+/g, ' ');
        if (t) { if (binds && !el.hasAttribute('data-bind-gate')) e.sample = t.slice(0, 200); else e.content = t.slice(0, 200); }
        // payload 2026-07-03 pm residual #7: skinned buttons (autoAttackBtn/retreatBtn/closeBtn/
        // leaveBtn) author their LABEL in an inner span — the wrapper's own computed font/colour
        // (inherited display serif, ink) mis-describes the label the engine must draw. When a leaf's
        // text lives only in descendants, take font/fontPx/color/align from the descendant carrying
        // the LONGEST direct text (the label span, not a short glyph span).
        if (t) {
          var direct = '';
          el.childNodes.forEach(function (c) { if (c.nodeType === 3) direct += c.nodeValue; });
          if (!direct.trim()) {
            var srcN = null, srcLen = 0;
            el.querySelectorAll('*').forEach(function (n) {
              var dt = '';
              n.childNodes.forEach(function (c) { if (c.nodeType === 3) dt += c.nodeValue; });
              dt = dt.trim();
              if (dt.length > srcLen) { srcLen = dt.length; srcN = n; }
            });
            if (srcN) {
              var lcs = getComputedStyle(srcN);
              var lcol = nearestToken(parseRGB(lcs.color), palette);
              if (lcol) e.color = lcol;
              if (parseFloat(lcs.fontSize)) { e.font = fontRole(lcs.fontFamily); e.fontPx = Math.round(parseFloat(lcs.fontSize) / DS * 100) / 100; }
              if (/center/.test(lcs.textAlign)) e.align = 'center';
              else if (/right|end/.test(lcs.textAlign)) e.align = 'right';
            }
          }
        }
      }
      // ADDENDUM A3 (2026-07-03): element-level NAMED PARTS — an element (not a list container) may
      // carry [data-part] descendants (previewBudgetTile's value/label spans): emit them as e.parts
      // (rect element-relative, real font/px/colour, own sample/content/binds) so value+label stop
      // flattening into one text run. When parts carry the element's text, the element-level
      // sample/content is DROPPED (the parts are the text — keeping both would double-draw).
      if (!el.hasAttribute('data-container')) {
        var pNodes = [];
        el.querySelectorAll('[data-part]').forEach(function (n) {
          if (!(n.getAttribute('data-part') || '').trim()) return;
          if (n.closest('[data-el]') !== el) return;            // belongs to a nested element
          if (n.closest('[data-tpl]')) return;                  // belongs to a template item
          pNodes.push(n);
        });
        if (pNodes.length) {
          var eRect = el.getBoundingClientRect();
          e.parts = pNodes.map(function (n) {
            var r2 = n.getBoundingClientRect();
            var s2 = {
              part: n.getAttribute('data-part').trim(),
              rect: [Math.round((r2.left - eRect.left) * px2native / DS), Math.round((r2.top - eRect.top) * px2native / DS),
                     Math.round(r2.width * px2native / DS), Math.round(r2.height * px2native / DS)],
            };
            Object.assign(s2, visual(n));
            var t2 = (n.textContent || '').trim().replace(/\s+/g, ' ');
            var b2 = n.getAttribute('data-binds');
            if (t2) { if (b2 && !n.hasAttribute('data-bind-gate')) s2.sample = t2.slice(0, 200); else s2.content = t2.slice(0, 200); }
            if (b2) s2.binds = b2;
            return s2;
          });
          var partsHaveText = e.parts.some(function (p) { return p.sample || p.content; });
          if (partsHaveText) { delete e.sample; delete e.content; }
        }
      }
      elements.push(el = e);
    });
    elements.sort(function (a, b) { return a.z - b.z; });

    return {
      id: rootEl.getAttribute('data-screen'),
      screen: { designSize: [designW, designH], elements: elements },
      templates: templates,
    };
  }

  // mirror style_tokens.js into the layout.json `style` block (§8)
  function RB_styleBlock(STYLE) {
    return { palette: STYLE.palette, fonts: STYLE.fonts, partStates: STYLE.partStates, pip: STYLE.pip,
             shadows: STYLE.shadows, frames: STYLE.frames, pulse: STYLE.pulse,
             interactionStates: STYLE.interactionStates };
  }

  root.RB_extractScreen = RB_extractScreen;
  root.RB_styleBlock = RB_styleBlock;
})(typeof window !== 'undefined' ? window : this);
