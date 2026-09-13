# Artifact policy

## Tracked planning artifacts

- Active plans live in `.agents/plans/`.
- Active specifications live in `.agents/specs/`.
- Active roadmaps live in `.agents/roadmaps/`.
- Completed planning artifacts leave the tracked tree under
  [completed-artifacts doctrine](../doctrine/completed-artifacts.md).

## Scratch and evidence

- Branch-scoped temporary work lives under
  `Z:\_agent-scratch\wild-bunch\<branch-name>`.
- Task briefs, worker reports, review packages, screenshots, and temporary notes
  are scratch artifacts, not tracked source.
- Clean the branch scratch directory when its worktree is removed.
- Do not create loose `PR_BODY.md`, review reports, screenshots, or session files
  at repo root or under product source.

## Durable outputs

- Architecture decisions belong in `docs/adr/`.
- Current agent operating rules belong in `.agents/doctrine/` or
  `.agents/runbooks/`.
- Repo-specific anti-slop profiles live under `.agents/unslop/`; portable
  profiles remain owned by `unslop-profiles`.
- Generated `INDEX.md` and `INDEX.json` files are navigation only and must be
  regenerated through the mesh tooling rather than edited by hand.
