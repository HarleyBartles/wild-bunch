# ADR-0001 Adopt a Markdown Decision Log

## Status

`live`

## Dated History

- `2026-06-01` - Adopted a numbered Markdown log for durable architecture and gameplay decisions, with explicit status and decision types.
- `2026-09-29` - Moved the log from `docs/adr` to `docs/decisions`; ADR-0037 records the related documentation decision.
- `2026-10-08` - Editorial clarification: the log preserves decisions and their history. The authored README catalogue supports progressive discovery; the log is not an implementation report or generated navigation mesh.

## Decision Type

`architecture`, `process`

## Related ADRs

- `related to`: ADR-0037

## Context

The repository needed a durable place to preserve decisions beyond issue discussions and worker handoffs. Architecture and gameplay decisions share one repository history and should be discoverable through one authored convention.

## Decision

Keep one numbered Markdown decision log under `docs/decisions`, with stable filenames, explicit status and decision type, cross-links, an authored catalogue, and a template. Preserve each decision as it was made and record later changes as dated amendments or successor decisions. The catalogue summarizes decisions so a reader can select relevant records without loading the entire log.

## Rationale and Alternatives

Issue comments remain useful for transient discussion but do not provide a stable decision history. Separate logs for architecture and gameplay would split related repository decisions without a distinct authority boundary.

## Consequences

The catalogue and records require semantic maintenance when decisions change. Operational plans, implementation inventories, test receipts, and backlog assignments belong in their owning repository surfaces rather than the ADR log.
