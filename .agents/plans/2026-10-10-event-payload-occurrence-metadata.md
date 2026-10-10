# Remove Volatile Event Occurrence Timestamps From Payloads Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Event payloads record immutable domain facts only; the three events that currently serialize a fresh `OccurredAt` value stop embedding that volatile getter while historical payloads remain readable.

**Architecture:** Persistence already records event timestamp metadata in `StoredEventEntity.OccurredAtUtc`, but `WorldGenerated`, `CaseFileGenerated`, and `StartingTownSelected` also serialize `OccurredAt => DateTimeOffset.UtcNow` into their payloads. Remove those non-fact properties, add one event upcaster for each payload-shape transition, and keep the persisted envelope timestamp unchanged. No event fact, replay result, query path, database schema, or migration changes.

**Tech Stack:** .NET 10, C#, EF Core, PostgreSQL, System.Text.Json, xUnit.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, architecture and event-history invariants; row 07 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; PS-14 in `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md`.

**Execution Strategy:** `executing-plans` inline and sequentially. The payload test, three upcaster transitions, Domain event cleanup, and production-registration proof are coupled by event type and version; they share one PostgreSQL and replay oracle. Separate per-task implementers would reconstruct the same event contract repeatedly without independent approval value.

## Global Constraints

- Start from `develop` merge `f450224b9f03fdf9b18f9fb2a33d153ccbf0810c`, the verified PR #239 merge; target `develop`.
- Record PR #239 source `bd757f532e839ac59b9bf8416598818b4003353d`, merge `f450224b9f03fdf9b18f9fb2a33d153ccbf0810c`, exact-head gate `38020962110`, develop push gate `38021242879`, and delivered `0.1.0-dev.54`. Retire the `.54` plan, point row 07 to this plan, record PR #239 delivery, and advance `Directory.Build.props` to `0.1.0-dev.55` in the planning commit.
- The current event versions are `WorldGenerated` v2, `CaseFileGenerated` v1, and `StartingTownSelected` v1. The new current versions are derived only from registered upcasters: v3, v2, and v2 respectively.
- Existing v1/v2 payloads are immutable history. Upcasters transform them in memory for the current Domain event; never rewrite stored event rows or discard domain facts.
- `StoredEventEntity.OccurredAtUtc` remains the persistence-owned recorded timestamp. Remove only the computed `occurredAt` JSON property from these three event payloads.
- Preserve strict CQRS, event replay, current event facts, all other event payload shapes, fail-closed malformed/future-version behavior, and the existing WorldGenerated v1-to-v2 `caseFile` transition.
- Do not add a database migration, table, hand-edited event-version registry, alternate event serializer, or separate event store.

## Review Focus

- A current write has no `occurredAt` fact in these payloads while the persistence envelope still has its recorded timestamp; prove both at the PostgreSQL repository boundary.
- Historical WorldGenerated v1 payloads still gain `caseFile: null` and then shed only `occurredAt`; v2 payloads preserve a populated caseFile. CaseFileGenerated and StartingTownSelected historical facts also survive their timestamp-removal upcasters.
- Malformed roots and future event versions remain fail-closed through the established loader and registry.

---

### Task 1: Record PR #239 and commit the `.55` successor plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-10-salt-source-cache-recovery.md`; create this plan.

- [ ] Verify the exact PR #239 source and merge SHAs and both hosted gate runs listed above.
- [ ] Record `.54` delivery and select the bounded PS-14 event-payload timestamp correction in row 07 and its delivery history.
- [ ] Retire the completed `.54` plan and advance the single authored application version to `0.1.0-dev.55`.
- [ ] Commit this planning handoff before editing production code or tests.

**Expected:** The roadmap points to this plan, records the verified `.54` delivery, and this plan states the exact event versions, preservation contract, and proof boundary.

### Task 2: Keep volatile occurrence time out of event facts

