---
name: repo-worker-base
description: Use when thin repo hygiene entrypoint for Codex workers in Harley's workspace.
  Use when a Codex worker is working in any repository in Harley's workspace and needs
  fresh-main discipline, worktree isolation, branch and PR hygiene, validation evidence,
  or publication proof.
metadata:
  source-id: repo-worker-base
  source-path: sources/first_party/skills/repo-worker-base/SKILL.md
  provenance-name: Repo Worker Base first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when thin repo hygiene entrypoint for Codex workers in Harley's workspace.
    Use when a Codex worker is working in any repository in Harley's workspace and
    needs fresh-main discipline, worktree isolation, branch and PR hygiene, validation
    evidence, or publication proof.
  use_when:
  - Use when thin repo hygiene entrypoint for Codex workers in Harley's workspace.
    Use when a Codex worker is working in any repository in Harley's workspace and
    needs fresh-main discipline, worktree isolation, branch and PR hygiene, validation
    evidence, or publication proof.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---

# Repo Worker Base

This skill is the compositional entrypoint for repo-backed worker tasks in Harley's workspace.

Use it to establish the boring repo baseline, then route out to the supporting skills that own the narrower concerns:

- `work-mode-router` for durable route classification before repo work begins;
- `linear-issue-shaping` for worker-ready Linear issue shaping and route-state handling;
- `boring-loop` for queue discipline and the next smallest safe move;
- `connector-safety` for blocked, sensitive, or permission-changing connector writes;
- `github-operations` for PR, branch, commit, status, merge, publication, and main-state proof.
- `unslop-plus` for worker-facing anti-slop profiles when a repo task needs tighter plan, review, or return discipline;
- `context-safety` for safer large text writes, bounded composition, and atomic replacement paths.

Keep this skill thin. Do not absorb broad process doctrine that belongs in the supporting skills.

## Fresh-main invariant

Before editing files, the worker must:

1. Fetch current `origin/main`.
2. Record the starting main SHA.
3. Create the worker branch from current `origin/main`, or update/rebase/merge the existing worker branch onto current `origin/main` before continuing.
4. Do not start implementation from a stale branch base.
5. If the branch cannot be updated from current main, stop and report BLOCKED or AMBER with the exact blocker.

## Worktree isolation gate

For Devin-backed repo tasks, work in a fresh dedicated worktree based on current `main` or the issue-specified base before any file mutation. Report before mutating:

- worktree path;
- branch name;
- base commit;
- `git status --short` before mutation;
- whether any pre-existing dirty state was present.

Do not overwrite pre-existing dirty state. Report it. This gate composes with the fresh-main invariant; it does not replace it.

### Worktree verification gate

After creating a worktree, the worker must verify they are working in the correct location before any file mutation. Before editing files, verify:

1. **Current working directory**: Confirm the working directory matches the worktree path (not the main checkout)
2. **Git worktree status**: Run `git worktree list` to confirm the current directory is a worktree, not the main checkout
3. **Branch verification**: Confirm the current branch matches the intended worker branch
4. **Path verification**: Confirm file paths resolve to the worktree location, not the main repo root

**Verification commands**:
```bash
pwd  # Should show worktree path, not main repo path
git worktree list  # Should show current directory as a worktree
git branch --show-current  # Should show worker branch
git status --short  # Should show clean state in worktree
```

**Stop signs for worktree verification**:
- If the current working directory is the main repo root instead of the worktree
- If `git worktree list` shows the current directory is not a worktree
- If the current branch is `main` instead of the worker branch
- If file paths resolve to the main checkout instead of the worktree

**Recovery procedure**:
If verification fails, stop and report the exact mismatch. Do not proceed with file edits until the working directory is corrected to the worktree location. Use `cd` to navigate to the worktree path before continuing.

This gate is mandatory for all Devin-backed repo tasks and must be completed before any file mutation begins.

### Repo-specific worktree locations

