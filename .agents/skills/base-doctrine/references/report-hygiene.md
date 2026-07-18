# Report hygiene

Use this reference when drafting or reviewing report-like surfaces — partition reports, worker returns, Linear/Codex status notes, issue comments, verification summaries, publication notes, and continuity notes — so claims do not become truth.

## Core lesson

Reports are not truth. Reports are compressed custody surfaces.

The failure mode is report laundering: a smooth report turns a worker claim, Linear comment, PR body, validation note, package receipt, inference, or partial proof into durable truth. Future GPT, workers, or humans may then treat the report as source state. A good report preserves category boundaries so later readers know what was observed, what was claimed, what was inferred, what was verified, and what remains unresolved.

## Scope

Use for report drafts, worker returns, verifier summaries, publication notes, issue comments, receipt summaries, proof pointers, validation receipts, continuity notes, or closure summaries when reporting language could change the authority of information.

Do not use this reference to dispatch workers, poll Linear, verify GitHub, run validation, mutate source evidence, close issues, or update repositories. Use `linear-issue-shaping` for Linear/Codex state, `github-operations` for GitHub proof, and `verification-before-completion` for completion readiness.

Use project-specific reporting wrappers only when local actor/domain law matters. Wrong-project wrappers are noise.

## Required partitions

Keep these lanes separate when relevant:

- `linear_state` - issue status, delegate, Codex thread, comments, attachments, and task links visible in Linear.
- `codex_worker_claim` - what Codex or another worker says happened.
- `github_proof` - PR, branch, commit, status, review, merge, or main evidence visible in GitHub.
- `source_evidence` - observed files, refs, receipts, proof pointers, tool results, package evidence, or repo facts.
- `validation_evidence` - checks selected, checks claimed, checks observed, skipped checks, and blocker rationale.
- `issue_goal_conformance` - observable goal, falsification surfaces, checks run, claim-vs-observed comparison, and judgment.
- `publication_proof` - remote-visible publication, PR attachment, pushed heads, merged PR, package evidence, or exact proof artifact.
- `inference` - what GPT or a verifier concludes from evidence.
- `verifier_judgment` - GREEN, AMBER, RED, BLOCKED, closure-ready, or not-ready judgment.
- `next_action` - the single next gate or action.

## Linear/Codex coding report pattern

For ordinary coding workflow status, keep reports boring:

```md
Linear state:
- ...

Codex state:
- ...

GitHub state:
- ...

Next gate:
- ...
```

Use this mapping:

- Codex thread exists, no completion comment: `delegated/running`.
- Codex completion comment exists, no PR attachment or `Created pull request` comment: `returned/pr-gate`; next gate is for your human partner to open the Codex task link and click `Create PR`.
- PR attachment or `Created pull request` comment exists: `pr-created`; next gate is GitHub PR verification.
- PR merged and main verified: `landed`; next gate is issue closeout only when authorized.

Do not use this reporting pattern as a substitute for the Linear/Codex state machine. It only phrases the state after the control-plane route has found evidence.

## Full verification pattern

For issue-backed verification or closure posture, require:

- `issue_goal_as_observable_state`
- `surfaces_that_should_reflect_goal`
- `falsification_checks_run`
- `worker_claim_vs_observed_state`
- `publication_proof`
- `validation_evidence`
- `judgment`
- `next_action`

Do not let validation, clean status, remote-head equality, changed-file lists, package creation, PR existence, or issue comments substitute for issue-goal conformance.

## Publication language

Use cautious language unless proof is visible:

- `Codex says it created...` when only the worker report exists.
- `Linear shows a PR attachment...` when Linear contains the attachment or created-PR comment.
- `GitHub shows PR #N...` only after GitHub proof is fetched.
- `landed on main` only after merge/main proof exists.

For skill packages, publication requires exact package evidence and a valid assistant-message `skill.zip` handoff surface. A package path in logs or prose is not a handoff.

## False-GREEN risks

- Treating reports as source truth.
- Collapsing Linear state, worker claim, GitHub proof, inference, and judgment.
- Treating Codex completion as PR publication.
- Treating PR existence as issue-goal conformance.
- Treating validation selection as proof that validation ran.
- Treating validation success as closure without checking the goal.
- Writing publication language without remote-visible proof.
- Treating a continuity export as current repo or issue truth.
- Letting project-specific report law leak into GPT-wide reporting hygiene.
