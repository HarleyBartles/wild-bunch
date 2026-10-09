# Retire Test-Only Whole-Session JSON Snapshot Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Remove the unsupported whole-session JSON snapshot API and its tests while preserving the production component, event, and event-derived repository paths.

**Architecture:** Production persistence writes versioned component snapshots beside immutable events and reconstructs through the repository's explicit event-aware path. The full `GameSession` JSON serializer is referenced only by tests and bypasses envelope/version/stream restoration, so remove that test-only path without changing `RestoreFromSnapshot` or production component serialization.

**Tech Stack:** .NET 10, EF Core, xUnit, Wild Bunch Persistence, PostgreSQL integration tests.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery” and “History and migration boundary”; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 07; persistence findings PS-13 and PS-14.

**Execution Strategy:** `executing-plans` - This is one sequential persistence-boundary cleanup: confirm consumers, retire the test-only snapshot contract and its tests, then verify retained repository and codec behavior.

## Rulings

- Ruling: Whole-session `Serialize(GameSession)` and `Deserialize(string)` are not a supported save format. Repository-wide call sites are tests only, while production persists component rows and reconstructs with envelope metadata plus event history. Remove this API and its `GameSessionSnapshot`; no prior local playtest save must be retained.
- Ruling: Preserve production component serializers, event codecs and upcasters, `RehydrateGameSession`, `GameSession.RestoreFromSnapshot`, and all repository cache-rebuild behavior. They serve the selected storage and replay path.
- Ruling: Remove only tests whose subject is the unsupported whole-session JSON representation. Keep meaningful repository persistence tests and component codec tests, including the supported CaseFile legacy component shapes. Do not replace deleted snapshot-shape assertions with source detectors or another serializer round trip.
- Ruling: Remove the unused `GameLogEntrySnapshot`, `JourneyTrailEventSnapshot`, and `TravelDiaryEncounterResolutionSnapshot` helpers after confirming no consumers. Remove the Domain test project's stale Persistence reference because no Domain test source uses it.
- Ruling: No ADR or feature-matrix change. ADR-0003's composed production component snapshots and ADR-0028/ADR-0038's event-authoritative reconstruction remain true; the removed serializer was not part of the production persisted shape or a player capability.
- Ruling: Advance the application identity once from `0.1.0-dev.45` to `0.1.0-dev.46`; the web build continues to derive its generated version from `Directory.Build.props`.

## Global Constraints

- Do not change PostgreSQL schema, migrations, event names/payloads/versions, component versions, replay semantics, cache validation, or public gameplay behavior.
- Preserve `GameSessionJsonSerializer`'s production component methods, generic component decoder, JSON options/converters, event serializer, event upcaster registry, and production event-aware restoration.
- Preserve Domain restoration needed by `GameSessionJsonSerializer.Rehydration.cs` and `EfGameSessionRepository`; do not remove `RestoreFromSnapshot` or snapshot restoration seams used by live persistence.
- Retire the completed `.45` upcaster plan only in this successor's first substantive commit, after checking its committed scope against PR #230 and its exact-head and develop gate evidence.
- Write changes on this fresh worktree from develop merge `9f9bba270e975453efdc57dd03f1f51650fcf129`; target `develop`, commit the plan before source edits, and preserve the `.46` identity through review, hosted validation, and merge.
- Keep the plan and roadmap live through PR completion. The next substantive successor assesses and retires this plan after verifying delivery evidence.

## Review Focus

- No production or test consumer requires whole-session JSON serialization after the obsolete tests are removed.
- Tests left behind prove real repository persistence or production component/event codec contracts, not the deleted aggregate snapshot shape.
- All three unused snapshot helper types and the unused Domain-test Persistence reference are removed without affecting retained callers.
- Event-aware restoration and the following legal command still work through the PostgreSQL repository path.
- Decision-record and feature-matrix review confirms no durable choice, supported save shape, player promise, or dependency changed.

---

### Task 1: Record PR #230 and commit this `.46` successor plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md`, and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; modify `Directory.Build.props`; delete `.agents/plans/2026-10-09-event-upcaster-registry-narrowing.md`; create this plan.

