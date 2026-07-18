---
name: cleanup-custody
description: Use when use this skill to classify whether a workspace or repository
  surface should stay live, move to cold store, move to governed trash, be deleted
  now, or block and route to an owning authority.
metadata:
  source-id: cleanup-custody
  source-path: sources/first_party/skills/cleanup-custody/SKILL.md
  provenance-name: Cleanup Custody first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when use this skill to classify whether a workspace or repository surface
    should stay live, move to cold store, move to governed trash, be deleted now,
    or block and route to an owning authority.
  use_when:
  - Use when use this skill to classify whether a workspace or repository surface
    should stay live, move to cold store, move to governed trash, be deleted now,
    or block and route to an owning authority.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---
# Cleanup Custody

Use this skill to classify whether a workspace or repository surface should stay live, move to cold store, move to governed trash, be deleted now, or block and route to an owning authority.

This is a GPT-native cleanup, custody, and anti-bloat self-check. It does not replace repo doctrine and does not authorize deletion by itself.

If older `No Shit` or profanity-bearing naming appears in provenance, treat it as history only. Extract the useful anti-bloat posture without preserving profanity-bearing public naming as the active source identity.

## Custody ladder

Use this ladder in order:

1. `keep_live` — current source, active doctrine, active code, required index, publication proof, protected evidence, or authoritative operating material.
2. `move_to_cold_store` — retained but inactive material with lawful custody and discoverability.
3. `move_to_governed_trash` — reversible deletion-staging custody with a repo-visible sentinel.
4. `delete_now` — exact disposable residue with no provenance, authority, or future retrieval value.
5. `block_and_route` — protected, ambiguous, actor-owned, source-law-sensitive, or authority-sensitive surfaces.

## Issue-goal conformance gate

When a repo and issue are in scope, cleanup review is not GREEN merely because git status is clean, residue was classified, or publication proof exists. First restate the issue goal as observable repo/workspace state, inspect the surfaces that would falsify that goal, compare worker claims to observed state, and then judge.

Require these lanes for issue-backed cleanup or self-check work:

- `issue_goal_as_observable_state`
- `repo_surfaces_that_should_reflect_goal`
- `falsification_checks_run`
- `worker_claim_vs_observed_state`
- `judgment`

If an ordinary repo tree, canary, sentinel, index, gitlink, or other observable marker still contradicts the issue goal, block GREEN/PASS and route the correction.

## Governed-trash sentinel rule

When governed trash is involved, require a repo-local `GovernedTrash/INDEX.md` sentinel posture or the repository's established equivalent. Any governed-trash add, remove, or clear action must refresh and publish the sentinel before GREEN.

Do not treat the sentinel as deletion authority. It is custody inventory and publication proof for trash handling.

## Trigger examples

Use this posture for:

- skill sprawl or duplicate operational entrypoints;
- stale helper wrappers or fake compatibility layers;
- temp, build, cache, scratch, and generated residue;
- obsolete governance duplicates;
- wrong-home cross-project artifacts;
- issue/process clutter created only to prove cleanup;
- governed-trash or cold-store custody decisions.

## Protected surfaces

Block and route instead of cleaning when a surface may be:

- archive evidence or provenance;
- canon/world truth or ambiguity-bearing material;
- manuscript prose, authorial notes, or draft candidates;
- live data, machine truth, backups, receipts, or publication proof;
- source-law, actor/domain authority, current operational doctrine, or project-protected material.

## Anti-bloat rule

Prefer the smallest final lawful surface, not the smallest immediate edit. Do not preserve wrappers, compatibility layers, or duplicate doctrine just because they already exist. But do not collapse actor/domain separation, provenance, ambiguity, or local binding pages merely to reduce file count.

## Return checks

When applying this posture, report:

- custody decision for each path or surface;
- protected surfaces checked;
- residue created, discovered, and cleaned before GREEN;
- whether governed-trash sentinel inspection/update is required;
- retained exceptions and their removal condition;
- false-green risks still open;
- for issue-backed work, the issue goal as observable state, falsification checks run, worker claim versus observed state, and judgment.

## False-green risks

- using anti-bloat as a deletion-first hammer;
- deleting evidence, canon, prose, receipts, reports, or proof surfaces;
- treating fewer files as automatically better;
- moving material to governed trash without refreshing the sentinel;
- calling cleanup GREEN while residue or stale indexes remain;
- calling issue-backed cleanup GREEN while observable repo/workspace state still contradicts the issue goal;
- treating clean status, remote-head equality, changed-file lists, or validation claims as substitutes for issue-goal conformance.
