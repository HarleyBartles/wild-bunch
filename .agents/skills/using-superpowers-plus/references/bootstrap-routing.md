# Bootstrap routing

Use this reference when the session starts, resumes, or the next action is
unclear. Pick the smallest sufficient request mode and hand off to the owning
skill.

## Request classification

| Mode | When | Route to |
|---|---|---|
| `ordinary_chat` | Acknowledgement, ping, preference, or side chat with no source evidence | Answer directly |
| `continuity_ingress` | Resume packet, inherited worktree, or next-session block | `/using-git-worktrees` for state; then `/repo-worker-base` if there is repo work to continue |
| `repo_worker` | Coding, repo-backed worker, issue handoff, PR gate, or source-truth claims | `/repo-worker-base` |
| `github_proof` | PR/branch/commit/review/merge/main verification after a GitHub artifact exists | `/using-github-mcp` |
| `linear_control` | Linear issue/project/comment/document mechanics | `/using-linear-mcp` |
| `publishing_source` | Decide how to publish source work: commit, tag, release, push source, or export a pack | `/publishing-source` |
| `artifact_work` | Document, spreadsheet, slide, PDF, image, package, receipt | The artifact skill the repo declares, or `/writing-with-clarity` for prose |
| `verification_or_reporting` | QA, closeout posture, validation, review-feedback, or report writing | `/verification-before-completion` and `/writing-with-clarity` |
| `skill_work` | Create, update, validate, package, install, or troubleshoot skills | `/writing-skills` |

## Repo-backed work handoff

For repo-backed work, the mandatory handoff is:

```text
using-superpowers-plus -> repo-worker-base (hygiene) -> stage skill (reads its baseline + local guide)
```

`repo-worker-base` supplies worktree, branch, scratch, validation, and
publication boundaries only; it no longer owns stage baselines or the
Superpowers composition table. Each stage skill owns its own baseline
reference (`references/<stage>-baseline.md`) and reads it together with the
repo's `.agents/runbooks/<stage>.md` as its own first step. For the
ordered stage composition table, see
[`superpowers-composition.md`](superpowers-composition.md).

Do not invoke a stage skill directly for repo work without the `repo-worker-base`
hygiene handoff.
