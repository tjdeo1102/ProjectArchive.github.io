# LLM API Key 및 Options 설정 가이드

이 폴더의 `llm_config.json`은 공유 가능한 템플릿 파일이다.
실제 API 키가 들어있는 파일은 Git에 포함하지 않고, 각 개발자가 로컬에서만 관리한다.

## 파일 역할

| 파일 | 역할 | 공유 여부 |
| --- | --- | --- |
| `llm_config.json` | Unity가 기본으로 읽는 LLM 설정 파일 | 템플릿만 공유 |
| `llm_config.local.json` | 실제 API 키가 들어있는 로컬 보관용 복사본 | 공유 금지 |
| `LLMTemplates/` | Provider별 요청 JSON 템플릿 | 별도 공유 시 포함 |

## 현재 프로젝트 적용 방식

Unity 에디터의 독립 실행 테스트는 `Assets/StreamingAssets/llm_config.local.json`을 우선 읽고, 없으면 `llm_config.json`을 읽는다.
공개 WebGL 빌드는 두 파일을 포함하지 않는다. 웹 사용자는 게임의 1단계에서 개인 JSON 파일을 업로드하며, 검증된 모델만 NPC별 선택 목록에 표시된다.
웹에서 업로드한 설정은 현재 탭의 게임 세션에서만 쓰이고 새로고침하면 초기화된다. 모델 검증과 게임 진행에 제공업체 API 사용량이 발생할 수 있다.

예시:

```json
{
  "provider": "Gemini",
  "apiKey": "실제 Gemini API Key",
  "modelName": "gemini-3.1-flash-lite",
  "customBaseUrl": "",
  "options": {
    "temperature": 0.7,
    "max_tokens": 512
  }
}
```

`OllamaLocal`은 로컬 서버를 사용하므로 일반적으로 `apiKey`를 비워둔다.

## options 동작 방식

각 provider의 `options` 객체는 요청 JSON body의 최상위 필드로 펼쳐져 들어간다.

예를 들어 다음 설정은:

```json
{
  "options": {
    "temperature": 0.4,
    "max_tokens": 256
  }
}
```

실제 요청 body에서는 다음처럼 들어간다.

```json
{
  "model": "...",
  "temperature": 0.4,
  "max_tokens": 256,
  "messages": []
}
```

따라서 OpenAI-compatible provider, Gemini, Groq, OpenRouter, Hugging Face, Anthropic 계열처럼 top-level 옵션을 받는 API에 공통으로 사용할 수 있다.

## Ollama options 예시

Ollama는 자체 세부 옵션을 `options`라는 nested 객체로 받는다.
이 경우 `llm_config.json`의 `options` 안에 다시 `options`를 넣는다.

```json
{
  "provider": "OllamaLocal",
  "apiKey": "",
  "modelName": "llama3.1:latest",
  "customBaseUrl": "http://localhost:11434/v1/chat/completions",
  "options": {
    "temperature": 0.3,
    "top_p": 0.75,
    "top_k": 30,
    "options": {
      "num_ctx": 4096,
      "num_predict": 256,
      "repeat_last_n": 256,
      "repeat_penalty": 1.25
    },
    "keep_alive": -1
  }
}
```

반복 문장이 계속 나올 때는 아래 값을 우선 조정한다.

| 옵션 | 역할 | 현재 권장값 | 조정 방향 |
| --- | --- | ---: | --- |
| `temperature` | 응답 무작위성 | `0.3` | 반복이 심하면 낮추고, 답변이 너무 경직되면 조금 올린다 |
| `top_p` | 후보 토큰 누적 확률 제한 | `0.75` | 반복이 심하면 낮춘다 |
| `top_k` | 후보 토큰 개수 제한 | `30` | 반복이 심하면 낮춘다 |
| `repeat_last_n` | 반복 패널티를 적용할 최근 토큰 범위 | `256` | 장문 반복이 있으면 늘린다 |
| `repeat_penalty` | 최근 토큰 반복 억제 강도 | `1.25` | 답변이 너무 어색해지면 `1.18` 근처로 낮춘다 |
| `num_predict` | 최대 생성 토큰 수 | `256` | 길게 반복되면 낮춘다 |

## Anthropic options 예시

Anthropic Claude는 `max_tokens`가 필수에 가깝기 때문에 `options`에 포함하는 것을 권장한다.

```json
{
  "provider": "AnthropicClaude",
  "apiKey": "실제 Anthropic API Key",
  "modelName": "claude-3-5-haiku-latest",
  "customBaseUrl": "",
  "options": {
    "max_tokens": 1024,
    "temperature": 0.7
  }
}
```

## 추천 작업 순서

1. `llm_config.json` 템플릿을 기준으로 각 provider의 `apiKey` 값을 채운다.
2. 필요하면 provider별 `options`에서 `temperature`, `max_tokens`, `num_ctx` 등을 조정한다.
3. 실사용 키가 들어간 파일을 `llm_config.local.json`으로 복사해 로컬에만 보관한다.
4. Unity에서 `TestModelConectionScene`을 열고 `LLMConnectionTester.testTargetProvider`를 테스트할 provider로 변경한다.
5. Play Mode 실행 후 Console에서 `[Connection Test Success]` 로그를 확인한다.
6. 외부 공유 또는 PR 전에는 `llm_config.json`의 키 값을 다시 placeholder로 되돌린다.

## 로컬 복사본으로 복원하는 방법

이미 `llm_config.local.json`에 실제 키가 저장되어 있다면, 테스트 전에 다음처럼 복원할 수 있다.

```powershell
Copy-Item Assets/StreamingAssets/llm_config.local.json Assets/StreamingAssets/llm_config.json -Force
```

복원 후 Unity를 실행하면 프로젝트에는 `llm_config.json`의 값이 적용된다.

## 템플릿으로 되돌리는 방법

공유 전에는 `llm_config.json`의 `apiKey` 값을 다시 다음 형태로 바꾼다.

```text
__SET_GEMINI_API_KEY_HERE__
__SET_GROQ_API_KEY_HERE__
__SET_OPENROUTER_API_KEY_HERE__
__SET_HUGGINGFACE_API_KEY_HERE__
```

API 키는 채팅, PR, 커밋, 스크린샷에 포함하지 않는다.

## 주의사항

- `llm_config.local.json`은 개인 로컬 파일이며 Git에 포함하지 않는다.
- `Assets/StreamingAssets`가 ignore되어 있어도 `git add -f`를 사용하면 강제로 포함될 수 있으므로 주의한다.
- provider마다 지원하는 옵션 이름이 다르다. 지원하지 않는 옵션을 넣으면 해당 API에서 오류가 날 수 있다.
- 무료 티어 provider는 사용량 제한이 작으므로 연결 테스트와 소규모 NPC 테스트 위주로 사용한다.
