# ADR-0012 GameContent in Code Now, DB-Backed Content Later

## Status

live

## Dated Status History

- 2026-06-01 - live: deterministic code-backed `WildBunch.GameContent` remains
  the current content source, while database-backed content is tracked as a
  future direction.

## Decision Type

architecture, content

## Related ADRs

- `depends on`: ADR-0001, ADR-0004
- `informs`: ADR-0003
- `related to`: GitHub issue #34

## Context

The current repo seeds gameplay content from deterministic code in
`WildBunch.GameContent`. That works for early development, but the long-term
content topology is expected to move toward database-backed records.

## Decision Drivers

- The current code-backed content is stable enough to support live development.
- Content should remain deterministic today.
- Future content authoring should be easier than editing code for every change.
- The database-backed direction must stay separate from runtime session state.

## Decision Summary

Keep the current code-backed `WildBunch.GameContent` model as the live content
source now, while treating DB-backed content as a future migration path tracked
separately.

## Detailed Decision Breakdown

The current content builders and seed factories construct worlds, cases,
suspects, and related setup data in code. That is accepted for the present,
because it keeps the setup deterministic and easy to test.

The future direction is a DB-backed content store, but that is not the current
implementation. This ADR records the live baseline and the future path without
claiming the migration is done.

## Options Considered and Rejected

- Freeze all content permanently in code with no future migration path.
- Move all content immediately to the database before the repo is ready.
- Mix runtime session state into the future content store design.

## When a Rejected Option Would Have Been Better

Permanent code-only content would only be better if the project never expected
content authoring to outgrow code edits. Immediate migration would only be
better if the database-backed content design were already implemented and
verified.

## Benefits

- The current setup remains deterministic and testable.
- Future content work can be planned without pretending it already exists.
- The repo keeps a clean separation between live runtime state and authorable
  content.

## Accepted Tradeoffs

- Content changes still require code edits for now.
- The future DB-backed migration remains a separate slice of work.

## Risks

- Readers could confuse the future direction with current implementation if the
  distinction is not kept explicit.
- A later content-store migration must preserve the current deterministic setup
  behavior.

## Consequences for Future Work

Future content-store work should start from the current code-backed baseline and
should not treat this ADR as proof that DB-backed content already exists.

## Implementation Status or Plan

Live for the current code-backed model; DB-backed content is future work tracked
separately, including GitHub issue `#34`.

## Related Stable Source Surfaces

- `src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs`
- `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`
- `src/WildBunch.GameContent/NewGame/SeedWorldFactory.cs`
- `src/WildBunch.GameContent/NewGame/CaseCharacterRoster.cs`
- `tests/WildBunch.GameContent.Tests/`

## Proof of Implementation or Explicit Non-Implementation

The current game setup is produced by code-backed `WildBunch.GameContent`
builders. There is no DB-backed content store in the current source evidence set
for this slice.

## Review Triggers

- When the DB-backed content store is implemented.
- When content authoring becomes painful enough to justify the migration slice.
