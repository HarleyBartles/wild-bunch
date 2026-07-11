# Superpowers Artifact Placement

This `AGENTS.md` is the routing surface for superpowers artifacts in this repo.

## Must Read When

- **Before creating, moving, or committing superpowers artifacts:** [`.agents/docs/artifact-policy.md`](../docs/artifact-policy.md) — scratch artifacts, superpowers artifact placement, prohibited locations, and unslop profiles.

## Historical Artifacts

`.agents/superpowers/plans/` and `.agents/superpowers/specs/` contain historical plans and specs. Do not treat them as live truth. They may contain stale paths, old directory assumptions, outdated design decisions, and completed work that does not match the current repo.

Only rely on superpowers documents that relate to the work you are currently doing. Before copying any pattern, path, or directory assumption from a plan or spec, verify it against:

- the current source tree,
- the nearest `AGENTS.md` routing file,
- the generated `INDEX.md` mesh,
- [`.agents/docs/artifact-policy.md`](../docs/artifact-policy.md).

If the current work has no dedicated plan, follow the current `AGENTS.md` and `.agents/docs/artifact-policy.md` instead of historical artifacts.
