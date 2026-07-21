For an unknown or non-Codex runtime, do not guess equivalence from model names. Inventory available models and classify them by capability:

```text
planner_orchestrator:
  best included/free model for the task’s reasoning and source access

engineer:
  best included/free software-engineering model

multimodal:
  best included/free model capable of the required visual evidence

technical_reviewer:
  strongest included/free live-source reviewer, preferably fresh context

architecture_intent_challenger:
  optional alternate model only when it offers a genuinely different competent lens

premium_backstop:
  disabled unless explicitly authorized
```

For each available model, capture:

* exact runtime label/slug;
* cost class;
* selectable reasoning levels;
* reasoning ceiling;
* context size;
* text or multimodal capability;
* preferred roles;
* prohibited roles;
* fallback route;
* whether selection can be enforced.

Prefer lowering reasoning on the strongest included model over selecting an older model solely because the task is easy, unless quotas, latency, or evaluation evidence favour the older model.
