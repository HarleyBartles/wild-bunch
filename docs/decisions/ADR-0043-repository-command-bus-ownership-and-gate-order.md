# ADR-0043 Repository Command Bus Ownership and Gate Order

## Status

`live`

## Dated History

- `2026-10-08` - Adopted one repository command bus for supported build, test, lint, format, and validation operations; the canonical commit/CI gate is check-only, fail-fast, and ordered from cheapest checks to most expensive.
- `2026-10-08` - Retired redundant command wrappers and their filename-oriented tests while preserving native scripts with real process or service lifecycle responsibilities.

## Decision Type

`architecture`, `operations`, `testing`

## Related ADRs

- `related to`: ADR-0037
- `related to`: ADR-0017

## Context

Repository operations were split among a Python runner, direct commands, and thin shell or PowerShell wrappers. This made the supported interface difficult to discover and allowed hook and CI behavior to drift. The repository has adopted an explicit command-bus contract and a candidate-preserving check-only hook.

## Decision

The repository command bus is the single agent-facing entrypoint for supported build, test, lint, format, and validation operations. Each operation has a discoverable purpose, explicit meaningful modes, truthful prerequisites and side effects, and preserves underlying output and exit status. Check operations do not repair maintained files; mutations are exposed through named apply operations.

The canonical commit and CI gate is check-only, fails at the first failed check, and orders checks from cheapest to most expensive. Fast formatting, lint, static, and repository-contract checks run before test suites or expensive build and test setup whenever their prerequisites allow. The same required checks and failure criteria apply to the staged candidate and its hosted committed counterpart. Aggregate diagnostics may exist only as a separate manual troubleshooting operation and never replace the canonical gate.

Repository scripts remain outside the bus only when they own a justified standalone or native process/service lifecycle. Thin wrappers that only forward to a bus operation are retired rather than maintained as alternate entrypoints.

## Rationale and Alternatives

A single discoverable interface reduces the commands contributors and agents must learn and gives hook, CI, and manual workflows one owner for validation policy. Fail-fast ordering gives a clear first repair and avoids waiting for expensive tests when a cheaper check has already failed. Maintaining duplicate wrappers adds names and drift without adding behavior; genuine native lifecycle commands retain their platform-specific ownership.

## Consequences

New supported repository operations are assessed for command-bus integration. New lint and formatting lanes join the canonical gate before behavioral tests and expensive builds where prerequisites permit. The gate reports the failed check and focused repair or recheck command while preserving its failure status. Manual aggregate diagnostics remain outside commit and CI enforcement.

Removal of thin CI and image-pipeline wrappers and their existence-oriented tests is part of the repository's recorded cleanup history; it does not prohibit future native lifecycle tools with a concrete owner and purpose.