**Files:** Modify `src/WildBunch.Domain/Events/WorldGenerated.cs`, `CaseFileGenerated.cs`, and `StartingTownSelected.cs`; create `WorldGeneratedV2ToV3Upcaster.cs`, `CaseFileGeneratedV1ToV2Upcaster.cs`, and `StartingTownSelectedV1ToV2Upcaster.cs` under `src/WildBunch.Persistence/Versioning`; register them in `DependencyInjection.cs`; add versioning tests under `tests/WildBunch.Integration.Tests/Versioning`; add a PostgreSQL repository test to `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; update the stable-baseline spec, ADR-0028, and persistence investigation/test follow-up.

- [ ] Add the PostgreSQL behavior test first. Create and persist a real setup session through the production repository, select its `WorldGenerated`, `CaseFileGenerated`, and `StartingTownSelected` rows, and assert each current payload omits `occurredAt`, has its derived current schema version, and retains a non-default envelope `OccurredAtUtc`.
- [ ] Run the focused PostgreSQL test before production changes and witness the intended RED: all three stored payloads currently contain a serializer-generated `occurredAt` and use the old event versions.
- [ ] Add minimal v2-to-v3 WorldGenerated, v1-to-v2 CaseFileGenerated, and v1-to-v2 StartingTownSelected upcasters. Each must require an object root, remove only `occurredAt`, and preserve every other JSON property. Keep WorldGenerated v1-to-v2 registered first so v1 continues through both transitions.
- [ ] Remove the three `OccurredAt => DateTimeOffset.UtcNow` properties from Domain events and register all new upcasters through `CreateDefaultUpcasters`; do not add a separate version registry.
- [ ] Add literal historical payload tests through the production `PersistedPayloadLoader`/registered chain. Prove WorldGenerated v1 and v2 inputs preserve seed, salt, entropy, world and caseFile facts while only the v1 transition supplies missing `caseFile: null`; prove CaseFileGenerated and StartingTownSelected retain their event facts. Keep malformed-root and generic future-version rejection behavior intact.
- [ ] Run focused versioning and PostgreSQL tests; prove a fresh repository load reconstructs the same event-established session facts while the stored envelope timestamp remains unchanged.
- [ ] Add a dated stable-spec clarification that event payloads contain established facts and persistence metadata stays in the envelope; add a dated ADR-0028 clarification because the implementation previously diverged from its event/envelope boundary. Record the implementation and tests in the existing persistence investigation owners.
- [ ] Commit the implementation and evidence updates; allow the normal check-only pre-commit hook to run the canonical gate.

**Expected:** New writes store the three event fact payloads without recomputed time values at current versions; historical payloads load through the registered upcaster chain without losing facts; the persistence envelope continues to own the recorded timestamp. No migration or product-facing behavior change occurs.

### Task 3: Verify, review and publish the slice

**Files:** All implementation and planning paths in Tasks 1-2.

- [ ] Ensure shared PostgreSQL with `.\tools\postgres-dev.ps1 ensure`. Run focused timestamp-upcaster, legacy WorldGenerated, and repository tests. Run `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`; no new migration is expected.
- [ ] Review the complete branch against `develop`, event-sourcing doctrine, ADR-0028, feature-matrix obligations, and this plan. Resolve Critical and Important findings with witnessed RED/GREEN cycles. Under the runtime's no-subagent instruction, disclose author self-review if separate reviewer dispatch is unavailable.
- [ ] Let the normal check-only pre-commit hook run the canonical staged-candidate gate; do not rerun the full local gate immediately before or after a successful hooked commit. Hosted CI must pass on the exact PR head and develop merge commit.
- [ ] Push and open a PR to `develop`; verify the remote head SHA, PR body, decision/feature checks, and exact-head hosted gate. Merge as authorized by the epic and verify the exact develop push gate.
- [ ] Record actual source/merge SHAs and hosted gate evidence in the roadmap through the next successor planning handoff, fast-forward `Z:\wild-bunch` to merged `develop`, then remove only this verified merged worktree and its local/remote branch.

**Expected:** The PR merges to `develop` at `0.1.0-dev.55` with both exact-head and merge hosted gates green; row 07 points to the next JIT outcome.
