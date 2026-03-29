/**
 * codeBlue.maps (Leaflet renderer + optional Google geocode fallback)
 *
 * Public API:
 *   codeBlue.maps.render(elementId, address, label)
 *   codeBlue.maps.render(elementId, address, label, lat, lng)
 *   codeBlue.maps.rerenderLast([elementId])
 *   codeBlue.maps.invalidate([elementId])
 *   codeBlue.maps.destroy([elementId])
 *
 * Scheduler API:
 *   cbScheduleMap.renderPins(pins, defaultLat, defaultLng)
 *   cbScheduleMap.invalidate()
 *   cbScheduleMap.isMobile()
 *   cbScheduleMap.unassignPin(id)
 *
 * Notes:
 * - Designed for Blazor + MudHidden breakpoint swaps where DOM nodes are created/destroyed.
 * - If the target element exists but is not renderable (0x0 or display:none), we keep retrying.
 * - Map instances are cached PER elementId, but will be recreated if the container DOM node changes.
 */
(function () {
  "use strict";

  const root = (window.codeBlue = window.codeBlue || {});
  const maps = (root.maps = root.maps || {});

  // ---------- config ----------
  const WAIT_TIMEOUT_MS = 15000;
  const WAIT_INTERVAL_MS = 75;
  const MIN_PX = 40;
  const DEFAULT_ZOOM = 16;
  const SCHEDULE_ELEMENT_IDS = ["cbScheduleMap", "cbScheduleMapMobile", "cbScheduleMapDesktop"];

  // ---------- internal state ----------
  const state = {
    byId: new Map(),      // elementId -> { map, marker, container }
    lastArgs: new Map(),  // elementId -> { elementId, address, label, lat, lng, zoom }
    pending: new Set(),   // elementId(s) waiting to become renderable
    observerStarted: false,
    resizeHandlerStarted: false,
    lastGlobal: null,

    schedule: {
      map: null,
      markers: [],
      lastPins: [],
      targetElementId: null,
      defaultLat: 36.175052,
      defaultLng: -115.156251,
      resizeObserver: null,
      dotNetRef: null,
      popupRestoreViewport: null,
      defaultZoom: 10
    }
  };

  function warn(...a) {
    console.warn("[codeBlue.maps]", ...a);
  }

  function hasLeaflet() {
    return !!(window.L && typeof window.L.map === "function");
  }

  function getEl(id) {
    if (!id) return null;
    return document.getElementById(id);
  }

  function isRenderable(el) {
    if (!el) return false;
    const r = el.getBoundingClientRect();
    return r.width >= MIN_PX && r.height >= MIN_PX;
  }

  function getScheduleEl(preferredId) {
    if (preferredId) {
      const el = getEl(preferredId);
      if (el) return el;
    }

    for (const id of SCHEDULE_ELEMENT_IDS) {
      const el = getEl(id);
      if (el) return el;
    }

    return null;
  }

  function getRenderableScheduleEl(preferredId) {
    if (preferredId) {
      const el = getEl(preferredId);
      if (isRenderable(el)) return el;
    }

    for (const id of SCHEDULE_ELEMENT_IDS) {
      const el = getEl(id);
      if (isRenderable(el)) return el;
    }

    return getScheduleEl(preferredId);
  }

  function toNum(v) {
    if (v === null || v === undefined) return null;
    if (typeof v === "number") return Number.isFinite(v) ? v : null;
    const s = String(v).trim();
    if (!s) return null;
    const n = Number(s);
    return Number.isFinite(n) ? n : null;
  }

  function normalizeArgs(elementId, address, label, lat, lng, zoom) {
    const nLat = toNum(lat);
    const nLng = toNum(lng);
    const nZoom = toNum(zoom) || DEFAULT_ZOOM;

    return {
      elementId: elementId || "",
      address: (address ?? "").toString(),
      label: (label ?? "").toString(),
      lat: nLat,
      lng: nLng,
      zoom: nZoom,
    };
  }

  function waitFor(checkFn, timeoutMs) {
    const start = Date.now();
    return new Promise((resolve) => {
      const tick = () => {
        try {
          const v = checkFn();
          if (v) return resolve(v);
        } catch {
          // ignore
        }
        if (Date.now() - start >= timeoutMs) return resolve(null);
        setTimeout(tick, WAIT_INTERVAL_MS);
      };
      tick();
    });
  }

  function ensureObserver() {
    if (state.observerStarted) return;
    state.observerStarted = true;

    const mo = new MutationObserver(() => {
      if (state.pending.size === 0) return;
      for (const id of Array.from(state.pending)) {
        tryRenderFromLast(id);
      }

      // Retry scheduler map when DOM changes too
      if (state.schedule.lastPins && state.schedule.lastPins.length > 0) {
        try {
            renderSchedulePinsInternal(
              state.schedule.targetElementId,
              state.schedule.lastPins,
              state.schedule.defaultLat,
              state.schedule.defaultLng
            );
        } catch {
          // ignore
        }
      }
    });

    try {
      mo.observe(document.body, { childList: true, subtree: true, attributes: true });
    } catch {
      // ignore
    }

    if (!state.resizeHandlerStarted) {
      state.resizeHandlerStarted = true;
      window.addEventListener("resize", () => {
        maps.rerenderLast();
        maps.invalidate();

        if (window.cbScheduleMap && typeof window.cbScheduleMap.invalidate === "function") {
          window.cbScheduleMap.invalidate();
        }
      });
    }
  }

  // ============================================================
  // Generic shared map renderer
  // ============================================================

  function ensureMapInstance(elementId, lat, lng, label, zoom) {
    const el = getEl(elementId);
    if (!el) return null;

    const existing = state.byId.get(elementId);
    if (existing && existing.map) {
      const container = existing.map.getContainer ? existing.map.getContainer() : existing.container;
      if (container === el) return existing;

      try { existing.map.remove(); } catch {}
      state.byId.delete(elementId);
    }

    const map = L.map(el, {
      zoomControl: true,
      attributionControl: true,
      zoomDelta: 0.25,
      zoomSnap: 0.25
    }).setView([lat, lng], zoom || DEFAULT_ZOOM);

    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      maxZoom: 19,
      attribution: "&copy; OpenStreetMap contributors",
    }).addTo(map);

    const marker = L.marker([lat, lng]).addTo(map);
    if (label) marker.bindPopup(String(label));

    const rec = { map, marker, container: el };
    state.byId.set(elementId, rec);

    if (window.ResizeObserver) {
      try {
        const ro = new ResizeObserver(() => maps.invalidate(elementId));
        ro.observe(el);
        rec._ro = ro;
      } catch {
        // ignore
      }
    }

    return rec;
  }

  function updateMap(rec, lat, lng, label, zoom) {
    try {
      if (rec.marker) {
        rec.marker.setLatLng([lat, lng]);
        if (label) rec.marker.bindPopup(String(label));
      }
      rec.map.setView([lat, lng], zoom || rec.map.getZoom() || DEFAULT_ZOOM, { animate: false });
      maps.invalidate();
    } catch {
      try { rec.map.remove(); } catch {}
    }
  }

  async function geocodeIfAvailable(address) {
    try {
      if (!(window.google && google.maps && google.maps.Geocoder)) return null;
      const geocoder = new google.maps.Geocoder();
      const res = await geocoder.geocode({ address });
      if (!res || !res.results || !res.results[0] || !res.results[0].geometry) return null;
      const loc = res.results[0].geometry.location;
      const lat = typeof loc.lat === "function" ? loc.lat() : null;
      const lng = typeof loc.lng === "function" ? loc.lng() : null;
      if (!Number.isFinite(lat) || !Number.isFinite(lng)) return null;
      return { lat, lng };
    } catch {
      return null;
    }
  }

  async function renderInternal(opts) {
    if (!hasLeaflet()) {
      warn("Leaflet not loaded (window.L missing). Make sure leaflet.js + leaflet.css are included.");
      return;
    }

    ensureObserver();

    const el = await waitFor(() => {
      const e = getEl(opts.elementId);
      return e ? e : null;
    }, WAIT_TIMEOUT_MS);

    if (!el) {
      return;
    }

    const renderable = await waitFor(() => isRenderable(getEl(opts.elementId)) ? true : null, WAIT_TIMEOUT_MS);
    if (!renderable) {
      state.pending.add(opts.elementId);
      return;
    }

    state.pending.delete(opts.elementId);

    let lat = opts.lat;
    let lng = opts.lng;

    if (!(typeof lat === "number" && typeof lng === "number")) {
      if (opts.address) {
        const pt = await geocodeIfAvailable(opts.address);
        if (pt) {
          lat = pt.lat;
          lng = pt.lng;
        }
      }
    }

    if (!(typeof lat === "number" && typeof lng === "number")) {
      warn("No lat/lng available; cannot render map for:", opts.elementId, opts.address);
      return;
    }

    const rec = ensureMapInstance(opts.elementId, lat, lng, opts.label, opts.zoom);
    if (!rec) {
      warn("Failed to create map instance for:", opts.elementId);
      return;
    }

    updateMap(rec, lat, lng, opts.label, opts.zoom);
  }

  async function tryRenderFromLast(elementId) {
    const args = state.lastArgs.get(elementId);
    if (!args) return;
    await renderInternal(args);
  }

  maps.render = function (elementId, address, label, lat, lng) {
    const opts = normalizeArgs(elementId, address, label, lat, lng);
    if (!opts.elementId) return;

    state.lastArgs.set(opts.elementId, opts);
    state.lastGlobal = opts;

    renderInternal(opts);
  };

  maps.rerenderLast = function (elementId) {
    if (elementId) {
      const args = state.lastArgs.get(elementId);
      if (!args) return;
      renderInternal(args);
      return;
    }

    const ids = new Set([...state.lastArgs.keys(), ...state.pending]);
    for (const id of ids) {
      const args = state.lastArgs.get(id);
      if (args) renderInternal(args);
    }
  };

  maps.invalidate = function (elementId) {
    const doOne = (id) => {
      const rec = state.byId.get(id);
      if (!rec || !rec.map) return;
      try {
        rec.map.invalidateSize(true);
      } catch {}
    };

    if (elementId) {
      doOne(elementId);
      return;
    }

    for (const id of state.byId.keys()) doOne(id);
  };

  maps.destroy = function (elementId) {
    const doOne = (id) => {
      const rec = state.byId.get(id);
      if (!rec) return;
      try { rec._ro && rec._ro.disconnect(); } catch {}
      try { rec.map && rec.map.remove(); } catch {}
      state.byId.delete(id);
    };

    if (elementId) {
      doOne(elementId);
      state.lastArgs.delete(elementId);
      state.pending.delete(elementId);
      return;
    }

    for (const id of Array.from(state.byId.keys())) doOne(id);
    state.lastArgs.clear();
    state.pending.clear();
  };

  maps.elementExists = function (elementId) {
    return !!getEl(elementId);
  };

  // ============================================================
  // Scheduler map API (merged from scheduleboard.map.MODE2.ultimate.js)
  // ============================================================

  function safeSchedule(fn) {
    try { return fn(); } catch (e) { console.warn("[cbScheduleMap]", e); return null; }
  }

  function escapeHtml(s) {
    return String(s ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#039;");
  }

  function normalizeBadge(b) {
    return String(b ?? "").trim();
  }

  const DAY_COLORS = {
    "m":  "rgba(37,99,235,0.95)",   // Mon - blue
    "t":  "rgba(22,163,74,0.95)",   // Tue - green
    "w":  "rgba(245,158,11,0.95)",  // Wed - amber
    "th": "rgba(147,51,234,0.95)",  // Thu - purple
    "f":  "rgba(239,68,68,0.95)",   // Fri - red
    "1":  "rgba(37,99,235,0.95)",
    "2":  "rgba(22,163,74,0.95)",
    "3":  "rgba(245,158,11,0.95)",
    "4":  "rgba(147,51,234,0.95)",
    "5":  "rgba(239,68,68,0.95)"
  };

  const UNSCHEDULED_COLOR = "rgba(107,114,128,0.95)";
  const ROUTE_COLOR_DEFAULT = "rgba(37,99,235,0.95)";

  function badgeColor(pin, badgeText) {
    const dayKey = String(pin.dayKey ?? pin.DayKey ?? "").toLowerCase();
    const isUnscheduled = (pin.isUnscheduled ?? pin.IsUnscheduled) === true || dayKey === "unscheduled";
    if (isUnscheduled) return UNSCHEDULED_COLOR;

    const b = String(badgeText ?? "").trim().toLowerCase();
    if (b in DAY_COLORS) return DAY_COLORS[b];

    const routeDayBadge = String(pin.routeDayBadge ?? pin.RouteDayBadge ?? "").trim().toLowerCase();
    if (routeDayBadge && (routeDayBadge in DAY_COLORS)) return DAY_COLORS[routeDayBadge];

    return ROUTE_COLOR_DEFAULT;
  }

  function makeBadgeIcon(badgeText, size, bg) {
    if (!badgeText) return null;

    const s = Number(size) || 34;
    const raw = normalizeBadge(badgeText);
    const t = escapeHtml(raw);

    const fontSize = raw.length >= 3
      ? Math.max(10, Math.round(s * 0.35))
      : Math.max(11, Math.round(s * 0.45));

    const color = String(bg || ROUTE_COLOR_DEFAULT);

    const html =
      `<div style="
          width:${s}px;height:${s}px;border-radius:999px;
          background:${color};
          color:#fff;font-weight:900;
          display:flex;align-items:center;justify-content:center;
          box-shadow:0 6px 14px rgba(0,0,0,0.22);
          border:2px solid rgba(255,255,255,0.92);
          font-size:${fontSize}px;
          line-height:1;
          user-select:none;
        ">${t}</div>`;

    return L.divIcon({
      html,
      className: "",
      iconSize: [s, s],
      iconAnchor: [Math.round(s / 2), s],
      popupAnchor: [0, -s]
    });
  }

  function safeFitBounds(m, pts) {
    safeSchedule(function () {
      if (!m || !pts || pts.length === 0) return;

      if (pts.length === 1) {
        m.setView(pts[0], 13);
        return;
      }

      try {
        const b = L.latLngBounds(pts);
        if (b && b.isValid && b.isValid()) {
          m.fitBounds(b, { padding: [24, 24] });
        }
      } catch (e) {
        console.warn("[cbScheduleMap] fitBounds failed:", e);
      }
    });
  }

  function preserveViewport(m) {
    return safeSchedule(function () {
      if (!m) return null;
      return {
        center: m.getCenter ? m.getCenter() : null,
        zoom: typeof m.getZoom === "function" ? m.getZoom() : null
      };
    });
  }

  function restoreViewport(m, viewport) {
    safeSchedule(function () {
      if (!m || !viewport || !viewport.center || !Number.isFinite(viewport.zoom)) return;
      m.setView(viewport.center, viewport.zoom, { animate: false });
    });
  }

  function invokeScheduleDotNet(methodName, ...args) {
    if (!state.schedule.dotNetRef || typeof state.schedule.dotNetRef.invokeMethodAsync !== "function") {
      return;
    }

    try {
      const pending = state.schedule.dotNetRef.invokeMethodAsync(methodName, ...args);
      if (pending && typeof pending.catch === "function") {
        pending.catch(function (err) {
          const msg = err && err.message ? String(err.message) : String(err || "");
          if (msg.includes("There is no tracked object") || msg.includes("already disposed")) {
            state.schedule.dotNetRef = null;
            return;
          }

          console.warn("[cbScheduleMap] dotNet callback failed:", err);
        });
      }
    } catch (err) {
      const msg = err && err.message ? String(err.message) : String(err || "");
      if (msg.includes("There is no tracked object") || msg.includes("already disposed")) {
        state.schedule.dotNetRef = null;
        return;
      }

      console.warn("[cbScheduleMap] dotNet callback threw:", err);
    }
  }

  function buildPopupHtml(pin, isUnscheduled) {
    const titleText = pin.label ?? pin.Label ?? "";
    const addrText = pin.address ?? pin.Address ?? "";
    const svcText = pin.serviceDescription ?? pin.ServiceDescription ?? "";
    const dayKey = String(pin.dayKey ?? pin.DayKey ?? "").toLowerCase();
    const shouldShowUnassign = dayKey !== "unscheduled" && isUnscheduled !== true;

    const title = titleText ? `<div style="font-weight:900;margin-bottom:4px;">${escapeHtml(titleText)}</div>` : "";
    const addr = addrText ? `<div style="margin-bottom:6px;">${escapeHtml(addrText)}</div>` : "";
    const svc = svcText ? `<div style="margin-bottom:10px;opacity:0.92;">${escapeHtml(svcText)}</div>` : "";

    const btn = shouldShowUnassign
      ? `<button type="button"
           onclick="window.cbScheduleMap && window.cbScheduleMap.unassignPin && window.cbScheduleMap.unassignPin('${escapeHtml(pin.id ?? pin.Id)}')"
           style="width:100%;padding:8px 10px;border-radius:10px;border:1px solid rgba(0,0,0,0.18);font-weight:800;cursor:pointer;">
           Unassign
         </button>`
      : "";

    return `${title}${addr}${svc}${btn}`;
  }

  function ensureScheduleMap(elementId, defaultLat, defaultLng) {
    return safeSchedule(function () {
      const el = getRenderableScheduleEl(elementId);
      if (!el || typeof L === "undefined") return null;
      if (!isRenderable(el)) return null;

      if (state.schedule.map) {
        const container = state.schedule.map.getContainer ? state.schedule.map.getContainer() : null;
        if (container === el) return state.schedule.map;

        try { state.schedule.resizeObserver && state.schedule.resizeObserver.disconnect(); } catch {}
        try { state.schedule.map.remove(); } catch {}

        state.schedule.map = null;
        state.schedule.markers = [];
        state.schedule.resizeObserver = null;
      }

      state.schedule.defaultLat = Number(defaultLat) || state.schedule.defaultLat;
      state.schedule.defaultLng = Number(defaultLng) || state.schedule.defaultLng;
      state.schedule.targetElementId = el.id;
      state.schedule.defaultZoom = Number(state.schedule.defaultZoom) || 10;

      const m = L.map(el, {
        zoomControl: true,
        zoomDelta: 0.25,
        zoomSnap: 0.25
      });
      state.schedule.map = m;

      L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: "&copy; OpenStreetMap contributors"
      }).addTo(m);

      m.setView([state.schedule.defaultLat, state.schedule.defaultLng], state.schedule.defaultZoom);

      // panes so unscheduled pins always sit on top
      if (!m.getPane("cbAssignedPins")) {
        const assignedPane = m.createPane("cbAssignedPins");
        assignedPane.style.zIndex = 640;
      }

      if (!m.getPane("cbUnscheduledPins")) {
        const unscheduledPane = m.createPane("cbUnscheduledPins");
        unscheduledPane.style.zIndex = 650;
      }

      if (window.ResizeObserver) {
        try {
          const ro = new ResizeObserver(() => {
            try { m.invalidateSize(true); } catch {}
          });
          ro.observe(el);
          state.schedule.resizeObserver = ro;
        } catch {}
      }

      setTimeout(() => {
        try { m.invalidateSize(true); } catch {}
      }, 50);

      setTimeout(() => {
        try { m.invalidateSize(true); } catch {}
      }, 200);

      return m;
    });
  }

  function clearScheduleMarkers() {
    safeSchedule(function () {
      for (const m of state.schedule.markers) {
        try { m.remove(); } catch {}
      }
      state.schedule.markers = [];
    });
  }

  async function renderSchedulePinsInternal(elementId, pins, defaultLat, defaultLng, options) {
    if (!hasLeaflet()) {
      warn("Leaflet not loaded for scheduler map.");
      return;
    }

    ensureObserver();

    const el = await waitFor(() => {
      const e = getScheduleEl(elementId);
      return e ? e : null;
    }, WAIT_TIMEOUT_MS);

    if (!el) {
      return;
    }

    const renderable = await waitFor(() => getRenderableScheduleEl(elementId) ? true : null, WAIT_TIMEOUT_MS);
    if (!renderable) {
      state.pending.add(elementId || "cbScheduleMap");
      return;
    }

    state.pending.delete(elementId || "cbScheduleMap");

    const m = ensureScheduleMap(elementId, defaultLat, defaultLng);
    if (!m) {
      warn("Failed to create scheduler map.");
      return;
    }

    const requestedDefaultZoom = Number(options && options.defaultZoom);
    if (Number.isFinite(requestedDefaultZoom)) {
      state.schedule.defaultZoom = requestedDefaultZoom;
    }

    const preserveView = !!(options && options.preserveView);
    const forceDefaultView = !!(options && options.forceDefaultView);
    const viewport = preserveView ? preserveViewport(m) : null;

    clearScheduleMarkers();
    state.schedule.lastPins = Array.isArray(pins) ? pins : [];

    const pts = [];
    if (!pins || pins.length === 0) {
      try { m.setView([state.schedule.defaultLat, state.schedule.defaultLng], state.schedule.defaultZoom); } catch {}
      return;
    }

    for (const pin of pins) {
      const lat = Number(pin.lat ?? pin.Lat);
      const lng = Number(pin.lng ?? pin.Lng);
      if (!Number.isFinite(lat) || !Number.isFinite(lng)) continue;

      const dayKey = String(pin.dayKey ?? pin.DayKey ?? "").toLowerCase();
      const isUnscheduled = (pin.isUnscheduled ?? pin.IsUnscheduled) === true || dayKey === "unscheduled";

      let badge = pin.badge ?? pin.Badge ?? null;
      const seq = pin.seq ?? pin.Seq ?? null;

      if ((!badge || String(badge).length === 0) && seq !== null && seq !== undefined) {
        badge = String(seq);
      }

      const badgeSize = isUnscheduled ? 34 : 24;
      const bg = badgeColor(pin, badge);
      const icon = makeBadgeIcon(badge, badgeSize, bg);

      const marker = icon
        ? L.marker([lat, lng], {
            icon,
            pane: isUnscheduled ? "cbUnscheduledPins" : "cbAssignedPins"
          }).addTo(m)
        : L.marker([lat, lng], {
            pane: isUnscheduled ? "cbUnscheduledPins" : "cbAssignedPins"
          }).addTo(m);

      if (isUnscheduled && marker.bringToFront) {
        try { marker.bringToFront(); } catch {}
      }

      const popup = buildPopupHtml(pin, isUnscheduled);
      if (popup) marker.bindPopup(popup);

      marker.on("popupclose", function () {
        safeSchedule(function () {
          if (state.schedule.popupRestoreViewport) {
            restoreViewport(m, state.schedule.popupRestoreViewport);
            state.schedule.popupRestoreViewport = null;
          }
        });
      });

      marker.on("click", function () {
        safeSchedule(function () {
          const id = pin.id ?? pin.Id;
          if (!id) return;

          if (popup && typeof marker.openPopup === "function") {
            state.schedule.popupRestoreViewport = preserveViewport(m);
            marker.openPopup();
          }

          invokeScheduleDotNet("OnMapPinArmed", String(id));

          if (isUnscheduled) {
            const card = document.getElementById(`wo-${String(id)}`);
            const tray = document.querySelector("[data-unscheduled-tray='true']");
            const dropzone = tray ? tray.querySelector(".sch-dropzone") : null;

            if (card && dropzone && dropzone.contains(card) && dropzone.firstElementChild !== card) {
              dropzone.insertBefore(card, dropzone.firstElementChild);
            }

            window.setTimeout(function () {
              safeSchedule(function () {
                if (window.ScheduleBoard) {
                  window.ScheduleBoard.scrollToColumn && window.ScheduleBoard.scrollToColumn("unscheduled");
                  window.ScheduleBoard.scrollToWorkOrderInColumn && window.ScheduleBoard.scrollToWorkOrderInColumn("unscheduled", String(id), { align: "top" });
                  window.ScheduleBoard.pulseWorkOrder && window.ScheduleBoard.pulseWorkOrder(String(id));
                }
              });
            }, 0);
          }
        });
      });

      state.schedule.markers.push(marker);
      pts.push([lat, lng]);
    }

    if (forceDefaultView) {
      try { m.setView([state.schedule.defaultLat, state.schedule.defaultLng], state.schedule.defaultZoom, { animate: false }); } catch {}
    } else if (preserveView && viewport) {
      restoreViewport(m, viewport);
    } else {
      safeFitBounds(m, pts);
    }
    try { m.invalidateSize(true); } catch {}
  }

  window.cbScheduleMap = window.cbScheduleMap || {};

  window.cbScheduleMap.renderPins = function (pins, defaultLat, defaultLng, options) {
    renderSchedulePinsInternal(null, pins, defaultLat, defaultLng, options);
  };

  window.cbScheduleMap.renderPinsTo = function (elementId, pins, defaultLat, defaultLng, options) {
    renderSchedulePinsInternal(elementId, pins, defaultLat, defaultLng, options);
  };

  window.cbScheduleMap.setDotNetRef = function (dotNetRef) {
    safeSchedule(function () {
      state.schedule.dotNetRef = dotNetRef || null;
    });
  };

  window.cbScheduleMap.invalidate = function () {
    safeSchedule(function () {
      if (state.schedule.map) {
        state.schedule.map.invalidateSize(true);
      }
    });
  };

  window.cbScheduleMap.isMobile = function () {
    try {
      return window.matchMedia("(max-width: 960px)").matches;
    } catch {
      return false;
    }
  };

  window.cbScheduleMap.unassignPin = function (id) {
    safeSchedule(function () {
      if (!id) return;
      invokeScheduleDotNet("OnMapPinRequested", String(id));
    });
  };

})();
