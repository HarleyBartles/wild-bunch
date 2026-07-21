# Superpowers directory hygiene

## Read when

Creating, reviewing, or cleaning up `.agents/superpowers/` working surfaces.

## Baseline

`.agents/superpowers/` is the workspace for Superpowers session artifacts. It has three distinct custody lanes:

- `plans/` — fully repo resident. Implementation plans live here and are version controlled. The index mesh generator creates `INDEX.md` here.
- `specs/` — fully repo resident. Design specs live here and are version controlled. The index mesh generator creates `INDEX.md` here.
- `sdd/` — local-only session working directory. The directory is repo resident, but its contents are gitignored. Track only `.gitignore`. No `INDEX.md` is generated because the directory is gitignored.

`sdd/` must contain a `.gitignore` with:

```gitignore
*
!.gitignore
```

This keeps the folder present in the repo while ignoring all volatile session artifacts. The index mesh generator (`tools/generate_index_mesh.py`) respects `.gitignore` rules and does not place `INDEX.md` files in folders where `INDEX.md` would be ignored. Do not place durable source, plans, or generated marketplace assets under `sdd/`.

## Routing to skills

- `/cleanup-custody` for classifying whether a surface should stay live, move to cold store, or be deleted.
- `/repo-worker-base` for worktree, branch, validation, and publication boundaries.
