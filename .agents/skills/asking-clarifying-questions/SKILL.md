---
name: asking-clarifying-questions
description: Use when an ambiguity remains after safe internal resolution and a single answer from your human partner would unblock the next action, without needing a full design session or a pre-action risk gate.
metadata:
  source-id: asking-clarifying-questions
  source-path: codex-marketplace/plugins/repo-worker-pack/skills/asking-clarifying-questions/SKILL.md
  provenance-name: Asking Clarifying Questions first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: mid-flight ambiguity resolution through a single clarifying question
  use_when:
  - Use when an ambiguity is internally unresolved and a single human decision would unblock the immediate next step.
  - Use when the agent is mid-plan, mid-execution, or inside another skill and a missing fact, term, scope, boundary, or output shape prevents safe progress.
  - Use when the answer is a concrete decision, not a design.
  do_not_use_when:
  - Do not use when the ambiguity needs a full spec or design; use brainstorming.
  - Do not use when the next action could violate scope, authority, source truth, canon, safety, or involve irreversible mutation; use risk-gates.
  - Do not use when the answer is already forced by durable source, policy, or a safe default; resolve internally.
  use_instead:
  - brainstorming
  - risk-gates
  related_skills:
  - brainstorming
  - risk-gates
  - writing-plans
  - executing-plans
  - handoff-gates
license: MIT
---

# Asking Clarifying Questions

Ask one narrow question per turn that your human partner can answer when a single unresolved ambiguity blocks the immediate next step. Use as many turns as needed: one fact per message, then continue.

This is an anytime escape hatch. If a single missing fact blocks the next step of the current skill, invoke this skill, record the answer, and continue; repeat for the next missing fact. Do not bundle multiple facts into one message — ask the next one in the following turn.

## Core pattern

1. State the immediate next action that depends on the answer.
2. State the ambiguity concisely (one missing fact, term, scope, boundary, or output shape).
3. State the risk of guessing.
4. Give a concrete recommendation and the available options.
5. Ask one question.
6. Record the answer and continue.
7. If another missing fact still blocks the next step, repeat from step 1 in the next turn.

## When to use

- Internal resolution is exhausted (rules, source truth, non-goals, safe defaults).
- A single missing decision separates the agent from the next action.
- The cost of guessing is wasted motion or reversible rework, not a canon or authority mistake.
- One fact is missing now; further missing facts can wait their own turn.

## When not to use

- The ambiguity needs a full design or spec: use `brainstorming`.
- The ambiguity affects scope, authority, source truth, canon, safety, or irreversible mutation: use `risk-gates` and accept a block if needed.
- The answer is already forced or harmless: resolve internally and do not ask.

## Common mistakes

- Asking a vague question instead of a single decision.
- Asking when the answer is already in durable source or policy.
- Treating a clarifying question as a substitute for a missing design or risk gate.
- Asking multiple questions in one turn — one fact per message, as many turns as needed.

## Relation to other skills

- `brainstorming` asks many questions to shape a design.
- `risk-gates` decides whether to proceed, repair, or block when hidden risk is present.
- `asking-clarifying-questions` handles the 'interactive'/'amber' outcome where a single human answer is the lawful next step.
