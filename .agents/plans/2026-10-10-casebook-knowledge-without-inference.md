# Preserve truthful casebook knowledge through capture Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the 0.1.0 casebook preserve learned clues and wanted information through settlement without inventing identity matches or conclusions.

**Architecture:** Give each generated wanted record an optional stable `SuspectId` association, preserve it through the event snapshot and cache serializers, and use it only in Application projections to apply recorded settlement status. Keep clues and public warrants as separate learned facts, remove feature/name-based lead resolution and captured-evidence exclusion, render only supplied facts in React, and retire the incomplete unused reference projector.

**Tech Stack:** C#/.NET 10, DDD aggregate and typed event snapshots, event payload upcasting, ASP.NET Application read models, React, TypeScript, styled-components, xUnit, Vitest and PostgreSQL integration tests.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially the casebook simplification decision, stable public identity association, capture-retention behavior, player-known knowledge boundary, and event-sourcing/cache recovery contract.

**Execution Strategy:** `executing-plans`, native inline and sequential. Domain identity, event persistence, projections, consumers and tests are coupled to one truthful casebook contract; complete one fresh whole-branch review after the final local gate.

**Roadmap:** Row 08, `Restore strict read ownership and honest projections`; successor plan `.61` after PR #245.

**Scope:** Add generated warrant-to-suspect identity association without name matching; carry that association through event/current-state serialization and versioning; project each learned warrant and clue independently; show recorded settlement status without dropping any learned detail; remove casebook feature/alias/route inference; remove the unused `CaseFileViewProjector`; update the PG-008 evidence and dated investigation dispositions.

**Out of scope:** New deduction or hypothesis mechanics, automatically resolving unnamed observations, showing hidden suspect links or culprit truth, revealing facts on capture, changing bounty or confrontation rules, changing the public wanted-poster lifetime, retrofitting historical records by matching names, changing the journal narration slice, or broad CaseFile DTO/file reorganization.

**Review Focus:**

- A legacy v2 `CaseFileGenerated` payload without stable warrant identity must remain reconstructible with an unknown association; prove no same-name fallback through the real upcaster chain.
- Two wanted records with identical display names must remain separate, and settlement of one `SuspectId` must affect only the matching record; prove through `CaseBoardMapper` from real domain records.
- An unnamed clue whose feature resembles a warrant must remain its own authored claim with its source and anchors; prove neither the serialized player DTO nor the rendered casebook calls it a named-person sighting or resolved lead.
- Captured people retain their known warrant and identity evidence with truthful alive/dead settlement details, while culprit identity and internal suspect IDs stay out of player JSON and rendered UI.

## Global Constraints

- Develop from `origin/develop` and target every implementation PR to `develop`; `main` remains the release line.
- The only authored application version is `Directory.Build.props`; this plan advances it to `0.1.0-dev.61` and does not add generated or duplicated version values.
- Preserve typed event sourcing, aggregate authority, event replay and existing upcaster history; never infer missing historical identity from display names.
- The event stream records occurred facts; a query/rebuild does not generate identities, reroll choices or append events.
- Casebook output contains learned public facts and explicit associations only; hidden culprit truth, internal links and developer audit details stay private.
- Keep all already learned clues and wanted information after settlement; expose settlement status from recorded identity and event facts, not from normalized text.
- Follow the unslop playbook and selected backend-architecture, play-surface-ui and code-review profiles; consult ADR-0005, ADR-0007, ADR-0009 and ADR-0028 through the decision-record playbook.
- Keep ADR history unchanged unless inspection finds an actual durable decision divergence; update `docs/features.md` and the linked historical investigation/test dispositions when evidence changes.
- Prove each new behavior test RED for its intended reason, then restore and verify GREEN; do not add route/file-presence tests, source-shape tests or tests for deleted code.
- Start PostgreSQL with `.\tools\postgres-dev.ps1 ensure` before PostgreSQL-backed validation; run `py -3 tools/run.py ci --check` as the final local gate.
- Before publication, independently assess the whole branch against the spec, ADRs, feature matrix, code-review runbook and current remote state; exact-head PR and develop-push hosted gates must pass before continuing.
- The completing PR retains this plan; the next successor plan retires it in its first substantive commit while preserving the spec and roadmap.

