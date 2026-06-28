---
name: unslop-superpowers
description: Use when shaping Linear issues, GitHub proof, worker returns, or closeout
  language needs repo-specific anti-slop controls, profile discovery or refresh, concrete
  evidence requirements, or a narrow direct-to-main unslop profile update.
metadata:
  source-id: unslop-superpowers
  source-path: sources/first_party/skills/unslop-superpowers/SKILL.md
  provenance-name: Unslop Superpowers first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when shaping Linear issues, GitHub proof, worker returns, or closeout
    language needs repo-specific anti-slop controls, profile discovery or refresh,
    concrete evidence requirements, or a narrow direct-to-main unslop profile update.
  use_when:
  - Use when shaping Linear issues, GitHub proof, worker returns, or closeout language
    needs repo-specific anti-slop controls, profile discovery or refresh, concrete
    evidence requirements, or a narrow direct-to-main unslop profile update.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---
# Unslop Superpowers

Use this skill when workflow shaping or review work needs a repo-specific anti-slop guard rather than a generic style pass.

## Core job

Turn the active repo's unslop profile into concrete controls for:

1. Linear issue shaping;
2. GitHub PR, review, and proof checks;
3. worker-return and closeout review;
4. evidence requirements and non-goals.

## Composition

Start with `@using-superpowers` as the workflow-selection entrypoint.

Use `@connector-safety` before any GPT connector write or blocked-write
recovery, especially direct-to-main profile custody writes.

Use `@unslop` when profile creation or refresh is the smallest necessary next step.

If a repo unslop profile already exists, apply it to the current work packet. If the profile is stale, weak, contradicted by current repo outputs, or missing relevant failure modes, refresh it. If no profile exists and local write access is lawful, create the smallest useful profile. If mutation is unavailable or unsafe, report the exact profile gap instead of pretending the guard ran.

Nesting rule:

- pick the smallest safe action;
- do not run `@unslop` as a blanket style lint;
- do not stack extra skills just because they exist.

## Anti-slop controls

- Reject generic goals such as `improve`, `clean up`, `make robust`, or `refactor` when no source seam, validation, or evidence is named.
- Prefer vertical slices of provable value over broad horizontal work.
- Treat chat memory, worker summaries, and unverified claims as untrusted until backed by repo evidence.
- Reject validation laundering where intended checks replace actual command output.
- Require concrete files, commands, generated artifacts, and proof paths when shaping or reviewing repo work.
- Flag PRs and worker returns that summarize intent without proving issue-goal conformance.
- Guard against GPT-overlay or generated-output drift contaminating canonical source.

## Profile custody

If the task is specifically to create, refresh, or repair the repo's unslop profile, a worker may commit that profile update directly to `main` without raising a PR solely for that artifact. That escape hatch applies only to the profile artifact and any minimal manifest or provenance file it requires.

For GPT connector use, the same narrow escape hatch applies only when the user explicitly authorizes the write and the connector can safely perform `discover -> read -> write -> verify` under `@connector-safety`. The GPT path must not widen the mutation beyond the profile artifact and any minimal manifest or provenance file it requires.

Even then, the worker must verify current `main`, inspect the current profile or prove absence, update the smallest relevant file set, base the change on concrete repo evidence, run relevant validation, and return commit SHA plus readback proof.

## Authority split

This skill shapes anti-slop controls and profile-aware workflow guardrails.

It does not replace `@unslop`, does not prove GitHub or source state by itself, and does not substitute for tests, validation output, or issue closeout proof.
