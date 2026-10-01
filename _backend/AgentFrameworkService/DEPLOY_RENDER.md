# Agent Framework + WebGL 배포 안내

현재 구조는 `ProjectArchive.github.io/main`의 정적 WebGL 화면과 Render의 .NET 백엔드를 분리한다. 같은 저장소의 `render.yaml`은 `_backend/AgentFrameworkService`를 Docker로 배포한다. `santamkj/webBuild`는 개발·빌드 원본이며 실제 서비스 반영은 검증된 변경을 이 저장소로 동기화한 후에 진행한다.

## 공개 전 필수 확인

1. **LLM 제공업체 계정에서 하드 사용 한도 또는 무료 쿼터를 설정한다.** 서비스의 `AGENT_FRAMEWORK_DAILY_TOKEN_BUDGET`은 메모리 기반이라 Render 무료 인스턴스가 잠들거나 재시작되면 초기화된다. 이 값만으로 비용 상한을 보장하지 않는다. 한도를 제공하지 않는 유료 키로 공개하지 않는다.
2. `AGENT_FRAMEWORK_PROVIDER_CONFIG_JSON`은 Render 대시보드의 비밀 환경변수로만 입력한다. Git, Unity StreamingAssets, WebGL, GitHub Pages, 이 문서에 실제 키를 쓰지 않는다. 공개 모드에서 브라우저가 보낸 Provider 키는 거부된다.
3. 현재 공개 게임은 서버 재시작 시 진행 중인 세션이 사라진다. 무료 플랜은 15분 동안 요청이 없으면 잠들며 첫 연결에 약 1분 걸릴 수 있다. 웹 로딩 화면은 백엔드 상태를 최대 2분간 재시도한 뒤 Unity를 시작한다.
4. 공개 배포 전에 프로젝트 `AGENTS.md`의 P0/P1 게이트와 새 WebGL 빌드 검증을 다시 수행한다. 기존에 남아 있는 Unity Console 오류가 빌드를 방해하면 먼저 해결한다.

## Render 백엔드 생성

1. `ProjectArchive.github.io/main`에 검증된 백엔드 소스와 `render.yaml`이 올라와 있는지 확인한다. API 키는 저장소에 포함하지 않는다.
2. [Render Dashboard](https://dashboard.render.com/)에서 **New → Blueprint**를 선택하고 `tjdeo1102/ProjectArchive.github.io` 저장소의 `render.yaml`을 사용한다. Free 플랜, `_backend/AgentFrameworkService` 루트, Docker 런타임, `main` 브랜치를 확인한다. 자동 재배포는 꺼 두었으므로 이후 배포는 P0/P1 확인 후 수동으로 시작한다.
3. 생성 화면에서 `AGENT_FRAMEWORK_PROVIDER_CONFIG_JSON`의 비밀 값을 입력한다. 기존 `llm_config.json`과 동일한 `providerOverride`/`providers` 구조를 사용하되, 실제로 사용할 원격 Provider와 모델만 남긴다. `OllamaLocal`과 `localhost` 주소는 Render 서버에서 로컬 PC의 모델을 가리키지 않는다. 모든 Provider에 양수 `rpm`/`tpm`을 설정한다. **키를 채팅이나 커밋에 붙여넣지 않는다.**
4. 서비스가 시작되면 `https://<서비스명>.onrender.com/agent-framework/health`가 `status: ok`를 반환하는지 확인한다. Render의 실제 서비스 주소를 기록한다. `AGENT_FRAMEWORK_ALLOWED_ORIGIN`은 경로가 아닌 `https://tjdeo1102.github.io`여야 한다.

## GitHub Pages 프론트 연결

1. 정적 사이트의 `/mafia-simulation/agent-framework-config.json`에 아래 형태로 **Render 공개 주소만** 적는다. 이 파일에는 키나 Provider 설정을 넣지 않는다.

   ```json
   { "apiBaseUrl": "https://<실제-서비스명>.onrender.com/agent-framework" }
   ```

2. 검증된 WebGL 빌드 디렉터리의 `index.html`, `Build`, `TemplateData`, `StreamingAssets`를 포트폴리오 저장소의 `/mafia-simulation/`에 반영한다. Pages가 게시되면 `https://tjdeo1102.github.io/ProjectArchive.github.io/mafia-simulation/`에서 열어 본다. `index.html`을 `file://`로 직접 열면 동작하지 않는다.
3. 브라우저에서 **서버 연결 대기 → 게임 파일 로딩 → NPC 설정 화면** 순서를 확인한다. 장시간 유휴 후 첫 접속과 두 번째 접속을 모두 시험한다. 실제 Provider 한도 안에서 세션 생성 및 짧은 게임도 검증한다.

## 실패 시 점검

- 로딩 화면에서 서버 연결이 끝나지 않음: Render 로그, `/agent-framework/health`, `AGENT_FRAMEWORK_PROVIDER_CONFIG_JSON`, 정확한 CORS origin, Pages의 `agent-framework-config.json` 주소를 확인한다.
- 게임 파일 로딩 실패: Pages의 `Build/*.unityweb` 요청 상태와 `TemplateData` 파일 경로를 확인한다.
- 게임 시작 후 세션 생성 실패: Render 로그의 예외 종류와 Provider의 모델명·키·RPM/TPM을 확인한다. `health` 성공은 LLM 호출 성공을 의미하지 않는다.
- 예기치 않은 비용: 즉시 제공업체 키를 비활성화하고 Render 서비스를 일시 중지한다. Render의 무료 호스팅 한도와 LLM 제공업체의 API 과금은 별개다.

참고: [Render Docker 배포](https://render.com/docs/docker), [Render Blueprint 설정](https://render.com/docs/blueprint-spec), [Render 무료 플랜 제한](https://render.com/docs/free).
