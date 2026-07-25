---
name: subagent-model-routing
description: Use when choosing a child subagent model, reasoning level, or context mode,
  or when retrying failed work by changing model, reasoning, or context.
metadata:
  source-id: subagent-model-routing
  source-path: sources/first_party/skills/subagent-model-routing/SKILL.md
  provenance-name: Subagent Model Routing first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when choosing a child subagent model, reasoning level, or context mode,
    or when retrying failed work by changing model, reasoning, or context.
  use_when:
  - Use before calling `spawn_agent` or an equivalent subagent tool.
  - Use when creating or selecting a named subagent configuration.
  - Use when recommending a child model, reasoning level, or context mode.
  - Use when retrying failed work by changing model, reasoning, or context.
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
  use_after:
  - inspecting-the-environment
license: MIT
---
# Subagent Model Routing

Use this skill before choosing a child subagent route. Detect the live dispatch
contract, load the shared policy and exactly one matching environment profile,
then choose the least escalated route the runtime actually exposes.

## Runtime contract

1. Detect the active child-dispatch contract.
2. Inventory the models, reasoning values, context controls, and capacity actually exposed.
3. Load `references/shared-policy.md` and exactly one matching profile.
4. Treat current runtime inventory as authoritative over stale profile metadata.
5. Choose the least escalated adequate exposed route; do not infer price or entitlement.
6. Record the profile, model or inheritance, reasoning or inheritance, context mode, rationale, and material limitation.
7. State explicitly when a desired route could not be enforced.

Routing chooses a route; it does not authorize delegation. Follow the current
task, environment, and repository rules before calling a child-dispatch tool.

## Profiles

| Live dispatch signature | Profile |
|---|---|
| `multi_agent_v1__spawn_agent` with Boolean `fork_context` | `references/codex-multi-agent-v1-profile.md` |
| `spawn_agent` with `fork_turns` | `references/codex-multi-agent-v2-profile.md` |
| Devin Desktop | `references/devin-desktop-profile.md` |
| Unknown or non-Codex runtime | `references/generic-free-first-profile.md` |

## Common pressure

When the obvious choice is unclear or contested, read `references/pressure-scenarios.md` first.
