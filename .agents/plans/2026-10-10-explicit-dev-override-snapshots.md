# Explicit Developer Override Snapshots Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Replace direct serialization of pending developer override Domain records with explicit Persistence-owned snapshots while preserving current component JSON and event-backed behavior.

**Architecture:** `GameSessionJsonSerializer.Components.cs` directly serializes `DevTravelOverride` and `DevSaloonOverride` as component caches. Introduce focused snapshot types with explicit `FromDomain` and `ToDomain` mappings. Map `DevTravelOverride.FoeProfile` through the existing `JourneyFoeProfileSnapshot` and map `DevSaloonOverride.ForcedSuspectId` through a snapshot retaining its current nested `value` shape. These component rows remain caches of event-established aggregate state. Do not change event payloads, Domain behavior, public APIs, component names or versions, projection versions, schema, or migrations.

**Tech Stack:** .NET 10, C#, EF Core, PostgreSQL, xUnit.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery”; row 07 of `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; PS-14 in `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md` and its test follow-up.

**Execution Strategy:** `executing-plans`, inline and sequential. The two serializers share one Persistence file and concern: preserving the current v1 component boundary for pending playtest overrides. Repository tests and literal payload tests provide distinct behavior and compatibility evidence without splitting the slice.

## Global Constraints

- Start from develop merge `7e861aa86cd91783f7741f63dba620de226b00af` in this fresh linked worktree and target `develop`.
- Commit this JIT plan, record PR #235 and its exact merge/CI evidence, retire the completed `.50` plan, and advance `Directory.Build.props` once from `0.1.0-dev.50` to `0.1.0-dev.51` before source edits.
- Preserve the current camel-case property names, numeric enum values, nested `foeProfile` fields, nested `forcedSuspectId.value` object, optional values, and null-removal behavior.
- Keep override state event-established and snapshots derivative. Do not add Domain setters, change event sourcing, expose developer controls, make controls a public release promise, or expand unrelated dev tooling.
- No ADR or feature-matrix change is expected because persisted behavior and player-facing capability remain unchanged; record the PS-14 test and disposition evidence in the investigation owners.

## Review Focus

- **Pending override component contract drift:** A Domain refactor must not silently alter either component's persisted field names, enum representation, nested value-object shape, or optional-field values. Protect the existing v1 contract with literal payload fixtures.
- **Lost pending state after fresh repository load:** Both travel and saloon overrides are event-backed state with persisted component caches. Verify actual PostgreSQL save/load returns the exact pending values and preserves their existing use by the normal aggregate action path.
- **Unnecessary scope expansion:** Only the two direct Domain-record serializers and their owning Persistence tests change. The event codec, Domain model, gameplay, HTTP exposure, schema, component versions, feature matrix, and ADRs remain unchanged.

---

### Task 1: Record PR #235 and commit this `.51` successor plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-10-explicit-travel-persistence-snapshots.md`; create this plan.

**Interfaces:** Record PR #235 source `bc6f90f84664f00fe153ae85886e7ec38d8b4f6c`, merge `7e861aa86cd91783f7741f63dba620de226b00af`, exact-head hosted canonical gate run `38012169295`, develop push gate run `38012463462`, and delivered identity `0.1.0-dev.50`. Row 07 remains executing and points to this `.51` plan.

- [x] Verify PR #235 is merged to `develop` at the stated source and merge SHAs and both hosted gates passed on those exact commits.
- [x] Record PR #235's explicit travel and diary nested Persistence snapshots, v1 payload compatibility, dated PS-14 dispositions, and disclosed self-review fallback in row 07; append PR #235 to the merged-PR list.
- [x] Retire the completed `.50` plan and update the dated handoff so the remaining PS-14 work is limited to direct pending developer-override component serialization.
- [x] Advance `Directory.Build.props` to `0.1.0-dev.51` and commit this plan before source edits.

**Expected:** The roadmap records verified PR #235 delivery, the `.50` plan is retired, and this committed plan is row 07's live pointer before implementation begins.

### Task 2: Map pending override values through explicit Persistence snapshots

**Files:** Modify `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs` and `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; append dated PS-14 dispositions to `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md` and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`.

**Interfaces:** Add private Persistence snapshot types for `DevTravelOverride`, `DevSaloonOverride`, and the nested `SuspectId` value. Map `JourneyFoeProfile` through `JourneyFoeProfileSnapshot`; map every override field both directions and preserve the old component JSON property names and null semantics. Keep the component methods and their consumers unchanged.

- [x] Add literal v1 component payload tests for a travel override with a populated foe profile and a saloon override with a suspect ID; assert exact enum, nested values, optional message/role fields and IDs after decode.
- [x] Add PostgreSQL repository save/load tests for both pending override values; compare the actual aggregate pending state before and after a fresh load and exercise each existing next-action path to prove it remains the same behavior.
- [x] Run the literal and repository behavior tests against the direct-serialization implementation first; the travel repository case correctly produces `Interrupted` while its forced foe encounter is pending, so its assertions prove the pending encounter facts rather than mistaking the interruption for a failed action.
- [x] Implement the explicit snapshots without changing component names, values, null behavior, event payloads, or component/projection versions. Corrupting the foe-profile mapping made the travel v1 fixture fail; corrupting the suspect-ID mapping made the saloon decoded-value assertion fail. Both were restored and the focused suite passed.
- [x] Run the focused PostgreSQL and codec tests plus `ProjectionVersionCompletenessTests`; all six tests passed.
- [x] Append dated PS-14 dispositions distinguishing explicit component snapshots from typed event and world snapshot serialization; source call-site review found no direct component serialization of these Domain override records.
- [x] Recheck ADR-0028 and `docs/features.md`; both remain unchanged because no durable decision or player-facing contract changes.

**Expected:** The two pending override component payloads retain their existing v1 JSON contract through explicit Persistence mappings, and a fresh PostgreSQL aggregate load retains the same pending values and next-action behavior.

### Task 3: Validate, review and publish to `develop`

- [x] Run `dotnet tool restore` and `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`; no new migration was generated. The local database reports two existing repository migrations pending.
- [x] Run the focused PostgreSQL and serializer behavior tests against the final candidate.
- [x] Run the canonical fail-fast `py -3 tools/run.py ci --check` gate; it passed on the candidate before commit.
- [x] Inspect the full branch diff, actual v1 component payload compatibility, both pending override continuations, PS-14 dispositions, ADR-0028, feature matrix, and applicable persistence/code-review unslop profiles. Reviewer-agent dispatch is unavailable under the current session constraints, so this slice received inline self-review and will disclose that in the PR.
- [ ] Stage the reviewed candidate and let the normal check-only commit hook pass before committing.
- [ ] Publish a Draft PR to `develop`, reconcile its remote head with local `HEAD`, correct and reread the PR body, mark it ready, and require hosted canonical CI to pass on that exact head before merging.
- [ ] Merge to `develop`, verify the push gate passes on the exact merge SHA, fast-forward the main checkout, and retire this plan in the next substantive successor after verifying delivery evidence.

**Expected:** `.51` is merged to `develop` with exact-head hosted validation; the two pending developer-override components use explicit Persistence snapshots without changing their v1 contract or behavior.
