# Row 17 N-16 Seed-Code Proof Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use executing-plans to implement this plan task-by-task inline under the authorized roadmap goal.

**Goal:** Replace weak seed-code test shapes with deterministic evidence that distinguishes raw UUID inputs, proves the resolver's representative-code producer against an independently authored fixture, and independently checks decoder behavior.

**Architecture:** `SeedWorldResolver` owns the existing UUID-shaped seed codec in GameContent. This slice changes only test fixtures and assertions; production generation behavior remains untouched. Fixed fixtures describe the current internal codec implementation and do not create a stable public bit-layout promise.

**Tech Stack:** C#/.NET 10, xUnit, `WildBunch.GameContent.Tests`.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), existing-behavior and test-quality decisions; [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17 and N-16 allocation; [whole-test quality findings](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), N-16; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), seed-code tests and helper.

**Execution Strategy:** executing-plans, inline and sequential. The two corrections share one codec boundary and independent test-fixture problem; retain the other N-16 entropy, world-generation, roster and map-test work for later JIT slices.

## Review Focus

- UUIDs that differ only in reserved bits must be asserted as unequal inputs before comparing their resolved facts; the resolver test in Task 2 owns both assertions.
- A producer/decoder round trip can hide a shared wrong bit position; Tasks 3 and 4 own fixed UUID fixtures for producer and decoder behavior independently.
- The internal fixture must not be misread as a public bit-layout guarantee; the ADR-0021 author/reviewer comparison in Tasks 1 and 4 preserves that boundary.

## Global Constraints

- Start from `develop` in this fresh worktree and deliver one PR back to `develop`; advance `Directory.Build.props` exactly once to `0.1.0-dev.131`.
- In the first substantive commit, record N-15 delivery evidence in the roadmap and retire `.agents/plans/2026-10-13-row17-n15-integration-negatives.md`; keep this N-16 plan as the active successor.
- Preserve the existing resolved seed-world semantics and public UUID-shaped input. Do not change production code, promise byte compatibility, or claim the full 128-bit UUID uniquely identifies a world.
- Use distinct actual `Guid` values for the distinct-input test, and compare only resolved world facts that intentionally ignore the input `SeedCode` identity.
- Prove the producer oracle fails when its encoded behavior is broken and the decoder oracle fails when its decoded behavior is broken; restore each mutation before continuing.
- Do not modify unrelated N-16 findings, add test-shape detectors, alter an ADR or feature promise, or add a compatibility rule absent from the specification.
- Before review, compare the diff against ADR-0021 through the decision-record playbook and state why the existing decision remains truthful; the bit-layout fixture is an internal test oracle only.
- Require the focused GameContent lane, full canonical repository gate, fresh whole-branch review and passing exact-head hosted CI before merge. After merge, require the develop-push gate before removing this plan's worktree and paired branch refs.

### Task 1: Record N-15 delivery and install this successor plan

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n15-integration-negatives.md`
- Add: `.agents/plans/2026-10-13-row17-n16-seed-codec.md`

**Interfaces:**
- Consumes: PR #315 source head `c2e34954c9ef20b70efa7a5914c386c1ad49313f`, squash merge `003a06f5b09dfe8183451adc945993220a7f6e43`, exact-head CI run `38336810087`, develop-push CI run `38337327045`, and verified N-15 worktree plus local/upstream branch removal.
- Produces: row 17 linked to N-16 at `0.1.0-dev.131`, a dated N-15 delivery record, and successor retirement of the N-15 plan.

- [x] **Step 1: Add N-15's proven delivery record.** Record PR #315, source and merge SHAs, both passing hosted gates, primary-checkout fast-forward, and removal/verification of the clean N-15 worktree and exact local/upstream refs. Preserve prior N-15 disposition text.

- [x] **Step 2: Advance the development version and retire the prior plan.** Change the single version authority from `.130` to `.131`, update row 17's active plan/version pointer, then delete the completed N-15 plan. Verify no other version values are hand-maintained.

- [x] **Step 3: Commit the active plan and successor handoff.** Check the roadmap pointer, successor record, deleted predecessor and `Directory.Build.props`; commit these planning artifacts before implementing the test changes. Run `git diff --check` and inspect the staged diff.

### Task 2: Make the distinct-UUID resolver case exercise distinct inputs

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldResolverTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldSeedCodeFactory.cs`
- Update call sites only if required: `tests/WildBunch.GameContent.Tests/GameSetupResolverTests.cs`, `tests/WildBunch.GameContent.Tests/SeedWorldFactoryTests.cs`, `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`

