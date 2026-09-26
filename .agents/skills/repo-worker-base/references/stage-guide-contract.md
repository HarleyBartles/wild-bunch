# Stage guide contract

## Read when

Read when locating, creating, or reviewing the consuming repository's local guide for design, planning, implementation, or code review.

## Contract

The canonical lifecycle-stage root home is `.agents/runbooks/`. The retired .agents/docs/guides/ home is forbidden for new authored guides. The canonical home contains the stage runbooks `design.md`, `planning.md`, `implementing.md`, `code-review.md`, and `pr.md`. Topical workflows belong under `.agents/playbooks/`; agents may use them directly, and stage runbooks may route to them when applicable.

Each runbook supplies repository-specific composition, paths, commands, exclusions, CI, and exceptions. It does not replace, override, reorder, or bypass the matching portable baseline or selected Superpowers lane. Migrate a legacy home through the repository's approved plan and keep a fallback pointer only when that policy explicitly requires it.

If no local guide exists, read the repository hygiene/layout policy and report the absent guide as a local-policy gap. Do not invent repository-specific commands or paths in this portable skill.
