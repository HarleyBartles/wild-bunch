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

## Repository composition discovery

For every repo-backed mode, use the root `AGENTS.md` routing pointers to locate the repository's workflow inventories. At session start, resume, and whenever the active concern changes:

1. Read `.agents/playbooks/INDEX.md` when the repository exposes it. Match the request's topical concerns against that inventory independently of runbook selection, then read only the applicable playbooks.
2. Use `.agents/runbooks/INDEX.md` to resolve the available lifecycle stages when the stage route is not already explicit. The selected stage skill still reads its owning runbook as part of the normal handoff.
3. Treat runbook-to-playbook links as additional predictable composition, not as the only way a playbook becomes available.

Do not read every playbook body speculatively. The indexes are the bounded discovery surface; a playbook body loads only when its named concern applies. If an inventory is absent, follow the repository's declared shape and do not invent a local workflow.

## Repo-backed work handoff

For repo-backed work, the mandatory handoff is:

```text
using-superpowers-plus -> repo-worker-base (hygiene) -> stage skill (reads its baseline + local guide)
```

`repo-worker-base` supplies worktree, branch, scratch, validation, and publication boundaries only; it no longer owns stage baselines or the Superpowers composition table. Each stage skill owns its own baseline reference (`references/<stage>-baseline.md`) and reads it together with the repo's `.agents/runbooks/<stage>.md` as its own first step. That runbook is the local stage composition root and resolves every applicable conditional composition under `.agents/playbooks/`. For the ordered stage composition table, see [`superpowers-composition.md`](superpowers-composition.md).

Do not invoke a stage skill directly for repo work without the `repo-worker-base` hygiene handoff.

The `tiny_reversible_change` fast path is the narrow exception to that handoff. If inspection reveals ambiguity, protected state, broader scope, or a second decision, leave the fast path and route normally. Do not perform precautionary skill fan-out before that evidence exists.
