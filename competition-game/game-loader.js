const buildConfig = {
  loaderUrl: "Build/ProjectT.loader.js",
  dataUrl: "Build/ProjectT.data",
  frameworkUrl: "Build/ProjectT.framework.js",
  codeUrl: "Build/ProjectT.wasm",
  streamingAssetsUrl: "StreamingAssets",
  companyName: "Competition Team",
  productName: "The Last Temple",
  productVersion: "0.1"
};

const buildState = document.querySelector("#build-state");
const placeholder = document.querySelector("#build-placeholder");
const unityContainer = document.querySelector("#unity-container");
const canvas = document.querySelector("#unity-canvas");
const progress = document.querySelector("#loading-progress");
const loadingLabel = document.querySelector("#loading-label");

async function hasWebGLBuild() {
  try {
    const response = await fetch(buildConfig.loaderUrl, { method: "HEAD", cache: "no-store" });
    return response.ok;
  } catch {
    return false;
  }
}

async function launchGame() {
  if (!await hasWebGLBuild()) {
    buildState.textContent = "BUILD PENDING";
    return;
  }

  buildState.textContent = "LOADING";
  placeholder.hidden = true;
  unityContainer.hidden = false;

  const loader = document.createElement("script");
  loader.src = buildConfig.loaderUrl;
  loader.onload = () => {
    createUnityInstance(canvas, buildConfig, value => {
      const percent = Math.round(value * 100);
      progress.style.transform = `scaleX(${value})`;
      loadingLabel.textContent = `${percent}%`;
    }).then(() => {
      buildState.textContent = "PLAYING";
      document.body.classList.add("game-ready");
    }).catch(error => {
      buildState.textContent = "LOAD FAILED";
      placeholder.hidden = false;
      unityContainer.hidden = true;
      console.error(error);
    });
  };
  loader.onerror = () => { buildState.textContent = "LOAD FAILED"; };
  document.body.appendChild(loader);
}

launchGame();
