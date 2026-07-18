# Stage guide contract

## Read when

Read when locating, creating, or reviewing the consuming repository's local
guide for design, planning, implementation, or code review.

## Contract

The canonical local guide home is .agents/guides/. The retired
.agents/docs/guides/ home is forbidden for new authored guides. The canonical
home contains four first-class thin overlays: design-guide.md, planning-guide.md,
implementing-guide.md, and code-review-guide.md. A repository may declare
additional guides, but each must name one stage and remain local.

Each guide supplies only repository-specific paths, commands, exclusions, CI,
and exceptions. It does not replace, override, reorder, or bypass the matching
portable baseline or selected Superpowers lane. Migrate a legacy home through
the repository's approved plan and keep a fallback pointer only when that
policy explicitly requires it.

If no local guide exists, read the repository hygiene/layout policy and report
the absent guide as a local-policy gap. Do not invent repository-specific
commands or paths in this portable skill.
