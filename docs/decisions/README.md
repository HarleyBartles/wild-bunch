# Architecture Decision Records

This is the human-facing catalogue and durable decision log for Wild Bunch. Each record states a decision made at a point in the repository's history; its dated history preserves what changed, including material removals. A decision record is not a status report, implementation plan, feature inventory, or claim that its decision is implemented.

Use the catalogue to select records relevant to the code, contract, or change under consideration. Follow a record's successor links when its scope has been superseded. Do not load every ADR by default. The repository's decision-record playbook defines when lifecycle work must read, create, correct, or supersede records.

## Recording decisions

- Keep original decision dates and the substance of what was decided at that time.
- Use `live`, `planned`, `partially superseded`, `superseded`, `deprecated`, or `rejected` to describe the decision's authority today. These values do not report implementation completion.
- When a later decision replaces only part of an earlier one, keep the earlier record and identify the surviving scope and successor. Do not rewrite history to make the new state appear original.
- Add a dated editorial note only when clarification is needed to correct the record; identify it as editorial and preserve what was originally decided.
- Link related records with explicit relationship labels. Do not use this log as an issue tracker or implementation checklist.
- Use the [ADR template](TEMPLATE.md) for new records. Keep source and test inventories, proof receipts, and future work in their owning operational documents.

## Catalogue