**Interfaces:** Record PR #230 source `691c0f7dfe4e9e2f965ec18c34f1d57cf0882d19`, merge `9f9bba270e975453efdc57dd03f1f51650fcf129`, exact-head hosted gate run `37995258466`, develop push gate run `37995831930`, and delivered identity `0.1.0-dev.45`. Row 07 remains executing and points to this `.46` plan.

- [x] Verify PR #230 is merged to `develop` at the source and merge SHAs above and both hosted runs passed on their respective exact commits.
- [x] Compare the `.45` plan's complete scope with the merged source and delivery evidence; record PS-12 as delivered, including the ECR Public PostgreSQL 16 mirror and unchanged event/projection persistence contracts.
- [x] Update row 07 with PR #230's exact source, merge, version, hosted runs and review outcome; select test-only whole-session JSON retirement as this successor's bounded PS-13 slice.
- [x] Update PS-13/PS-14 test dispositions with the selected consumer boundary and remaining production behavior proof. Leave unrelated schema/history compatibility work open.
- [x] Advance `Directory.Build.props` from `0.1.0-dev.45` to `0.1.0-dev.46`, remove the completed `.45` plan and stale roadmap link, and commit this plan before source edits.

**Expected:** Develop delivery evidence is accurate, the completed predecessor plan is retired, and the committed `.46` plan is the live row 07 pointer.

### Task 2: Retire the non-production whole-session snapshot path

**Files:** Modify `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.cs`, `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Travel.cs`, and `tests/WildBunch.Integration.Tests/GameSessionDifficultyPersistenceTests.cs`; delete `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs` and `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Log.cs`; modify `tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`; remove dated PS-13 dispositions for the retired surfaces from the persistence investigation and test follow-up.

- [ ] Recheck whole-repository call sites for `Serialize(GameSession)`, `Deserialize(string)`, `GameSessionSnapshot`, and the three dead helper types; classify each remaining use before editing.
- [ ] Remove the whole-session `Serialize`/`Deserialize` API and its private snapshot record while retaining generic `Deserialize<T>`, JSON options, converters, component serialization, event codecs, and explicit production rehydration.
- [ ] Remove only tests that exercise the unsupported whole-session snapshot representation. Retain the real repository difficulty/entropy round trip, repository town-visit scenarios, component-codec behavior, and supported legacy component-shape tests. Remove helper factories/imports made unused by these retirements.
- [ ] Remove `GameLogEntrySnapshot`, `JourneyTrailEventSnapshot`, and `TravelDiaryEncounterResolutionSnapshot` after confirming each has no consumer; retain the live domain-record serialization path.
- [ ] Remove the unused Persistence project reference and stale comment from the Domain test project. Do not add a test that merely asserts the serializer, type, or reference is absent.
- [ ] Append dated PS-13 implementation and test dispositions explaining the production call-graph boundary, the full-session path and tests removed, the production behaviors retained, and the unchanged event/cache restoration contracts. Leave ADR-0003, ADR-0028, and ADR-0038 unchanged.

**Expected:** Production persistence exposes only the component/event formats it actually stores, useful behavior coverage remains at repository and codec boundaries, and unsupported whole-session serialization is no longer an alternate test authority.

### Task 3: Verify, review, publish and integrate the `.46` slice

- [ ] Ensure local PostgreSQL, run the focused integration/domain selections that cover retained repository and event/component behavior, and inspect all remaining serializer call sites.
- [ ] Stage intended changes and let the check-only commit hook run the canonical fail-fast gate; verify generated web version identity is `0.1.0-dev.46`.
- [ ] Independently review the full branch diff against the accepted cache-recovery specification, ADR-0003, ADR-0028, ADR-0038, PS-13/14 and the testing follow-up. Confirm the PR author and reviewer decision-record and feature-matrix checks resolve with no durable changes.
- [ ] Publish a Draft PR to `develop`, verify its head matches local `HEAD`, mark it ready after review, and require the hosted canonical gate to pass on that exact head.
- [ ] Merge only after review and exact-head CI pass; verify the push-triggered gate passes on the resulting develop merge SHA. Retain this plan and the roadmap for the next substantive successor's custody review.

**Expected:** The `.46` retirement is merged with exact-head review and both hosted gates; event-authoritative repository reconstruction and retained component/event compatibility remain intact.