For repos in Harley's workspace, worktrees should be placed in the centralized location `../_agent-worktrees/<repo-name>` (relative to the repo root) where `<repo-name>` is the name of the repository (e.g., `../_agent-worktrees/wild-bunch`, `../_agent-worktrees/agent-asset-marketplace`).

This centralized location keeps worktrees outside the repo directories and is the preferred location for these projects. Individual repos may document this preference in their AGENTS.md as a declared preference that should be respected by the using-git-worktrees skill.

## Scratch folder usage

For temporary work files that should not be committed, use the centralized scratch folder at `../_agent-scratch/<repo-name>/<branch-name>` (relative to the repo root).

### Scratch folder properties

- **Disposable**: Not persistent beyond the agent's session
- **Outside repo**: Prevents accidental commits
- **Per-branch**: Matches worktree/branch name for isolation
- **Auto-cleanup**: Agents must clean up scratch folder when cleaning up worktree
- **Not for durable work**: Use the repo for persistent changes

### Scratch folder structure

- **Scratch root**: `../_agent-scratch/`
- **Per-repo**: `../_agent-scratch/<repo-name>/`
- **Per-branch**: `../_agent-scratch/<repo-name>/<branch-name>/`

### Examples

- For wild-bunch on branch `bunch-123-feature`: `../_agent-scratch/wild-bunch/bunch-123-feature/`
- For agent-asset-marketplace on branch `main`: `../_agent-scratch/agent-asset-marketplace/main/`

### Usage guidance

- Use scratch folders for temporary files, logs, intermediate outputs, or disposable workspace
- Create scratch folders when needed, but ensure cleanup when work is complete
- Scratch folder creation failures should not block work (fallback to in-repo temp if needed)
- Do not use scratch folders for durable changes that should be committed to the repo

### Cleanup contract

Agents must clean up their scratch folder when cleaning up their worktree. This ensures the scratch space remains clean and does not accumulate orphaned temporary files across sessions.

## Branch and PR discipline

Use a task branch for repo work. Do not treat direct push to `main` as the normal path.

Normal publication path:

1. edit on worker branch;
2. commit on worker branch;
3. push worker branch;
4. open or enable a PR into `main`;
5. return PR evidence;
6. let GPT/Harley verify the PR and mainline state after merge.

Do not use shell GitHub credentials or PAT workarounds unless the task explicitly authorizes that route.

## Validation discipline

Run the validation appropriate to the repo and issue. If no repo-specific command is known, run the smallest meaningful text/build/test checks available and report what was skipped.

Validation evidence must distinguish:

* commands run;
* result;
* skipped checks;
* why skipped checks were acceptable or blocking.

## GREEN gate

Do not return GREEN unless all relevant facts are true:

* source work is complete;
* branch was based on or updated from current `origin/main`;
* final commit SHA is recorded;
* worker branch was pushed or exact no-publication blocker is recorded;
* PR URL is returned when publication is available;
* validation was run or explicitly justified;
* working tree is clean or exact dirty state is reported;
* no known mergeability blocker remains.

If the PR is not mergeable, return AMBER and ask for or perform branch update from current `main`.

## Required return evidence

Every repo-backed worker return must include:

* repository;
* issue ID or task identifier;
* branch name;
* starting main SHA;
* final head SHA;
* whether branch was created from current main, rebased onto current main, or merged with current main;
* PR URL, or exact reason no PR exists;
* changed files;
* validation commands and results;
* working tree state;
* GREEN / AMBER / RED / BLOCKED judgment;
* any remaining blockers or follow-up needed.

## Stop signs

Stop and report instead of continuing when:

* current main cannot be fetched;
* branch cannot be updated from current main;
* repo target is ambiguous;
* required validation cannot run and no acceptable substitute exists;
* merge conflicts need human/product judgment;
* task asks for direct main mutation without explicit latest-turn authorization;
* required secrets or local-only resources are unavailable.

Keep this skill thin. Do not include project-specific doctrine. Project-local `AGENTS.md`, issue bodies, and repo docs override this base where they are more specific, but they do not remove the fresh-main invariant unless explicitly stated.
