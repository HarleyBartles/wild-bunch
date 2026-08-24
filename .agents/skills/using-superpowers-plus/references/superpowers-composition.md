# Superpowers composition

## Read when

Read before selecting a Superpowers lane for repo-backed design, planning,
implementation, or code review.

## Required pairing

Use the same ordered contract for every repo-backed stage:

~~~text
using-superpowers-plus -> repo-worker-base (hygiene) -> stage skill (reads its baseline + local guide)
~~~

| Stage | Baseline (owned by the stage skill) | Local guide | Lane |
| --- | --- | --- | --- |
| Design | brainstorming/references/design-baseline.md | .agents/runbooks/design.md | brainstorming |
| Planning | writing-plans/references/planning-baseline.md | .agents/runbooks/planning.md | writing-plans |
| Implementation | executing-plans/references/implementation-baseline.md | .agents/runbooks/implementing.md | executing-plans or subagent-driven-development |
| Review | requesting-code-review/references/code-review-baseline.md | .agents/runbooks/code-review.md | requesting-code-review |

The repository-local hygiene/layout policy remains the authority for local
paths, commands, exclusions, CI, and exceptions. `repo-worker-base` supplies
worktree, branch, scratch, validation, and publication boundaries only; it no
longer owns stage baselines or this composition table. The local guide
supplies its stage overlay; the stage skill supplies stage technique and reads
its own baseline as its first step. Local guidance cannot override, reorder,
or bypass the required hygiene, baseline, local-guide, and lane sequence.

Do not use this pairing to recursively reclassify work or to copy local policy
into generic guidance. If a declared local guide is absent, read the local
hygiene/layout policy, preserve the portable baseline, and surface the missing
guide as a repository-local gap.
