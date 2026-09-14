# Artifact custody doctrine

## Active tracked artifacts

- Active plans live in `.agents/plans/`.
- Active specifications live in `.agents/specs/`.
- Active roadmaps live in `.agents/roadmaps/`.
- Completed plans, specifications, roadmaps, checkpoints, audits, trackers, and
  reports do not remain in the tracked tree. Git history is the historical
  record.
- Use `/cleanup-custody` to classify a candidate, protect evidence and
  provenance, choose the lawful disposition, and verify cleanup.
- Before removal, promote enduring architecture decisions into `docs/adr/` and
  current Wild Bunch rules into doctrine or runbooks.
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
- Current agent rules belong in `.agents/doctrine/` or `.agents/runbooks/`.
- Repo-specific anti-slop profiles live under `.agents/unslop/`; portable
  profiles remain owned by `unslop-profiles`.
- Generated indexes are navigation only and are regenerated through the mesh
  tooling rather than edited by hand.
