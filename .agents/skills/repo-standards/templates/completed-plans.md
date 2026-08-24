## Scope

`.agents/plans/completed/` and `.agents/specs/completed/`

## Purpose

The `completed/` folders hold historical records of finished work. They preserve context, sequencing, and the state of the repo at a point in time. They are not live pattern sources.

## Rule

Do not use completed plans or specs as:
- a source of canonical command sequences,
- a template for current implementation,
- or an authoritative example of repo conventions.

They may contain outdated tooling, stale links, or superseded patterns.

For current conventions, use:
- `.agents/doctrine/*.md`
- `.agents/runbooks/*.md`
- active plans and specs in `.agents/plans/` and `.agents/specs/`
- the `repo-standards` and `handoff-gates` skills

Completed plans and specs are still useful for understanding how the repo evolved, but any pattern they contain must be cross-checked against current doctrine before use.
