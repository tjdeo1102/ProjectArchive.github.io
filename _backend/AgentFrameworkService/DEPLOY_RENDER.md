# Agent Framework + WebGL 배포 안내

현재 구조는 GitHub Pages의 정적 WebGL 화면과 Render의 .NET 백엔드를 분리한다. `santamkj/webBuild`는 빌드 원본이고, 실제 배포용 `render.yaml`과 검증된 서비스 소스는 `tjdeo1102/ProjectArchive.github.io/main`에 둔다.

## 공개 전 필수 확인

1. 방문자는 개인 Provider JSON을 업로드한다. **모델 확인 요청과 게임 진행은 방문자 본인의 API 사용량·과금에 반영될 수 있다.** 제공업체의 사용 한도 설정을 권장하며, 서비스의 세션 한도만으로 비용 상한을 보장하지 않는다.
2. `AGENT_FRAMEWORK_PROVIDER_CONFIG_JSON`은 공개 Render 설정에 넣지 않는다. 실제 키를 Git, Unity StreamingAssets, WebGL, GitHub Pages 또는 문서에 쓰지 않는다. 업로드된 키는 HTTPS로 서비스에 전송되어 게임 세션 메모리에만 유지되며, 닫기 요청 또는 유휴 만료 시 삭제된다. 브라우저 새로고침은 설정을 지운다.
3. 현재 공개 게임은 서버 재시작 시 진행 중인 세션이 사라진다. 무료 플랜은 15분 동안 요청이 없으면 잠들며 첫 연결에 약 1분 걸릴 수 있다. 웹 로딩 화면은 백엔드 상태를 최대 2분간 재시도한 뒤 Unity를 시작한다.
4. 공개 배포 전에 프로젝트 `AGENTS.md`의 P0/P1 게이트와 새 WebGL 빌드 검증을 다시 수행한다. 기존에 남아 있는 Unity Console 오류가 빌드를 방해하면 먼저 해결한다.

## Render 백엔드 생성

1. GitHub 인증을 확인하고, `santamkj/webBuild`에서 검증된 서비스 변경을 `ProjectArchive.github.io/main`의 `_backend/AgentFrameworkService`에 동기화한다. API 키는 어느 저장소에도 포함하지 않는다.
2. [Render Dashboard](https://dashboard.render.com/)에서 **New → Blueprint**를 선택하고 `tjdeo1102/ProjectArchive.github.io` 저장소의 `render.yaml`을 사용한다. Free 플랜, `_backend/AgentFrameworkService` 루트, Docker 런타임, `main` 브랜치를 확인한다. 자동 재배포는 꺼 두었으므로 이후 배포는 P0/P1 확인 후 수동으로 시작한다.
3. Blueprint 생성 화면에 Provider 비밀값 입력란이 없어야 한다. 표시된다면 최신 `render.yaml`이 반영되지 않은 것이다. 공개 서비스는 임의 `customBaseUrl`과 `OllamaLocal`을 거부한다. 방문자가 업로드할 JSON에는 실제 사용할 원격 Provider의 `apiKey`, `modelName`, 선택적 `allowedModels`, 양수 `rpm`/`tpm`을 넣는다. 한 번에 최대 8개 모델을 확인하며, 모델당 소량의 검증 호출이 실행된다. **키를 채팅이나 커밋에 붙여넣지 않는다.**
4. 서비스가 시작되면 `https://<서비스명>.onrender.com/agent-framework/health`가 `status: ok`를 반환하는지 확인한다. Render의 실제 서비스 주소를 기록한다. `AGENT_FRAMEWORK_ALLOWED_ORIGIN`은 경로가 아닌 `https://tjdeo1102.github.io`여야 한다.

## GitHub Pages 프론트 연결

1. 정적 사이트의 `/mafia-simulation/agent-framework-config.json`에 아래 형태로 **Render 공개 주소만** 적는다. 이 파일에는 키나 Provider 설정을 넣지 않는다.

   ```json
   { "apiBaseUrl": "https://<실제-서비스명>.onrender.com/agent-framework" }
   ```

2. 검증된 WebGL 빌드 디렉터리의 `index.html`, `Build`, `TemplateData`, `StreamingAssets`를 포트폴리오 저장소의 `/mafia-simulation/`에 반영한다. Pages가 게시되면 `https://tjdeo1102.github.io/ProjectArchive.github.io/mafia-simulation/`에서 열어 본다. `index.html`을 `file://`로 직접 열면 동작하지 않는다.
3. 브라우저에서 **서버 연결 대기 → 게임 파일 로딩 → Provider 업로드·모델 확인 → NPC별 모델 선택 → 프롬프트 → 참가 방식 → 게임** 순서를 확인한다. 게임 중 **설정 다시 하기**는 이전 세션을 닫고 새 게임 설정을 보여야 한다. 새로고침과 창 닫기 후 서버 세션 삭제 또는 유휴 만료를 확인한다. 실제 Provider 한도 안에서 짧은 게임도 검증한다.

## 실패 시 점검

- 로딩 화면에서 서버 연결이 끝나지 않음: Render 로그, `/agent-framework/health`, 정확한 CORS origin, Pages의 `agent-framework-config.json` 주소를 확인한다.
- 게임 파일 로딩 실패: Pages의 `Build/*.unityweb` 요청 상태와 `TemplateData` 파일 경로를 확인한다.
- 게임 시작 후 세션 생성 실패: Render 로그의 예외 종류와 Provider의 모델명·키·RPM/TPM을 확인한다. `health` 성공은 LLM 호출 성공을 의미하지 않는다.
- 예기치 않은 비용: 사용자가 즉시 해당 제공업체 키를 비활성화하고 사용 내역을 확인하도록 안내한다. Render의 무료 호스팅 한도와 방문자 LLM 제공업체의 API 과금은 별개다.

참고: [Render Docker 배포](https://render.com/docs/docker), [Render Blueprint 설정](https://render.com/docs/blueprint-spec), [Render 무료 플랜 제한](https://render.com/docs/free).
