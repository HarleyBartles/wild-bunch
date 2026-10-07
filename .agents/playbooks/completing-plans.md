# Completing plans playbook

## When

A Wild Bunch plan, specification, roadmap, checkpoint, audit, or tracker reaches
completion, explicit abandonment, or successor-slice retirement.

## Required capabilities

- Decide when plans and specs are complete and ready for retirement or promotion.

## Optional capabilities

- None.

## Unslop before work

Before promoting or retiring authored artifacts, read [writing guards](../unslop/writing.md) in full; changes to agent guidance also require [routing](../unslop/routing.md). Preserve useful [observations](../unslop/observations.md) when retiring a guard; artifact age or a completed checklist does not erase its evidence.

## Composition

1. At a successor slice, inspect prior plans, specifications, roadmaps, and
   checkpoints alongside their code and delivery evidence. Classify the whole
   scope as shipped, still live, or explicitly abandoned.
2. Promote durable decisions and operating rules to their current owners.
   Retain future or ambiguous artifacts; a marker, unchecked step, or merged
   PR prompts assessment but cannot determine completion by itself.
3. Remove eligible artifacts and stale links in the successor's first
   substantive commit. A status marker can help humans but is neither
   required nor sufficient for classification.
4. Record explicit abandonment and promote durable content before removing an
   artifact whose scope did not ship.

## Doctrine and contracts

- [Completed-artifact doctrine](../doctrine/completed-artifacts.md)
- [Artifact custody doctrine](../doctrine/artifact-custody.md)
- [Repository command declaration](../contracts/repo-standards-commands.json)

## Local commands and paths

- Active homes: `.agents/plans/`, `.agents/specs/`, `.agents/roadmaps/`
- Optional convenience copy: the repo-segregated completed-artifact directory
  beneath the scratch root resolved by host or repository policy.
- Verify: `py -3 tools\run.py ci --check`; the normal hook checks the staged
  candidate without applying changes or staging corrections. Run `ci --apply`
  explicitly before staging when repository-owned metadata needs refreshing.

Plan creation is committed before execution. Retain the current plan and spec
through their completing pull request. The next substantive slice assesses
them semantically and retires only work shown to be complete or abandoned.

## Evidence contract

- [ ] Completion proof covers the artifact's acceptance criteria.
- [ ] Durable decisions and rules exist at their current authority owners.
- [ ] The successor classification covers the full artifact scope and current delivery evidence.
- [ ] Durable content is promoted before eligible artifacts and stale links are removed.
- [ ] The selected standards are current.
- [ ] Git records both the artifact's creation and completed removal.

## Prohibited combinations

- Do not delete an active, ambiguous, or evidence-bearing artifact.
- Do not move completed artifacts into a tracked archive.
- Do not preserve workflow instructions merely because they appeared in a
  completed plan.

## Runbook routing

- [Pull request](../runbooks/pr.md)
