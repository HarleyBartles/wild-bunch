# Completing plans playbook

## When

A Wild Bunch plan, specification, roadmap, checkpoint, audit, or tracker reaches
completion, explicit abandonment, or successor-slice retirement.

## Required skills

- `/completing-planning-artifacts`
- `/cleanup-custody` when completion, abandonment, or promotion is ambiguous.

## Composition

1. Use `/completing-planning-artifacts` in the completing slice to promote
   durable content, mark the artifact `completed-awaiting-retirement`, and keep
   it tracked through that slice's merge.
2. In the next substantive slice, use the same skill's successor ingress lane
   before substantive edits. Add `/cleanup-custody` only when promotion or
   lifecycle state is ambiguous.
3. Remove eligible marked artifacts and live links as that successor slice's
   first commit, then run `py -3 tools/run.py ci --apply` to regenerate navigation.
4. Record explicit abandonment and promote durable content before removing an
   artifact that did not complete.

## Doctrine and contracts

- [Completed-artifact doctrine](../doctrine/completed-artifacts.md)
- [Artifact custody doctrine](../doctrine/artifact-custody.md)
- [Repository command declaration](../contracts/repo-standards-commands.json)

## Local commands and paths

- Active homes: `.agents/plans/`, `.agents/specs/`, `.agents/roadmaps/`
- Optional convenience copy: the repo-segregated completed-artifact directory
  beneath the scratch root resolved by host or repository policy.
- Regenerate: `py -3 tools\run.py ci --apply`
- Verify: `py -3 tools\run.py ci --check`

Plan creation is committed before execution. Completion marking remains in the
completing PR; removal and generated index changes arrive in the next
substantive successor PR.

## Evidence contract

- [ ] Completion proof covers the artifact's acceptance criteria.
- [ ] Durable decisions and rules exist at their current authority owners.
- [ ] The completing slice retains each `completed-awaiting-retirement` artifact.
- [ ] The successor slice removes only eligible marked artifacts and live links.
- [ ] The generated mesh is current.
- [ ] Git records both the artifact's creation and completed removal.

## Prohibited combinations

- Do not delete an active, ambiguous, or evidence-bearing artifact.
- Do not move completed artifacts into a tracked archive.
- Do not preserve workflow instructions merely because they appeared in a
  completed plan.

## Runbook routing

- [Pull request](../runbooks/pr.md)
