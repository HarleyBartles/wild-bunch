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
| [ADR-0002](ADR-0002-gamesession-is-the-command-aggregate-root.md) | live | GameSession Is the Command Aggregate Root |
| [ADR-0003](ADR-0003-composed-jsonb-session-persistence.md) | live | Composed JSONB Session Persistence |
| [ADR-0004](ADR-0004-postgresql-local-development-and-validation-lane.md) | live | PostgreSQL Local Development and Validation Lane |
| [ADR-0005](ADR-0005-casefile-is-a-session-owned-case-component.md) | live | CaseFile Is a Session-Owned Aggregate/Subaggregate |
| [ADR-0006](ADR-0006-investigation-reveals-knowledge-not-gang-pressure.md) | live | Investigation Reveals Knowledge, Not Gang Pressure |
| [ADR-0007](ADR-0007-hidden-culprit-truth-and-hidden-progress-boundaries.md) | live | Hidden Culprit Truth and Hidden Progress Boundaries |
| [ADR-0008](ADR-0008-town-visit-investigation-source-refresh.md) | live | Town-Visit Investigation Source Refresh |
| [ADR-0009](ADR-0009-structured-clue-anchors-and-lead-plausibility.md) | live | Structured Clue Anchors and Lead Plausibility |
| [ADR-0010](ADR-0010-lawman-evidence-is-event-derived-not-seeded.md) | planned | Lawman Evidence Is Event-Derived, Not Seeded |
| [ADR-0011](ADR-0011-cockpit-hosted-modal-play-surfaces-before-routing.md) | superseded by ADR-0027 | Cockpit-Hosted Modal Play Surfaces Before Routing |
| [ADR-0012](ADR-0012-gamecontent-in-code-now-db-backed-content-later.md) | live | GameContent in Code Now, DB-Backed Content Later |
| [ADR-0013](ADR-0013-travel-journey-is-a-session-owned-aggregate-subtree.md) | live | Travel Journey Is a Session-Owned Aggregate Subtree |
| [ADR-0014](ADR-0014-use-ddd-onion-cqrs-repositories-and-first-class-unit-of-work.md) | live | Use DDD, Onion dependency direction, CQRS handlers, repositories, and first-class Unit of Work |
| [ADR-0015](ADR-0015-use-aspnet-core-minimal-apis-as-the-game-http-boundary.md) | live | Use ASP.NET Core Minimal APIs as the game HTTP boundary |
| [ADR-0016](ADR-0016-use-react-vite-react-query-and-styled-components-for-the-web-client.md) | live | Use React, Vite, TanStack React Query, and styled-components for the web client |
| [ADR-0017](ADR-0017-use-xunit-vitest-testing-library-and-explicit-postgresql-validation-lanes.md) | partially superseded by ADR-0047 | Use xUnit and Vitest/Testing Library for their surviving test boundaries, with explicit PostgreSQL validation; ADR-0047 adds native Chromium browser proof. |
| [ADR-0018](ADR-0018-target-net10-with-nullable-enabled-sdk-style-projects.md) | live | Target .NET 10 with nullable-enabled SDK-style projects and implicit usings. |
| [ADR-0019](ADR-0019-use-a-manual-typed-frontend-api-client-until-generated-clients-are-justified.md) | live | Use a manual typed frontend API client until generated clients are justified |
| [ADR-0020](ADR-0020-aggregate-domain-authority-and-root-persistence-posture.md) | live | Aggregate Domain Authority and Root Persistence Posture |
| [ADR-0021](ADR-0021-uuid-shaped-setup-seeds-resolve-to-legal-starting-world-descriptors.md) | live | UUID-Shaped Setup Seeds Resolve to Legal Starting-World Descriptors |
| [ADR-0022](ADR-0022-ui-browser-checks-are-a-manual-evidence-lane.md) | live | Treat browser checks as a manual evidence lane for changes that affect visible game flows or when requested. |
| [ADR-0023](ADR-0023-difficulty-and-entropy-vocabulary-and-fairness-contract.md) | live | Difficulty and Entropy Vocabulary and Fairness Contract |
| [ADR-0024](ADR-0024-source-taxonomy-implications-for-difficulty-and-entropy.md) | live | Source Taxonomy Implications for Difficulty and Entropy |
| [ADR-0025](ADR-0025-suspect-legal-and-bounty-vocabulary-boundary.md) | live | Suspect Legal and Bounty Vocabulary Boundary |
| [ADR-0026](ADR-0026-turn-in-outcome-semantics-for-bounty-and-murder-case-separation.md) | live | Turn-In Outcome Semantics for Bounty and Murder-Case Separation |
| [ADR-0027](ADR-0027-ui-v0-1-spa-shell-routing-and-player-debug-separation.md) | live | UI v0.1 SPA Shell, Routing, Data Layer, and Player/Debug Separation |
| [ADR-0028](ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md) | live | Onion, DDD, CQRS, Event Sourcing, and Projections Posture |
| [ADR-0029](ADR-0029-heat-is-future-lawman-pressure-not-trail-danger.md) | live | Heat Is Future Lawman Pressure, Not Trail Danger |
| [ADR-0030](ADR-0030-dev-overlay-and-dev-endpoint-namespace.md) | live | Dev Overlay and Dev Endpoint Namespace |
| [ADR-0031](ADR-0031-event-sourced-dev-travel-controls.md) | live | Event-Sourced Dev Travel Controls |
| [ADR-0032](ADR-0032-event-sourced-dev-saloon-controls.md) | live | Event-Sourced Dev Saloon Controls |
| [ADR-0033](ADR-0033-repo-documentation-mesh-posture.md) | superseded | Repo Documentation Mesh — Agents, Index, and README Posture |
| [ADR-0034](ADR-0034-playthrough-archive-lifecycle-and-one-active-playthrough-invariant.md) | live | Playthrough Archive Lifecycle and One-Active-Playthrough Invariant |
| [ADR-0035](ADR-0035-react-shell-with-phaser-as-renderer-input-adapter-for-playfield-surfaces.md) | live | React shell with Phaser as renderer/input adapter for playfield surfaces |
| [ADR-0036](ADR-0036-dev-enabled-action-pattern.md) | live | Dev-Enabled Action Pattern |
| [ADR-0037](ADR-0037-ambient-capabilities-opt-in-standards-and-decision-documentation.md) | partially superseded | Later contracts replaced its standards deployment model; ADR-0043 clarifies command-bus ownership while its ADR location and retirement of generated index navigation remain authoritative. |
| [ADR-0043](ADR-0043-repository-command-bus-ownership-and-gate-order.md) | live | Route supported repository build, test, lint, format, and validation through one command bus with a cheap-first, fail-fast check gate. |
| [ADR-0047](ADR-0047-add-playwright-for-native-browser-contracts.md) | live | Use Playwright with Chromium for native browser contracts that JSDOM cannot prove, while retaining Vitest and the separate manual browser lane. |
| [ADR-0048](ADR-0048-gitflow-and-semver-release-lifecycle.md) | live | Use `develop` for integration, `main` for stable releases, and one SemVer identity source with verified development, candidate, and stable release identities. |
| [ADR-0049](ADR-0049-planning-custody-across-integration-and-promotion.md) | live | Keep live planning on develop, retire completed artifacts before cuts, exclude planning from main, and restore the exact current planning tree on reconciliation. |
