# Code style playbook

## When

Writing or reviewing source whose language or framework conventions matter.

## Required capabilities

- Apply C# and .NET design and style guidance.
- Design React component structure and browser styling.

## Optional capabilities

- None.

## Required repository-owned skills

- None.

## Optional repository-owned skills

- None.

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

Use the focused formatter/compiler owned by the touched project, then the
canonical gate in [testing](testing.md).

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