**Interfaces:**
- Consumes: the seed-code fixture helper and `SeedWorldResolver.Resolve(Guid)`.
- Produces: an honest behavior test proving two unequal UUID inputs with identical encoded world fields resolve to equal represented world facts, while their stored input IDs remain distinct.

- [x] **Step 1: Prove the current false premise.** Run the existing `MultipleUuidSeedsCanResolveToTheSameSeedWorld` test and inspect both generated GUIDs; demonstrate that the helper's `salt`/`tail` argument is ignored and the inputs are equal before changing the test.

- [x] **Step 2: Give the test fixture its stated input variation.** Rename the helper's unused `salt` argument to `highBits`; after `CreateRepresentativeSeedCode(target)`, copy the resulting UUID bytes, write `highBits` into bytes 8-15 with `BitConverter.TryWriteBytes`, and return the new UUID. The test must follow this shape:

```csharp
var seedA = CreateSeedCode(0, 1, 3, 0, highBits: 1);
var seedB = CreateSeedCode(0, 1, 3, 0, highBits: 2);
Assert.NotEqual(seedA, seedB);
var worldA = SeedWorldResolver.Resolve(seedA);
var worldB = SeedWorldResolver.Resolve(seedB);
Assert.Equal(worldA with { SeedCode = Guid.Empty }, worldB with { SeedCode = Guid.Empty });
```

Keep the existing resolver-field comparison only if it adds a useful diagnostic beyond the full record comparison with `SeedCode` normalized. Update helper call-site parameter names and retain their existing semantic field values.

- [x] **Step 3: Prove the test guards both claims.** Temporarily make the helper ignore its high-bit argument and confirm the raw-input inequality assertion fails. Then temporarily make the resolver consume that ignored portion into a compared world field and confirm semantic equality fails. Restore production code byte-for-byte; retain no mutation or production change.

- [x] **Step 4: Run all helper consumers and the focused resolver case.** Run `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter FullyQualifiedName~SeedWorldResolverTests` and the focused tests for every helper caller changed by the corrected helper signature.

### Task 3: Test the real representative-code producer against an independent fixture

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/NewGame/SeedWorldResolverCodecTests.cs`

**Interfaces:**
- Consumes: a fixed `SeedWorld`, `SeedWorldResolver.CreateRepresentativeSeedCode`, and the independent UUID decoder fixture.
- Produces: proof that the real producer emits the expected current internal UUID fixture and that decoding that fixture yields the selected `BuildingLayoutPalette`.

- [x] **Step 1: Replace the mislabeled producer test.** Start from `CreateCanonicalSeedWorld()` and set `BuildingLayoutPalette` to `NoSpurs_ClusterMiddle`. Call `CreateRepresentativeSeedCode` and assert the result is the independently calculated fixed UUID `2020c0c4-0000-0000-0000-000000000000`; resolve that same fixed UUID and assert `NoSpurs_ClusterMiddle`. The canonical input fields are variant 0, accusation 1, culprit 3, cash 0, town count 8, prosperity 0, reserved name bits 1, cluster count 1, sparse graph, outlier 0, and layout palette 1. Do not manually set producer bits inside the test or use `Guid.NewGuid()`.

- [x] **Step 2: Keep the independent decoder coverage deterministic.** Retain the direct palette-decoder test as a decoder-owned behavior check, replacing random starting UUID material with a fixed input fixture. Keep its expected palette values independent of the production producer.

- [x] **Step 3: Prove both codec directions are observed.** Temporarily change the producer's palette encoding and confirm the exact producer fixture assertion fails. Restore it, temporarily change the decoder's palette extraction and confirm the independent decoder assertion fails. Restore source byte-for-byte and confirm there is no production diff.

- [x] **Step 4: Run the focused codec lane.** Run `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter FullyQualifiedName~SeedWorldResolverCodecTests` and require all resolver codec cases to pass deterministically.

### Task 4: Remove codec constants and arithmetic self-checks while retaining decoder behavior

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldResolverCodecTests.cs`

