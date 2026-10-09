# Event Upcaster Registry Narrowing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Make the persistence upcaster contract represent only supported event upcasting while projections continue to rebuild through their existing projection-version policy.

**Architecture:** `IEventUpcaster` becomes the sole extension contract accepted by `PayloadUpcasterRegistry`. The registry stores event chains by payload type, validates duplicate and non-contiguous transitions at construction, and preserves event version lookup and ordered transforms. Remove projection-kind scaffolding and the reflection test that fabricates a chain the constructor cannot produce; do not change persisted event shapes, projection rebuilds, or schema.

**Tech Stack:** .NET 10, EF Core, xUnit, Wild Bunch Persistence and PostgreSQL integration test lane.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery”; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 07; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md`, PS-12.

**Execution Strategy:** `executing-plans` - Keep the registry contract, its chain tests and production DI together because they form one private persistence extension boundary. Native inline execution is the best fit; a separate implementation lane would split tightly coupled source and test changes without gaining independent verification.

## Rulings

- Ruling: Persisted projection components and travel-diary rows continue to use `ProjectionVersions` and rebuild from authoritative events; no projection upcaster is introduced because that is the selected invalid-cache path. Cost if wrong: a future independently versioned projection shape would need an explicit migration policy before its old payload could be loaded.
- Ruling: Event upcasters remain necessary compatibility machinery. Preserve the production `WorldGeneratedV1ToV2Upcaster`, event versions, payload transforms, and fail-closed handling of unknown or future data. Cost if wrong: supported historical events would stop loading.
- Ruling: Registry construction owns the invariant that every event chain starts at version 1 and has no duplicate or missing transition. Remove the runtime missing-transition branch and its reflection-mutated test because private registry state cannot bypass construction; prove duplicate rejection and an interior gap through the constructor. Cost if wrong: an unsupported future mutation of private chain state would surface as a missing-key error rather than the current domain-specific message.
- Ruling: Keep the existing `PayloadUpcasterRegistry` name to avoid a mechanical rename across unrelated persistence fixtures. Narrow its accepted interface and internal key shape without changing consumers' event-loading behavior. Cost if wrong: the broader class name remains slightly less specific than its event-only input contract.
- Ruling: No ADR changes. This removes unused implementation scaffolding and preserves ADR-0028's existing event-authoritative compatibility and projection-rebuild decision.
- Ruling: No player feature promise, dependency, or release disposition changes. Record the prior PR #229 delivery evidence in the feature matrix; retain PLAT-001's current assessment.

## Global Constraints

- The immutable event stream remains authoritative. Event upcasting changes the representation consumed by replay, not the recorded historical fact or stream schema.
- Projection components and diary rows continue to rebuild through their current `ProjectionVersions` path; do not add projection upcasters or change projection versions.
- Preserve `WorldGeneratedV1ToV2Upcaster`, production DI registration, ordered multi-step transforms, duplicate and gap rejection, and fail-closed future/unknown event behavior.
- Do not change event types, event payloads, migrations, database schema, command or query semantics, or cache validity policy.
- Start from the verified `develop` merge `7c58f8119ab7a81f9b2879dee634e1a2762c6c3f` in a fresh worktree; commit this plan before implementation and retire the completed predecessor plan in this first successor commit.
- Advance the sole authored application version in `Directory.Build.props` from `0.1.0-dev.44` to `0.1.0-dev.45` once for this implementation PR. The normal web build generates `src/WildBunch.Web/dist/version.json`; the canonical check gate verifies it against `Directory.Build.props`.
- Target `develop`; require a fresh whole-branch review, `Canonical tracked commit gate` success on the exact final PR head, merge through the PR, and push-triggered gate success on the resulting `develop` merge SHA before beginning another slice.

## Review Focus

- Duplicate event transitions and an interior missing transition must fail during registry construction through reachable inputs.
- A valid multi-step chain must still transform stored v1 JSON through each ordered step to the current event shape.
- Production DI must still register every concrete event upcaster, and the production payload loader must retain its event-version behavior.
- A future stored event version must still fail closed; projection cache-version mismatch must still rebuild rather than enter an upcaster path.

---

### Task 1: Record PR #229 and create the `.45` successor plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`, `docs/features.md`, and `Directory.Build.props`; delete `.agents/plans/2026-10-09-snapshot-tail-retirement.md`; create this plan.

**Interfaces:** Record PR #229 source `854e8f119d90e891905ad2fa368efea38579aa2d`, merge `7c58f8119ab7a81f9b2879dee634e1a2762c6c3f`, exact-head PR gate run `37988802976`, post-merge push gate run `37989496794`, and delivered version `0.1.0-dev.44`. The roadmap keeps row 07 executing and points to this `.45` successor.

