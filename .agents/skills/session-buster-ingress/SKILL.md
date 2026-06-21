---
name: session-buster-ingress
description: ingest session busters, continuity exports, resume packets, package queues, and handoff blocks without laundering stale state. use after bootstrap and before acting on continuity fields, especially Linear/Codex worker state, PR-gate claims, GitHub heads, package installs, GPT-native skill queues, or legacy dispatch packets. treat suggested_next_session_name as navigation metadata only. partitions verified, fallback-only, unavailable, contradicted, and blocked claims; extracts the next safe directive without mutating repos, issues, packages, or worker state.
metadata:
  source-id: session-buster-ingress
  source-path: sources/first_party/skills/session-buster-ingress/SKILL.md
  provenance-name: "MARK-9 chunk ledger \xC3\xA2\xE2\u201A\xAC\xE2\u20AC\x9D base and control plane"
license: "MIT"
---
# Session Buster Ingress

Ingest an incoming continuity block without trusting it blindly and without letting it become task content.

This skill extracts a safe directive. It does not perform project bootstrap, dispatch, Linear mutation, GitHub mutation, repo work, package handoff, issue closure, or visual/artifact production.

## Core rule

Continuity is not authority.

`suggested_next_session_name` is navigation metadata only. It can help the next session choose a starting label, but it is not truth, task authority, or a reason to skip verification.

A session buster, handoff block, worker summary, package queue, or resume note is a stale-prone pointer set. It may name durable surfaces, but it does not prove their current state. Verify the smallest live source needed before action.

For coding work after Linear/Codex adoption, the normal durable surfaces are Linear issue state/comments/attachments, Codex task links recorded in Linear, GitHub PRs/commits/main, and package evidence for skill handoffs. Session busters are fallback continuity, not the primary workflow surface.

## Use

Use when the input includes a session buster, continuity export, handoff block, resume packet, next-session prompt, package queue, old worker packet, or similar continuity material.

Use after any required project bootstrap and before acting on fields such as `recommended_next_action`, `first_post_bootstrap_action`, `next_session_sequence`, `open_queue`, `current_running_work`, package install state, or worker status.

## Quick workflow

1. Treat the block as bounded ingress, not passive context.
2. Identify claims: Linear issues, Codex worker state, PRs, repo heads, commits, branches, package installs, skill queues, blockers, caveats, and next actions.
3. Partition each material claim as `verified`, `fallback_only`, `unverified`, `unavailable`, `contradicted`, or `blocked`.
4. Verify current durable sources when available and needed for the next action.
5. Extract one compact safe directive: subject, scope, mode, route, lane, required sources, hard do-not items, and success condition.
6. Route downstream to the owning skill. Do not execute from ingress alone.

## Linear/Codex continuity after adoption

When a handoff concerns coding work, first prefer Linear/Codex state over session narrative.

Check Linear when the buster names a Linear issue, project, Codex worker, worker return, PR gate, side-discovery issue, or next dispatch. Look for:

- issue status, assignee/delegate, labels/project, and relationships;
- Codex thread/comment existence;
- Codex completion or return comments;
- Codex task links;
- PR attachments or `Created pull request` comments.

Interpret the state through `worker-dispatch-linear`:

- `planned`: issue exists, not delegated.
- `delegated/running`: Codex delegate/thread exists, no completion comment.
- `returned/pr-gate`: completion comment exists, no PR attachment or created-PR comment. Tell Harley to open the Codex task link from Linear and click `Create PR`.
- `pr-created`: PR attachment or created-PR comment exists. Route to the repo/GitHub proof surface for PR proof.
- `landed`: PR merged and main verified.

Do not use old worker packet state or session prose to override live Linear/GitHub evidence.

## GPT-native skillwork and package queues

If the handoff concerns GPT-native skill creation, update, validation, packaging, installation, or queue state, do not route it to Codex Cloud merely because it is called work or dispatch.

Use the skill stack source of truth:

`skill-creator -> skill-validator -> skill-packager -> skill-handoff`

A package queue claim is fallback-only until the required same-target stack evidence exists. Installed-skill state in a buster is fallback-only unless Harley confirms it or a live installed-skill/resource view supports it. Once the canonical agent asset repo exists, prefer repo source plus package evidence over installed-skill narrative.

## Repo, GitHub, and publication claims

Verify current GitHub or repo state before accepting claims about:

- branch heads, main heads, commits, PR numbers, changed files, CI/status, merges, issue comments, or closure posture;
- GitHub-visible publication from Codex;
- shell Git credentials or PAT needs;
- final GREEN or landed status.

A Codex completion comment without a PR is not publication proof. A PR existence claim is not issue-goal conformance. Passing checks is not closure proof.

## Source route posture

When checking incoming claims, distinguish:

- Linear connector results for issue/project/comment/attachment truth;
- live GitHub/API routes for exact PR, commit, branch, file, status, and issue evidence;
- repo files when source content matters;
- package evidence receipts for skill archives;
- fallback-only chat memory, worker reports, or old session busters.

If the required live route is unavailable, preserve uncertainty instead of pretending the continuity block is current.

## Operator-context quarantine

A session buster is an instruction packet for the assistant. It is not creative source material by default.

Do not use bootstrap steps, repo rollups, source-zip rebuilds, skill install lists, queue state, worker status, verification notes, or continuity metadata as image content, slide copy, deck structure, asset-sheet text, or audience-facing narrative unless Harley explicitly asks for that status/handoff artifact.

## Output posture

Keep ingress reports short unless Harley asks for full detail.

State:

- what durable IDs or surfaces were found;
- what was verified;
- what remains fallback-only or unavailable;
- what is contradicted;
- the next safe action.

Do not claim GREEN, repo state, queue state, package state, worker state, or installed-skill state from a buster until verified.

## Stop rules

Do not enter a broad skill-reading loop. Once the safe directive is extracted and the owning downstream skill is clear, stop ingress and act from that owner.

Do not dispatch, mutate repo files, close issues, edit Linear, post comments, package, or hand off archives from ingress alone.
