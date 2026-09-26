## Scope

Completed planning-artifact custody truth for this repository.

## Doctrine

Completed plans, specifications, roadmaps, checkpoints, and similar execution
artifacts are not retained in the tracked repository. Git history is the
immutable record. A completed artifact is not an authority: do not use it as
a source of canonical command sequences, a template for current
implementation, or an authoritative example of repo conventions. Completion
does not create a durable exception for an artifact type.

A completing slice marks every governed artifact exactly
`completed-awaiting-retirement` and retains it through that slice's merge.
The next substantive successor slice removes those marked artifacts as its
first commit after verifying that durable content was promoted. An artifact
may instead be abandoned only through an explicit recorded decision; absence,
inactivity, scratch output, or deletion does not establish abandonment.

When a completed artifact leaves the tracked tree, an optional disposable
convenience copy may live at
`<main-checkout>/../_agent-scratch/<repo-name>/completed/<artifact-type>/`
with no manifest, retention promise, or evidentiary role.

Durable content promotes before removal: enduring architecture decisions
belong in the repository's declared ADR home; operating rules belong in
`.agents/doctrine/` or `.agents/runbooks/`. The completion runbook names the
concrete destinations.

## Ownership

`completing-planning-artifacts` owns the two-slice completion and retirement
lifecycle. `cleanup-custody` owns ambiguous custody classification and
promotion-before-removal judgment. The `completing-plans.md` playbook binds
those capabilities to Wild Bunch paths and evidence. For current conventions,
use `.agents/doctrine/*.md`, `.agents/runbooks/*.md`,
`.agents/playbooks/*.md`, and active plans and specs.
