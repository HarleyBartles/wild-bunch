# Pull request runbook

Use this runbook for pull-request workflow and publication proof in this repo.

## When

Opening, updating, or publishing a pull request in this repository.

## Required skills

- `publishing-source` - publication lifecycle and proof forms.
- `repo-worker-base` - worktree, branch, and validation boundaries.
- `requesting-code-review` - review dispatch.
- `receiving-code-review` - review-response handling.
- `verification-before-completion` - completion evidence.
- `completing-planning-artifacts` - completion marking and promotion boundary.

## Composition

Enter through `using-superpowers-plus` and follow its publication handoff. The routed stage skill reads this runbook for local binding. Draft lifecycle, commit discipline, review sequencing, and publication handoff belong to the routed portable skills; this runbook records only the consumer repository's commands, CI behavior, proof surface, and exceptions.

Before Ready, use the `completing-planning-artifacts` completing-slice lane: promote enduring content, mark governed artifacts `completed-awaiting-retirement`, retain them in the PR, and verify the published head contains them.

Draft is normally a commercial and CI posture, not evidence that implementation is unfinished. When the agent hands off a fully reviewable Draft, every agent-owned plan item is complete and human-owned Ready or merge actions must not remain unchecked. Keep the plan open only when the Draft is explicitly declared incomplete. Whoever later changes the PR state applies the repository's Ready preflight at that time.

## Doctrine and contracts

- Read root [`AGENTS.md`](../../AGENTS.md) `## Publication proof for repo work`.
- Read [`.devin/rules/tools.md`](../../.devin/rules/tools.md) for validation commands.

## Local commands and paths

- Base branch: <!-- name the repository base branch -->
- Local validation command: <!-- exact command -->
- Remote check command or surface: <!-- exact command or URL owner -->
- Draft-aware CI behavior: <!-- local workflow trigger/gate -->
- Publication proof surface: root `AGENTS.md`
- Exceptions: <!-- repository-specific exceptions, or none -->

## Evidence contract

A valid publication-proof form as declared by root `AGENTS.md` `## Publication proof for repo work`.

## Prohibited combinations

none

## Playbook routing

None by default.
