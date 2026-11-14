// Injected into every page: D-pad navigation between links/buttons, plus the stick cursor.
(() => {
  if (window.__couch) return;

  const SEL = 'a[href],button,input:not([type=hidden]),select,textarea,summary,' +
    '[tabindex]:not([tabindex="-1"]),[role=button],[role=link],[role=tab],[role=menuitem],' +
    '[role=option],[contenteditable="true"],[contenteditable=""]';
  let cur = null;

  const style = () => {
    if (document.getElementById('__couch_style')) return;
    const st = document.createElement('style');
    st.id = '__couch_style';
    st.textContent =
      '.__couch_focus{outline:3px solid #3d8bff !important;outline-offset:2px !important;' +
      'box-shadow:0 0 0 6px rgba(61,139,255,.35) !important;border-radius:6px !important}' +
      '#__couch_cursor{position:fixed;width:22px;height:22px;margin:-11px 0 0 -11px;border-radius:50%;' +
      'border:3px solid #fff;background:rgba(61,139,255,.55);box-shadow:0 0 8px rgba(0,0,0,.6);' +
      'pointer-events:none;z-index:2147483647;display:none}';
    (document.head || document.documentElement).appendChild(st);
  };

  const shown = el => {
    const r = el.getBoundingClientRect();
    if (r.width < 4 || r.height < 4) return false;
    const s = getComputedStyle(el);
    return s.visibility !== 'hidden' && s.display !== 'none' && parseFloat(s.opacity || '1') > 0.05;
  };
  const inView = r => r.bottom > 0 && r.top < innerHeight && r.right > 0 && r.left < innerWidth;
  const list = () => Array.from(document.querySelectorAll(SEL)).filter(e => !e.disabled && shown(e));
  const center = r => [r.left + r.width / 2, r.top + r.height / 2];
  const valid = () => cur && cur.isConnected && shown(cur) && inView(cur.getBoundingClientRect());

  const set = el => {
    if (cur) cur.classList.remove('__couch_focus');
    cur = el;
    if (!el) return;
    style();
    el.classList.add('__couch_focus');
    try { el.focus({ preventScroll: true }); } catch (e) {}
    el.scrollIntoView({ block: 'nearest', inline: 'nearest' });
  };

  window.__couch = {
    move(dir) {
      const els = list();
      if (!valid()) {
        const vis = els.filter(e => inView(e.getBoundingClientRect()));
        vis.sort((a, b) => {
          const ra = a.getBoundingClientRect(), rb = b.getBoundingClientRect();
          return (ra.top - rb.top) || (ra.left - rb.left);
        });
        set(vis[0] || null);
        return !!cur;
      }
      const [cx, cy] = center(cur.getBoundingClientRect());
      let best = null, score = Infinity;
      for (const e of els) {
        if (e === cur || e.contains(cur) || cur.contains(e)) continue;
        const [x, y] = center(e.getBoundingClientRect());
        const dx = x - cx, dy = y - cy;
        let main, cross;
        if (dir === 'up')        { if (dy > -4) continue; main = -dy; cross = Math.abs(dx); }
        else if (dir === 'down') { if (dy < 4)  continue; main = dy;  cross = Math.abs(dx); }
        else if (dir === 'left') { if (dx > -4) continue; main = -dx; cross = Math.abs(dy); }
        else                     { if (dx < 4)  continue; main = dx;  cross = Math.abs(dy); }
        // Don't jump more than a screen away: let the app scroll instead.
        if ((dir === 'up' || dir === 'down') && main > innerHeight * 0.9) continue;
        const sc = main + cross * 2.5;
        if (sc < score) { score = sc; best = e; }
      }
      if (!best) return false;
      set(best);
      return true;
    },

    rect() {
      if (!valid()) return null;
      const [x, y] = center(cur.getBoundingClientRect());
      return {
        x: Math.max(1, Math.min(innerWidth - 1, x)),
        y: Math.max(1, Math.min(innerHeight - 1, y))
      };
    },

    editable() {
      const e = document.activeElement;
      if (!e) return false;
      if (e.isContentEditable || e.tagName === 'TEXTAREA') return true;
      if (e.tagName === 'INPUT')
        return !/^(button|submit|reset|checkbox|radio|range|color|file|image|hidden)$/i.test(e.type || 'text');
      return false;
    },

    cursor(x, y) {
      style();
      let c = document.getElementById('__couch_cursor');
      if (!c) {
        c = document.createElement('div');
        c.id = '__couch_cursor';
        document.documentElement.appendChild(c);
      }
      c.style.left = x + 'px';
      c.style.top = y + 'px';
      c.style.display = 'block';
    },

    hideCursor() {
      const c = document.getElementById('__couch_cursor');
      if (c) c.style.display = 'none';
    },

    clearFocus() {
      if (cur) cur.classList.remove('__couch_focus');
      cur = null;
    }
  };
})();