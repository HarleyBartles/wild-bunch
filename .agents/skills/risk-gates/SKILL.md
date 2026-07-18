---
name: risk-gates
description: Use when a pre-action risk gate is needed before a mutation, dispatch,
  canon claim, analogy reliance, or resolution that could violate scope, authority,
  source truth, canon, safety, or user intent. Routes to the relevant gate reference
  docs based on the action and project context.
metadata:
  source-id: risk-gates
  source-path: sources/first_party/skills/risk-gates/SKILL.md
  provenance-name: Risk Gates first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when a pre-action risk gate is needed before a mutation, dispatch, canon
    claim, analogy reliance, or resolution that could violate scope, authority, source
    truth, canon, safety, or user intent. Routes to the relevant gate reference docs
    based on the action and project context.
  use_when:
  - Use when about to act, dispatch, mutate a durable surface, or treat a claim as
    resolved and hidden risk could make the action unsafe or false.
  - Use when an ambiguous term, scope, target, source, authority, output shape, time
    reference, or vocabulary item could cause the wrong action if guessed.
  - Use when about to make, change, summarize, publish, dispatch, or rely on a durable
    canon or truth claim.
  - Use when binding constraints (authority, scope, source hierarchy, workflow law,
    data/schema, provenance/license, canon/doctrine, safety/privacy) may be violated
    by a proposed move.
  - Use when about to rely on an analogy, metaphor, comparison, or frame to make a
    durable decision.
  do_not_use_when:
  - Do not use when the action is ordinary, unconstrained, and has no protected surfaces
    or required workflow steps.
  - Do not use when another more specific skill owns the task.
  - Do not use when the task is a broad planning or research workflow without a concrete action to gate.
  related_skills:
  - verification-before-completion
  - connector-safety
  - rooms-risk-gates
license: MIT
---
# Risk Gates

Use this skill before an action that could mutate a durable surface, dispatch work, make a canon claim, rely on an analogy, or treat a claim as resolved. A risk gate exposes hidden risk before GPT acts and turns unresolved risk into one of three safe outcomes: proceed, repair before proceeding, or block.

## Owned decision

Given a proposed next action and the risk domain a gate owns, decide whether the next action is:

- `green` — clear enough to proceed through the stated lawful route.
- `amber` — plausible, but a real unresolved choice, assumption, or evidence gap remains.
- `red` — proceeding would likely violate scope, authority, source truth, safety, canon, or user intent.
- `blocked` — the required context, authority, source access, or upstream work is unavailable.

## Gate modes

Use `internal_mode` when there is only one legitimate path forward. Resolve forced decisions privately and proceed, repair, or block without burdening the user with fake choices.

Use `interactive_mode` only when a lawful decision-maker must choose among real options, when the user explicitly asks to gate the item together, or when GPT cannot safely choose between multiple legitimate paths.

Use `blocked_mode` when missing authority, inaccessible evidence, absent source payloads, or unfinished upstream work prevents a safe green or repair.

## Queue contract

Interactive gate queues are not neutral questionnaires. Each visible item should state:

- the risk;
- why it matters before the proposed action;
- what green requires;
- GPT's recommendation;
- the decision needed from the user or other lawful authority.

Default to short conversational queues. Use structured formats only when the destination requires copyable YAML, JSON, issue text, a worker packet, a schema, or another formal artifact.

## Output-surface boundary

A gate green approves only the next action it actually checked. It does not turn a report, queue item, validator result, receipt, tool log, assistant-authored summary, or session memory into stronger proof than it is.

If a downstream workflow requires a specific output surface, such as a PR, issue comment, source file, validation log, or artifact path, a gate cannot launder that requirement into another surface. If the wrong surface is used after green, the green is stale; stop and recover through the owning workflow.

## Gate routing table

Read only the gate reference docs whose use-when matches the current action. Skip gates whose do-not-use-when matches. Do not read all reference docs by default.

### Generic gates (apply in any project)

| Gate | Use when | Do not use when | Reference |
|------|----------|-----------------|-----------|
| ambiguity-gate | An action or answer depends on interpreting an ambiguous term, scope, target, source, authority, output shape, time reference, or vocabulary item, and guessing wrong would cause the wrong scope, target, route, artifact, or answer. | The ambiguity is harmless, already resolved by durable source, or does not affect the immediate safe next step. | `references/gates/ambiguity-gate.md` |
| canon-gate | About to make, change, summarize, publish, dispatch, or rely on a durable canon/truth claim — project doctrine, world state, character facts, source-of-truth records, schemas, accepted decisions, or policy. | The claim is not canon-facing (ordinary conversation, non-durable working notes, or a claim with no truth-surface consequences). | `references/gates/canon-gate.md` |
| invariant-gate | About to take an action, answer, plan, dispatch, or durable mutation where binding constraints (authority, scope, source hierarchy, workflow law, data/schema, provenance/license, canon/doctrine, safety/privacy) may be violated. | No binding invariants are implicated — the action is ordinary, unconstrained, and has no protected surfaces or required workflow steps. | `references/gates/invariant-gate.md` |
| analogy-gate | About to rely on an analogy, metaphor, comparison, role model, frame, or project-specific shorthand to answer, plan, dispatch, or make a durable decision. | No analogy is doing evidentiary or decision work — the reasoning is source-grounded without metaphorical scaffolding. | `references/gates/analogy-gate.md` |
| feedback-gate | Review, verifier, worker, issue, PR, automated-check, or external feedback appears and could become action, scope, evidence, closure posture, or a worker instruction before current source reality and lawful ownership are checked. | The feedback is ordinary conversation, already verified against current source, or does not affect the immediate safe next step. | `references/gates/feedback-gate.md` |

## Project-specific overlays

Rooms-specific gate profiles (canon pressure, ambiguity preservation, analogy validation, zoom-out compression) live in the separate `rooms-risk-gates` overlay skill. When working in Rooms, Mostly, compose this base skill with `rooms-risk-gates` for rooms-specific gate questions. For non-Rooms projects, this base skill is sufficient.

## Workflow

1. Name the exact action that would happen after the gate.
2. Identify which gates are material using the routing table above. Read only those reference docs.
3. Identify hidden risk, unresolved decisions, unstable assumptions, contradictions, source gaps, authority gaps, or canon drift that could make the next action unsafe or false.
4. Classify the gate mode (internal, interactive, blocked).
5. Resolve forced decisions internally when policy, source authority, current scope, or user instruction leaves only one legitimate route.
6. Surface only unresolved legitimate choices.
7. Return green only when the next action has a lawful route, required authority, sufficient evidence, and the correct output surface.

## Boundaries

Do not use gates as broad planning, research, or execution workflows. Do not use a gate to create permission that the user, source surface, policy, project doctrine, or downstream skill has not granted. Do not import project-specific law into generic gate references. Do not read all gate reference docs by default — use the routing table to select only material gates.
