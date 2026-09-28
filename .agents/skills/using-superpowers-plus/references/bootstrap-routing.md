# Bootstrap routing

Use this reference when the session starts, resumes, or the next action is unclear. Pick the smallest sufficient request mode and hand off to the owning skill.

## Request classification

| Mode                        | When                                                                                                                                             | Route to                                                                                                                                                               |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ordinary_chat`             | Acknowledgement, ping, preference, or side chat with no source evidence                                                                          | Answer directly                                                                                                                                                        |
| `tiny_reversible_change`    | Fully specified, local, reversible edit with one obvious target and no product, taste, authority, safety, publication, or architectural decision | Read only `using-superpowers-plus`, make the bounded edit, run one focused check, and report; do not load repo-worker, connector, design, or broad verification skills |
| `continuity_ingress`        | Resume packet, inherited worktree, or next-session block                                                                                         | `using-git-worktrees` for state; then `repo-worker-base` if there is repo work to continue                                                                             |
| `repo_worker`               | Coding, repo-backed worker, issue handoff, PR gate, or source-truth claims                                                                       | `repo-worker-base`                                                                                                                                                     |
| `github_proof`              | PR/branch/commit/review/merge/main verification after a GitHub artifact exists                                                                   | `using-github-mcp`                                                                                                                                                     |
| `linear_control`            | Linear issue/project/comment/document mechanics                                                                                                  | `using-linear-mcp`                                                                                                                                                     |
| `publishing_source`         | Decide how to publish source work: commit, tag, release, push source, or export a pack                                                           | `publishing-source`                                                                                                                                                    |
| `artifact_work`             | Document, spreadsheet, slide, PDF, image, package, receipt                                                                                       | The artifact skill the repo declares, or `writing` for prose                                                                                                           |
| `verification_or_reporting` | QA, closeout posture, validation, review-feedback, or report writing                                                                             | `verification-before-completion` and `writing-with-clarity`                                                                                                            |
| `skill_work`                | Create, update, validate, package, install, or troubleshoot skills                                                                               | `writing-skills`                                                                                                                                                       |

## Repository-resident workflow guidance

For repo-backed modes, inspect repository-resident guidance related to the active workflow when the repository declares it. Follow the repository's own entrypoints, inventories, and paths. Do not assume a particular filename, directory, or runbook/playbook system.

1. Read the repository's declared workflow entrypoint or inventory, when one exists, and use it to identify topical guidance that applies to the request.
2. When the lifecycle stage is not explicit, use the repository's declared workflow guidance, if available, to resolve it. The selected stage skill also consults local guidance relevant to its stage when that guidance exists.
3. Treat links between local workflow artifacts as useful routing, not as the only way relevant guidance can be declared.

Read only artifacts that apply to the active concern; do not inspect every local guide speculatively. If the repository declares no relevant artifact, continue with the portable skill baseline and do not invent a local workflow. Surface a missing artifact only when the repository's own guidance requires it or its absence changes a material assumption.

## Repo-backed work handoff

For repo-backed work, the mandatory handoff is:

```text
using-superpowers-plus -> repo-worker-base (hygiene) -> stage skill (reads its baseline + applicable repository guidance, when present)
```

`repo-worker-base` supplies worktree, branch, scratch, validation, and publication boundaries only; it does not own stage baselines or repository-specific workflow paths. Each stage skill owns its baseline reference (`references/<stage>-baseline.md`) and consults repository-resident guidance relevant to that stage when the repository declares it. The repository decides whether that guidance lives in a runbook, playbook, root instruction file, or another local artifact. For the portable stage-to-skill mapping, see [`superpowers-composition.md`](superpowers-composition.md).

Do not invoke a stage skill directly for repo work without the `repo-worker-base` hygiene handoff.

The `tiny_reversible_change` fast path is the narrow exception to that handoff. If inspection reveals ambiguity, protected state, broader scope, or a second decision, leave the fast path and route normally. Do not perform precautionary skill fan-out before that evidence exists.
