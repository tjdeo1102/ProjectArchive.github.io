const projects = [
  {
    title: "Project Darknight",
    shortTitle: "DARKNIGHT",
    type: "ROGUELIKE RPG",
    year: "2025",
    period: "2025.04 — 2025.07",
    role: "게임 제작 총괄 / PM",
    status: "개인 프로젝트 · 완성도 90%",
    accent: "#ff6534",
    scene: {
      bg: "#f2e1d8",
      accent: "#ff6534",
      accent2: "#5f2c15",
      grid: "rgba(255, 101, 52, 0.2)",
      title: "DARKNIGHT"
    },
    sourceUrl: "https://app.notion.com/p/261cce9c8a2380fbad4ac93b8449262a",
    videoUrl: "https://www.youtube.com/watch?v=Ce79yYLEzII",
    summary: "무한 던전에 갇힌 다크나이트의 모험. 절차적 생성과 런타임 최적화를 중심으로 설계한 로그라이크 RPG입니다.",
    contributions: [
      "BSP 알고리즘 기반의 방·복도 절차적 생성",
      "청크 스트리밍과 메쉬 컴바인을 이용한 렌더링 최적화",
      "UniTask 기반 Dynamic NavMesh 갱신",
      "상태 패턴과 Behaviour Tree를 활용한 캐릭터 AI"
    ],
    implementation: [
      ["절차적 던전 생성", "공간을 재귀적으로 분할하고 방 중심을 복도로 연결해 폐쇄된 방이 생기지 않는 BSP 던전 생성기를 구현했습니다."],
      ["청크 렌더링 최적화", "Dirty Flag로 시야 거리 내 청크만 활성화하고, 정적 메시를 결합해 오브젝트 수와 드로우콜을 관리했습니다."],
      ["프레임 분산", "NavMesh 데이터 수집과 오브젝트 풀 보충을 여러 프레임에 분산해 생성 시점의 병목과 프레임 드롭을 줄였습니다."]
    ],
    tech: ["Unity", "C#", "BSP", "UniTask", "NavMesh", "Behaviour Tree"]
  },
  {
    title: "냥젤리 조향 공방",
    shortTitle: "SCENT ATELIER",
    type: "MULTIPLAYER VR",
    year: "2025",
    period: "2024.12 — 2025.01",
    role: "PL / 게임 시스템·VR 콘텐츠",
    status: "팀 프로젝트 · 완성도 80%",
    accent: "#d7ff3f",
    scene: {
      bg: "#f5f0df",
      accent: "#d7ff3f",
      accent2: "#7dcfb6",
      grid: "rgba(29, 120, 100, 0.18)",
      title: "SCENT ATELIER"
    },
    sourceUrl: "https://app.notion.com/p/25fcce9c8a2380dbab68e573c974ed52",
    videoUrl: "https://www.youtube.com/watch?v=3o21pXPUIN0",
    summary: "고양이들과 함께 향수를 만드는 네트워크 기반 VR 힐링 게임. 손으로 붓고 흔드는 조향 과정을 상호작용으로 옮겼습니다.",
    contributions: [
      "게임 상태·밤낮·저장 및 불러오기 시스템",
      "재료와 레시피 기반 향수 조합 시스템",
      "병 기울기·액체 전달·흔들기 조합 VR 인터랙션",
      "PUN2 소유권 전환과 Grab 동기화 예외 처리"
    ],
    implementation: [
      ["손동작 기반 액체 표현", "병 방향과 월드 하단 벡터의 내적으로 기울기를 계산하고 Raycast로 액체 전달 대상을 판별했습니다."],
      ["데이터 주도 레시피", "재료와 레시피를 ScriptableObject로 분리해 코드 변경 없이 조합식과 밸런스를 확장하도록 설계했습니다."],
      ["멀티플레이 일관성", "Grab 소유권과 상태를 지속 동기화하고 RPC 지연을 보상해 플레이어마다 다른 물리 결과가 보이는 문제를 완화했습니다."]
    ],
    tech: ["Unity", "C#", "XR Toolkit", "PUN2", "Firebase", "Shader", "DOTween"]
  },
  {
    title: "Tower Thumble",
    shortTitle: "TOWER THUMBLE",
    type: "ONLINE PUZZLE",
    year: "2024",
    period: "2024.11 — 2024.12",
    role: "PM / 게임 루프·UI",
    status: "팀 프로젝트 · 완성도 85%",
    accent: "#536cff",
    scene: {
      bg: "#eef0ff",
      accent: "#536cff",
      accent2: "#ff6534",
      grid: "rgba(83, 108, 255, 0.18)",
      title: "TOWER THUMBLE"
    },
    sourceUrl: "https://app.notion.com/p/25fcce9c8a2380279f92fb143df7a6f9",
    videoUrl: "https://www.youtube.com/watch?v=RyzszKiiTKU",
    summary: "친숙한 블록 쌓기에 네트워크 경쟁을 더한 퍼즐 게임. 세 가지 모드를 하나의 확장 가능한 게임 루프로 설계했습니다.",
    contributions: [
      "Puzzle·Race·Survival 모드별 게임 규칙",
      "공통 게임 흐름을 GameState 부모 클래스로 추상화",
      "서버 시간 기반 시작 시점 동기화와 지연 보상",
      "Firebase Auth와 PUN2 기반 로비·매치메이킹 UI"
    ],
    implementation: [
      ["확장 가능한 게임 모드", "스폰·시작·종료·UI·네트워크 예외 처리를 부모 클래스에 모으고 모드별 규칙만 오버라이드했습니다."],
      ["권한 기반 판정", "방장 클라이언트만 충돌 영역과 점수를 판정하고 결과를 RPC로 공유해 중복 집계를 방지했습니다."],
      ["로그인부터 인게임까지", "인증, 닉네임, 로비, 방 생성과 참가를 하나의 UI 흐름으로 연결하고 방 속성으로 모드 설정을 동기화했습니다."]
    ],
    tech: ["Unity", "C#", "PUN2", "Firebase Auth", "Realtime DB", "Physics"]
  },
  {
    title: "Super Mario 64 NDS",
    shortTitle: "SM64 NDS",
    type: "3D PLATFORMER",
    year: "2026",
    period: "2024.09 / 2026.02 — 03",
    role: "PM / 게임 총괄 제작",
    status: "개인 모작 · 완성도 80%",
    accent: "#ffcb3f",
    scene: {
      bg: "#fff2c5",
      accent: "#ffcb3f",
      accent2: "#e3342f",
      grid: "rgba(227, 52, 47, 0.17)",
      title: "SM64 NDS"
    },
    sourceUrl: "https://app.notion.com/p/31dcce9c8a2380e9a786e7f12e074e61",
    summary: "슈퍼 마리오 64 DS의 폭탄병 전장을 Unity로 재구성하며 3D 플랫포머의 물리와 플레이 감각을 분석했습니다.",
    contributions: [
      "걷기·가속·슬라이드·3단 점프 이동 시스템",
      "접촉면 Normal을 활용한 벽과 지면 판별",
      "상태 패턴 기반 굼바·폭탄병 행동 구현",
      "코인·스타·VFX·SFX 등 스테이지 플레이 루프"
    ],
    implementation: [
      ["플랫포머 이동 감각", "입력 지속 시간에 따른 가속과 3단 점프를 구현하고, GroundCheck와 Slide 상태를 조합해 동작 조건을 제어했습니다."],
      ["메시 표면 판별", "거대한 단일 MeshCollider에서도 Contact Normal과 서브메시 재질을 사용해 지면, 벽, 미끄러운 경사를 구분했습니다."],
      ["몬스터 상태 관리", "공통 상태 머신과 Blackboard를 두고 배회·추격·피격·공격 로직을 몬스터별로 구체화했습니다."]
    ],
    tech: ["Unity", "C#", "Rigidbody", "New Input System", "NavMesh", "State Pattern"]
  },
  {
    title: "Spellcraft VR",
    shortTitle: "SPELLCRAFT VR",
    type: "VR GAME CONCEPT",
    year: "R&D",
    period: "Concept / Side Project",
    role: "시스템·3D·레벨 디자인",
    status: "사이드 프로젝트 콘셉트",
    accent: "#c77dff",
    scene: {
      bg: "#eee7ff",
      accent: "#c77dff",
      accent2: "#4cc9f0",
      grid: "rgba(199, 125, 255, 0.19)",
      title: "SPELLCRAFT VR"
    },
    sourceUrl: "https://app.notion.com/p/24dcce9c8a2381dfbf22ec4c38e29c2d",
    summary: "제스처와 문법을 조합해 자신만의 마법을 만드는 VR 던전 크롤러 콘셉트입니다.",
    contributions: [
      "충돌 순서·이동 경로 기반 제스처 입력 탐색",
      "동사·명사·반복 연산으로 구성한 마법 문법",
      "로그라이크 던전 탐험과 중거리 마법 전투",
      "시스템·3D 그래픽·레벨·사운드 디자인"
    ],
    implementation: [
      ["제스처 입력 연구", "공간 충돌 순서, 손 이동 방향, 경로 텍스처 인식 등 VR에서 사용할 수 있는 제스처 판별 방식을 비교했습니다."],
      ["조합형 마법 문법", "발사한다 + 불 + 반복처럼 명령 요소를 순서대로 해석해 새로운 주문을 생성하는 문법 구조를 설계했습니다."],
      ["던전 크롤 전투", "중거리 마법 전투와 방 단위 탐험을 결합해 직접 만든 주문을 반복적으로 시험하는 플레이 루프를 구상했습니다."]
    ],
    tech: ["Unity", "VR", "Gesture Input", "System Design", "Procedural Dungeon"]
  }
];

