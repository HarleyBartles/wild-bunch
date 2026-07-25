---
name: work-mode-router
description: Use when a project session starts, resumes, or may involve repo/source
  work.
metadata:
  source-id: work-mode-router
  source-path: sources/first_party/skills/work-mode-router/SKILL.md
  provenance-name: Work Mode Router first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Cross-runtime bootstrap router for request classification and worker handoff.
  use_when:
  - Use when a new project session starts and may involve repo or source work
  - Use when a session resumes with continuity ingress or an inherited worktree
  - Use when the request may involve repo/source evidence, workers, or issues
  - Use when the request may involve artifacts, verification, or mutation
  - Use when the request may involve publication or skill/package work
  - Use when a worker start needs route-state classification before implementation
  - Use when the human partner says to start with /work-mode-router
  do_not_use_when:
  - Do not use when another more specific skill owns the task
  - Do not use after classification has already occurred in the same turn
  - Do not use for repo hygiene, source inspection, or connector work
  - Do not use for executing project work
  - Do not use as a doctrine store
  - Do not use as a replacement for repo-worker-base or using-superpowers
  - Do not use as a replacement for project doctrine skills
  related_skills:
  - using-superpowers
  - repo-worker-base
  - using-git-worktrees
  - inspecting-the-environment
  - base-doctrine
  use_before:
  - repo-worker-base
license: MIT
---
# Work Mode Router

Use this skill to classify the first request in a project-scoped or workflow-sensitive session and hand it to the smallest controlling skill surface.

This skill does not execute project work, inspect source, or store doctrine. It routes repo-backed work through `repo-worker-base` before any baseline, local `.agents/guides/` guide, or Superpowers lane.

## When to Use

- A new project session starts and may involve repo or source work.
- A session resumes with continuity ingress or an inherited worktree.
- The request may involve repo/source evidence, workers, issues, artifacts, verification, mutation, publication, or skill/package work.
- A worker start needs route-state classification before implementation.
- The human partner says to start with `/work-mode-router`.

Do not use when a more specific skill owns the task, after classification has already occurred, or for repo hygiene/source inspection.

## Core Pattern

1. Classify the request into the smallest sufficient mode.
2. For repo-backed work, classify durable route state from Linear/repo evidence.
3. Hand off to `repo-worker-base` + matching baseline + local guide, then `/using-superpowers` for the lane.
4. Stop at sign-off gates.
5. Do not invoke this router recursively after classification.

## Common Mistakes

- Loading extra skills after the controlling skill has been read.
- Bypassing the `repo-worker-base` + baseline + local guide handoff.
- Treating this skill as a doctrine store or execution lane.

## Boundaries

Do not mutate repos, post comments, build artifacts, delegate Codex, or close issues from this skill. Use the skill that owns the task.

For detailed guidance, load the matching `references/` file: `core-posture.md`, `route-states.md`, `workflow-phases.md`, `output-and-shape-guards.md`, or `source-and-evidence-posture.md`.
