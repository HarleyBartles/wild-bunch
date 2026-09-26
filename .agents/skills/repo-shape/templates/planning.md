# Planning runbook

Use this runbook for repository planning and successor-slice ingress.

## When

Starting a substantive repository slice or converting approved requirements into an executable plan.

## Required skills

- `repo-worker-base` - fresh-base and isolated-worktree hygiene.
- `completing-planning-artifacts` - successor-slice retirement of completion-marked artifacts.
- `writing-plans` - executable plan authoring.
- `handoff-gates` - plan-readiness evidence.

## Composition

After refreshing `main` and creating the slice branch/worktree, run the `completing-planning-artifacts` successor-slice ingress lane before substantive edits. Then write and commit the new in-flight plan.

## Doctrine and contracts

- Read the consumer's completed-artifacts doctrine and repository command contract.

## Local commands and paths

- In-flight planning homes: `.agents/plans/`, `.agents/specs/`, and `.agents/roadmaps/`.
- Name the consumer's index/mesh regeneration command here.

## Evidence contract

Eligible predecessor artifacts are retired in the first commit of this eventual PR, and the new committed plan identifies exact validation and publication proof.

## Prohibited combinations

Do not create a cleanup-only PR or call an in-flight planning artifact durable repository truth.

## Playbook routing

None by default.
