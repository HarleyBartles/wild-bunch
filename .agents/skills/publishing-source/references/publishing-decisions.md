# Publishing decisions

Use this reference when the `publishing-source` decision checklist reaches
the "pick the surface" step. Choose the smallest sufficient surface for the
change, then hand off to the owning skill for the mechanics.

## Surface route table

| Change shape | Surface | Owns mechanics | Proof to record |
| --- | --- | --- | --- |
| Authorized direct-main work (small, low-risk, explicitly approved) | Direct-main commit | `using-github-mcp` (git push) | Verified main commit SHA |
| Ordinary repo work (default) | Pull request into main | `using-github-mcp` (PR open) | PR URL + branch + head SHA |
| Branch closeout after PR review | Merge + branch delete | `finishing-a-development-branch` | Merge commit SHA or PR URL |
| Versioned release of merged source | Tag + GitHub release | `release-engineering` + `using-github-mcp` | Tag URL + release URL |
| Marketplace pack change (sources/, registry) | Regenerate + PR | `publishing-source` decides; `repo-worker-base` hygiene | PR URL + regenerated-surface commit SHA |
| Pack export artifact (installable archive) | Export archive from regenerated bundle | `release-engineering` | Export artifact hash + source commit SHA |

## Selection rules

- Prefer a PR into main for ordinary worker execution. Use a direct-main
  commit only when direct-main work is explicitly authorized.
- A tag or release always follows a merged source change, never a local
  branch. Tag the merged commit, not the working tree.
- A pack export must be built from a regenerated, validated marketplace bundle. Record
  both the export artifact and the source commit it was built from.
- Publication proof is required for any repo-work return. Local commit hashes,
  local validation output, or an unpublished branch alone are not proof.

## Handoff boundary

`publishing-source` owns the decision only. Once the surface is chosen, the
owning skill performs the mechanics:

- `using-github-mcp` for git push, PR open, tag, and release mechanics.
- `release-engineering` for release pipeline and pack export operations.
- `finishing-a-development-branch` for branch closeout.
- `verification-before-completion` if the change is not yet green.
