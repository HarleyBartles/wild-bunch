---
name: github-operations
description: Use when verify GitHub repository evidence without taking over coding
  workflow routing. Use after a Linear/Codex task has a GitHub PR, branch, commit,
  review, merge, status, or file-state question; when checking publication proof,
  PR diff scope, mergeability, CI/status evidence, final main state, or GitHub-specific
  closure proof. Do not use as the default issue/dispatch controller for coding work
  when Linear/Codex is available; route worker state and issue planning through the
  Linear/Codex control plane first.
metadata:
  source-id: github-operations
  source-path: sources/first_party/skills/github-operations/SKILL.md
  provenance-name: Github Operations first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when verify GitHub repository evidence without taking over coding workflow
    routing. Use after a Linear/Codex task has a GitHub PR, branch, commit, review,
    merge, status, or file-state question; when checking publication proof, PR diff
    scope, mergeability, CI/status evidence, final main state, or GitHub-specific
    closure proof. Do not use as the default issue/dispatch controller for coding
    work when Linear/Codex is available; route worker state and issue planning through
    the Linear/Codex control plane first.
  use_when:
  - Use when verify GitHub repository evidence without taking over coding workflow
    routing. Use after a Linear/Codex task has a GitHub PR, branch, commit, review,
    merge, status, or file-state question; when checking publication proof, PR diff
    scope, mergeability, CI/status evidence, final main state, or GitHub-specific
    closure proof. Do not use as the default issue/dispatch controller for coding
    work when Linear/Codex is available; route worker state and issue planning through
    the Linear/Codex control plane first.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---

# GitHub Operations

Use this skill to verify what GitHub and Git evidence prove before accepting repo, PR, commit, publication, merge, or closure claims.

This is a GPT-wide proof skill. It owns GitHub evidence discipline. It does not own coding dispatch, Linear issue planning, Codex worker routing, project-domain doctrine, or GPT-native skill maintenance.

## Default coding workflow boundary

For coding work, the normal control plane is Linear/Codex:

1. Linear issue is the task contract.
2. Codex executes through the delegated issue when the golden gate says the task is Codex-executable.
3. Linear comments and attachments are the worker event log.
4. The human publication gate is the Codex UI `Create PR` action.
5. GitHub becomes authoritative once a PR, branch, commit, status, review, or merge exists.

Do not load or continue this skill merely to check whether a Codex worker has returned. Check Linear issue comments and attachments first. Use this skill only after a GitHub artifact exists, or when the latest user asks a GitHub-specific verification question.

## Golden gate composition

If a task is still being shaped, delegated, or routed, use the Linear/Codex dispatch doctrine before this skill.

This skill starts after the gate has produced one of these GitHub-facing questions:

- Is the PR present and what does it change?
- Is the branch/commit published?
- Do statuses, checks, reviews, or comments block merge?
- Did the PR land on the expected base?
- Does main now reflect the accepted work?
- Does observable GitHub state satisfy the issue goal?

If there is no GitHub artifact yet and the Linear issue has a Codex completion comment but no PR attachment/comment, the next action is the human PR gate: tell your human partner to open the Codex task link from Linear and click `Create PR`. Do not look for shell Git credentials or recommend PATs as the default workaround.

## Core lesson

A repo return is not true because it is well reported.

Worker reports, changed-file lists, validation summaries, clean-tree claims, commit messages, local paths, issue comments, and Linear comments can all be useful inputs. None of them prove completion by themselves. Verification requires separating what was claimed from what observable repository, publication, and validation evidence actually show.

False GREEN often comes from accepting a narrow true fact, such as `a commit exists`, as if it proved the real issue goal. This skill exists to prevent that laundering.

## Reference load triggers

Keep `SKILL.md` as the verification control plane. Load references only when their topic is actually in scope.

- Load `references/source-route-posture.md` when verification depends on source-route coverage, connector availability, repository-search breadth, exact repository reads, local/disk evidence, or a claim that a source route is unavailable.
- Load `references/pr-review-writes.md` only when the latest user request explicitly asks to create, submit, or repair a native GitHub pull request review with inline review comments, review comment arrays, file/line comments, or same-token agent review comments.

Do not load source-route guidance for ordinary chat, Linear worker-state checks, or verification that can be answered from already-visible PR/commit evidence. Connector, file, source, or tool presence is not itself a task signal.

## Verification skill-read stop rule

For PR verification, commit checks, issue-goal conformance, or closure-readiness reporting, this skill plus any already-read validation/reporting skill is usually sufficient. Do not load more skills merely to feel safer.

Load an additional skill only when a named unresolved decision remains outside this skill's ownership, and the candidate skill directly owns that decision. Do not load continuity-ingress, dispatch-packet, artifact, visual, or wrong-project wrapper skills during ordinary GitHub verification unless the current user request actually requires that route.

When a project wrapper exists, use only the wrapper for the active project and only for local domain/protected-surface law. Do not load unrelated project wrappers with similar reporting or GitHub names.

If the user asks to stop skill reading, stop immediately and finish verification from the evidence and skills already loaded unless a safety blocker remains.

## Native PR review write boundary

This skill may own a narrow GitHub write when the latest user request explicitly asks to submit a native PR review with inline comments or review-comment arrays. In that case, read `references/pr-review-writes.md`, keep the connector action narrow, and submit a review rather than falling back to a generic PR timeline comment.

Do not use this exception during ordinary PR verification, mergeability checks, status checks, closure judgments, or feedback interpretation. Without explicit current-turn authorization to write a PR review, keep GitHub tooling read-only.

## Simple issue-mutation boundary

This skill is for verification. It is not the default route for creating, updating, classifying, or closing issues in coding work.