const track = document.querySelector("#project-track");
const detailTitle = document.querySelector("#detail-title");
const detailSummary = document.querySelector("#detail-summary");
const projectMeta = document.querySelector("#project-meta");
const contributionList = document.querySelector("#contribution-list");
const implementationList = document.querySelector("#implementation-list");
const techList = document.querySelector("#tech-list");
const mediaFrame = document.querySelector("#media-frame");
const currentIndex = document.querySelector("#current-index");
const totalCount = document.querySelector("#total-count");
const sliderProgress = document.querySelector("#slider-progress");
const sceneTitle = document.querySelector("#scene-title");

const ACTIVE_PROJECT_STORAGE_KEY = "portfolio-active-project";

function loadActiveProject() {
  try {
    const urlIndex = Number.parseInt(new URLSearchParams(window.location.search).get("project"), 10) - 1;
    if (Number.isInteger(urlIndex) && urlIndex >= 0 && urlIndex < projects.length) return urlIndex;
  } catch {
    // Fall back to session storage when URL state is unavailable.
  }

  try {
    const savedIndex = Number.parseInt(sessionStorage.getItem(ACTIVE_PROJECT_STORAGE_KEY), 10);
    return Number.isInteger(savedIndex) && savedIndex >= 0 && savedIndex < projects.length ? savedIndex : 0;
  } catch {
    return 0;
  }
}

