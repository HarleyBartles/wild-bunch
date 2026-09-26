---
name: completing-planning-artifacts
description: Use when a repository work slice is completing or a new substantive slice starts while completed plans, specifications, roadmaps, or checkpoints remain tracked.
metadata:
  source-id: completing-planning-artifacts
  source-path: codex-marketplace/plugins/repo-worker-pack/skills/completing-planning-artifacts/SKILL.md
  provenance-name: Completing Planning Artifacts first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Two-slice completion and retirement lifecycle for planning artifacts.
  use_when:
    - a completing PR contains plans, specifications, roadmaps, or checkpoints that governed the work.
    - a new substantive slice starts from main with completed planning artifacts awaiting retirement.
  do_not_use_when:
    - an artifact is still governing active work.
    - custody or completion is ambiguous; use cleanup-custody to classify it first.
license: MIT
---

# Completing Planning Artifacts

Plans, specifications, roadmaps, checkpoints, and similar files are **committed, in-flight execution artifacts**, not durable repository truth. Their lifecycle spans two substantive slices so squash merging records them in canonical Git history without requiring a cleanup-only PR.

## Completing-slice lane

Before handing off a substantially complete PR, whether it remains Draft or is made Ready:

1. Verify implementation and review are complete.
2. Promote enduring architecture decisions to the repository's ADR home and operating rules to current doctrine, runbooks, or playbooks.
3. Mark every governed artifact with the exact state `completed-awaiting-retirement`.
4. Keep those artifacts tracked in the completing PR. Do not delete them before merge: a squash merge cannot preserve a file absent from the final PR tree.
5. Verify the published PR contains the completion-marked artifacts.
6. Make the plan fully checked for every agent-owned obligation. Human-owned Ready or merge actions belong in the handoff or PR state, not as unchecked plan work. Draft is a commercial and CI posture unless the PR is explicitly declared incomplete; a fully reviewable Draft completes the agent's slice.

The agent's completing slice ends at the verified, fully reviewable PR handoff. The artifact lifecycle then leaves the completion-marked files in that PR and on `main` after merge. They are no longer active authority even while tracked temporarily.

## Successor-slice ingress lane

After refreshing `main` and creating the next substantive branch/worktree, but before its substantive edits:

1. Discover artifacts marked `completed-awaiting-retirement` in the branch's base tree.
2. Verify that base is current `main`; branch-only residue is not retirement proof.
3. Verify enduring content was promoted. Use **REQUIRED SUB-SKILL:** `cleanup-custody` if completion or promotion is ambiguous.
4. If the consumer permits a completed-artifact convenience copy, resolve its off-repo scratch root from host or repository policy; do not invent a drive or fixed absolute path. Keep repositories segregated beneath that root as `<scratch-root>/<repo-name>/completed/<artifact-type>/`, where `<repo-name>` is the canonical repository identity rather than a feature-worktree leaf. Copy the files there as disposable, non-evidentiary convenience copies. A consumer-declared resolver or more specific repo-segregated layout takes precedence over this conventional shape; never flatten artifacts from multiple repositories into one shared `completed/` directory.
5. Remove the tracked artifacts and stale links or generated indexes, then run the consumer's owning regeneration command.
6. Commit retirement as the first commit in the next substantive slice's eventual PR. Never open a cleanup-only PR merely to remove them.

If no later substantive slice occurs, the marked artifacts remain on `main`. If concurrent successor slices target the same artifacts, the first merged PR retires them; later slices refresh their base and drop redundant deletions.

## Stop conditions

Do not retire an artifact when it lacks the completion marker, still governs active work, did not arrive through the current `main` base, or contains an unpromoted durable decision. Classify and repair that custody before removal.

## Common mistake

Deleting completed artifacts in the completing PR does not preserve them in canonical history under squash merge. Feature-branch or forge retention is not a substitute for the repository's `main` history.
