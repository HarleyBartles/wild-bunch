---
name: subagent-model-routing
description: Use when choosing a child subagent model, reasoning level, context tier,
  or paid route, or when retrying failed work by changing model/reasoning/context.
metadata:
  source-id: subagent-model-routing
  source-path: sources/first_party/skills/subagent-model-routing/SKILL.md
  provenance-name: Subagent Model Routing first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when choosing a child subagent model, reasoning level, context tier,
    or paid route, or when retrying failed work by changing model/reasoning/context.
  use_when:
  - Use before calling `spawn_agent` or an equivalent subagent tool.
  - Use when creating or selecting a named subagent configuration.
  - Use when recommending a child model, reasoning level, context tier, or paid route.
  - Use when retrying failed work by changing model/reasoning/context.
  - Use when selecting an implementation, code-review, architecture-review, or adjudication
    agent.
  do_not_use_when:
  - Do not use to switch the current parent session when the runtime cannot change
    models mid-session.
  - Do not use when another more specific skill owns the task.
  related_skills:
  - dispatching-parallel-agents
  - risk-gates
  - work-mode-router
  - repo-worker-base
license: MIT
---
# Subagent Model Routing

Use this skill before choosing a child subagent route. Detect the runtime, load the shared policy and exactly one matching environment profile, then return the cheapest adequate included or approved route.

## Runtime contract

1. Detect the active environment.
2. Inventory the models and controls actually available.
3. Load `references/shared-policy.md` and exactly one matching profile.
4. Treat current runtime inventory as authoritative over stale profile metadata.
5. Choose the cheapest adequate included route.
6. Record a concise rationale and fallback when material.
7. State explicitly when a desired route could not be enforced.

## Profiles

| Runtime | Profile |
|---|---|
| OpenAI Codex / ChatGPT | `references/codex-profile.md` |
| Devin Desktop | `references/devin-desktop-profile.md` |
| Unknown or non-Codex runtime | `references/generic-free-first-profile.md` |

## Common pressure

When the obvious choice is unclear or contested, read `references/pressure-scenarios.md` first.
