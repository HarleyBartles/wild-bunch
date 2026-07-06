# Write-Tool Phantom Files (Windows)

Cross-project doctrine for the `write` tool's phantom-file bug on Windows.

## The Problem

The `write` tool on Windows creates phantom files in parent directories of deeply nested paths with hyphenated components. When writing to a path like `c:/WORK/.../.worktrees/bunch-124-url-routing/.some-file.txt`, the tool creates files named after prefix fragments of the path's directory components in every parent directory:

- `.worktrees/b` — prefix of `bunch-124-url-routing`
- `.worktrees/bunch` — longer prefix
- `.worktrees/bunch-` — prefix including the hyphen
- `.worktrees/bunch-124` — longer prefix
- `.worktrees/bunch-124-url` — longer prefix

Each phantom file contains the same content as the target file. The bug is inconsistent — not every `write` call triggers it, and it does not affect the `edit` or `exec` tools.

## Detection

After batch writes, check for phantom files:

```powershell
# Check .worktrees/ for prefix-fragment files
Get-ChildItem -File -Force .worktrees/ | Select-Object Name

# Check the worktree root for unexpected files
cd .worktrees/<worktree-name>
Get-ChildItem -File -Force | Where-Object { $_.Name -notlike ".git*" -and $_.Name -ne "AGENTS.md" -and $_.Name -ne "INDEX.md" -and $_.Name -ne "README.md" -and $_.Name -ne "WildBunch.sln" } | Select-Object Name
```

Phantom files are recognizable because their names are prefix fragments of directory names in the path, and they all have the same byte content as the intended target file.

## Cleanup

```powershell
# Remove phantom files in .worktrees/
Remove-Item -Force .worktrees/b, .worktrees/bunch, .worktrees/bunch-, .worktrees/bunch-124, .worktrees/bunch-124-url -ErrorAction SilentlyContinue

# Remove phantom files inside the worktree (check each directory level)
Get-ChildItem -File -Force | Where-Object { <filter for unexpected files> } | Remove-Item -Force
```

## Prevention

There is no way to prevent the bug itself — it's a tool issue. The mitigation is:

1. **After any batch of `write` calls, check for phantom files.** This is especially important before committing — phantom files must not be committed.
2. **Prefer `edit` over `write`** when modifying existing files — `edit` does not create phantom files.
3. **Use `exec` with `Set-Content`** for scratch files when the content is simple — `exec` does not create phantom files.
4. **Before claiming work is done, verify the workspace is clean** — no phantom files in any parent directory of a write target.

## Scope

This is a cross-project tooling issue, not specific to any repo. Any agent using the `write` tool on Windows in a path with hyphenated directory components may encounter it. This doctrine document exists so future agents don't trip over it without knowing what it is.
