// scheduleboard.touchfix.v3.js
// Prevent Mud DnD from hijacking touch scrolling:
// - Only allow drag if the touch/pointer started on .sch-drag-handle
// - Do NOT preventDefault (so native scrolling still works)
// Defensive: never throw (Blazor Server safety)
(function () {
  "use strict";

  function safe(fn) { try { fn(); } catch (e) { console.warn("[ScheduleBoard touchfix]", e); } }

  var armed = false;

  function isTouch(ev) {
    return ev && (ev.pointerType === "touch" || ev.type === "touchstart" || ev.type === "touchmove");
  }

  function startedOnHandle(target) {
    return !!(target && target.closest && target.closest(".sch-drag-handle"));
  }

  function inWorkItem(target) {
    return !!(target && target.closest && target.closest(".sch-item"));
  }

  function onDown(ev) {
    safe(function () {
      if (!isTouch(ev)) return;
      armed = startedOnHandle(ev.target);
      if (!armed && inWorkItem(ev.target)) {
        // Block Mud's drag-start listeners from seeing this touch sequence
        ev.stopPropagation();
        if (ev.stopImmediatePropagation) ev.stopImmediatePropagation();
      }
    });
  }

  function onMove(ev) {
    safe(function () {
      if (!isTouch(ev)) return;
      if (!armed && inWorkItem(ev.target)) {
        // Keep blocking during move so Mud doesn't start a drag on move-threshold
        ev.stopPropagation();
        if (ev.stopImmediatePropagation) ev.stopImmediatePropagation();
      }
    });
  }

  function onUp() { armed = false; }

  function attach() {
    safe(function () {
      document.addEventListener("pointerdown", onDown, true);
      document.addEventListener("touchstart", onDown, true);

      document.addEventListener("pointermove", onMove, true);
      document.addEventListener("touchmove", onMove, true);

      document.addEventListener("pointerup", onUp, true);
      document.addEventListener("touchend", onUp, true);
      document.addEventListener("touchcancel", onUp, true);
    });
  }

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", attach);
  else attach();
})();
