# Superpowers composition

## Read when

Read before selecting a Superpowers lane for repo-backed design, planning, implementation, or code review.

## Required pairing

Use the same ordered contract for every repo-backed stage:

```text
using-superpowers-plus -> repo-worker-base (hygiene) -> stage skill (reads its baseline + applicable repository guidance, when present)
```

| Stage          | Baseline (owned by the stage skill)                       | Lane                                           |
| -------------- | --------------------------------------------------------- | ---------------------------------------------- |
| Design         | brainstorming/references/design-baseline.md               | brainstorming                                  |
| Planning       | writing-plans/references/planning-baseline.md             | writing-plans                                  |
| Implementation | executing-plans/references/implementation-baseline.md     | executing-plans or subagent-driven-development |
| Review         | requesting-code-review/references/code-review-baseline.md | requesting-code-review                         |

Each stage skill supplies stage technique and reads its own baseline. It also consults repository-resident guidance relevant to that stage when the repository declares such guidance, following the repository's own entrypoints and paths. Repositories may use runbooks and playbooks or another local organization; this skill pack does not require a particular composition root. The repository-local hygiene/layout policy remains the authority for local paths, commands, exclusions, CI, and exceptions. `repo-worker-base` supplies worktree, branch, scratch, validation, and publication boundaries. Local guidance cannot override, reorder, or bypass the required hygiene, baseline, and lane sequence.

Do not use this pairing to recursively reclassify work or to copy local policy into generic guidance. When no applicable local guidance exists, preserve the portable baseline and continue; surface a repository-local gap only when the repository's own declared workflow requires that artifact.
