# Artifact custody doctrine

## Scratch and evidence

- Branch-scoped temporary work lives under
  `Z:\_agent-scratch\wild-bunch\<branch-name>`.
- Task briefs, worker reports, review packages, screenshots, and temporary notes
  are scratch artifacts, not tracked source.
- Clean the branch scratch directory when its worktree is removed.
- Do not create loose review reports, screenshots, or session files at repo root
  or under product source.

## Durable outputs

- Architecture decisions belong in `docs/adr/`.
- Current agent rules belong in `.agents/doctrine/`, `.agents/contracts/`, or
  `.agents/runbooks/` according to their authority role.
- Binding repo-specific anti-slop profiles live under
  `.agents/contracts/unslop/`; scoped profiles use
  `<scope>/.agents/contracts/unslop/`. Portable profiles remain owned by
  `unslop-profiles`.
- Generated indexes are navigation only and are never edited by hand.
