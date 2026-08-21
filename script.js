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
    imageUrl: "assets/projects/darknight.gif",
    imageAlt: "Project Darknight 던전 플레이 화면",
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
    decisions: [
      ["왜 BSP를 선택했는가", "던전을 청크 단위로 스트리밍하려면 생성 결과를 일정한 공간 단위로 관리할 수 있어야 했습니다. BSP로 공간을 재귀 분할한 뒤 방 크기를 무작위화하고 중심점을 복도로 연결해, 청크 규격을 유지하면서도 매번 다른 구조와 끊기지 않는 동선을 확보했습니다."],
      ["런타임 NavMesh 병목 해결", "던전 생성 직후 전체 계층을 다시 탐색하면 큰 프레임 스파이크가 발생했습니다. 청크마다 NavMeshModifier 목록을 미리 수집하고, 데이터 수집과 UpdateNavMesh 단계를 분리한 뒤 UniTask로 프레임 사이에 양보하도록 파이프라인을 재구성했습니다."],
      ["생성과 플레이의 충돌 방지", "청크 연결은 네 방향 이웃과 얇은 벽을 RaycastAll로 검사한 뒤 터널을 만들고, 메시 결합과 오브젝트 풀 보충은 한 프레임에 몰지 않았습니다. 풀의 부족분도 프레임당 하나씩 채워 탐색 중 순간적인 끊김을 줄였습니다."]
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
    imageUrl: "assets/projects/scent-atelier.gif",
    imageAlt: "냥젤리 조향 공방 프로젝트 대표 화면",
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
    decisions: [
      ["영속 상태와 밸런스 데이터 분리", "씬을 넘어 유지돼야 하는 진행 상태는 싱글턴 게임 매니저가 맡고, 일회성 수치와 밸런스는 ScriptableObject로 분리했습니다. 저장 시 스테이지 정보를 JSON으로 직렬화하고 PUN 닉네임을 키로 Firebase 트랜잭션과 스냅샷을 사용해 불러왔습니다."],
      ["손동작을 조향 규칙으로 변환", "병을 기울였다는 감각을 단순 각도가 아니라 병 방향과 아래쪽 벡터의 내적으로 판정했습니다. Raycast로 받을 용기를 찾고 FillAmount 셰이더로 양을 표현했으며, 흔들기 임계값을 넘으면 레시피를 LINQ로 비교해 결과 향수를 결정했습니다."],
      ["VR Grab 동기화 문제 해결", "여러 사용자가 같은 오브젝트를 잡을 때 소유권과 물리 레이어가 엇갈리는 문제가 있었습니다. Grab 상태를 지속 동기화하고 상호작용 순간에 소유권과 레이어를 제한했으며, 액체 상태와 조합 결과는 RPC로 공유해 플레이어별 결과 차이를 줄였습니다."]
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
    imageUrl: "assets/projects/tower-thumble.gif",
    imageAlt: "Tower Thumble 프로젝트 대표 화면",
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
    decisions: [
      ["세 모드를 하나의 흐름으로 관리", "Puzzle, Race, Survival은 승리 조건이 다르지만 시작·종료·블록 생성·UI·네트워크 예외 처리는 같았습니다. 이를 추상 GameState 부모에 모으고 각 모드는 판정 규칙만 오버라이드해 기능 추가가 공통 흐름을 흔들지 않게 했습니다."],
      ["동시에 시작하는 것처럼 보이게", "클라이언트마다 로딩과 RPC 도착 시간이 달라 로컬 타이머만으로는 공정한 출발을 만들 수 없었습니다. PUN 서버 시간을 기준으로 시작 시각을 공유하고, 메시지를 늦게 받은 클라이언트는 지연량을 계산해 카운트다운을 보정했습니다."],
      ["판정 권한을 한곳에 집중", "모든 클라이언트가 충돌과 점수를 계산하면 중복 집계가 발생합니다. Puzzle의 Overlap 판정은 방장만 수행하고 결과를 공유했으며, Race는 RayCastBox로 최고 높이를 비교하고 Survival은 HP와 Freeze 상태를 동기화하도록 모드별 판정 경계를 정했습니다."]
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
    imageUrl: "assets/projects/sm64-nds.gif",
    imageAlt: "Super Mario 64 NDS 재구현 플레이 화면",
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
    decisions: [
      ["거대한 단일 지형에서 표면 구분", "원본 맵은 하나의 큰 MeshCollider로 구성되어 레이어만으로 벽과 바닥을 나눌 수 없었습니다. 충돌 지점의 Contact Normal 중 y값이 임계치 이상이면 지면, 작으면 벽으로 판단해 이동과 점프 반응을 구분했습니다."],
      ["원작의 이동 감각 재구성", "걷기와 달리기는 목표 속도로 즉시 바꾸지 않고 가속도를 누적해 관성을 만들었습니다. GroundCheck와 Slide 상태를 점프 단계와 함께 검사하고, 서브메시 재질과 Physics Material의 마찰값을 이용해 미끄러운 경사면을 별도 처리했습니다."],
      ["몬스터마다 반복되는 로직 정리", "굼바와 킹폭탄의 순찰·추격·피격 흐름을 각각 작성하면 상태 전환 코드가 중복됐습니다. 공통 추상 상태와 Blackboard에 감지 대상과 이동 정보를 모으고, 몬스터별 공격 방식과 피격 타입만 구체화했습니다."]
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
    imageUrl: "assets/projects/spellcraft-vr.png",
    imageAlt: "Spellcraft VR 마법 전투 콘셉트 화면",
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
    decisions: [
      ["제스처 인식 방식 비교", "VR에서 빠르고 반복 가능한 주문 입력을 만들기 위해 순서가 있는 충돌 박스, 손의 진행 방향, 이동 경로를 텍스처로 만든 뒤 OCR로 읽는 방식을 후보로 두었습니다. 정확도뿐 아니라 사용자가 실패 이유를 이해할 수 있는지도 판단 기준으로 삼았습니다."],
      ["주문을 문법으로 조합", "주문을 개별 프리셋으로 늘리는 대신 동사·명사·반복 요소를 순서대로 해석하는 구조를 설계했습니다. 예를 들어 불 속성에 발사 동작과 반복 수식을 붙이는 방식으로, 적은 입력 규칙에서 다양한 결과가 나오도록 범위를 정했습니다."],
      ["콘셉트 단계의 검증 범위", "D&D의 역할 수행, Noita의 조합식 마법, Isaac의 반복 탐험에서 핵심 경험을 추출했습니다. 현재는 완성 기능을 주장하기보다 중거리 마법 전투와 절차적 던전이 VR에서 읽히는지 검증하는 R&D 프로젝트로 명확히 구분했습니다."]
    ],
    tech: ["Unity", "VR", "Gesture Input", "System Design", "Procedural Dungeon"]
  }
];

const caseStudies = {
  DARKNIGHT: {
    process: [
      ["01", "타일맵 정의", "청크 너비와 높이로 TileType 2차원 배열을 만들고, 기본값은 Wall, 생성된 방과 복도는 Floor로 기록했습니다."],
      ["02", "BSP 재귀 분할", "공간을 Left/Right 노드로 나누되 분할선을 전체 길이의 0.3~0.8 사이에서 정했습니다. 최소 방 크기 조건으로 지나치게 작은 공간을 방지했습니다."],
      ["03", "방과 복도 연결", "리프 노드에 크기가 다른 방을 배치하고 부모 노드를 역순회했습니다. 양쪽 자식 방의 중심을 ㄱ·ㄴ자 경로로 연결해 모든 방의 도달 가능성을 보장했습니다."],
      ["04", "런타임 청크 완성", "사방 이웃의 연결 상태를 검사해 가장 얇은 벽을 찾고 RaycastAll로 통로를 열었습니다. 연결 완료 후 메시 결합, NavMesh 갱신, 풀 오브젝트 배치를 순차 실행했습니다."]
    ],
    troubleshooting: [
      ["배치와 활성 오브젝트가 계속 증가", "청크 좌표를 키로 딕셔너리화하고 플레이어 시야 거리 안의 청크만 Dirty Flag로 활성화했습니다. 정적 오브젝트와 커스텀 셰이더가 많은 구조라 매 프레임 오버헤드가 있는 GPU Instancing보다 생성 시 한 번 비용을 지불하는 Mesh Combine을 선택했습니다."],
      ["던전 확장 순간 프레임 드롭", "Mesh Combine, NavMesh 데이터 수집, 풀 보충이 동시에 실행되는 것이 원인이었습니다. 무거운 작업을 단계별 함수로 분해하고 Coroutine과 UniTask로 프레임에 분산했으며, 풀 부족분은 프레임당 하나만 생성하도록 제한했습니다."],
      ["NavMesh 갱신 때 전체 계층 탐색", "기본 수집 방식은 청크가 늘수록 탐색 비용이 커졌습니다. 각 청크가 자신의 NavMeshModifier 목록을 미리 보관하게 하고 갱신 범위의 데이터만 수동 조합해 UpdateNavMesh에 전달했습니다."]
    ]
  },
  "SCENT ATELIER": {
    process: [
      ["01", "게임 데이터 분리", "향수 종류와 레시피를 ScriptableObject로 정의하고 재료 조합은 LINQ로 비교했습니다. 진행 상태와 밸런스 데이터를 분리해 레시피 추가가 코드 수정으로 이어지지 않게 했습니다."],
      ["02", "병 기울기 판정", "컨트롤러가 잡은 병의 방향과 월드 아래 방향을 내적해 따르는 상태를 계산했습니다. 병 입구의 Raycast가 다른 용기를 찾았을 때만 액체 전달을 시작했습니다."],
      ["03", "액체와 조합 피드백", "전달량을 FillAmount 셰이더에 반영하고, 흔들림이 임계값을 넘으면 현재 재료 목록을 레시피와 대조했습니다. 결과에 따라 완성 향수와 화면 피드백을 갱신했습니다."],
      ["04", "저장과 네트워크 연결", "스테이지 정보를 JsonUtility로 직렬화한 뒤 PUN 닉네임을 키로 Firebase Realtime Database에 저장했습니다. Grab 상태, 소유권, 액체량은 RPC와 상태 동기화로 공유했습니다."]
    ],
    troubleshooting: [
      ["같은 병을 잡을 때 물리 결과가 달라짐", "상호작용 시작 시 오브젝트 소유권을 요청하고 원격 플레이어가 다시 잡지 못하도록 레이어를 제한했습니다. 잡고 있는 상태를 일회성 RPC가 아닌 지속 상태로 동기화해 늦게 입장한 사용자도 같은 상태를 받게 했습니다."],
      ["씬 이동 후 진행 정보가 사라짐", "게임 매니저를 씬 전환에도 유지되는 싱글턴으로 구성하고 상태 변경 콜백을 두었습니다. 저장 데이터는 Firebase 트랜잭션과 스냅샷으로 읽어 중복 쓰기와 비동기 순서 문제를 줄였습니다."],
      ["밤낮 변화가 장면마다 따로 움직임", "시간 상태를 한곳에서 관리하고 DOTween으로 Directional Light와 Skybox 셰이더 값을 함께 보간해 조명과 배경이 동일한 시간축을 따르도록 만들었습니다."]
    ]
  },
  "TOWER THUMBLE": {
    process: [
      ["01", "공통 상태 흐름", "GameState 추상 클래스에 준비, 시작, 블록 생성, 종료, UI 갱신과 네트워크 예외 처리를 정의했습니다."],
      ["02", "모드 규칙 주입", "Puzzle은 목표 형태 충돌, Race는 최고 높이, Survival은 HP와 Freeze를 승패 기준으로 구현하고 공통 흐름에는 모드별 판정만 연결했습니다."],
      ["03", "동시 시작 보정", "방장이 PUN 서버 시간 기준의 시작 시각을 공유했습니다. 각 클라이언트는 수신 시점과 목표 시각의 차이를 계산해 동일한 게임 시간에 진입했습니다."],
      ["04", "로그인부터 매치까지", "Firebase Auth 인증, 닉네임 설정, PUN 로비 접속, 방 생성·참가를 하나의 UI 흐름으로 연결하고 방 Custom Property에 선택 모드를 기록했습니다."]
    ],
    troubleshooting: [
      ["클라이언트마다 시작 시간이 다름", "RPC를 받은 순간 카운트다운을 시작하면 네트워크 지연이 그대로 경기 차이가 됐습니다. 서버의 절대 시작 시각을 기준으로 남은 시간을 역산해 지연된 클라이언트를 보정했습니다."],
      ["점수와 충돌이 중복 판정됨", "Puzzle의 Overlap과 점수 판정을 Master Client만 수행하도록 권한을 집중했습니다. 다른 플레이어는 결과만 받아 UI와 상태를 갱신해 중복 집계를 막았습니다."],
      ["모드 추가 때 기존 코드가 흔들림", "공통 생명주기와 모드 규칙을 분리해 새로운 모드는 GameState를 상속하고 판정 메서드만 구현하도록 변경했습니다. UI와 네트워크 흐름은 재사용했습니다."]
    ]
  },
  "SM64 NDS": {
    process: [
      ["01", "입력과 물리 주기 분리", "PlayerInput 콜백에서 입력을 저장하고 FixedUpdate 주기에 MoveDir로 반영해 Rigidbody 물리와 입력 처리 시점을 맞췄습니다."],
      ["02", "표면 상태 판정", "Contact Normal의 y값으로 벽과 지면을 구분하고, 충돌한 서브메시의 재질에서 미끄러짐 임계값을 가져와 Slide 진입 여부를 정했습니다."],
      ["03", "이동 감각 구성", "걷기가 유지되면 가속해 달리기로 전환하고 Physics Material의 마찰을 조절해 경사면 슬라이드를 만들었습니다. GroundCheck와 Slide 상태를 조합해 3단 점프 조건을 제한했습니다."],
      ["04", "플레이 루프 완성", "굼바와 폭탄병의 상태, Punch·Press·Boom 피격 타입, 코인과 스타를 연결했습니다. 일반·블루·레드 코인과 100코인 스타까지 원작의 목표 흐름을 재구성했습니다."]
    ],
    troubleshooting: [
      ["하나의 MeshCollider에서 벽과 바닥을 구분할 수 없음", "폭탄병의 전쟁터 맵이 거대한 단일 콜라이더라 레이어 분리가 불가능했습니다. 접촉 법선의 수직 성분을 사용해 지면, 벽, 경사면을 런타임에 분류했습니다."],
      ["이동이 즉각 변해 원작의 관성이 사라짐", "목표 속도를 바로 대입하지 않고 걷기 지속 시간을 기준으로 가속도를 누적했습니다. Slide에서는 별도의 마찰과 애니메이션 상태를 사용해 조작감의 연속성을 유지했습니다."],
      ["몬스터별 상태 코드가 반복됨", "상태 전환과 Blackboard 데이터는 추상 계층으로 올리고, 순찰·추격은 Navigation 기반 공통 상태로 만들었습니다. 몬스터별 공격과 사망 반응만 독립 구현했습니다."]
    ]
  },
  "SPELLCRAFT VR": {
    process: [
      ["01", "핵심 경험 정의", "D&D의 주문 캐스팅, Noita의 조합 마법, Isaac의 반복 탐험을 분석해 직접 만든 주문으로 던전을 공략하는 VR 경험을 목표로 정했습니다."],
      ["02", "제스처 입력 후보", "Collision Box 통과 순서, 손의 이동 방향, 경로 텍스처 OCR을 후보로 두고 정확도, 피드백 가능성, VR 성능 비용을 비교했습니다."],
      ["03", "주문 문법 설계", "쏘다 같은 동사와 불·아이스 같은 명사를 순서대로 해석하고 반복 연산을 수식어로 추가하는 구조를 설계했습니다."],
      ["04", "전투 루프 가설", "중거리 마법 전투와 절차적 던전 탐험을 결합하고 주문 제작, 전투 검증, 보상 획득, 다음 방 진입의 반복 흐름을 검증 범위로 설정했습니다."]
    ],
    troubleshooting: [
      ["복잡한 제스처일수록 인식 실패 이유가 불명확", "정확도만 높이는 방식보다 사용자가 어느 구간에서 실패했는지 보여줄 수 있는 단계형 입력을 우선 검토했습니다. 충돌 순서 방식은 디버깅과 시각 피드백이 명확하다는 장점이 있습니다."],
      ["주문 종류가 늘수록 개별 구현이 폭증", "완성 주문을 하나씩 하드코딩하지 않고 동사·명사·반복을 조합하는 문법으로 확장 지점을 정의했습니다. 두 번 반복한 발사+불은 2연발 파이어볼처럼 해석됩니다."],
      ["아직 구현되지 않은 내용을 완성 기능처럼 보일 위험", "이 프로젝트는 구현 완료 사례가 아니라 시스템 R&D임을 명시하고, 입력 후보 비교와 주문 문법처럼 실제로 설계·검증한 범위만 포트폴리오에 제시합니다."]
    ]
  }
};

const caseStudyVisuals = {
  DARKNIGHT: [
    { src: "assets/projects/darknight.gif", caption: "PROCEDURAL DUNGEON / GAMEPLAY" },
    { src: "assets/projects/details/darknight-bsp.png", caption: "BSP PARTITION MAP / NOTION ARCHIVE" },
    { src: "assets/projects/details/darknight-combine-after.png", caption: "MESH COMBINE / OPTIMIZED" },
    { src: "assets/projects/details/darknight-streaming.gif", caption: "CHUNK STREAMING / RUNTIME" }
  ],
  "SCENT ATELIER": [
    { src: "assets/projects/scent-atelier.gif", caption: "VR SCENT ATELIER / PROJECT VISUAL" },
    { src: "assets/projects/details/scent-architecture.png", caption: "SYSTEM ARCHITECTURE / CLASS DIAGRAM" },
    { src: "assets/projects/details/scent-liquid.gif", caption: "LIQUID TRANSFER / VR INTERACTION" },
    { src: "assets/projects/details/scent-sync-issue.gif", caption: "NETWORK SYNC / ISSUE ANALYSIS" }
  ],
  "TOWER THUMBLE": [
    { src: "assets/projects/tower-thumble.gif", caption: "ONLINE PUZZLE / PROJECT VISUAL" },
    { src: "assets/projects/details/tower-penalty.gif", caption: "FALL PENALTY / GAME RULE" },
    { src: "assets/projects/details/tower-height.gif", caption: "HEIGHT DETECTION / RACE MODE" },
    { src: "assets/projects/details/tower-login.gif", caption: "LOGIN & LOBBY / NETWORK FLOW" }
  ],
  "SM64 NDS": [
    { src: "assets/projects/sm64-nds.gif", caption: "BOB-OMB BATTLEFIELD / GAMEPLAY" },
    { src: "assets/projects/details/mario-ground.gif", caption: "GROUND CHECK / CONTACT NORMAL" },
    { src: "assets/projects/details/mario-slide.gif", caption: "SLIDE MOVEMENT / PHYSICS" },
    { src: "assets/projects/details/mario-goomba.gif", caption: "ENEMY STATE / GOOMBA" }
  ],
  "SPELLCRAFT VR": [
    { src: "assets/projects/spellcraft-vr.png", caption: "SPELLCASTING VR / CONCEPT VISUAL" },
    { caption: "GESTURE INPUT TEST / 추후 이미지 추가 예정" },
    { caption: "SPELL GRAMMAR / 추후 이미지 추가 예정" },
    { caption: "DUNGEON LOOP / 추후 이미지 추가 예정" }
  ]
};

const track = document.querySelector("#project-track");
const detailTitle = document.querySelector("#detail-title");
const detailSummary = document.querySelector("#detail-summary");
const projectMeta = document.querySelector("#project-meta");
const contributionList = document.querySelector("#contribution-list");
const implementationList = document.querySelector("#implementation-list");
const decisionList = document.querySelector("#decision-list");
const notionDetailLink = document.querySelector("#notion-detail-link");
const processList = document.querySelector("#process-list");
const troubleshootingList = document.querySelector("#troubleshooting-list");
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
  mediaFrame.classList.remove("image-missing");
  mediaFrame.innerHTML = `
    <div class="media-cover">
      ${project.imageUrl ? `<img class="project-visual" src="${project.imageUrl}" alt="${project.imageAlt || project.title}" loading="lazy">` : ""}
      <div class="image-placeholder" role="img" aria-label="${project.title} 설명 이미지 추후 추가 예정">
        <span>PROJECT VISUAL</span>
        <strong>추후 이미지 추가 예정</strong>
      </div>
      <div class="media-shade" aria-hidden="true"></div>
      <div class="media-overlay">
        <p>PROJECT VISUAL / NOTION ARCHIVE</p>
        ${embedUrl ? `<button class="media-play-button" type="button"><span aria-hidden="true">▶</span> PLAY GAMEPLAY</button>` : `<strong>${project.shortTitle}</strong>`}
      </div>
      <div class="media-links">
        ${project.sourceUrl ? `<a href="${project.sourceUrl}" target="_blank" rel="noreferrer">NOTION SOURCE ↗</a>` : ""}
        ${project.videoUrl ? `<a class="secondary" href="${project.videoUrl}" target="_blank" rel="noreferrer">YOUTUBE ↗</a>` : ""}
      </div>
    </div>
  `;

  const image = mediaFrame.querySelector(".project-visual");
  if (!image) mediaFrame.classList.add("image-missing");
  else image.addEventListener("error", () => mediaFrame.classList.add("image-missing"), { once: true });

  const playButton = mediaFrame.querySelector(".media-play-button");
  if (!playButton || !embedUrl) return;

  playButton.addEventListener("click", () => {
    const separator = embedUrl.includes("?") ? "&" : "?";
    const origin = window.location.origin;
    const finalUrl = `${embedUrl}${separator}autoplay=1&vq=hd1080&rel=0&enablejsapi=1&origin=${encodeURIComponent(origin)}&widgetreferrer=${encodeURIComponent(window.location.href)}`;
    mediaFrame.innerHTML = `
      <iframe src="${finalUrl}" title="${project.title} gameplay video" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" allowfullscreen referrerpolicy="strict-origin-when-cross-origin"></iframe>
    `;
  });
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
  const caseStudy = caseStudies[project.shortTitle];
  const studyVisuals = caseStudyVisuals[project.shortTitle];
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
  decisionList.innerHTML = project.decisions
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
  notionDetailLink.href = project.sourceUrl || "#";
  processList.innerHTML = caseStudy.process.map((item, itemIndex) => `
    <article class="case-step">
      <figure class="case-visual${studyVisuals[itemIndex].src ? "" : " case-visual-empty"}">
        ${studyVisuals[itemIndex].src ? `<img src="${studyVisuals[itemIndex].src}" alt="${project.title} ${item[1]} 관련 자료" loading="lazy">` : `<span>VISUAL<br>TO BE ADDED</span>`}
        <figcaption>${studyVisuals[itemIndex].caption}</figcaption>
      </figure>
      <div class="case-step-copy">
        <span>${item[0]}</span>
        <h4>${item[1]}</h4>
        <p>${item[2]}</p>
      </div>
    </article>
  `).join("");
  troubleshootingList.innerHTML = caseStudy.troubleshooting.map((item, itemIndex) => `
    <article class="trouble-card">
      <p class="trouble-label">ISSUE ${String(itemIndex + 1).padStart(2, "0")}</p>
      <h4>${item[0]}</h4>
      <p>${item[1]}</p>
    </article>
  `).join("");
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
