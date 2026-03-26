window.codeBlue = window.codeBlue || {};

(function () {
  const root = window.codeBlue;
  const loaders = new Map();

  function loadScript(key, src) {
    if (loaders.has(key)) {
      return loaders.get(key);
    }

    const existing = document.querySelector(`script[data-cb-key="${key}"]`);
    if (existing) {
      const ready = Promise.resolve();
      loaders.set(key, ready);
      return ready;
    }

    const promise = new Promise((resolve, reject) => {
      const script = document.createElement("script");
      script.src = src;
      script.async = true;
      script.dataset.cbKey = key;
      script.onload = () => resolve();
      script.onerror = () => reject(new Error(`Failed to load ${src}`));
      document.head.appendChild(script);
    });

    loaders.set(key, promise);
    return promise;
  }

  function loadStyle(key, href) {
    if (loaders.has(key)) {
      return loaders.get(key);
    }

    const existing = document.querySelector(`link[data-cb-key="${key}"]`);
    if (existing) {
      const ready = Promise.resolve();
      loaders.set(key, ready);
      return ready;
    }

    const promise = new Promise((resolve, reject) => {
      const link = document.createElement("link");
      link.rel = "stylesheet";
      link.href = href;
      link.dataset.cbKey = key;
      link.onload = () => resolve();
      link.onerror = () => reject(new Error(`Failed to load ${href}`));
      document.head.appendChild(link);
    });

    loaders.set(key, promise);
    return promise;
  }

  root.assets = root.assets || {};

  root.assets.ensureMaps = async function () {
    await loadStyle("leaflet-css", "https://unpkg.com/leaflet@1.9.4/dist/leaflet.css");
    await loadScript("leaflet-js", "https://unpkg.com/leaflet@1.9.4/dist/leaflet.js");
    await loadScript("codeblue-maps", "/js/codeBlue.maps.js?v=20260322c");
  };

  root.assets.ensureSchedule = async function () {
    await root.assets.ensureMaps();
    await loadScript("scheduleboard-scroll", "/js/scheduleboard.scroll.js?v=20260322c");
    await loadScript("scheduleboard-touchfix", "/js/scheduleboard.touchfix.js?v=20260322c");
  };
})();
