# Code style runbook

## When

Writing or reviewing source whose language or framework conventions matter.

## Required skills

- `/dotnet` for C# and .NET surfaces.
- `/react` for React component structure.
- `/web-styling` for browser styling choices.

## Composition

Invoke only the capability matching the touched surface; combine capabilities
only when the change crosses those boundaries.

## Doctrine and contracts

[Coding discipline](../doctrine/coding-discipline.md) binds all source. Browser
work also binds [frontend standards](../doctrine/frontend-standards.md).

## Local commands and paths

Use the focused formatter/compiler owned by the touched project, then the
canonical gate in [testing](testing.md).

## Evidence contract

Changed source follows the applicable local doctrine and passes its compiler or
typecheck lane.

## Prohibited combinations

- Do not apply frontend conventions to backend code or vice versa.
- Do not restate portable language guidance here.
