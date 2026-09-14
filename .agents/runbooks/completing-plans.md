# Completion runbook

## When

An active Wild Bunch plan, specification, roadmap, checkpoint, audit, or tracker
has completed its work and needs removal from the live tree.

## Required skills

- `/verification-before-completion`
- `/cleanup-custody`
- `/publishing-source`

## Composition

Verify the work first. Use `/cleanup-custody` to classify the artifact and
promote durable content. Remove the completed artifact and stale links, apply
the generated mesh, then let `/publishing-source` carry the deletion in the
normal commit and PR lifecycle.

## Doctrine and contracts

- [Completed-artifact doctrine](../doctrine/completed-artifacts.md)
- [Artifact custody doctrine](../doctrine/artifact-custody.md)
- [Repository command declaration](../contracts/repo-standards-commands.json)

## Local commands and paths

- Active homes: `.agents/plans/`, `.agents/specs/`, `.agents/roadmaps/`
- Optional convenience copy: `Z:\_agent-scratch\wild-bunch\completed\<artifact-type>\`
- Regenerate: `py -3 tools\run.py ci --apply`
- Verify: `py -3 tools\run.py ci --check`

Plan creation is committed before execution. Its completed removal and any
generated index changes are committed afterward.

## Evidence contract

The work is proven complete, durable content exists at its current owner, the
artifact and all live links are absent, the mesh is current, and Git records
both creation and removal.

## Prohibited combinations

- Do not delete an active, ambiguous, or evidence-bearing artifact.
- Do not move completed artifacts into a tracked archive.
- Do not preserve workflow instructions merely because they appeared in a
  completed plan.