function saveActiveProject(index) {
  try {
    sessionStorage.setItem(ACTIVE_PROJECT_STORAGE_KEY, String(index));
  } catch {
    // Storage can be unavailable in strict privacy modes; the UI still works in memory.
  }

  try {
    const url = new URL(window.location.href);
    const projectValue = String(index + 1);
    if (url.searchParams.get("project") !== projectValue) {
      url.searchParams.set("project", projectValue);
      window.history.replaceState(window.history.state, "", url);
    }
  } catch {
    // URL state is an additional persistence layer; storage remains as a fallback.
  }
}

let activeProject = loadActiveProject();
let dragStartX = 0;
let dragStartScroll = 0;
let isDragging = false;
let wheelSettleTimer = 0;
let resizeSettleTimer = 0;
let isSlideTransitioning = false;
let wheelInputLocked = false;
let lastWheelInputAt = 0;

function renderCards() {
  track.innerHTML = projects.map((project, index) => `
    <article
      class="project-card${index === activeProject ? " active" : ""}"
      data-index="${index}"
      style="--card-accent: ${project.accent}"
      aria-label="${project.title} 상세 보기"
    >
      <div class="card-grid" aria-hidden="true"></div>
      <div class="card-index">
        <span>${String(index + 1).padStart(2, "0")}</span>
        <span>${project.year}</span>
      </div>
      <div class="card-content">
        <h2>${project.shortTitle}</h2>
        <div class="card-meta">
          <span>${project.type}</span>
          <span>VIEW PROJECT ↘</span>
        </div>
      </div>
    </article>
  `).join("");
}

function syncTrackPadding() {
  const firstCard = track.firstElementChild;
  if (!firstCard) return;

  const centerPadding = Math.max(0, (track.clientWidth - firstCard.clientWidth) / 2);
  const currentPadding = parseFloat(track.style.getPropertyValue("--track-center-padding")) || 0;
  if (Math.abs(currentPadding - centerPadding) > 0.5) {
    track.style.setProperty("--track-center-padding", `${centerPadding}px`);
  }
}

function getNearestProjectIndex() {
  const trackCenter = track.getBoundingClientRect().left + track.clientWidth / 2;

  return [...track.children].reduce((nearest, card, index) => {
    const rect = card.getBoundingClientRect();
    const distance = Math.abs(rect.left + rect.width / 2 - trackCenter);
    return distance < nearest.distance ? { index, distance } : nearest;
  }, { index: 0, distance: Infinity }).index;
}

