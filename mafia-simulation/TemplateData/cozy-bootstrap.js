(function () {
  "use strict";

  const screen = document.getElementById("loading-screen");
  const status = document.getElementById("loading-status");
  const detail = document.getElementById("loading-detail");
  const serviceHost = document.getElementById("provider-service-host");
  const progress = document.getElementById("loading-progress");
  const retry = document.getElementById("retry-button");
  const launch = document.getElementById("launch-button");
  const canvas = document.getElementById("unity-canvas");
  const container = document.getElementById("unity-container");
  const fullscreen = document.getElementById("fullscreen-button");
  const settings = document.getElementById("settings-button");
  const maxWakeMilliseconds = 120000;
  let unityInstance = null;
  let liveSession = null;

  function closeSession(session) {
    if (!session) return;
    const url = `${session.baseUrl}/sessions/${encodeURIComponent(session.sessionId)}/close`;
    const body = new Blob([session.token], { type: "text/plain" });
    if (!navigator.sendBeacon || !navigator.sendBeacon(url, body))
      fetch(url, { method: "POST", body, keepalive: true }).catch(() => {});
  }

  window.cozySettingsMenu = {
    setVisible(visible) { settings.hidden = !visible; }
  };
  settings.addEventListener("click", () => {
    if (unityInstance) unityInstance.SendMessage("WebNpcSetupController", "RequestReconfigure");
  });

  window.cozySessionLifecycle = {
    register(baseUrl, sessionId, token) {
      closeSession(liveSession);
      liveSession = { baseUrl, sessionId, token };
    },
    clear() {
      const session = liveSession;
      liveSession = null;
      closeSession(session);
    }
  };
  window.setInterval(() => {
    const session = liveSession;
    if (!session) return;
    fetch(`${session.baseUrl}/sessions/${encodeURIComponent(session.sessionId)}/heartbeat`, {
      method: "POST",
      headers: { "X-Agent-Session-Token": session.token }
    }).catch(() => {});
  }, 120000);
  window.addEventListener("pagehide", () => {
    const session = liveSession;
    liveSession = null;
    closeSession(session);
  });
  window.addEventListener("pageshow", event => {
    if (event.persisted) window.location.reload();
  });

  retry.addEventListener("click", () => window.location.reload());

  function fail(message, explanation) {
    launch.hidden = true;
    status.textContent = message;
    detail.textContent = explanation;
    progress.classList.remove("waking");
    progress.style.width = "100%";
    progress.style.background = "#bb6b59";
    retry.hidden = false;
  }

  async function readApiBaseUrl() {
    if (window.location.protocol === "file:")
      throw new Error("게임을 파일로 직접 열 수 없습니다. HTTPS 웹 주소에서 실행해 주세요.");

    const configUrl = new URL("agent-framework-config.json", window.location.href);
    const response = await fetch(configUrl, { cache: "no-store" });
    if (!response.ok) {
      if (window.location.hostname.endsWith(".github.io"))
        throw new Error("서버 주소 설정이 아직 배포되지 않았습니다.");
      return new URL("/agent-framework", window.location.origin).href;
    }

    const setting = await response.json();
    const api = new URL(setting.apiBaseUrl);
    const isLocal = ["localhost", "127.0.0.1"].includes(window.location.hostname);
    if ((!isLocal && api.protocol !== "https:") ||
        (isLocal && !["http:", "https:"].includes(api.protocol)) ||
        !api.pathname.replace(/\/$/, "").endsWith("/agent-framework") ||
        api.username || api.password || api.search || api.hash)
      throw new Error("서버 주소 설정이 올바르지 않습니다.");
    return api.href.replace(/\/$/, "");
  }

  async function waitForBackend(apiBaseUrl) {
    const started = Date.now();
    progress.classList.add("waking");
    status.textContent = "마을 서버를 깨우고 있어요…";
    while (Date.now() - started < maxWakeMilliseconds) {
      const elapsed = Math.floor((Date.now() - started) / 1000);
      detail.textContent = `무료 서버의 첫 연결은 약 1분 걸릴 수 있어요. (${elapsed}초)`;
      const controller = new AbortController();
      const timeout = window.setTimeout(() => controller.abort(), 12000);
      try {
        const response = await fetch(`${apiBaseUrl}/health`, {
          cache: "no-store", signal: controller.signal
        });
        if (response.ok) {
          const health = await response.json();
          if (health.status === "ok" &&
              typeof health.framework === "string" &&
              health.framework.includes("Microsoft Agent Framework")) return;
        }
      } catch (_) {
        // A sleeping free instance or a transient network failure is retried below.
      } finally {
        window.clearTimeout(timeout);
      }
      await new Promise(resolve => window.setTimeout(resolve, 2500));
    }
    throw new Error("서버 응답이 지연되고 있습니다. 잠시 후 다시 시도해 주세요.");
  }

  function loadUnity() {
    return new Promise((resolve, reject) => {
      const loader = document.createElement("script");
      loader.src = window.cozyUnityBuild.loaderUrl;
      loader.onerror = () => reject(new Error("게임 파일을 불러오지 못했습니다."));
      loader.onload = () => {
        if (typeof createUnityInstance !== "function") {
          reject(new Error("게임 로더를 실행하지 못했습니다."));
          return;
        }
        createUnityInstance(canvas, window.cozyUnityBuild.config, value => {
          progress.classList.remove("waking");
          progress.style.width = `${Math.round(value * 100)}%`;
          detail.textContent = `게임 데이터를 불러오는 중… ${Math.round(value * 100)}%`;
        }).then(resolve, reject);
      };
      document.body.appendChild(loader);
    });
  }

  async function start() {
    try {
      const apiBaseUrl = await readApiBaseUrl();
      serviceHost.textContent = new URL(apiBaseUrl).host;
      await waitForBackend(apiBaseUrl);
      status.textContent = "서버 연결 완료!";
      detail.textContent = "마을 입장하기를 눌러 게임과 소리를 시작해 주세요.";
      progress.classList.remove("waking");
      launch.hidden = false;
      await new Promise(resolve => launch.addEventListener("click", resolve, { once: true }));
      launch.hidden = true;
      status.textContent = "게임을 준비하고 있어요.";
      detail.textContent = "마을 지도를 불러오는 중…";
      container.hidden = false;
      const unity = await loadUnity();
      unityInstance = unity;
      screen.hidden = true;
      fullscreen.addEventListener("click", () => unity.SetFullscreen(1));
    } catch (error) {
      fail("연결을 완료하지 못했어요.", error.message || "다시 시도해 주세요.");
    }
  }

  start();
})();
