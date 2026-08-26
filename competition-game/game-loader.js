const buildConfig = {
  loaderUrl: "Build/WebBuild.loader.js",
  dataUrl: "Build/WebBuild.data.unityweb",
  frameworkUrl: "Build/WebBuild.framework.js.unityweb",
  codeUrl: "Build/WebBuild.wasm.unityweb",

  streamingAssetsUrl: "StreamingAssets",
  companyName: "SquareGame",
  productName: "Chroma Shell",
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
    const response = await fetch(buildConfig.loaderUrl, {
      method: "HEAD",
      cache: "no-store"
    });

    return response.ok;
  } catch {
    return false;
  }
}

async function launchGame() {
  if (!await hasWebGLBuild()) {
    buildState.textContent = "BUILD PENDING";
    console.error("Unity loader not found:", buildConfig.loaderUrl);
    return;
  }

  buildState.textContent = "LOADING";

  placeholder.hidden = true;
  unityContainer.hidden = false;

  const loader = document.createElement("script");
  loader.src = buildConfig.loaderUrl;

  loader.onload = () => {
    createUnityInstance(
      canvas,
      buildConfig,
      value => {
        const percent = Math.round(value * 100);

        progress.style.width = `${percent}%`;
        loadingLabel.textContent = `${percent}%`;
      }
    )
      .then(unityInstance => {
        buildState.textContent = "PLAYING";
        document.body.classList.add("game-ready");

        console.log("Unity WebGL loaded.", unityInstance);
      })
      .catch(error => {
        buildState.textContent = "LOAD FAILED";

        placeholder.hidden = false;
        unityContainer.hidden = true;

        console.error("Unity WebGL load failed:", error);
      });
  };

  loader.onerror = error => {
    buildState.textContent = "LOAD FAILED";
    placeholder.hidden = false;
    unityContainer.hidden = true;

    console.error("Unity loader script failed:", error);
  };

  document.body.appendChild(loader);
}

launchGame();