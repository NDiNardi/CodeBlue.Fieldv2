// codeBlue.places.loader.js
// Lazy-loads Google Maps JS Places library when a page requests it.
(function () {
  window.codeBlue = window.codeBlue || {};
  const root = window.codeBlue;

  root.placesLoader = root.placesLoader || {};
  root.places = root.places || {};

  let loadPromise = null;
  let configPromise = null;
  const instances = new Map();

  function alreadyLoaded() {
    return !!(window.google && window.google.maps && window.google.maps.places);
  }

  async function resolveSrc() {
    if (root.googlePlacesSrc) {
      return root.googlePlacesSrc;
    }

    if (!configPromise) {
      const isLocalHost =
        window.location.hostname === "localhost" ||
        window.location.hostname === "127.0.0.1";
      const configuredUrls = Array.isArray(root.googlePlacesConfigUrls)
        ? root.googlePlacesConfigUrls
        : [];
      const fallbackUrls = isLocalHost
        ? ["/appsettings.Development.json", root.googlePlacesConfigUrl || "/appsettings.json"]
        : [root.googlePlacesConfigUrl || "/appsettings.json"];
      const urls = [...configuredUrls, ...fallbackUrls]
        .filter(url => typeof url === "string" && url.trim().length > 0);

      configPromise = (async () => {
        for (const url of urls) {
          try {
            const response = await fetch(url, { cache: "no-store" });
            if (response.ok) {
              return await response.json();
            }
          } catch {
          }
        }

        return null;
      })();
    }

    const config = await configPromise;
    const apiKey = config && config.Google && typeof config.Google.ApiKey === "string"
      ? config.Google.ApiKey.trim()
      : "";

    root.googlePlacesSrc = apiKey
      ? `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}&libraries=places`
      : "";

    return root.googlePlacesSrc;
  }

  root.placesLoader.load = async function () {
    if (loadPromise) return loadPromise;
    if (alreadyLoaded()) return true;

    loadPromise = (async () => {
      const src = await resolveSrc();
      if (!src) {
        console.warn("[codeBlue.placesLoader] Google Places API key is not configured.");
        return false;
      }

      if (document.querySelector('script[data-codeblue-places="1"]')) {
        return new Promise(resolve => {
          const tick = setInterval(() => {
            if (alreadyLoaded()) {
              clearInterval(tick);
              resolve(true);
            }
          }, 50);

          setTimeout(() => {
            clearInterval(tick);
            resolve(alreadyLoaded());
          }, 8000);
        });
      }

      return new Promise(resolve => {
        const script = document.createElement("script");
        script.setAttribute("data-codeblue-places", "1");
        script.async = true;
        script.defer = true;
        script.src = src;
        script.onload = () => resolve(true);
        script.onerror = () => {
          console.error("[codeBlue.placesLoader] Failed to load:", src);
          resolve(false);
        };
        document.head.appendChild(script);
      });
    })();

    return loadPromise;
  };

  function pick(components, type) {
    if (!Array.isArray(components)) return null;
    return components.find(component => Array.isArray(component.types) && component.types.includes(type)) || null;
  }

  function parsePlace(place) {
    const components = place && place.address_components ? place.address_components : [];
    const streetNumber = pick(components, "street_number")?.long_name || "";
    const route = pick(components, "route")?.long_name || "";
    const street1 = `${streetNumber} ${route}`.trim();
    const city =
      pick(components, "locality")?.long_name ||
      pick(components, "postal_town")?.long_name ||
      pick(components, "sublocality")?.long_name ||
      pick(components, "administrative_area_level_2")?.long_name ||
      "";
    const state = pick(components, "administrative_area_level_1")?.short_name || "";
    const zip = pick(components, "postal_code")?.long_name || "";
    const latitude = typeof place?.geometry?.location?.lat === "function" ? place.geometry.location.lat() : null;
    const longitude = typeof place?.geometry?.location?.lng === "function" ? place.geometry.location.lng() : null;

    return { street1, city, state, zip, latitude, longitude };
  }

  root.places.init = function (dotNetRef, inputId) {
    return root.placesLoader.load().then(ok => {
      if (!ok) return false;

      let element = document.getElementById(inputId);
      if (element && element.tagName !== "INPUT") {
        element = element.querySelector("input");
      }

      if (!element) {
        console.warn("[codeBlue.places] input not found:", inputId);
        return false;
      }

      root.places.destroy(inputId);

      const ac = new google.maps.places.Autocomplete(element, {
        fields: ["address_components", "formatted_address", "geometry"],
        types: ["address"]
      });

      const listener = ac.addListener("place_changed", () => {
        try {
          const selection = parsePlace(ac.getPlace());
          dotNetRef?.invokeMethodAsync("OnPlaceSelected", selection)
            .catch(error => console.error("[codeBlue.places] OnPlaceSelected failed:", error));
        } catch (error) {
          console.error("[codeBlue.places] place_changed error:", error);
        }
      });

      instances.set(inputId, { listener });
      return true;
    });
  };

  root.places.destroy = function (inputId) {
    const instance = instances.get(inputId);
    if (!instance) return;

    try {
      if (instance.listener && window.google?.maps?.event?.removeListener) {
        google.maps.event.removeListener(instance.listener);
      }
    } catch {
    }

    instances.delete(inputId);
  };
})();
