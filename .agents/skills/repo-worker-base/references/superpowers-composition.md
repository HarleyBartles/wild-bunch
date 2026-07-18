# Superpowers composition

## Read when

Read before selecting a Superpowers lane for repo-backed design, planning,
implementation, or code review.

## Required pairing

Use the same ordered contract for every repo-backed stage:

~~~text
repo-worker-base -> matching baseline -> local guide -> selected Superpowers lane
~~~

| Stage | Baseline | Local guide | Lane |
| --- | --- | --- | --- |
| Design | design-baseline.md | .agents/guides/design-guide.md | brainstorming |
| Planning | planning-baseline.md | .agents/guides/planning-guide.md | writing-plans |
| Implementation | implementation-baseline.md | .agents/guides/implementing-guide.md | executing-plans or subagent-driven-development |
| Review | code-review-baseline.md | .agents/guides/code-review-guide.md | requesting-code-review |

The repository-local hygiene/layout policy remains the authority for local
paths, commands, exclusions, CI, and exceptions. The local guide supplies its
stage overlay; the Superpowers lane supplies stage technique; this base skill
supplies portable hygiene, custody, evidence, and publication boundaries.
Local guidance cannot override, reorder, or bypass the required base,
matching-baseline, local-guide, and lane sequence.

Do not use this pairing to recursively reclassify work or to copy local policy
into generic guidance. If a declared local guide is absent, read the local
hygiene/layout policy, preserve the portable baseline, and surface the missing
guide as a repository-local gap.
