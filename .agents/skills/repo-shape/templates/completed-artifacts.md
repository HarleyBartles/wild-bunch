## Scope

Completed planning-artifact custody truth for this repository.

## Doctrine

Completed plans, specifications, roadmaps, checkpoints, and similar execution artifacts remain tracked through their completing PR so squash-merged `main` records them, then leave the tracked repository in the first commit of the next substantive slice. Git history is the immutable record. A completed artifact is not an authority: do not use it as a source of canonical command sequences, a template for current implementation, or an authoritative example of repo conventions. Completion does not create a durable exception for an artifact type.

Mark the artifact `completed-awaiting-retirement` in the completing slice. When it leaves the tracked tree in the successor slice, an optional disposable convenience copy may live at `<main-checkout>/../_agent-scratch/<repo-name>/completed/<artifact-type>/` with no manifest, retention promise, or evidentiary role.

Every governed artifact ends through an explicit repository-owned decision: either it is marked `completed-awaiting-retirement`, or it is abandoned with a recorded reason. Abandonment is not inferred from scratch output, a rejected candidate, inactivity, or deletion. Promote any durable content before removing an abandoned artifact.

Durable content promotes before removal: enduring architecture decisions belong in the repository's declared ADR home; operating rules belong in `.agents/doctrine/`, `.agents/runbooks/`, or `.agents/playbooks/`. The portable completion skill owns the lifecycle; the repository's planning and PR runbooks name the concrete destinations and commands.

## Ownership

`completing-planning-artifacts` owns the normal two-slice lifecycle. `cleanup-custody` owns ambiguous custody classification and promotion-before-removal decisions. The repository's planning and PR runbooks bind those portable owners to local paths and commands. For current conventions, use `.agents/doctrine/*.md`, `.agents/runbooks/*.md`, `.agents/playbooks/*.md`, and active plans and specs.