| ADR | Status | Decision |
| --- | --- | --- |
| [ADR-0001](ADR-0001-adopt-markdown-adr-log.md) | live | Keep durable repository decisions in a numbered Markdown ADR log with stable filenames, explicit status, types, and cross-links. |
| [ADR-0002](ADR-0002-gamesession-is-the-command-aggregate-root.md) | live | `GameSession` is the single top-level command aggregate root for live play, with coherent session-owned components beneath it. |
| [ADR-0003](ADR-0003-composed-jsonb-session-persistence.md) | live | Persist session state as a composed envelope and JSONB component payloads, with ordered rows for log and diary history. |
| [ADR-0004](ADR-0004-postgresql-local-development-and-validation-lane.md) | live | Use PostgreSQL for local development and the repository's database validation lane. |
| [ADR-0005](ADR-0005-casefile-is-a-session-owned-case-component.md) | live | `CaseFile` owns case-local invariants and evidence beneath `GameSession`, not as a separate command root or repository. |
| [ADR-0006](ADR-0006-investigation-reveals-knowledge-not-gang-pressure.md) | live | Investigation actions reveal knowledge; any gang-pressure model is a separate decision. |
| [ADR-0007](ADR-0007-hidden-culprit-truth-and-hidden-progress-boundaries.md) | live | Keep culprit identity and hidden progress inside the domain and out of player-facing read surfaces. |
| [ADR-0008](ADR-0008-town-visit-investigation-source-refresh.md) | live | Scope investigation-source use to town visits and preserve per-town history in session-owned state. |
| [ADR-0009](ADR-0009-structured-clue-anchors-and-lead-plausibility.md) | live | Represent clue subjects, places, times, and directions with structured anchors for plausible leads and readable case-board rendering. |
| [ADR-0010](ADR-0010-lawman-evidence-is-event-derived-not-seeded.md) | planned | Lawman evidence is to derive from gameplay events and state changes rather than a seeded evidence roster. |
| [ADR-0011](ADR-0011-cockpit-hosted-modal-play-surfaces-before-routing.md) | superseded by ADR-0027 | Major play surfaces were to begin as cockpit-hosted modals before promotion to canonical routes. |
| [ADR-0012](ADR-0012-gamecontent-in-code-now-db-backed-content-later.md) | live | Keep game content code-backed for now, with database-backed content as a separately tracked future migration. |
| [ADR-0013](ADR-0013-travel-journey-is-a-session-owned-aggregate-subtree.md) | live | Keep journey state as a cohesive session-owned subtree rather than a separate command root or repository. |
| [ADR-0014](ADR-0014-use-ddd-onion-cqrs-repositories-and-first-class-unit-of-work.md) | live | Use DDD, Onion dependency direction, CQRS handlers, aggregate-scoped repositories, and a first-class Unit of Work. |
| [ADR-0015](ADR-0015-use-aspnet-core-minimal-apis-as-the-game-http-boundary.md) | live | Use ASP.NET Core Minimal APIs as a thin HTTP boundary that delegates to application handlers. |
| [ADR-0016](ADR-0016-use-react-vite-react-query-and-styled-components-for-the-web-client.md) | live | Build the web client with React, Vite, TanStack React Query, and component-scoped styled-components. |
| [ADR-0017](ADR-0017-use-xunit-vitest-testing-library-and-explicit-postgresql-validation-lanes.md) | live | Use xUnit and Vitest/Testing Library with explicit PostgreSQL validation for the corresponding test boundaries. |
| [ADR-0018](ADR-0018-target-net10-with-nullable-enabled-sdk-style-projects.md) | live | Target .NET 10 with nullable-enabled SDK-style projects and implicit usings. |
| [ADR-0019](ADR-0019-use-a-manual-typed-frontend-api-client-until-generated-clients-are-justified.md) | live | Maintain a typed frontend API client until a source-backed need justifies generated clients. |
| [ADR-0020](ADR-0020-aggregate-domain-authority-and-root-persistence-posture.md) | live | Aggregates own legality and invariants within their consistency boundaries; one aggregate cannot mutate another's authority. |
| [ADR-0021](ADR-0021-uuid-shaped-setup-seeds-resolve-to-legal-starting-world-descriptors.md) | live | Resolve UUID-shaped setup seeds into legal descriptors for starting-world variability. |
| [ADR-0022](ADR-0022-ui-browser-checks-are-a-manual-evidence-lane.md) | live | Treat browser checks as a manual evidence lane for changes that affect visible game flows or when requested. |
| [ADR-0023](ADR-0023-difficulty-and-entropy-vocabulary-and-fairness-contract.md) | live | Difficulty governs ordinary challenge and entropy governs world variation without rewriting established culprit truth. |
| [ADR-0024](ADR-0024-source-taxonomy-implications-for-difficulty-and-entropy.md) | live | Difficulty and entropy may vary source pressure and presentation while preserving settled facts and case solvability. |
| [ADR-0025](ADR-0025-suspect-legal-and-bounty-vocabulary-boundary.md) | live | Keep bounty eligibility, murder guilt, and case resolution distinct in the legal vocabulary. |
| [ADR-0026](ADR-0026-turn-in-outcome-semantics-for-bounty-and-murder-case-separation.md) | live | Separate legal eligibility, turn-in outcome, settlement, murder-case resolution, and hidden culprit truth. |
| [ADR-0027](ADR-0027-ui-v0-1-spa-shell-routing-and-player-debug-separation.md) | live | Use a routed SPA shell with React Query session state and player-facing navigation. |
| [ADR-0028](ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md) | live | Use Onion architecture, DDD, strict CQRS, event-sourced session history, and categorized read projections. |
| [ADR-0029](ADR-0029-heat-is-future-lawman-pressure-not-trail-danger.md) | live | Heat represents future lawman attention accumulated in town, not trail danger. |
| [ADR-0030](ADR-0030-dev-overlay-and-dev-endpoint-namespace.md) | live | Place developer controls in a distinct overlay and endpoint namespace. |
| [ADR-0031](ADR-0031-event-sourced-dev-travel-controls.md) | live | Model developer travel overrides as event-sourced controls on travel outcomes. |
| [ADR-0032](ADR-0032-event-sourced-dev-saloon-controls.md) | live | Model developer saloon overrides as event-sourced controls on saloon outcomes. |
| [ADR-0033](ADR-0033-repo-documentation-mesh-posture.md) | superseded by ADR-0037 | The earlier documentation mesh and navigation posture was replaced by the later repository-owned routing decision. |
| [ADR-0034](ADR-0034-playthrough-archive-lifecycle-and-one-active-playthrough-invariant.md) | live | Archive playthroughs through an event and allow one active playthrough per user. |
| [ADR-0035](ADR-0035-react-shell-with-phaser-as-renderer-input-adapter-for-playfield-surfaces.md) | live | Keep React authoritative while Phaser renders playfield data and returns player intent through callbacks. |
| [ADR-0036](ADR-0036-dev-enabled-action-pattern.md) | live | Orchestrate developer-enabled actions through preparation, optional injection, and public action phases. |
| [ADR-0037](ADR-0037-ambient-capabilities-opt-in-standards-and-decision-documentation.md) | partially superseded | Later contracts replaced its standards deployment model; its ADR location and decision to retire generated index navigation remain authoritative. |
