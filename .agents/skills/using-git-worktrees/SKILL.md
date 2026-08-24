---
name: using-git-worktrees
description: Use when starting feature work that needs isolation from current
  workspace or before executing implementation plans - ensures an isolated
  workspace exists via native tools or git worktree fallback
metadata:
  source-id: using-git-worktrees
  source-path: codex-marketplace/plugins/superpowers-plus/skills/using-git-worktrees/SKILL.md
  provenance-name: Using Git Worktrees first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when starting feature work that needs isolation from current workspace
    or before executing implementation plans - ensures an isolated workspace exists
    via native tools or git worktree fallback
  use_when:
  - Use when starting feature work that needs isolation from the current workspace.
  - Use before executing implementation plans if no isolated workspace exists.
  - Use when the repo declares or expects a canonical sibling-folder worktree root.
  do_not_use_when:
  - Do not use when already in an isolated workspace.
  - Do not use when the user declines a worktree.
  - Do not use when native tools already manage isolation.
  related_skills:
  - using-superpowers-plus
  - refreshing-installed-skills
  - executing-plans
  - subagent-driven-development
  - finishing-a-development-branch
license: MIT
---

## Provenance

This skill is a first-party authored derivation of `obra/superpowers` v6.2.0, released under the MIT License. The original upstream snapshot is retained in `codex-marketplace/plugins/superpowers-plus/skills/using-git-worktrees/` for reference.

# Using Git Worktrees

## Overview

Ensure work happens in an isolated workspace. Prefer your platform's native worktree tools. Fall back to manual git worktrees only when no native tool is available.

**Core principle:** Detect existing isolation first. Then use native tools. Then fall back to git. Never fight the harness.

**Announce at start:** "I'm using the using-git-worktrees skill to set up an isolated workspace."

**Mandatory pre-flight:** Invoke this skill BEFORE running `git worktree add`, not after. If you are about to create a worktree without having invoked this skill, stop and invoke it first. Creating a worktree ad hoc without consulting this skill risks placing the worktree in a non-canonical location that violates repo conventions.

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

**You have two mechanisms. Try them in this order.**

### 1a. Native Worktree Tools (preferred)

The user has asked for an isolated workspace (Step 0 consent). Do you already have a way to create a worktree? It might be a tool with a name like `EnterWorktree`, `WorktreeCreate`, a `/worktree` command, or a `--worktree` flag. If you do, use it and skip to Step 2.

Native tools handle directory placement, branch creation, and cleanup automatically. Using `git worktree add` when you have a native tool creates phantom state your harness can't see or manage.

Only proceed to Step 1b if you have no native worktree tool available.

Use the `new-worktree`/`remove-worktree` scripts bundled with this skill as the Step 1b fallback. If the repo also provides its own worktree helpers (for example, in a `scripts/` directory at the repo root), prefer the repo-specific ones. The bundled scripts are installed at `.agents/skills/using-git-worktrees/scripts/` and place the worktree at the canonical sibling-folder root (`../_agent-worktrees/<repo-name>/<branch>`), automatically refreshing installed skills after creation. If `refreshing-installed-skills` is not available, the script creates the worktree and prints a warning instead of failing.

The bundled `new-worktree` script does not require `--allow-shared-checkout`; a new worktree is an isolated linked worktree, so child skill scripts can write inside it without the flag. Example: `py -3 .agents/skills/using-git-worktrees/scripts/new_worktree.py --apply <branch>`. Preview with `--check <branch>` first.

### 1b. Git Worktree Fallback

**Only use this if Step 1a does not apply** — you have no native worktree tool available. Create a worktree manually using git.

#### Directory Selection

Follow this priority order. Explicit user preference always beats observed filesystem state.

1. **Check your instructions for a declared worktree directory preference.** If the user has already specified one, use it without asking.

2. **If the repo instructions declare a canonical sibling-folder worktree root, use that location.** For example, use `../_agent-worktrees/<repo-name>` when the repo's AGENTS file names that path.

3. **Otherwise, check for an existing project-local worktree directory:**
   ```bash
   ls -d .worktrees 2>/dev/null     # Preferred (hidden)
   ls -d worktrees 2>/dev/null      # Alternative
   ```
   If found, use it. If both exist, `.worktrees` wins.

4. **If there is no other guidance available**, default to `.worktrees/` at the project root.

#### Safety Verification (project-local directories only)

**MUST verify directory is ignored before creating worktree:**

```bash
git check-ignore -q .worktrees 2>/dev/null || git check-ignore -q worktrees 2>/dev/null
```

**If NOT ignored:** Add to .gitignore, commit the change, then proceed.

