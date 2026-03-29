// wwwroot/js/scheduleboard.scroll.js (v6)
// Supports: full-width Unscheduled tray (col-unscheduled not inside .sch-columns).
// Safety: never throw (Blazor Server circuit protection).

(function () {
  const root = (window.ScheduleBoard = window.ScheduleBoard || {});

  function safe(fn, fallback) { try { return fn(); } catch { return fallback; } }
  function qs(sel, within) { return safe(() => (within || document).querySelector(sel), null); }
  function qsa(sel, within) { return safe(() => Array.from((within || document).querySelectorAll(sel)), []); }
  function byId(id) { return safe(() => document.getElementById(id), null); }
  function clamp(n, min, max) { return Math.max(min, Math.min(max, n)); }
  function isRenderable(el) {
    return safe(() => {
      if (!el) return false;
      const rect = el.getBoundingClientRect();
      return rect.width > 0 && rect.height > 0;
    }, false);
  }
  function firstRenderable(elements) {
    return safe(() => elements.find(isRenderable) || elements[0] || null, null);
  }

  function getScrollParent(el) {
    return safe(() => {
      let cur = el;
      while (cur && cur !== document.body) {
        const cs = getComputedStyle(cur);
        const oy = cs.overflowY;
        if ((oy === "auto" || oy === "scroll") && cur.scrollHeight > cur.clientHeight + 2) return cur;
        cur = cur.parentElement;
      }
      return null;
    }, null);
  }
  window.cbSchedule = window.cbSchedule || {};
window.cbSchedule.detectMobile = function () {
  return window.matchMedia("(max-width: 900px)").matches;
};

  function scrollIntoScroller(scroller, target, pad) {
    return safe(() => {
      if (!scroller || !target) return false;
      const sRect = scroller.getBoundingClientRect();
      const tRect = target.getBoundingClientRect();
      const p = (typeof pad === "number") ? pad : 12;

      // target top relative to scroller content
      const deltaTop = (tRect.top - sRect.top);
      let targetTop = scroller.scrollTop + deltaTop - p;

      const maxTop = scroller.scrollHeight - scroller.clientHeight;
      targetTop = clamp(targetTop, 0, Math.max(0, maxTop));
      scroller.scrollTo({ top: targetTop, behavior: "smooth" });
      return true;
    }, false);
  }

  root.scrollToColumn = function (dayKey, opts) {
    return safe(() => {
      const key = String(dayKey || "");
      const behavior = (opts && opts.behavior) || "smooth";
      const align = (opts && opts.align) || "center";
      const pad = (opts && typeof opts.pad === "number") ? opts.pad : 16;

      // Unscheduled tray is full-width and not inside the horizontal columns scroller.
      if (key === "unscheduled") {
        const tray = firstRenderable([
          byId("col-unscheduled"),
          byId("col-unscheduled-mobile"),
          ...qsa("[data-unscheduled-tray='true']")
        ].filter(Boolean));
        if (!tray) return true;
        const scroller = getScrollParent(tray);
        if (scroller) scrollIntoScroller(scroller, tray, 8);
        return true;
      }

      const container = qs(".sch-columns");
      const col = byId(`col-${key}`);
      if (!col) return false;

      // Horizontal scroll if container exists.
      if (container) {
        const cRect = container.getBoundingClientRect();
        const colRect = col.getBoundingClientRect();
        const currentLeft = container.scrollLeft;
        const deltaLeft = colRect.left - cRect.left;

        let targetLeft;
        if (align === "start") targetLeft = currentLeft + deltaLeft - pad;
        else if (align === "end") targetLeft = currentLeft + deltaLeft - (cRect.width - colRect.width) + pad;
        else targetLeft = currentLeft + deltaLeft - (cRect.width / 2) + (colRect.width / 2);

        const maxLeft = container.scrollWidth - container.clientWidth;
        targetLeft = clamp(targetLeft, 0, Math.max(0, maxLeft));
        container.scrollTo({ left: targetLeft, behavior });
      } else {
        // If no horizontal scroller (mobile), ensure the selected col is visible within its scroll parent.
        const scroller = getScrollParent(col);
        if (scroller) scrollIntoScroller(scroller, col, 8);
      }

      return true;
    }, false);
  };

  function scrollDropzoneToCard(dz, card, opts) {
    return safe(() => {
      const behavior = (opts && opts.behavior) || "smooth";
      const pad = (opts && typeof opts.pad === "number") ? opts.pad : 12;
      const align = (opts && opts.align) || "center";

      const dzRect = dz.getBoundingClientRect();
      const cardRect = card.getBoundingClientRect();
      const fullyVisible = cardRect.top >= dzRect.top + pad && cardRect.bottom <= dzRect.bottom - pad;

      if (!fullyVisible) {
        const deltaTop = cardRect.top - dzRect.top;
        let targetTop = dz.scrollTop + deltaTop - pad;
        const maxTop = dz.scrollHeight - dz.clientHeight;
        targetTop = clamp(targetTop, 0, Math.max(0, maxTop));
        dz.scrollTo({ top: targetTop, behavior });
      }
      return true;
    }, false);
  }

  root.scrollToWorkOrderInColumn = function (dayKey, workOrderId, opts) {
    return safe(() => {
      // For tray usability on mobile: if unscheduled, default to top-align
      if (dayKey === "unscheduled" && (!opts || !opts.align)) {
        opts = Object.assign({ align: "top" }, opts || {});
      }
      const key = String(dayKey || "");
      let col = byId(`col-${key}`);
      if (!col && key === "unscheduled") {
        col = firstRenderable(qsa("[data-unscheduled-tray='true']"));
      }
      if (!col) return false;

      const dz = qs(".sch-dropzone", col);
      if (!dz) return false;

      const card = byId(`wo-${workOrderId}`);
      if (!card) return false;

      if (dz.contains(card)) return scrollDropzoneToCard(dz, card, opts);

      const nearest = safe(() => card.closest(".sch-dropzone"), null);
      if (nearest) return scrollDropzoneToCard(nearest, card, opts);

      return false;
    }, false);
  };

  root.pulseWorkOrder = function (workOrderId) {
    return safe(() => {
      const el = byId(`wo-${workOrderId}`);
      if (!el) return false;

      const cls = "wo-highlight";
      el.classList.remove(cls);
      void el.offsetWidth;
      el.classList.add(cls);

      // Inline fallback (works even if highlight CSS isn't present)
      const prev = {
        outline: el.style.outline,
        outlineOffset: el.style.outlineOffset,
        boxShadow: el.style.boxShadow,
        borderRadius: el.style.borderRadius,
        transition: el.style.transition,
        transform: el.style.transform
      };

      el.style.transition = "box-shadow 250ms ease, transform 250ms ease";
      el.style.outline = "2px solid rgba(59,130,246,0.85)";
      el.style.outlineOffset = "2px";
      el.style.borderRadius = "12px";
      el.style.boxShadow = "0 0 0 6px rgba(59,130,246,0.20)";
      el.style.transform = "scale(1.01)";

      setTimeout(() => {
        el.style.boxShadow = "0 0 0 0 rgba(59,130,246,0)";
        el.style.transform = "scale(1)";
      }, 500);

      setTimeout(() => {
        el.classList.remove(cls);
        el.style.outline = prev.outline;
        el.style.outlineOffset = prev.outlineOffset;
        el.style.boxShadow = prev.boxShadow;
        el.style.borderRadius = prev.borderRadius;
        el.style.transition = prev.transition;
        el.style.transform = prev.transform;
      }, 1400);

      return true;
    }, false);
  };

  root.safeFitBounds = function (map, bounds, padding) {
    return safe(() => {
      if (!map || !bounds) return false;
      try {
        if (typeof bounds.isValid === "function" && !bounds.isValid()) return false;
        if (typeof map.invalidateSize === "function") map.invalidateSize(true);
        map.fitBounds(bounds, { padding: padding || [24, 24] });
        return true;
      } catch {
        return false;
      }
    }, false);
  };
})();
