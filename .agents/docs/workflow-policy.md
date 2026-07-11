# Workflow Policy

Use this reference when managing git workflow, claiming completion, publishing PRs, or verifying issue-goal alignment.

## Branch + PR Workflow
- Workers branch from current `main`.
- Workers push a branch and **open or return a PR as draft** while work is in progress.
- A PR is not marked ready for review until work is complete, the branch is current with `origin/main`, and the local CI preflight (`.\scripts\ci-preflight.ps1`) passes.
- The PR is the normal publication surface.
- Direct pushes to `main` require explicit latest-turn authorization.
- `GREEN` means PR-ready with validation and evidence, not direct-main landing.
- Merge and landing verification are separate GPT or human steps after PR review and merge.

## Draft PR CI gating
- CI runs on `push` to `main` and on non-draft `pull_request` events.
- CI does not run on draft PRs.
- When a draft PR is marked ready for review, CI is triggered as the final gate.
- Use the local CI preflight (`.\scripts\ci-preflight.ps1`) to catch failures before moving a PR out of draft.

## GREEN Checklist

Before claiming work is complete or requesting review, verify:

- [ ] Work pushed to branch
- [ ] PR raised as draft
- [ ] Branch is current with `origin/main`
- [ ] Local CI preflight passed (`.\scripts\ci-preflight.ps1`)
- [ ] PR is marked ready for review
- [ ] PR body fresh (matches actual implementation, not stale plan)
- [ ] Linear issue fresh (updated with current status if applicable)
- [ ] CI passing (all relevant checks green)
- [ ] Index mesh regenerated (if file structure changed)
- [ ] Plan committed with all checkboxes checked (if implementation plan exists)

## Source of Truth
- Current repo state is the source of truth.
- Worker reports, issue comments, conversation summaries, and session notes are not proof.
- Always report exact branch, head commit, remote head, PR URL, and changed files.

## Issue-Goal Conformance
- Restate the task as observable repo state.
- Run falsification checks.
- Compare claim vs observed state.
- Do not close or claim closure unless explicitly asked and fully proven.

## GREEN Standard
- `GREEN` requires implementation, validation, a clean worktree, branch head proof, PR publication, issue-goal conformance, and complete worker-owned cleanup proof when validation touched local workspace resources.
- Passing tests alone is not `GREEN`.
- A commit existing is not `GREEN`.
- If the worker started long-running helpers for validation or browser checks, `GREEN` also requires stopping or explicitly accounting for those worker-owned processes and browser sessions before return.
- If validation touched `C:/WORK/**`, `GREEN` requires a post-cleanup proof block that accounts for worker-owned helpers, used ports, and repo/file-lock risk before the return. Missing or partial cleanup proof is `AMBER` or `BLOCKED`, not `GREEN`.

## ADR Log Freshness
- The ADR log at `docs/adr/` is the durable record of architecture and gameplay decisions. It must represent the system as it exists today, not as it existed when each ADR was written.
- **If you read the ADR log, you check the whole log for freshness.** Reading any ADR creates a responsibility to verify that the rest of the log is not stale against current source. If you find a stale ADR, update it or mark it `superseded` and create its replacement in the same pass.
- `docs/adr/INDEX.md` carries a per-ADR "Last checked" freshness table. When you complete a freshness check, update the timestamp for each ADR you verified so the next worker can infer which files are likely fresh and which may need re-checking. A file with a stale timestamp (weeks old, or older than the last merge to `main`) should be re-read before trusting it.
- Staleness means: the ADR describes behavior, identifiers, fields, or mechanics that no longer match current source. Historical status entries (dated `live` entries that record what happened at a point in time) are not stale — they are the audit trail. The current `Status` line and `Decision` section must match the system today.
- When you change behavior that an ADR documents, update that ADR in the same PR. Do not leave the ADR log behind the code.