Do not invoke or continue reading GitHub Operations for a simple known-target comment, label, assignment, or non-closure state change unless the user asks for verification or the operation depends on current repo evidence. If another skill has already classified the task as a known-target mutation, return to that mutation path.

Do not let verification doctrine intercept ordinary instructions such as `add this note to Linear issue HAR-257`. If no GitHub verification judgment is being made, do not load this skill's references, do not inspect commits or files, and do not reread this `SKILL.md`.

## GitHub connector mutation-tool trap

During verification, reassessment, source inspection, issue-goal conformance checks, or any read-only repo task, keep the connector route read-only. Do not call GitHub write tools such as `create_tree`, `create_commit`, `create_file`, `update_file`, `delete_file`, `update_issue`, `add_comment_to_issue`, `update_ref`, or other create/update/delete/add/remove routes unless the latest user request explicitly authorizes that mutation as the task.

Do not search for a `tree` or `repo tree` tool and then call `create_tree`. `create_tree` is a low-level Git mutation primitive for constructing commit trees, not a read/list repository structure helper. If a read/list-tree route is unavailable, say the connector does not expose that route and use targeted read-only routes instead: `fetch_file`, `fetch`, `search`, `compare_commits`, `fetch_commit`, `get_commit_combined_status`, `fetch_pr`, `fetch_pr_patch`, `get_pr_diff`, and issue/comment fetch routes when explicitly needed.

Before any GitHub tool call in a verification or reassessment turn, classify the intended tool as `read_only` or `mutation`. If it is `mutation` and the latest user message did not explicitly authorize that exact mutation, do not call it.

## Evidence partitions

Keep these lanes separate:

- `linear_evidence`: issue body, delegate, Codex thread/comment state, PR attachments, and created-PR comments when Linear is the task surface.
- `worker_claim`: what the worker or report says happened.
- `github_evidence`: PR, commit, branch, file, compare, status, review, merge, and main state visible through GitHub.
- `local_claim`: reported local worktree, local validation, local path, or unpublished file state.
- `validation_evidence`: checks selected, checks reportedly run, outputs visible to GPT, skipped checks, and blocker criteria.
- `publication_proof`: GitHub-visible PRs, pushed remote heads, merge state, artifact publication, or explicit no-op classification.
- `issue_goal_conformance`: whether observable Linear/GitHub/repo state satisfies the actual issue goal.
- `verifier_judgment`: GREEN, AMBER, RED, or BLOCKED based on the lanes above.

Do not move information into a stronger lane than its source supports.

## Verification workflow

For Linear/Codex-backed coding work with a PR:

1. Identify the Linear issue, repository, PR number, base branch, and head branch/commit.
2. Fetch current PR state, including title/body, base/head, mergeability, draft state, comments/reviews when relevant, and diff/patch when scope matters.
3. Restate the Linear issue goal as observable repo state.
4. Name the repo surfaces that should reflect that goal.
5. Run falsification checks against the PR diff and any relevant files/statuses.
6. Compare worker/PR claims to observed GitHub state.
7. Judge GREEN, AMBER, RED, or BLOCKED.

For non-Linear GitHub-backed work, follow the same evidence logic but do not invent a Linear surface.

GREEN requires enough evidence to satisfy the goal and no known falsification. AMBER means a plausible path exists but evidence, scope, or source coverage is incomplete. RED means observed evidence contradicts the claim or the wrong goal was satisfied. BLOCKED means required access, source state, or authority is missing.

## Issue-goal conformance

Do not call work closure-ready from any single proxy:

- worker says GREEN;
- Linear has a PR attachment;
- GitHub PR exists;
- commit exists;
- changed files look plausible;
- validation passed;
- remote head moved;
- issue comment says done;
- clean worktree is reported;
- no obvious error appears.

First ask: what observable state was the issue supposed to create, change, remove, preserve, or prove? Then inspect the surfaces that would disprove it.

Use or require these fields when reporting issue-backed verification:

- `issue_goal_as_observable_state`
- `repo_surfaces_that_should_reflect_goal`
- `falsification_checks_run`
- `worker_claim_vs_observed_state`
- `judgment`

## Publication proof

For tracked repo mutation, distinguish:

- worker local implementation;
- Codex task completion;
- human PR gate readiness;
- GitHub-visible PR/branch/commit;
- merged or final remote branch state;
- final main state after landing;
- artifact publication, if any.

Local implementation alone is not publication. A Codex completion comment alone is not publication. A GitHub PR is a review/publication artifact, not final main truth. A commit hash alone is not enough if the task requires pushed remote state, merge proof, artifact publication, or issue-goal conformance.

## Validation evidence

Validation supports a judgment only when it checks a meaningful falsification surface.

Separate:

- selected validation class;
- checks the worker says ran;
- logs or outputs visible to GPT;
- skipped checks and rationale;
- whether validation checks the actual issue goal.

Validation does not replace publication proof or issue-goal conformance.

## Project wrapper composition

Use this base for generic GitHub and Git evidence discipline. Compose with project wrappers only for project-specific protected surfaces, domain rules, or validation preferences.

Project wrappers must not replace the GPT-wide Linear/Codex dispatch route. They should add local constraints, not own worker routing.

## Output posture

When verification is straightforward, answer with the judgment and the shortest evidence summary that supports it.

For normal Linear/Codex coding status, use this compact shape:

- Linear state: planned / running / returned-pr-gate / pr-created / landed.
- GitHub state: no artifact / PR present / merge blocked / merged / main verified.
- Next gate: create PR, verify PR, request changes, merge, or close/update issue.

When evidence is mixed, partition it by lane instead of smoothing it into a narrative.

When evidence is missing, say what route was available, what was checked, and what remains unverified. Do not upgrade missing evidence into GREEN.