## Task 1: Commit the successor handoff and version identity

**Files:** `.agents/plans/2026-10-10-unify-world-map-read-surface.md`, `.agents/plans/2026-10-10-casebook-knowledge-without-inference.md`, `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, and `Directory.Build.props`.

- [x] Verify PR #245 merged from exact source `e983963ea7ad04ab0f7702ceac510d2e5f0fcb1f` as `bf5899b66c2c8f3f38e9dfb2f0341502f13ffab5`; exact-head gate `38047913069` and develop-push gate `38048254702` both passed.
- [x] Retire the completed `.60` plan in this successor and preserve its history in Git.
- [x] Update row 08 with PR #245 source/merge identities and both hosted gate IDs, point its active plan at this plan, and identify this `.61` casebook slice.
- [x] Set the sole authored application version to `0.1.0-dev.61`.
- [x] Commit this handoff before changing implementation source.

## Task 2: Preserve stable wanted-person identity through event history

**Files:** `src/WildBunch.Domain/Cases/CaseWarrants.cs`, `src/WildBunch.Domain/Cases/CaseFileSnapshot.cs`, `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`, `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`, `src/WildBunch.Persistence/DependencyInjection.cs`, a new `src/WildBunch.Persistence/Versioning/CaseFileGeneratedV2ToV3Upcaster.cs`, and `tests/WildBunch.Domain.Tests/CaseFileGeneratedEventTests.cs`, `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`, `tests/WildBunch.Integration.Tests/Versioning/UpcasterCorrectnessTests.cs`, and `tests/WildBunch.Integration.Tests/EventSourcingEndToEndTests.cs`.

- [x] Add nullable `Warrant.TargetSuspectId` as internal stable identity metadata; generated public warrants receive the exact roster suspect ID, while callers without an explicit association remain unknown.
- [x] Preserve the association in the Domain `WarrantSnapshot` and the Persistence component serializer; never derive it from `TargetName`, aliases, features or clue text.
- [x] Add and register the contiguous `CaseFileGenerated` v2-to-v3 upcaster, explicitly preserving old warrants with a null association; retain the existing v1-to-v2 step and derived-version policy.
- [x] First add a failing behavior test that generates a case and proves each poster’s target ID matches its independently selected suspect, then prove a fresh event replay retains that association.
- [x] Add a real v2 fixture to the production upcaster chain and prove its warrants remain readable without guessed associations, including two equal display names.
- [x] Run the focused Domain, GameContent and Integration lanes, including the PostgreSQL event serialization/replay path; do not alter SQL schema or event history beyond the payload-versioned association.

## Task 3: Project learned case facts without inference

**Files:** `src/WildBunch.Application/Games/Models/CaseReadDtos.cs`, `src/WildBunch.Application/Games/Models/GameDtos.cs`, `src/WildBunch.Application/Games/Mapping/CaseBoardMapper.cs`, `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`, `src/WildBunch.Application/Games/Mapping/JournalMapper.cs`, `src/WildBunch.Application/Games/Mapping/WantedPosterMapper.cs` only if its current consumer requires the same identity distinction, `src/WildBunch.Application/Projections/CaseFileViewProjector.cs`, `src/WildBunch.Application/Projections/CaseFileViewProjection.cs`, `tests/WildBunch.Application.Tests/Mappers/CaseBoardMapperTests.cs`, `tests/WildBunch.Application.Tests/Mappers/JournalMapperTests.cs`, and the relevant cases in `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs`, plus affected application and integration response-contract tests.

- [x] First add failing mapper behaviors for equal-name warrants with distinct stable IDs, settlement of only one identity, retained clue evidence after capture, and an unnamed feature observation that remains separate from a similarly described wanted record.
- [x] Reshape the casebook read contract to carry each known warrant and each learned clue independently with its authored display facts, provenance and structured anchors; remove resolved/possible-match links and feature/alias/route identity handles that have no player-authored association.
- [x] Mark a known warrant’s recorded alive/dead settlement only by matching its explicit `TargetSuspectId` to the settlement ID; do not expose internal suspect IDs in player DTOs, including the existing discovered-suspect read record that the Web client does not consume.
- [x] Keep captured clues and warrant detail visible in the casebook; preserve the separate active-poster list behavior, filter captures by stable identity, and leave unknown associations active.
- [x] Keep existing hidden-truth safety and actual source/time facts, while removing only tests that positively require auto-resolution or evidence disappearance; retain negative tests that prove unrevealed information remains private.
- [x] Remove the incomplete `CaseFileViewProjector` and `CaseFileViewProjection` because source search proves they have no production caller; retire only their behavior/source-shape tests and leave the live journal, HUD, full-audit and travel-day projectors intact.
- [x] Run focused Application behavior and API serialization tests; prove the player DTO ID exclusion fails when the ID is restored, and prove identity association fails when generator association is removed or name-based matching is restored.

## Task 4: Render the casebook as accumulated player knowledge

**Files:** `src/WildBunch.Web/src/api/types.ts`, `src/WildBunch.Web/src/components/CaseFileSurface.tsx`, `src/WildBunch.Web/src/ui/formatters.ts` only for retired inference labels, `src/WildBunch.Web/src/tests/CaseFileSurface.test.tsx`, `src/WildBunch.Web/src/tests/JournalSurface.test.tsx` only if its real consumer contract changes, and any additional consumer found by `rg` for the retired DTO fields.

- [x] First add a failing rendered behavior scenario with a learned unnamed clue, a known wanted record and a settlement; verify source text and clue anchors remain, captured/warrant detail remains, and the surface asserts no inferred identity.
- [x] Render the supplied casebook facts using component-owned styled contracts; remove automatic resolution, loose-lead and strongest-lead inference without adding a manual notes or deduction UI.
- [x] Keep alive/dead settlement copy tied to the recorded outcome and retain the overlay's accessible name, keyboard dismissal/focus behavior, responsive behavior and hidden-truth safety.
- [x] Prove same-name warrant records remain separate, only the explicitly settled record shows its recorded outcome, and an unnamed observation remains independent; the fixture contains both disputed facts and records.
- [x] Run the complete Web target, including lint/typecheck, all Vitest tests and production build. Repository search found no browser/e2e harness capable of a real API-backed casebook journey, so no browser journey is claimed.

## Task 5: Reconcile evidence, review and publish

**Files:** `docs/features.md`, `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`, `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`, and any ADR only if the diff reveals a durable decision was changed rather than implemented.

- [x] Add dated dispositions for AP-16/AP-17 and WB-20 that distinguish retired incomplete projection, retained player-known facts, stable identity settlement and remaining unsolved concerns; update test follow-ups to record removed hostage tests and the behavior replacing them.
- [x] Update PG-008’s assessment with the exact delivered evidence and remaining boundaries; keep it partial while the journey-journal narration and other casebook consumers remain unverified.
- [x] Re-read ADR-0005, ADR-0007, ADR-0009 and ADR-0028 against the actual diff. They remain unchanged because this slice preserves their case ownership, hidden-truth, authored-anchor and immutable-event/cache decisions; report that ruling in the PR.
- [x] Run PostgreSQL ensure, `py -3 tools/run.py ci --check`, `dotnet tool restore`, and `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`; confirm that no row 08 schema migration was introduced.
- [ ] Build a fresh whole-branch review package and obtain an independent review of the committed diff against this plan, the spec, selected unslop profiles, relevant ADRs and PG-008; resolve all Critical and Important findings and rerun the full gate.
- [ ] Publish the PR to `develop`, verify exact source SHA and body, set it ready for hosted validation, and wait for the exact-head PR gate to pass before merge.
- [ ] Merge only after review and hosted gate pass; verify the develop-push gate on the merge SHA, update row 08 with source/merge SHAs and both run IDs, and record the next row 08 target without prewriting its plan.
