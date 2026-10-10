# Persist Developer Audit Occurrence Metadata Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Make the developer audit report the occurrence timestamp and sequence stored with each event, so repeated reads of immutable history return the same recorded facts.

**Architecture:** Persistence owns stored event envelopes and decodes their typed event payloads. The Application repository boundary exposes a narrow recorded-event value containing the typed event, persisted sequence and persisted occurrence time. `FullAuditProjector` formats those records without consulting the clock; `GetSessionAuditHandler` preserves the existing session-existence and developer-only route behavior. Domain events remain free of storage metadata.

**Execution Strategy:** Execute inline and sequentially. Repository contract, production decoding, audit projection and HTTP proof share one metadata-pairing boundary, so separate implementers would duplicate context and create extra handoffs without useful independence.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md` (event-envelope ownership, strict query behavior and honest projections); `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 08; AP-14 in `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`; AP-14 test disposition in `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`.

## Delivery contract

- Start from `develop` merge `026e0badfba0170c67b02dc317b30b91b534a1c6` after PR #242; target `develop`.
- PR #242 source `46c022a29fdc424c0b130d77e94cdbd0311e07f4` merged to `develop` at `026e0badfba0170c67b02dc317b30b91b534a1c6`; exact-head gate `38029156057` and develop gate `38029484447` succeeded. It delivered `0.1.0-dev.57` and the sole player-facing journal.
- Retire the completed `.57` journal plan only after verifying its merged implementation and the hosted gate evidence above. Keep row 08 executing and point it at this successor plan.
- Advance the single authored version in `Directory.Build.props` to `0.1.0-dev.58` in this committed handoff before source implementation.
- Use a fresh canonical worktree from `develop`, commit this plan handoff before implementation, and publish a PR to `develop`.
- Require hosted canonical CI success on the exact PR head and exact develop merge commit before retiring this worktree and branch. Record actual source SHA, merge SHA, and gate runs in the next successor handoff.

## Design and scope

- The audit timestamp means the persisted event-envelope `OccurredAtUtc`, not a timestamp generated when a projection is read.
- The audit sequence means the persisted stream sequence. Preserve the existing event type and summary formatting and keep the output developer-only.
- Introduce an Application-owned recorded-event abstraction that carries only the typed event, sequence and occurrence time needed by this read. Do not expose `StoredEventEntity`, EF types, payload JSON, correlation IDs or schema details to Application consumers.
- Add a repository read that obtains ordered stored envelopes, decodes payloads through the existing production loader/upcaster path, and pairs each decoded event with that envelope's sequence and occurrence time. Retain `GetEventStreamAsync` for existing gameplay/replay consumers; avoid duplicating event-decoding behavior.
- Remove the clock read from `FullAuditProjector`. Do not retain an overload that accepts bare domain events and fabricates timestamps.
- Keep the existing 404 behavior for a missing session, the development-environment guard, the `/api/dev/sessions/{id}/audit` route, and the existing summary text.
- Do not add event fields, migrations, Domain timestamps, a new public route, or player-facing audit output. No ADR amendment is needed: this implements ADR-0028's existing separation between domain facts and persistence-owned envelope metadata.

**Execution ruling, 2026-10-10:** The first RED is the PostgreSQL-backed developer audit endpoint with independently assigned persisted timestamps. This proves the complete metadata path through the production decoder, projection and serialized response; a second repository-only forwarding test would repeat the same behavior without adding an independent oracle.

## Review focus

- Distinct fixed timestamps and sequences must survive persistence decoding, projection, handler mapping and the existing HTTP response unchanged.
- Reading the same immutable audit twice must return identical occurrence time and sequence even when the reads happen at different wall-clock times.
- A meaningful malformed/missing-session case remains a 404 and the non-development route guard remains a 403.
- Tests must fail if the projector substitutes `UtcNow`, renumbers a stream from one, or pairs a decoded event with another row's envelope metadata.
- Existing player journal, HUD and travel-day projections remain independent and unchanged.

## Tasks

### Task 1: Commit the `.58` successor handoff

