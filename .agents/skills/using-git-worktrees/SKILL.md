---
name: using-git-worktrees
description: Use when feature work or plan execution needs an isolated Git workspace.
metadata:
  source-id: using-git-worktrees
  source-path: codex-marketplace/plugins/superpowers-plus/skills/using-git-worktrees/SKILL.md
  provenance-name: Using Git Worktrees first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  use_when:
    - starting feature work that needs isolation from the current workspace.
    - an implementation plan is ready but no isolated workspace exists.
    - the repo declares or expects a canonical sibling-folder worktree root.
  do_not_use_when:
    - already in an isolated workspace.
    - the user declines a worktree.
    - the task is read-only and needs no new worktree.
  related_skills:
    - using-superpowers-plus
    - refreshing-installed-skills
    - executing-plans
    - subagent-driven-development
    - finishing-a-development-branch
license: MIT
---

## Provenance

This marketplace-maintained derivative is based on `obra/superpowers` v6.4.1 commit `5bf4e78011075bcfc0dc295f0724994cd123ee71` under the MIT License. Upstream source is not vendored; this directory contains the maintained Superpowers+ implementation.

# Using Git Worktrees

## Overview

Ensure work happens in an isolated workspace. Use this skill's bundled creation script so the worktree lands in the canonical sibling location.

**Core principle:** Detect existing isolation first. Use the bundled script for creation. Verify the resulting location and branch.

**Announce at start:** "I'm using the using-git-worktrees skill to set up an isolated workspace."

**Mandatory pre-flight:** Invoke this skill before creating a worktree. Do not create one through a native app tool or direct Git commands; those routes can place it outside the repository's canonical worktree root.

**Session resume check:** If you are resuming a session that inherited a worktree from a previous conversation, verify the worktree location matches the repo's declared canonical worktree root before proceeding with substantive work. If the worktree is in a non-canonical location, move it with `git worktree move` before continuing.

## Step 0: Detect Existing Isolation

**Before creating anything, check if you are already in an isolated workspace.**

```bash
GIT_DIR=$(cd "$(git rev-parse --git-dir)" 2>/dev/null && pwd -P)
GIT_COMMON=$(cd "$(git rev-parse --git-common-dir)" 2>/dev/null && pwd -P)
BRANCH=$(git branch --show-current)
```

**Submodule guard:** `GIT_DIR != GIT_COMMON` is also true inside git submodules. Before concluding "already in a worktree," verify you are not in a submodule:

```bash
# If this returns a path, you're in a submodule, not a worktree — treat as normal repo
git rev-parse --show-superproject-working-tree 2>/dev/null
```

**If `GIT_DIR != GIT_COMMON` (and not a submodule):** You are already in a linked worktree. Skip to Step 2 (Project Setup). Do NOT create another worktree.

Report with branch state:

- On a branch: "Already in isolated workspace at `<path>` on branch `<name>`."
- Detached HEAD: "Already in isolated workspace at `<path>` (detached HEAD, externally managed). Branch creation needed at finish time."

**If `GIT_DIR == GIT_COMMON` (or in a submodule):** You are in a normal repo checkout.

Has the user already indicated their worktree preference in your instructions? If not, ask for consent before creating a worktree:

> "Would you like me to set up an isolated worktree? It protects your current branch from changes."

Honor any existing declared preference without asking. If the user declines consent, work in place and skip to Step 2.

## Step 1: Create Isolated Workspace

Use the bundled `new_worktree.py` script for every new worktree. It creates the worktree at the canonical sibling root (`../_agent-worktrees/<repo-name>/<branch>`), installs dependencies, and refreshes installed skills. A native Codex worktree tool may instead choose its own app directory and violate the consumer's location policy. Do not use it as an alternate creation path.

From the main checkout, preview and then create:

```text
py -3 .agents/skills/using-git-worktrees/scripts/new_worktree.py --check <branch>
py -3 .agents/skills/using-git-worktrees/scripts/new_worktree.py --apply <branch>
```

The preview exits nonzero when the worktree does not exist yet; read its proposed path. After creation, verify the returned path, branch, and Git registration against the consumer's policy before editing. If the script cannot create the canonical worktree, stop and report the blocker instead of choosing another creator or working in the shared checkout. Worktree and branch retirement belongs to `finishing-a-development-branch`.

## Step 2: Project Setup

The bundled script performs dependency setup. Check its result; do not repeat installation unless the consumer's guidance requires an additional step.

## Step 3: Verify Clean Baseline

Run tests to ensure workspace starts clean:

```bash
# Use project-appropriate command
npm test / cargo test / pytest / go test ./...
```

**If tests fail:** Report failures, ask whether to proceed or investigate.

**If tests pass:** Report ready.

### Report

```
Worktree ready at <full-path>
Tests passing (<N> tests, 0 failures)
Ready to implement <feature-name>
```

## Bundled scripts

| Script                    | Purpose                                                | Safe invocation                                                                                        |
| ------------------------- | ------------------------------------------------------ | ------------------------------------------------------------------------------------------------------ |
| `scripts/new_worktree.py` | Create a linked worktree at the canonical sibling root | `py -3 scripts/new_worktree.py --check <branch>` then `py -3 scripts/new_worktree.py --apply <branch>` |

All scripts support `--help` and classify each flag as `read-only` or `mutating`. `--check` is the default; `--apply` is required for any filesystem or git mutation.

## Quick Reference

| Situation                          | Action                                                       |
| ---------------------------------- | ------------------------------------------------------------ |
| Already in linked worktree         | Skip creation (Step 0)                                       |
| In a submodule                     | Treat as normal repo (Step 0 guard)                          |
| New worktree needed                | Preview and apply bundled `new_worktree.py`                  |
| Native worktree tool available     | Still use bundled `new_worktree.py`                          |
| Creation fails                     | Report blocker; do not switch creator or checkout            |
| Tests fail during baseline         | Report failures + ask                                        |
| Bundled `new-worktree` script      | Use it for creation                                          |
| Completed branch/worktree          | Use `finishing-a-development-branch` for verified retirement |
| Skills need refresh after creation | `new-worktree` auto-runs `refreshing-installed-skills`       |

## Common Rationalizations

| Excuse                                               | Reality                                                                                                                         |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| "I'm obviously not in a worktree — no need to check" | Run Step 0. Harness-created isolation and submodules both fool eyeballing; the detection commands settle it.                    |
| "Codex has a worktree button"                        | The app can choose a different root. Use the bundled script and verify its canonical result.                                    |
| "A different creator is close enough"                | The bundled script owns placement and setup. Stop if it cannot create the canonical worktree.                                   |
| "The workspace is fresh — baseline tests can wait"   | A dirty baseline makes every later failure ambiguous. Run the tests now; proceeding past failures is your human partner's call. |

## Retirement handoff

When the feature is complete or a published PR has merged, use `finishing-a-development-branch`. That skill owns integration proof, branch retirement, worktree removal, submodule teardown, and locked-directory stops.