**Why critical:** Prevents accidentally committing worktree contents to repository.

#### Create the Worktree

```bash
# Determine path based on chosen location
path="$LOCATION/$BRANCH_NAME"

git worktree add "$path" -b "$BRANCH_NAME"
cd "$path"
```

**Sandbox fallback:** If `git worktree add` fails with a permission error (sandbox denial), tell the user the sandbox blocked worktree creation and you're working in the current directory instead. Then run setup and baseline tests in place.

## Step 2: Project Setup

Auto-detect and run appropriate setup:

```bash
# Node.js
if [ -f package.json ]; then npm install; fi

# Rust
if [ -f Cargo.toml ]; then cargo build; fi

# Python
if [ -f requirements.txt ]; then pip install -r requirements.txt; fi
if [ -f pyproject.toml ]; then poetry install; fi

# Go
if [ -f go.mod ]; then go mod download; fi
```

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

| Script | Purpose | Safe invocation |
|---|---|---|
| `scripts/new_worktree.py` | Create a linked worktree at the canonical sibling root | `py -3 scripts/new_worktree.py --check <branch>` then `py -3 scripts/new_worktree.py --apply <branch>` |
| `scripts/remove_worktree.py` | Remove a linked worktree | `py -3 scripts/remove_worktree.py --check <branch>` then `py -3 scripts/remove_worktree.py --apply <branch>` |

All scripts support `--help` and classify each flag as `read-only` or `mutating`. `--check` is the default; `--apply` is required for any filesystem or git mutation.

## Quick Reference

| Situation | Action |
|-----------|--------|
| Already in linked worktree | Skip creation (Step 0) |
| In a submodule | Treat as normal repo (Step 0 guard) |
| Native worktree tool available | Use it (Step 1a) |
| No native tool | Git worktree fallback (Step 1b) |
| Repo declares canonical sibling-folder root | Use `../_agent-worktrees/<repo-name>` |
| `.worktrees/` exists | Use it (verify ignored) |
| `worktrees/` exists | Use it (verify ignored) |
| Both exist | Use `.worktrees/` |
| Neither exists | Check instruction file, then default `.worktrees/` |
| Directory not ignored | Add to .gitignore + commit |
| Permission error on create | Sandbox fallback, work in place |
| Tests fail during baseline | Report failures + ask |
| No package.json/Cargo.toml | Skip dependency install |
| Bundled `new-worktree` script | Use it instead of `git worktree add` |
| Bundled `remove-worktree` script | Use it to remove a worktree and deinit submodules |
| Skills need refresh after creation | `new-worktree` auto-runs `refreshing-installed-skills` |

## Common Rationalizations

| Excuse | Reality |
|--------|---------|
| "I'm obviously not in a worktree — no need to check" | Run Step 0. Harness-created isolation and submodules both fool eyeballing; the detection commands settle it. |
| "`git worktree add` is quicker than hunting for a native tool" | A native tool (e.g. `EnterWorktree`) owns placement, branching, and cleanup. Bypassing it is the #1 mistake — it creates phantom state your harness can't see or manage. |
| "The worktree directory is surely ignored already" | Run `git check-ignore`. An unignored worktree directory commits the whole tree into the repo. |
| "Any directory name works" | Explicit instructions beat an existing project-local directory, which beats the `.worktrees/` default. |
| "The workspace is fresh — baseline tests can wait" | A dirty baseline makes every later failure ambiguous. Run the tests now; proceeding past failures is your human partner's call. |

## Remove a Worktree

When a feature branch is complete, remove the isolated worktree to avoid stale copies.

1. Run the bundled `remove-worktree` script if available:
   ```bash
   bash .agents/skills/using-git-worktrees/scripts/remove-worktree.sh --apply <branch-name>
   # or on Windows:
   .agents/skills/using-git-worktrees/scripts/remove-worktree.ps1 --apply <branch-name>
   ```

   Preview first with `--check <branch-name>`.
   If the script reports that the directory is locked, **stop immediately**. The git worktree is already deregistered; the locked on-disk folder can be deleted later once no process holds it.

2. If no bundled script is available, use `git worktree remove` directly:
   ```bash
   git worktree remove <path-to-worktree>
   ```
   Then manually deinitialize submodules if the repo uses them.

Never remove the main repository checkout with this command.

## Red Flags

Never run `rm -rf`, `rmdir /s /q`, or `Remove-Item -Recurse -Force` on a worktree directory that `remove-worktree` failed to delete.
A locked directory is usually another process's current working directory or an open file handle; force-deleting it can delete the wrong directory or other repositories.
