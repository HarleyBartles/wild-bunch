## Scope

Completed planning-artifact custody.

## Purpose

Completed plans, specifications, roadmaps, checkpoints, and similar execution
artifacts are not retained in the tracked repository. Remove them from Git when
their work is complete. A convenience copy may live in the central disposable
scratch store at `<main-checkout>/../_agent-scratch/<repo-name>/completed/`,
split into `plans/`, `specs/`, `roadmaps/`, `checkpoints/`, or another accurate
artifact-type folder. It has no manifest, retention promise, or evidentiary
role. Git history is the immutable record.

## Rule

Do not use a completed artifact as:
- a source of canonical command sequences,
- a template for current implementation,
- or an authoritative example of repo conventions.

They may contain outdated tooling, stale links, or superseded patterns.

For current conventions, use:
- `.agents/doctrine/*.md`
- `.agents/runbooks/*.md`
- active plans and specs in `.agents/plans/` and `.agents/specs/`
- the `repo-standards` and `handoff-gates` skills

Before removal, promote enduring architecture decisions into ADRs and operating
rules into current doctrine or runbooks. Completion does not create a durable
exception for an artifact type.
