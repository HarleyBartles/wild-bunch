# Code style playbook

## When

Writing or reviewing source whose language or framework conventions matter.

## Required capabilities

- Apply C# and .NET design and style guidance.
- Design React component structure and browser styling.

## Unslop before work
Before work in this scope, follow the [unslop playbook](unslop.md) and its scoped profile selection. Consult the [decision-record playbook](decision-records.md) when work makes, changes, corrects, or materially removes a durable decision.

## Composition

1. Classify each touched source surface as .NET, React, or browser styling.
2. Invoke only its declared capability (C# and .NET implementation conventions, React component architecture, or browser styling conventions)
   and bind coding discipline plus any frontend doctrine.
3. Run the focused formatter, compiler, or typecheck for that surface, then the
   testing runbook's canonical gate at delivery.

## Doctrine and contracts

[Coding discipline](../doctrine/coding-discipline.md) binds all source. Browser
work also binds [frontend standards](../doctrine/frontend-standards.md).

## Local commands and paths

Use the command bus for formatting and linting. VS Code format-on-save is
configured for supported source languages, and Codex Desktop/CLI project hooks
format only supported files changed by a tool call when the exact hook
definition has been reviewed and trusted. These adapters call the same command
bus; they do not define formatter policy. If an editor or agent runtime does
not provide these adapters, apply mechanical formatting explicitly with
`py -3 tools/run.py format --apply <paths>`, review the diff, then use
`py -3 tools/run.py format --check <paths>` and
`py -3 tools/run.py lint --check <paths>`. The check targets never modify
maintained files; pre-commit and hosted CI remain check-only. For delivery, use
the canonical fail-fast gate in [testing](testing.md), which runs cheap format
and lint checks before builds and behavioral tests.

## Evidence contract

- [ ] Each touched surface follows its applicable capability and local doctrine.
- [ ] Its focused compiler, formatter, or typecheck passes.
- [ ] Cross-boundary changes use each relevant capability without importing
  conventions from an unrelated layer.

## Prohibited combinations

- Do not apply frontend conventions to backend code or vice versa.
- Do not restate portable language guidance here.

## Runbook routing

- [Implementing](../runbooks/implementing.md)
- [Code review](../runbooks/code-review.md)
- [Decision records](decision-records.md) - when a style or architecture change establishes or revises a durable convention.
