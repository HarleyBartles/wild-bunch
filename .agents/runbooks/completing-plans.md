# Completion runbook

## When

An active Wild Bunch plan, specification, roadmap, checkpoint, audit, or tracker
has completed its work and needs removal from the live tree.

## Required skills

- `/verification-before-completion`
- `/cleanup-custody`
- `/publishing-source`

## Composition

1. Use `/verification-before-completion` to prove the artifact's work is complete.
2. Use `/cleanup-custody` to classify each enduring decision or rule and promote
   it to its doctrine, contract, ADR, or runbook owner before removal.
3. Remove the completed artifact and live links, then run
   `py -3 tools/run.py ci --apply` to regenerate navigation.
4. Use `/publishing-source` to carry the deletion and generated changes through
   the normal hooked commit and PR lifecycle.

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

- [ ] Completion proof covers the artifact's acceptance criteria.
- [ ] Durable decisions and rules exist at their current authority owners.
- [ ] The completed artifact and every live link to it are absent.
- [ ] The generated mesh is current.
- [ ] Git records both the artifact's creation and completed removal.

## Prohibited combinations

- Do not delete an active, ambiguous, or evidence-bearing artifact.
- Do not move completed artifacts into a tracked archive.
- Do not preserve workflow instructions merely because they appeared in a
  completed plan.