**Interfaces:**
- Consumes: the explicit fixed UUID fixture `2020c0c4-0000-0000-0000-000000000000` and the current `SeedWorld` fields it represents.
- Produces: an independent decoder assertion for the full represented world, while remaining checks protect actual producer/decoder, range and rejected-input behavior.

- [x] **Step 1: Add a fixed decoder fixture before removing weak tests.** Add `Resolve_DecodesFixedSeedWorldFixture` using the literal UUID above and a separately constructed expected `SeedWorld` with `Canonical`, town count 8, reserved name bits 1, `UniformProsperous`, one cluster, sparse graph, accusation 1, culprit 3, cash 0, outlier 0 and `NoSpurs_ClusterMiddle`. Compare the complete record with `SeedWorldResolver.Resolve(seedCode)`; do not call the producer or use a value returned by the resolver to build the expected record.

- [x] **Step 2: Remove only unowned constants and implementation-shape checks.** Delete `ResolverContractVersion_IsV17`, `TownCount_Bounds_Are5To10`, `TownCount_ModuloWrapping_ClampsToDefinedRange`, `ProsperityPalette_ModuloWrapping_ClampsToDefinedRange` and `OutlierSlotType_BitPosition_Is27_28`. Keep actual town-count round trips, full field round trip, reserved-bit round trips, outlier encode/decode round trip and invalid-outlier validation. Prosperity values remain covered by `SeedWorldResolverTests.SeedWorldResolution_DecodesAllProsperityPaletteValues`; legal town bounds remain exercised by resolver and validation behavior, not constants alone.

- [x] **Step 3: Prove the decoder fixture catches a wrong field.** Temporarily decode cash bonus from an incorrect low-bit position, run only `Resolve_DecodesFixedSeedWorldFixture`, and confirm the full expected record assertion fails. Restore `SeedWorldResolver.Resolve` byte-for-byte; retain no production change.

- [x] **Step 4: Run the focused codec lane.** Run `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter FullyQualifiedName~SeedWorldResolverCodecTests` and require the independent fixture and retained real codec tests to pass.

### Task 5: Reconcile N-16 progress and close the delivery

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Keep active: `.agents/plans/2026-10-13-row17-n16-seed-codec.md`

**Interfaces:**
- Consumes: completed producer/decoder tests and the honest distinct-UUID resolver test.
- Produces: a scoped N-16 disposition that marks only seed-code identity/codec proof complete while leaving the unrelated N-16 entropy, roster, layout and map concerns active for later plans.

- [x] **Step 1: Reconcile only the covered N-16 subfindings.** Update the investigation's dated disposition and roadmap finding coverage to describe the corrected helper, distinct input proof, producer fixture and deterministic decoder check. Do not mark N-16 or row 17 complete.

- [x] **Step 2: Run focused and canonical validation.** Re-run the focused GameContent test selection and `py -3 tools/run.py ci --check`; require the full gate to pass and report its existing skipped test explicitly. The focused selection passed 51 tests; the canonical gate passed with 535 Domain, 161 Application, 184 GameContent, 331 Integration, 1 API, and 376 Web tests, plus the one existing skipped Domain test.

- [ ] **Step 3: Review and publish.** Run the PR author decision-record and feature-matrix checks, then obtain a fresh whole-branch review. Create one PR against `develop`, verify exact head and hosted gate success, and merge only after that gate passes.

- [ ] **Step 4: Close out after the develop gate.** Fast-forward the primary checkout to the merged commit and verify the develop-push gate passes. Remove the clean N-16 worktree before deleting only its exact local and upstream branch refs; verify the worktree and both refs are absent before starting another plan.