function getClosestDetailSlideIndex() {
  const viewportCenter = window.innerHeight / 2;
  return [...document.querySelectorAll(".detail-slide")].reduce((nearest, slide, index) => {
    const rect = slide.getBoundingClientRect();
    const distance = Math.abs(rect.top + rect.height / 2 - viewportCenter);
    return distance < nearest.distance ? { index, distance } : nearest;
  }, { index: 0, distance: Infinity }).index;
}

function releaseWheelInputWhenIdle() {
  const idleFor = Date.now() - lastWheelInputAt;
  if (isSlideTransitioning || idleFor < 180) {
    window.setTimeout(releaseWheelInputWhenIdle, Math.max(80, 180 - idleFor));
    return;
  }
  wheelInputLocked = false;
}

function runCinematicTransition(target, direction) {
  if (!target || isSlideTransitioning) return;

  const startY = window.scrollY;
  const targetY = target.classList.contains("hero")
    ? 0
    : Math.round(startY + target.getBoundingClientRect().top);
  if (Math.abs(targetY - startY) < 2) return;

  isSlideTransitioning = true;
  wheelInputLocked = true;

  const currentSlide = document.querySelector(".detail-slide.is-visible");
  const targetIsSlide = target.classList.contains("detail-slide");
  const duration = 500;
  const startedAt = performance.now();

  document.body.classList.add("is-slide-transitioning", direction > 0 ? "transition-forward" : "transition-backward");
  currentSlide?.classList.add("is-leaving");
  if (targetIsSlide) {
    target.classList.remove("is-visible");
    target.classList.add("is-entering");
  }

  document.documentElement.classList.add("cinematic-jump");
  void document.documentElement.offsetHeight;

  function animateScroll(now) {
    const progress = Math.min(1, (now - startedAt) / duration);
    const eased = 0.5 - Math.cos(Math.PI * progress) / 2;
    window.scrollTo(0, startY + (targetY - startY) * eased);

    if (targetIsSlide && progress >= 0.12) {
      target.classList.remove("is-entering");
      target.classList.add("is-visible");
    }

    if (progress < 1) {
      window.requestAnimationFrame(animateScroll);
      return;
    }

    window.scrollTo(0, targetY);
    document.documentElement.classList.toggle("detail-snap", targetIsSlide);
    document.documentElement.classList.remove("cinematic-jump");

    currentSlide?.classList.remove("is-leaving");
    target.classList.remove("is-entering");
    if (targetIsSlide) target.classList.add("is-visible");
    document.body.classList.remove("is-slide-transitioning", "transition-forward", "transition-backward");
    isSlideTransitioning = false;
    releaseWheelInputWhenIdle();
  }

  window.requestAnimationFrame(animateScroll);
}

