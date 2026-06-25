# ADR Log

This directory holds the durable architecture decision log for Wild Bunch.
The intent is to keep architecture, gameplay, UI, persistence, operations,
content, and testing decisions in one shared convention rather than splitting
them into parallel logs.

## Numbering

- ADR numbers use padded identifiers from `ADR-0001` through `ADR-9999`.
- If the log ever grows beyond four digits, continue with five-digit padding.
- File names use the pattern `ADR-0001-short-kebab-title.md`.
- Each ADR should keep the file name stable even if the wording is refined.

## Status Values

Use one of these values in the `Status` section:

- `planned`
- `live`
- `superseded`
- `deprecated`
- `rejected`

The status history should be dated so readers can see when a decision was
promoted, replaced, deprecated, or rejected.

## Decision Types

Decision type is a free-form list field that may contain one or more values.
Common values include:

- `architecture`
- `gameplay`
- `ui`
- `persistence`
- `operations`
- `content`
- `testing`
- `process`

Use the narrowest set that accurately describes the decision. For example, a
decision can be both `architecture` and `persistence`, or both `gameplay` and
`ui`.

## Cross-Linking Rules

- Link coupled ADRs with explicit relationship labels such as `depends on`,
  `informs`, `supersedes`, `superseded by`, or `related to`.
- Keep the links stable and canonical, preferably by ADR number and title.
- Use GitHub issue numbers only as supporting context when they help a reader
  locate the original work item.
- Do not turn the ADR log into a second issue tracker.

## Writing Rules

- Write ADRs as durable human-facing decisions, not worker reports.
- Separate current live behavior from future constraints.
- Include the evidence surface that proves the decision is implemented or
  explicitly mark that it is not implemented yet.
- Keep the wording stable and avoid brittle line-level references unless the
  path itself is a canonical source surface.

## Initial Backfill

- [ADR-0001 Adopt Markdown ADR log](ADR-0001-adopt-markdown-adr-log.md)
- [ADR-0002 GameSession is the command aggregate root](ADR-0002-gamesession-is-the-command-aggregate-root.md)
- [ADR-0003 Composed JSONB session persistence](ADR-0003-composed-jsonb-session-persistence.md)
- [ADR-0004 PostgreSQL local development and validation lane](ADR-0004-postgresql-local-development-and-validation-lane.md)
- [ADR-0005 CaseFile is a session-owned aggregate/subaggregate](ADR-0005-casefile-is-a-session-owned-case-component.md)
- [ADR-0006 Investigation reveals knowledge, not gang pressure](ADR-0006-investigation-reveals-knowledge-not-gang-pressure.md)
- [ADR-0007 Hidden culprit truth and hidden progress boundaries](ADR-0007-hidden-culprit-truth-and-hidden-progress-boundaries.md)
- [ADR-0008 Town-visit investigation source refresh](ADR-0008-town-visit-investigation-source-refresh.md)
- [ADR-0009 Structured clue anchors and lead plausibility](ADR-0009-structured-clue-anchors-and-lead-plausibility.md)
- [ADR-0010 Lawman evidence is event-derived, not seeded](ADR-0010-lawman-evidence-is-event-derived-not-seeded.md)
- [ADR-0011 Cockpit-hosted modal play surfaces before routing](ADR-0011-cockpit-hosted-modal-play-surfaces-before-routing.md)
- [ADR-0012 GameContent in code now, DB-backed content later](ADR-0012-gamecontent-in-code-now-db-backed-content-later.md)
- [ADR-0013 Travel journey is a session-owned aggregate subtree](ADR-0013-travel-journey-is-a-session-owned-aggregate-subtree.md)
- [ADR-0014 Use DDD, Onion dependency direction, CQRS handlers, repositories, and first-class Unit of Work](ADR-0014-use-ddd-onion-cqrs-repositories-and-first-class-unit-of-work.md)
- [ADR-0015 Use ASP.NET Core Minimal APIs as the game HTTP boundary](ADR-0015-use-aspnet-core-minimal-apis-as-the-game-http-boundary.md)
- [ADR-0016 Use React, Vite, TanStack React Query, and styled-components for the web client](ADR-0016-use-react-vite-react-query-and-styled-components-for-the-web-client.md)
- [ADR-0017 Use xUnit, Vitest, Testing Library, and explicit PostgreSQL validation lanes](ADR-0017-use-xunit-vitest-testing-library-and-explicit-postgresql-validation-lanes.md)
- [ADR-0018 Target .NET 10 with nullable-enabled SDK-style projects](ADR-0018-target-net10-with-nullable-enabled-sdk-style-projects.md)
- [ADR-0019 Use a manual typed frontend API client until generated clients are justified](ADR-0019-use-a-manual-typed-frontend-api-client-until-generated-clients-are-justified.md)
- [ADR-0020 Aggregate domain authority and root persistence posture](ADR-0020-aggregate-domain-authority-and-root-persistence-posture.md)
- [ADR-0021 UUID-shaped setup seeds resolve to legal starting-world descriptors](ADR-0021-uuid-shaped-setup-seeds-resolve-to-legal-starting-world-descriptors.md)
- [ADR-0022 UI browser checks are a manual evidence lane](ADR-0022-ui-browser-checks-are-a-manual-evidence-lane.md)
- [ADR-0023 Difficulty and entropy vocabulary and fairness contract](ADR-0023-difficulty-and-entropy-vocabulary-and-fairness-contract.md)
- [ADR-0024 Source taxonomy implications for difficulty and entropy](ADR-0024-source-taxonomy-implications-for-difficulty-and-entropy.md)
- [ADR-0025 Suspect legal and bounty vocabulary boundary](ADR-0025-suspect-legal-and-bounty-vocabulary-boundary.md)
- [ADR-0026 Turn-In Outcome Semantics for Bounty and Murder-Case Separation](ADR-0026-turn-in-outcome-semantics-for-bounty-and-murder-case-separation.md)
- [ADR-0027 UI v0.1 SPA shell, routing, and player/debug separation](ADR-0027-ui-v0-1-spa-shell-routing-and-player-debug-separation.md)
- [ADR-0028 Onion, DDD, CQRS, Event Sourcing, and projections posture](ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md)
- [ADR-0029 Heat is future lawman pressure, not trail danger](ADR-0029-heat-is-future-lawman-pressure-not-trail-danger.md)

## Current Working Set

The initial log backfill intentionally focuses on the highest-value decisions
called out by issue `#36`. New ADRs should be added when a decision becomes
stable enough to survive beyond the current work slice. ADR-0023 extends the
log with the accepted difficulty/entropy vocabulary and fairness contract.
ADR-0024 extends the log with the source-taxonomy implications map for those
axes. ADR-0025 extends the log with the suspect legal and bounty vocabulary
boundary. ADR-0026 extends the log with the turn-in outcome semantics that sit
on top of that vocabulary. ADR-0028 extends the log with the true event-sourcing
posture: typed domain events, command-produces-event-then-applies, replay,
optimistic concurrency, snapshot as cache, safe projections, and the
single-repository-port persistence path. ADR-0029 extends the log with the
heat model: heat is lawman pursuit pressure from time spent in town, not
trail danger — it increases by 1 per full town day, resets to 0 on leaving
town, and has no mechanical effect yet.
