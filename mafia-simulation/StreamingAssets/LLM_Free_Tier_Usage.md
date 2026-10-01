# LLM Free Tier Usage Notes

Last checked: 2026-06-18

This file summarizes the free-tier or free-credit behavior used by the current NPC provider setup in `llm_config.json`.
Provider policies and model availability change frequently, so re-check the linked official pages before a long test session.

## Current Test Mapping

| NPC | Provider | Current model |
| --- | --- | --- |
| NPC1 | OllamaLocal | `llama3.1:latest` |
| NPC2 | Gemini | `gemini-3.1-flash-lite` |
| NPC3 | Groq | `llama-3.1-8b-instant` |
| NPC4 | OpenRouter | `nex-agi/nex-n2-pro:free` |
| NPC5 | HuggingFace | `openai/gpt-oss-120b:cheapest` |

## Gemini

Recommended model:

```text
gemini-3.1-flash-lite
```

Free-tier behavior:

- Gemini pricing lists selected models, including `gemini-3.1-flash-lite` and `gemini-3.5-flash`, with free input and output tokens on Free Tier.
- Exact RPM, TPM, and RPD limits are project/model specific and should be checked in Google AI Studio.
- Rate limits are applied per Google Cloud project, not per API key.
- If the project is in Paid/Prepay mode and credits are depleted, free-tier model names will still fail with a billing or `RESOURCE_EXHAUSTED` error.

Usage page:

- Google AI Studio > Dashboard > Usage
- Google AI Studio > Rate limits

Official docs:

- https://ai.google.dev/gemini-api/docs/pricing
- https://ai.google.dev/gemini-api/docs/rate-limits

## Groq

Recommended model:

```text
llama-3.1-8b-instant
```

Free Plan limits for `llama-3.1-8b-instant`:

| Metric | Limit |
| --- | ---: |
| RPM | 30 requests/min |
| RPD | 14.4K requests/day |
| TPM | 6K tokens/min |
| TPD | 500K tokens/day |

Notes:

- Groq does not use a `:free` model suffix.
- The API key works under the account's current plan; Free Plan calls are limited by the rate-limit table.
- Multiple NPCs can hit RPM or TPM quickly, so keep `LLMQueueProcessor.dispatchIntervalDelay` conservative during tests.

Usage page:

- Groq Console > Limits
- Groq Console > Usage

Official docs:

- https://console.groq.com/docs/rate-limits

## OpenRouter

Recommended model:

```text
nex-agi/nex-n2-pro:free
```

Free model behavior:

| Account state | Free model limit |
| --- | ---: |
| No purchased credits | 50 free model API requests/day |
| Purchased at least 10 credits | 1000 free model API requests/day |

Notes:

- OpenRouter free models usually use the `:free` suffix.
- Free model availability changes often. If a `:free` slug returns 404, replace it with a current free model from the Models API or model browser.
- `openrouter/free` can automatically route to a free model, but a fixed free model is easier for NPC comparison tests.

Usage page:

- OpenRouter > Activity
- OpenRouter > Credits

Official docs:

- https://openrouter.ai/docs/faq
- https://openrouter.ai/api/v1/models

## Hugging Face Inference Providers

Recommended model:

```text
openai/gpt-oss-120b:cheapest
```

Free-credit behavior:

| Account type | Monthly credits |
| --- | ---: |
| Free user | $0.10/month |
| PRO user | $2.00/month |
| Team/Enterprise org | $2.00/month per seat |

Notes:

- Hugging Face Inference Providers uses monthly credits, not a simple request/day limit.
- Costs depend on the routed provider and model. The `:cheapest` suffix asks Hugging Face to select the lowest-cost available provider for the model.
- Create a fine-grained Hugging Face token with `Make calls to Inference Providers` permission.
- After credits are exhausted, extra usage requires purchasing credits.

Usage page:

- Hugging Face > Settings > Billing
- Hugging Face > Inference Providers settings

Official docs:

- https://huggingface.co/docs/inference-providers/en/pricing
- https://huggingface.co/docs/inference-providers/en/tasks/chat-completion
- https://huggingface.co/settings/tokens

## Test Order

1. Test `OllamaLocal` first to confirm the scene/prompt path.
2. Test `Groq`; it has the clearest request/token free-plan limits.
3. Test `OpenRouter`; replace the model if the `:free` slug expires.
4. Test `HuggingFace`; watch monthly credit consumption.
5. Test `Gemini`; confirm the project is actually Free Tier or has positive Prepay credits.

## Unity Connection Test

Use `Assets/Scenes/TestModelConectionScene.unity`.

Set `LLMConnectionTester.testTargetProvider` to one of:

```text
Gemini
Groq
OpenRouter
HuggingFace
OllamaLocal
```

Expected first logs:

```text
[Connection Test] Start. Target provider: <Provider>
[Connection Test] Config path: ...
[Connection Test] Loaded provider=<Provider>, model=<Model>
```

Expected success log:

```text
[Connection Test Success]
```