function getYouTubeEmbedUrl(url) {
  if (!url) return "";

  try {
    // 1. 다양한 유튜브 URL 패턴에서 11자리 Video ID를 매칭하는 정규식
    const regExp = /^.*(youtu.be\/|v\/|u\/\w\/|embed\/|shorts\/|watch\?v=|\&v=)([^#\&\?]*).*/;
    const match = url.match(regExp);
    
    // 2. 매칭된 결과가 있고, 그 값이 유튜브 비디오 ID 규격인 11자리가 맞는지 확인
    const videoId = (match && match[2].length === 11) ? match[2] : null;

    // 3. 보안과 개인정보 보호를 위해 youtube-nocookie.com 사용을 권장합니다.
    return videoId ? `https://www.youtube-nocookie.com/embed/${videoId}` : "";
  } catch {
    return "";
  }
}

function renderMedia(project) {
  const embedUrl = getYouTubeEmbedUrl(project.videoUrl);
  mediaFrame.style.setProperty("--detail-accent", project.accent);

  if (embedUrl) {
    // 💡 1. 153 에러 방지를 위해 현재 웹사이트의 도메인(출처) 정보 추출
    const currentOrigin = window.location.origin;

    // 💡 2. 화질 저하 방지(vq=hd1080) 및 출처 인증(origin) 파라미터 결합
    // 믹스인 조건(? 파라미터 유무 체크)을 고려해 안전하게 쿼리 스트링 조립
    const separator = embedUrl.includes('?') ? '&' : '?';
    const finalEmbedUrl = `${embedUrl}${separator}vq=hd1080&rel=0&enablejsapi=1&origin=${encodeURIComponent(currentOrigin)}&widgetreferrer=${encodeURIComponent(window.location.href)}`;

    mediaFrame.innerHTML = `
      <iframe
        src="${finalEmbedUrl}"
        title="${project.title} gameplay video"
        loading="lazy"
        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
        allowfullscreen
        referrerpolicy="strict-origin-when-cross-origin"
      ></iframe>
    `;
    return;
  }

  // embedUrl이 없을 때 작동하는 플레이스홀더 영역 (기존 코드 유지)
  mediaFrame.innerHTML = `
    <div class="media-placeholder">
      <span class="play-icon" aria-hidden="true">▶</span>
      <p>GAMEPLAY VIDEO</p>
      <small>유튜브 URL을 videoUrl에 넣으면 이 영역에서 바로 재생됩니다.</small>
      <div class="media-links">
        ${project.sourceUrl ? `<a href="${project.sourceUrl}" target="_blank" rel="noreferrer">NOTION SOURCE ↗</a>` : ""}
        <a class="secondary" href="#projects">SELECT ANOTHER</a>
      </div>
    </div>
  `;
}

function applyScene(project) {
  const scene = project.scene;
  document.body.style.setProperty("--scene-bg", scene.bg);
  document.body.style.setProperty("--scene-accent", scene.accent);
  document.body.style.setProperty("--scene-accent-2", scene.accent2);
  document.body.style.setProperty("--scene-grid", scene.grid);
  document.body.style.setProperty("--scene-title", `"${scene.title}"`);
  sceneTitle.textContent = scene.title;
}

function renderDetail(index) {
  const project = projects[index];
  activeProject = index;
  saveActiveProject(index);
  applyScene(project);

  detailTitle.textContent = project.title;
  detailSummary.textContent = project.summary;
  projectMeta.innerHTML = `
    <div><dt>PERIOD</dt><dd>${project.period}</dd></div>
    <div><dt>ROLE</dt><dd>${project.role}</dd></div>
    <div><dt>STATUS</dt><dd>${project.status}</dd></div>
  `;
  contributionList.innerHTML = project.contributions
    .map(item => `<li>${item}</li>`)
    .join("");
  implementationList.innerHTML = project.implementation
    .map((item, itemIndex) => `
      <div class="implementation-item">
        <span class="item-number">${String(itemIndex + 1).padStart(2, "0")}</span>
        <div>
          <h4>${item[0]}</h4>
          <p>${item[1]}</p>
        </div>
      </div>
    `)
    .join("");
  techList.innerHTML = project.tech.map(item => `<span>${item}</span>`).join("");
  renderMedia(project);
  document.querySelector(".project-detail").style.setProperty("--detail-accent", project.accent);

  document.querySelectorAll(".project-card").forEach((card, cardIndex) => {
    card.classList.toggle("active", cardIndex === index);
  });

  currentIndex.textContent = String(index + 1).padStart(2, "0");
  sliderProgress.style.transform = `scaleX(${(index + 1) / projects.length})`;
}

function scrollToProjectCard(index, behavior = "smooth") {
  const card = track.children[index];
  if (!card) return;

  const trackRect = track.getBoundingClientRect();
  const cardRect = card.getBoundingClientRect();
  const centerOffset = cardRect.left + cardRect.width / 2 - (trackRect.left + trackRect.width / 2);
  const maxScroll = Math.max(0, track.scrollWidth - track.clientWidth);
  const targetScrollLeft = Math.min(maxScroll, Math.max(0, track.scrollLeft + centerOffset));

  track.scrollTo({
    left: targetScrollLeft,
    behavior
  });
}

function goToProject(index, scrollToDetail = false) {
  const safeIndex = (index + projects.length) % projects.length;
  if (!track.children[safeIndex]) return;

  scrollToProjectCard(safeIndex);
  
  renderDetail(safeIndex);

  if (scrollToDetail) {
    window.setTimeout(() => {
      runCinematicTransition(document.querySelector(".detail-slide-intro"), 1);
    }, 100);
  }
}




renderCards();
syncTrackPadding();
renderDetail(activeProject);
totalCount.textContent = String(projects.length).padStart(2, "0");
window.requestAnimationFrame(() => scrollToProjectCard(activeProject, "auto"));

window.addEventListener("resize", () => {
  window.clearTimeout(resizeSettleTimer);
  resizeSettleTimer = window.setTimeout(syncTrackPadding, 100);
});

const slideObserver = new IntersectionObserver(entries => {
  entries.forEach(entry => {
    entry.target.classList.toggle("is-visible", entry.isIntersecting);
  });
}, {
  rootMargin: "-8% 0px -8% 0px",
  threshold: 0.35
});
document.querySelectorAll(".detail-slide").forEach(slide => slideObserver.observe(slide));

const detailModeObserver = new IntersectionObserver(entries => {
  document.documentElement.classList.toggle("detail-snap", entries[0].isIntersecting);
}, { threshold: 0 });
detailModeObserver.observe(document.querySelector("#detail"));

document.querySelectorAll("[data-scroll-target]").forEach(cue => {
  cue.addEventListener("click", () => {
    const targetName = cue.dataset.scrollTarget;
    const target = targetName === "footer"
      ? document.querySelector("footer")
      : document.querySelectorAll(".detail-slide")[Number(targetName)];
    runCinematicTransition(target, 1);
  });
});

document.querySelector('nav a[href="#detail"]').addEventListener("click", event => {
  event.preventDefault();
  runCinematicTransition(document.querySelector(".detail-slide-intro"), 1);
});

document.querySelectorAll('a[href="#projects"]').forEach(link => {
  link.addEventListener("click", event => {
    event.preventDefault();
    runCinematicTransition(document.querySelector(".hero"), -1);
  });
});

window.addEventListener("wheel", event => {
  if (event.target instanceof Element && event.target.closest("#project-track")) return;
  if (Math.abs(event.deltaY) < 8 || Math.abs(event.deltaY) <= Math.abs(event.deltaX)) return;

  const direction = event.deltaY > 0 ? 1 : -1;
  const detailSlides = [...document.querySelectorAll(".detail-slide")];
  const detailMode = document.documentElement.classList.contains("detail-snap");
  const heroVisible = document.querySelector(".hero").getBoundingClientRect().bottom > 0;
  const footerRect = document.querySelector("footer").getBoundingClientRect();

  let target = null;
  if (detailMode) {
    const currentSlideIndex = getClosestDetailSlideIndex();
    target = detailSlides[currentSlideIndex + direction]
      || (direction > 0 ? document.querySelector("footer") : document.querySelector(".hero"));
  } else if (direction > 0 && heroVisible) {
    target = detailSlides[0];
  } else if (direction < 0 && footerRect.top < window.innerHeight) {
    target = detailSlides[detailSlides.length - 1];
  }

  if (!target) return;
  event.preventDefault();
  lastWheelInputAt = Date.now();

  if (wheelInputLocked) return;
  runCinematicTransition(target, direction);
}, { passive: false });

track.addEventListener("click", event => {
  if (isDragging) return;
  const card = event.target.closest(".project-card");
  if (!card) return;
  goToProject(Number(card.dataset.index), true);
});

track.addEventListener("keydown", event => {
  if (event.key === "ArrowRight") goToProject(activeProject + 1);
  if (event.key === "ArrowLeft") goToProject(activeProject - 1);
});

document.querySelector("#prev-project").addEventListener("click", () => {
  goToProject(activeProject - 1);
});

document.querySelector("#next-project").addEventListener("click", () => {
  goToProject(activeProject + 1);
});

track.addEventListener("pointerdown", event => {
  dragStartX = event.clientX;
  dragStartScroll = track.scrollLeft;
  isDragging = false;
  track.setPointerCapture(event.pointerId);
  track.classList.add("is-dragging");
});

track.addEventListener("pointermove", event => {
  if (!track.hasPointerCapture(event.pointerId)) return;
  const distance = event.clientX - dragStartX;
  if (Math.abs(distance) > 6) isDragging = true;
  track.scrollLeft = dragStartScroll - distance;
});

track.addEventListener("pointerup", event => {
  track.releasePointerCapture(event.pointerId);
  track.classList.remove("is-dragging");

  if (isDragging) {
    goToProject(getNearestProjectIndex());
    window.setTimeout(() => { isDragging = false; }, 0);
  }
});

track.addEventListener("pointercancel", () => {
  track.classList.remove("is-dragging");
  isDragging = false;
});

track.addEventListener("wheel", event => {
  if (Math.abs(event.deltaY) <= Math.abs(event.deltaX)) return;
  event.preventDefault();
  track.scrollLeft += event.deltaY;

  window.clearTimeout(wheelSettleTimer);
  wheelSettleTimer = window.setTimeout(() => {
    goToProject(getNearestProjectIndex());
  }, 120);
}, { passive: false });
