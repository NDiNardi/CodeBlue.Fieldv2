// wwwroot/js/pwa-install.js
(function () {
  const host = window.location.hostname;
  const isLocal =
    host === "localhost" ||
    host === "127.0.0.1" ||
    host === "::1";

  let deferredPrompt = null;
  const installStateSubscribers = new Set();

  function isIosSafari() {
    const ua = navigator.userAgent || "";
    const isIos = /iPhone|iPad|iPod/i.test(ua);
    const isSafari = /Safari/i.test(ua) && !/Chrome|CriOS|EdgiOS|FxiOS/i.test(ua);
    return isIos && isSafari;
  }

  function currentState() {
    return {
      installed: window.pwaIsInstalled(),
      canPrompt: !isLocal && !!deferredPrompt,
      isIosSafari: isIosSafari()
    };
  }

  function notifySubscribers() {
    const state = currentState();
    installStateSubscribers.forEach(dotNetRef => {
      try {
        dotNetRef.invokeMethodAsync("OnPwaInstallStateChanged", state);
      } catch {
      }
    });
  }

  window.addEventListener("beforeinstallprompt", (e) => {
    if (isLocal) return;
    e.preventDefault();
    deferredPrompt = e;
    notifySubscribers();
  });

  window.addEventListener("appinstalled", () => {
    deferredPrompt = null;
    notifySubscribers();
  });

  window.pwaCanPromptInstall = function () {
    return !isLocal && !!deferredPrompt;
  };

  window.pwaPromptInstall = async function () {
    if (isLocal) return { ok: false, reason: "local-dev" };
    if (!deferredPrompt) return { ok: false, reason: "not-available" };

    deferredPrompt.prompt();
    const choice = await deferredPrompt.userChoice;
    deferredPrompt = null;

    return { ok: choice.outcome === "accepted", outcome: choice.outcome };
  };

  window.pwaIsInstalled = function () {
    return window.matchMedia("(display-mode: standalone)").matches
      || window.navigator.standalone === true;
  };

  window.pwaIsIosSafari = function () {
    return isIosSafari();
  };

  window.pwaGetInstallState = function () {
    return currentState();
  };

  window.pwaSubscribeInstallState = function (dotNetRef) {
    installStateSubscribers.add(dotNetRef);
    try {
      dotNetRef.invokeMethodAsync("OnPwaInstallStateChanged", currentState());
    } catch {
    }
  };

  window.pwaUnsubscribeInstallState = function (dotNetRef) {
    installStateSubscribers.delete(dotNetRef);
  };
})();