**Files:** `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `Directory.Build.props`, `.agents/plans/2026-10-10-single-player-journal.md`, and this plan.

- [x] Verify PR #242 is merged at `026e0badfba0170c67b02dc317b30b91b534a1c6`, source `46c022a29fdc424c0b130d77e94cdbd0311e07f4`, and exact-head/develop gates `38029156057`/`38029484447` passed.
- [x] Record the completed journal outcome and PR evidence in row 08, keep the row executing, and point it at this next JIT plan.
- [x] Retire the completed `.57` journal plan and advance `Directory.Build.props` to `0.1.0-dev.58`.
- [x] Commit the planning handoff before changing production source or tests.

**Expected:** The roadmap represents the merged `.57` result accurately and the next bounded row 08 outcome has a committed implementation plan.

### Task 2: Carry persisted metadata through the audit read

**Files:** `src/WildBunch.Application/Abstractions/IGameSessionRepository.cs`, the new Application recorded-event abstraction, `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`, `src/WildBunch.Application/Projections/FullAuditProjector.cs`, `FullAuditProjection.cs`, `src/WildBunch.Application/Dev/Queries/GetSessionAuditHandler.cs`, `SessionAuditDto.cs`, `tests/WildBunch.Application.Tests/TestDoubles/InMemoryGameSessionRepository.cs`, `tests/WildBunch.Application.Tests/Execution/GameSessionCommandHandlerTests.cs`, audit projector/handler tests, `tests/WildBunch.Integration.Tests/Dev/DevEndpointTests.cs`, and `tests/WildBunch.Integration.Tests/EventSourcingEndToEndTests.cs`.

- [x] Add a PostgreSQL-backed behavior test through the existing developer audit route with independently assigned envelope timestamps; run it before implementation and confirm it fails because the read returns `UtcNow`.
- [x] Add the Application read value and repository method; decode using the production payload loader and return each typed event with its persisted sequence and `OccurredAtUtc`.
- [x] Keep `GetEventStreamAsync` behavior available for current replay and gameplay callers; avoid introducing a second event-decoding implementation.
- [x] Change the full-audit projector to consume recorded events only, preserve their stored sequence/time, and retain current summary text. Remove any bare-event overload that could fabricate metadata.
- [x] Map the existing audit handler and DTO to the recorded values without changing session-not-found or environment-guard behavior.
- [x] Add an independent projector assertion with a nontrivial sequence and fixed timestamp, and make the in-memory repository provide deterministic recorded metadata to its consumer tests.

**Expected:** The existing developer audit endpoint reports stable timestamps and original stream positions from persisted envelopes on repeated reads.

### Task 3: Reconcile dispositions and validate

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`, and all implementation/test files above.

- [x] Add a dated AP-14 disposition recording the metadata path and what AP-14's static finding no longer describes; retain the historical audit evidence and its audience boundary.
- [x] Update AP-14's test disposition to name the persisted-record pairing and repeated HTTP read behavior now protected; retain only tests with independent timestamp and sequence oracles.
- [x] Re-read ADR-0028 and verify this change follows its existing persistence-envelope ownership; leave the ADR and feature matrix unchanged unless source inspection establishes a durable decision or feature assessment changed.
- [x] Review the full diff against the spec, repository boundaries, event upcasting, audit authorization, test anti-pattern guidance and this plan.
- [x] Independent review found the endpoint test did not prove event-to-envelope pairing. The test now asserts event type against the persisted event type for each sequence; reversing the implementation's event pairing made it fail, and restoring the implementation returned the PostgreSQL-backed test to green.
- [x] Run `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter FullyQualifiedName~FullAuditProjector` and `dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~EventStorePersistenceTests|FullyQualifiedName~DevEndpointTests"`; ensure the local PostgreSQL test dependency is running before integration tests.
- [x] Use the check-only pre-commit hook as the canonical fail-fast gate. Do not rerun `py -3 tools/run.py ci --check` immediately before or after a successful hooked commit.
- [ ] Push and open a PR to `develop`; verify hosted canonical CI on the exact PR head, merge under the active epic authorization, and verify the exact develop push gate.
- [ ] Record actual source/merge SHAs and both hosted gate runs in the next successor handoff; sync the shared checkout and retire only this verified merged worktree and branch.

**Expected:** Developer audit timestamps and sequence are durable projections of stored event-envelope facts, tests distinguish correct metadata propagation from read-time fabrication, and row 08 remains accurately open for its other JIT slices.
