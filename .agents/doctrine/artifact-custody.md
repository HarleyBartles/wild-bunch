# Artifact custody doctrine

## Active tracked artifacts

- Active plans live in `.agents/plans/`.
- Active specifications live in `.agents/specs/`.
- Active roadmaps live in `.agents/roadmaps/`.
- Completed plans, specifications, roadmaps, checkpoints, audits, trackers, and
  reports do not remain in the tracked tree. Git history is the historical
  record.
- Optional convenience copies may use
  `Z:\_agent-scratch\wild-bunch\completed\`; they have no retention promise or
  evidentiary role.

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
- Repo-specific anti-slop profiles live under `.agents/unslop/`; portable
  profiles remain owned by `unslop-profiles`.
- Generated indexes are navigation only and are never edited by hand.