- [x] Verify PR #229 is merged to `develop`, its source and merge SHAs match the values above, the PR gate passes on the source SHA, and the post-merge push gate passes on the merge SHA.
- [x] Compare the `.44` plan with the merged source and its behavior test; classify command snapshot-tail retirement and stale-cache continuation as delivered, with query full replay unchanged.
- [x] Update the roadmap, persistence test follow-up and feature matrix with PR #229's exact merge, validation and review evidence; state that PLAT-001's assessed capability remains partial and the feature truth is unchanged.
- [x] Remove the completed `.44` plan and its stale roadmap link in this successor's first substantive commit. Leave PS-12 and unrelated persistence findings open until implementation.
- [x] Advance `Directory.Build.props` from `0.1.0-dev.44` to `0.1.0-dev.45`; verify this plan names the generated web identity check and commit the plan snapshot before source edits.

**Expected:** `develop` delivery evidence for PR #229 is accurate, its completed plan is retired, and this committed `.45` plan is the live row 07 pointer.

### Task 2: Narrow registry and test the reachable event-chain contract

**Files:** Rename `src/WildBunch.Persistence/Versioning/IPayloadUpcaster.cs` to `src/WildBunch.Persistence/Versioning/IEventUpcaster.cs`; modify `src/WildBunch.Persistence/Versioning/PayloadUpcasterRegistry.cs`, `src/WildBunch.Persistence/DependencyInjection.cs`, `tests/WildBunch.Integration.Tests/Versioning/UpcasterCorrectnessTests.cs`, `tests/WildBunch.Integration.Tests/Versioning/UpcasterChainCompletenessTests.cs`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md`, and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`.

**Interfaces:** Make `IEventUpcaster` carry `PayloadType`, `FromVersion`, and `Upcast`; remove `IPayloadUpcaster` and `PayloadKind`. `PayloadUpcasterRegistry` accepts `IEnumerable<IEventUpcaster>` and stores `SortedDictionary<int, IEventUpcaster>` chains keyed by payload type. Keep its event-facing `CurrentVersion` and `Upcast` signatures unchanged.

- [ ] Change `Registry_NonContiguousChain_ThrowsAtConstruction` to include v1-to-v2 and v3-to-v4 upcasters with the missing v2-to-v3 transition; add `Registry_DuplicateTransition_ThrowsAtConstruction` using two v1 transitions.
- [ ] Run `.\tools\postgres-dev.ps1 ensure`, then run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~UpcasterCorrectnessTests|FullyQualifiedName~UpcasterChainCompletenessTests|FullyQualifiedName~VersionMismatchBehaviorTests" --no-restore` against the current implementation. Prove each negative test can fail by temporarily bypassing its corresponding duplicate or contiguous-chain guard. Restore production code and rerun the same focused command.
- [ ] Change `IEventUpcaster` to declare the event properties directly; remove `IPayloadUpcaster` and `PayloadKind`.
- [ ] Key registry chains by event payload type alone, accept only `IEventUpcaster`, and validate every chain during construction. Keep duplicate detection, v1-start/contiguous validation, future-version failure and ordered transforms.
- [ ] Remove the runtime missing-transition branch that can only be reached by mutating the private chain after validation. Remove `Upcast_MissingUpcasterInChain_ThrowsAtRuntime` and its reflection fixture; do not add a projection-upcaster path to retain the test.
- [ ] Remove `RegisteredPayloadTypes` and its nonbehavioral nonempty assertion; keep the behavior test that every concrete `IEventUpcaster` in the Persistence assembly appears in DI and ensure registry construction validates registered chains.
- [ ] Change `CreateDefaultUpcasters` to return event upcasters and update stale comments that claim event upcasting is still hypothetical. Keep the payload loader and repository constructor behavior unchanged.
- [ ] Append dated PS-12 and test-follow-up dispositions explaining the removed projection capability, unreachable private-state test, preserved constructor and event-chain guarantees, and unchanged projection rebuild. Leave ADR-0028 unchanged.
- [ ] Run the focused upcaster/version selection through the command bus, then stage intended changes and let the normal check-only commit hook run the complete fail-fast gate. Confirm its web build generated `src/WildBunch.Web/dist/version.json` with `0.1.0-dev.45`; do not run `ci --check` immediately before the hooked commit.

**Expected:** Only event upcasters can enter the registry. Duplicate or incomplete chains fail at construction, valid event chains remain ordered and versioned, production event loading remains intact, and projection caches continue to rebuild from events through their existing path.

### Task 3: Review, publish and integrate the `.45` slice

- [ ] Independently review the full plan-to-head diff against the cache-recovery specification, ADR-0028, ADR-0038, backend-architecture and testing guidance; repeat the decision-record and feature-matrix checks.
- [ ] Publish a Draft PR to `develop`, verify its head matches local `HEAD`, include the `.45` generated version identity validation, then mark it ready and require the hosted canonical gate to pass on that exact head.
- [ ] Merge only after review and exact-head CI pass; verify the push-triggered canonical gate passes on the resulting merge SHA. Keep this plan and the roadmap/spec live for retirement in a later substantive successor.

**Expected:** The `.45` event-upcaster narrowing is merged to `develop` with exact-head review and hosted-gate proof; event and projection compatibility remain in their selected owners.
