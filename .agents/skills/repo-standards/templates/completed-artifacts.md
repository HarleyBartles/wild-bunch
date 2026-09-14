## Scope

Completed planning-artifact custody truth for this repository.

## Doctrine

Completed plans, specifications, roadmaps, checkpoints, and similar execution
artifacts are not retained in the tracked repository. Git history is the
immutable record. A completed artifact is not an authority: do not use it as
a source of canonical command sequences, a template for current
implementation, or an authoritative example of repo conventions. Completion
does not create a durable exception for an artifact type.

When a completed artifact leaves the tracked tree, an optional disposable
convenience copy may live at
`<main-checkout>/../_agent-scratch/<repo-name>/completed/<artifact-type>/`
with no manifest, retention promise, or evidentiary role.

Durable content promotes before removal: enduring architecture decisions
belong in the repository's declared ADR home; operating rules belong in
`.agents/doctrine/` or `.agents/runbooks/`. The completion runbook names the
concrete destinations.

## Ownership

`cleanup-custody` owns the custody classification and the
promotion-before-removal method. The `completing-plans.md` runbook (or the
repo's mapped completion runbook) owns the composition that applies this
doctrine. For current conventions, use `.agents/doctrine/*.md`,
`.agents/runbooks/*.md`, and active plans and specs.
