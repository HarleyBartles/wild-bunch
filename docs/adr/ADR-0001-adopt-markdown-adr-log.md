# ADR-0001 Adopt Markdown ADR Log

## Status

live

## Dated Status History

- 2026-06-01 - live: the repo now has a dedicated Markdown ADR log, template,
  and index under `docs/adr`.

## Decision Type

architecture, process

## Related ADRs

- `informs`: ADR-0002 through ADR-0012

## Context

Wild Bunch needed a stable way to preserve high-value architecture and gameplay
decisions without copying issue threads, worker notes, or chat history into
source docs. The repo already had durable documentation for testing lanes and
local PostgreSQL, but no dedicated ADR log.

## Decision Drivers

- Decisions must survive beyond a single worker pass.
- Architecture and gameplay decisions belong in one shared convention.
- Status and decision-type metadata need to stay explicit.
- The log must be easy for humans to scan and for future workers to extend.

## Decision Summary

Create a Markdown ADR log under `docs/adr` with padded ADR numbers, a reusable
template, a lightweight index, explicit status values, decision-type metadata,
and cross-linking rules.

## Detailed Decision Breakdown

The ADR system uses one directory, one numbering convention, one template, and
one status taxonomy. That keeps the repository from drifting into parallel logs
for architecture, gameplay, or operations decisions.

The index in `docs/adr/README.md` documents numbering, naming, status values,
decision types, and cross-linking rules. The template in
`docs/adr/TEMPLATE.md` captures the required sections so future ADRs stay
consistent.

The initial backfill uses ADR numbers `0001` through `0012` so the log can cover
the stable decisions already visible in the repo and the remaining planned
boundaries from issue `#36`.

## Options Considered and Rejected

- Keep decisions in issue comments and worker handoffs only.
- Create separate logs for architecture and gameplay.
- Skip status metadata and rely on prose alone.

## When a Rejected Option Would Have Been Better

Issue comments would be better for transient discussion that is not ready to be
promoted. Separate logs would only be better if the repo had truly independent
decision systems, which it does not.

## Benefits

- Future maintainers can find a decision without reconstructing a worker pass.
- Architecture and gameplay stay in one stable human-facing convention.
- The log can record both live decisions and future constraints cleanly.

## Accepted Tradeoffs

- The log adds one more durable doc surface to maintain.
- Some decisions need to be summarized rather than exhaustively re-litigated.

## Risks

- The log can become stale if it is not updated when the repo changes.
- Over-linking to brittle implementation details could make ADRs noisy.

## Consequences for Future Work

Future ADRs should follow the same template and cross-link deliberately. New
decision families should reuse the existing status and type vocabulary instead
of inventing a new system.

## Implementation Status or Plan

Live. The ADR directory, README index, template, and initial files now exist.

## Related Stable Source Surfaces

- `docs/adr/README.md`
- `docs/adr/TEMPLATE.md`

## Proof of Implementation or Explicit Non-Implementation

The repository now contains a dedicated ADR directory with an index and
template. That is the durable evidence that the log convention exists.

## Review Triggers

- When a new decision type or status value is needed.
- When the ADR log starts to duplicate issue-tracking behavior.
- When the template no longer covers a recurring decision pattern.
