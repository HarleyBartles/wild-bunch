---
name: crew-buster
description: apply Crew thinking roles to a concrete plan, route choice, issue shape, skill update, failure-mode question, or durable-fix prompt. use when you need a pre-action Crew read on route, authority, fallback, or proof boundaries before the downstream owner is chosen. owns planning and route interrogation only: preserve the linear/codex default coding workflow, route gpt-native skillwork to the skill stack, and defer execution/proof to specialist skills.
metadata:
  source-id: crew-buster
  source-path: sources/first_party/skills/crew-buster/SKILL.md
  provenance-name: "MARK-9 chunk ledger \xC3\xA2\xE2\u201A\xAC\xE2\u20AC\x9D base and control plane"
license: "MIT"
---
# Crew Buster

## Purpose

Use `crew-buster` to apply the Crew to a specific plan, implementation topic, issue shape, route choice, failure-mode question, or durable-fix prompt before action.

This is an action skill. It returns a clearer plan, repaired route, missing-gate finding, named next owner, or specialist handoff. It does not execute work, mutate repos, write dispatches, close issues, package skills, or replace specialist busters.

## Composition

`crew` is the doctrine source for the six Crew thinking roles, proper-noun role names, TPS handoff discipline, and the golden-plan contract. The Crew are GPT thinking roles, not autonomous agents or workers.

Read `skills://crew/SKILL.md` when the role model, dependency chain, TPS handoff, or golden-plan proof materially matters. Do not copy Crew doctrine into this skill. Use this skill to apply that doctrine to the current task.

Read `references/output-modes.md` when deciding how much structure to show.

## Current workflow boundaries

Linear/Codex is the normal coding implementation route. Crew-buster may recommend or repair that route, but it does not own Linear issue mechanics, Codex worker state, PR-gate handling, or GitHub proof.

Before recommending implementation delegation, run the route question through the current golden gate:

  - `linear_codex_candidate`: repo-backed coding work that Codex Cloud can execute from an accessible repo environment. Next owner: `worker-dispatch-linear`.
  - `gpt_native_skillwork`: installed ChatGPT skill or doctrine work not proven repo-backed. Next owner: `skill-creator` -> `skill-validator` -> `skill-packager` -> `skill-handoff`.
  - `repo_backed_skill_source`: skill/plugin/source work in a known editable repo. Next owner: `worker-dispatch-linear` only after repo path, publication path, and validation route are known.
- `github_proof`: PR, commit, branch, status, review, or main-state question. Next owner: the repo/GitHub proof surface.
- `legacy_plan_b`: non-Linear worker packet only when Linear/Codex is unavailable, unsuitable, or explicitly rejected. Next owner: legacy dispatch skills.

A plan that sends GPT-native installed-skill work to Codex Cloud fails the route gate unless the editable source has first been proven repo-backed and Codex-accessible.

## Use posture

Use the lightest useful mode.

- Use light mode for ordinary side questions and introspective prompts. Answer in compact prose and surface only the Crew roles that found material signal.
- Use formal mode when the user explicitly asks to run crew-buster, asks for a structured Crew assessment, or when the plan has significant mutation, Linear/Codex, issue, skill, repo, artifact, or closeout consequences.

## Triggers

Typical triggers include:

- "what is your plan?"
- "why is that the plan?"
- "why did you choose that tool?"
- "how many issues?"
- "anything for GPT-side skill surface?"
- "what issues should cover this?"
- "what else would you add?"
- "how can we make it boring?"
- "what is the breakout plan?"
- "what is the durable fix?"
- "do you agree?"
- "what are your thoughts?"
- "are you sure?"

Do not invoke this skill for ordinary factual answers, routine writing, direct coding tasks, or normal project discussion unless the user is pressing on plan, route, authority, fallback, evidence, artifact shape, or durable-memory reasoning.

## Workflow

1. Classify the task and decide whether Crew reasoning is actually needed.
2. If the full dependency model matters, load `crew` and apply the roles in order: Index, Silk, Writ, Klause, Rollback, Receipt.
3. Treat role misses as active repair signals when the repair is deterministic, lawful, authorized, and local to the current lane.
4. Apply the current workflow boundary before naming a next owner: Linear/Codex for repo-backed coding, skill stack for GPT-native skillwork, GitHub Operations for GitHub proof, legacy dispatch only as Plan B.
5. Defer to specialist skills when they own the next decision.
6. Return the lightest useful result: boring route, repaired route, blocker, or next owner.

## Output-shape guard

Follow base artifact-shape law. In workspaces where YAML blocks are reserved for dispatches, session busters, or explicit user-requested YAML artifacts, do not use YAML for ordinary Crew assessments, buster summaries, plans, or status explanations. Use prose, a small markdown table, a JSON code block, or a text block when useful.

Do not use dispatch-shaped YAML unless the user explicitly asks for a worker dispatch and the legacy dispatch path has cleared. Crew-buster can recommend Linear issue shaping or legacy dispatch prep; it does not emit send-ready dispatch packets.

## Specialist boundaries

Defer to specialist skills instead of absorbing their work:

- Use `worker-dispatch-linear` for normal coding dispatch, Codex worker status, Linear issue handoff, PR-gate handling, and Linear/Codex state checks.
- Use `boring-buster` when the task is to judge one issue/proposal as boring, red, blocked, or ready.
- Use the current dispatch gate only when proving a non-Linear worker packet after Plan B is selected.
- Use `ambiguity-buster` for unresolved actor, scope, source, terminology, or success-condition ambiguity.
- Use `invariant-buster` for authority, source-of-truth, mutation, safety, validation, publication, or cleanup invariant pressure.
- Use the validation decision surface for validation class selection after code, PR, package, artifact, or validation evidence exists.
- Use `tps-reporting` for report partitioning.
- Use the repo/GitHub proof surface for GitHub evidence, PRs, commits, statuses, reviews, merges, and main verification.
- Use the GitHub issue-management surface only when GitHub Issues are explicitly requested or Linear/Codex is unavailable.
- Use `skill-creator`, `skill-validator`, `skill-packager`, and `skill-handoff` for GPT-native skill creation, validation, packaging, and handoff.

## Evidence posture for language patterns

When identifying user prompts as introspective triggers, grade the evidence: `observed_recurring`, `observed_current_session`, `inferred`, or `user_supplied`.

Recent-context matching is useful, but do not present it as durable recurring evidence unless it is actually recurring or user-supplied.

## Anti-loop and non-goals

Do not start broad skill-reading loops. Load another skill only when a named unresolved decision belongs to that skill and the project/workflow context matches. Stop once the route owner is identified.

Do not turn this skill into project lore. The Crew names are operational mnemonics only. Do not import project-specific domain law unless the active task is actually in that project and the project wrapper is the next owner.
