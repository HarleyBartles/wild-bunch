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
