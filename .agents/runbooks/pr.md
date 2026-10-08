# Publication proof and PR instructions

## When

Opening, updating, or publishing a Wild Bunch pull request.

## Required capabilities

- Verify repository state, validation evidence, and publication prerequisites.
- Publish a branch and open or update a Draft PR, then verify its head and checks.

## Before publication

Follow the [unslop playbook](../playbooks/unslop.md) and its scoped profile selection for the actual diff. Consult the [decision-record playbook](../playbooks/decision-records.md) when the diff makes, changes, corrects, or materially removes a durable decision.

## Decision-record check

Before requesting review, compare the actual proposed diff with applicable
records through the [decision-record playbook](../playbooks/decision-records.md).
Include required ADR creation, correction, or supersession, or state why no
durable decision changes. The reviewer must independently repeat this check
against the diff; the author's statement or the presence of an ADR edit is not
proof that the log remains true.

## Composition

Use `develop` as the repository's default base for ordinary development PRs. `main` is the release line; target it only when the approved release or hotfix flow requires it. Follow the active roadmap or execution plan for campaign-specific sequencing and version requirements; do not duplicate those temporary rules in this durable publication procedure.

1. Use repository branch, source, validation, and publication guidance to confirm dedicated-worktree, branch, clean-tree,
   and source-custody state.
2. Resolve the author decision-record check above before requesting review.
3. Use evidence-based result verification to bind the publication claim to the
   committed head and canonical validation result.
4. Use GitHub branch and Draft PR publication to push the task branch and create or update the
   Draft PR against the selected base branch (`develop` by default).
5. Read back the GitHub PR and reconcile its head SHA, body, Draft state, and
   applicable hosted checks with the published tree.

## Doctrine and contracts

- This runbook is the publication-proof surface routed from root
  [AGENTS.md](../../AGENTS.md).
- [Repository command declaration](../contracts/repo-standards-commands.json)
  defines the canonical apply and check capabilities; pre-commit and hosted
  validation invoke only check.

## Local commands and paths

- Default base branch: `develop`
- Default PR state: Draft
- Hook setup: `py -3 tools/run.py setup-hooks --apply`
- Check: `py -3 tools/run.py ci --check`
- Manual diagnostics: `py -3 tools/run.py ci --check --diagnostics` (not the commit/CI gate; may continue after failures)
- Run the named owning maintenance command before staging when a particular
  maintained artifact needs updating; review and stage those changes deliberately.
- The commit hook validates the staged candidate without mutating or staging it.
- Pull-request jobs run when the PR is not Draft.
- Direct pushes to `main` require explicit authorization.

## Evidence contract

- [ ] The local tree is clean and the branch is published from a dedicated worktree.
- [ ] The GitHub PR targets the selected base branch (`develop` by default) and its remote head equals local `HEAD`.
- [ ] The PR body describes current scope and validation evidence.
- [ ] Draft state and hosted-check expectations match the repo policy.

## Prohibited combinations

- Do not treat a local branch or push without a PR as publication proof.

## Playbook routing

- [Completing plans](../playbooks/completing-plans.md) - before handoff when this slice's plans, specifications, roadmaps, checkpoints, audits, or trackers have completed.
- [Testing](../playbooks/testing.md) - when establishing the committed validation evidence used by the publication claim.
