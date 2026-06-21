---
name: buster-framework
description: 'Use this skill when designing, running, repairing, or interpreting a buster: a pre-action review gate that exposes hidden risk before GPT acts, dispatches, mutates a durable surface, or treats a claim as resolved.'
metadata:
  source-id: buster-framework
  source-path: sources/first_party/skills/buster-framework/SKILL.md
  provenance-name: MARK-19 core generic buster House Skills source slice
license: "MIT"
---
# Buster Framework

Use this skill when designing, running, repairing, or interpreting a buster: a pre-action review gate that exposes hidden risk before GPT acts, dispatches, mutates a durable surface, or treats a claim as resolved.

A true buster is not a generic workflow name. It exists before action and turns unresolved risk into one of three safe outcomes: proceed, repair before proceeding, or block.

## Owned decision

Given a proposed next action and the risk domain a buster owns, decide whether the next action is:

- `green` â€” clear enough to proceed through the stated lawful route.
- `amber` â€” plausible, but a real unresolved choice, assumption, or evidence gap remains.
- `red` â€” proceeding would likely violate scope, authority, source truth, safety, canon, or user intent.
- `blocked` â€” the required context, authority, source access, or upstream work is unavailable.

## Buster modes

Use `internal_mode` when there is only one legitimate path forward. Resolve forced decisions privately and proceed, repair, or block without burdening the user with fake choices.

Use `interactive_mode` only when a lawful decision-maker must choose among real options, when the user explicitly asks to bust the item together, or when GPT cannot safely choose between multiple legitimate paths.

Use `blocked_mode` when missing authority, inaccessible evidence, absent source payloads, or unfinished upstream work prevents a safe green or repair.

## Core workflow

1. Name the exact action that would happen after the buster.
2. Name the risk domain being checked.
3. Identify hidden risk, unresolved decisions, unstable assumptions, contradictions, source gaps, authority gaps, or canon drift that could make the next action unsafe or false.
4. Classify the buster mode.
5. Resolve forced decisions internally when policy, source authority, current scope, or user instruction leaves only one legitimate route.
6. Surface only unresolved legitimate choices.
7. Return green only when the next action has a lawful route, required authority, sufficient evidence, and the correct output surface.

## Queue contract

Interactive buster queues are not neutral questionnaires. Each visible item should state:

- the risk;
- why it matters before the proposed action;
- what green requires;
- GPT's recommendation;
- the decision needed from the user or other lawful authority.

Default to short conversational queues. Use structured formats only when the destination requires copyable YAML, JSON, issue text, a worker packet, a schema, or another formal artifact.

## Output-surface boundary

A buster green approves only the next action it actually checked. It does not turn a report, queue item, validator result, receipt, tool log, assistant-authored summary, or session memory into stronger proof than it is.

If a downstream workflow requires a specific output surface, such as a PR, issue comment, source file, validation log, or artifact path, a buster cannot launder that requirement into another surface. If the wrong surface is used after green, the green is stale; stop and recover through the owning workflow.

## Relationship to specific busters

Specific busters own their domain checks. Use this framework for mechanics, but do not replace the domain buster with generic language.

Examples:

- `ambiguity-buster-v1` checks unresolved meaning, scope, referents, and choice ambiguity.
- `boring-buster-v1` checks whether work is small, dull, falsifiable, and implementation-ready.
- `invariant-buster-v1` checks binding constraints and non-negotiable rules.
- `analogy-buster-v1` checks whether an analogy clarifies or distorts.
- `canon-buster-v1` checks canon/source drift before durable truth claims or mutations.

## Boundaries

Do not use buster language to rename ordinary planning, execution, cleanup, or reporting workflows. Do not use a buster to create permission that the user, source surface, policy, project doctrine, or downstream skill has not granted. Do not import project-specific overlays into this generic framework.
