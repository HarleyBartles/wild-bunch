# Retire the incomplete Rifle shell

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Stable 0.1.0 exposes one coherent firearm path: the revolver and its ammunition. Remove the incomplete Rifle/Rifle-ammunition capability from current play, including inventory, store, travel, event projections, API and browser contracts. Preserve the full feature as a named future capability in the feature matrix.

**Architecture:** Domain remains the gameplay authority. The Rifle shell currently crosses `ItemKind`, `Inventory`, store catalogs, inventory capabilities, travel runtime and theft, aggregate event creation, diary projection, Application DTO mapping, API JSON and Web formatting. Remove its runtime/domain items rather than masking them in the UI. Current event and component JSON encode enum numbers, so values 8 and 9 cannot simply disappear while existing pre-alpha GameSessions remain. Add an explicit forward migration deleting pre-alpha GameSessions; their FK-cascaded event streams, snapshots, diary projections and startup-operation rows are discarded. Do not drop or recreate the database, rewrite events, or add an upcaster that falsifies immutable history. Preserve the existing schema migration chain and fail explicitly if asked to reverse the data-discard migration.

**Spec and row:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, PG-004; `.agents/investigations/stable-0.1.0/2026-10-07-store-and-inventory-boundaries.md`; roadmap row 10. The future complete rifle story is PG-004-B in `docs/features.md`.

**Execution Strategy:** Native inline execution. Domain legality, replay-breaking persisted enum removal, public DTO changes, browser fixtures and a PostgreSQL data migration are coupled around one retirement boundary. A continuous TDD implementation with a fresh whole-branch review is safer than separate PRs that would temporarily leave the shell reachable or incompatible.

## Constraints

- Keep current store, prosperity-driven prices/availability, revolver acquisition, revolver ammo stacking, revolver travel combat and all unrelated goods unchanged.
- Remove Rifle and Rifle-ammunition as current Domain item kinds. Do not reuse their numeric values 8 and 9 for another item in this slice.
- `FirearmThreatAvailable` and `GunfightCapable` derive from the existing revolver path only. Remove the unused `RifleUsable` read capability instead of returning a permanently false product field.
- Travel ammo accounting, theft, spending and journal projection use only revolver ammo. Do not add weapon-specific damage or any new combat behavior.
- A purchase request with retired numeric item kind values must fail without changing wallet, inventory, or event history.
- The explicit data migration may discard pre-alpha playthroughs and is not reversible. It must preserve the database schema and migration history, and leave the application able to store a new session afterward.
- Record the complete future rifle feature, including the ways it depends on PG-004 and PG-007. Do not turn that backlog into implementation in this slice.
- Do not add an ADR: this is the settled product disposition and implementation retirement, with no new architecture decision. Verify ADR-0013 and ADR-0028 remain truthful.
- No auth, user ownership, multi-playthrough support, vendor differentiation, weapon variants, new combat mechanics, economy tuning or unrelated gameplay work.

## Review Focus

Confirm no production gameplay, transport or browser path still offers, creates, stores, consumes, steals, spends, narrates or advertises a Rifle/Rifle-ammunition item. Check enum numeric values and event/snapshot compatibility against the migration: a database upgraded from the last pre-alpha migration loses session-owned dependants before the new code loads, and a new legal session round-trips. Ensure the revolver path remains the only firearm capability and that travel ammo deltas agree between command and projector. Confirm the API rejects numeric item kinds 8 and 9 without mutation. The matrix must preserve a coherent future rifle feature rather than treating current shell removal as permanent product rejection.

---

### Task 1: Commit this successor plan and retire the `.69` plan

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-store-and-inventory-boundaries.md`
- Modify: `docs/features.md`
- Delete: `.agents/plans/2026-10-10-horse-death-inventory.md`
- Create: `.agents/plans/2026-10-10-retire-rifle-shell.md`
- Modify: `Directory.Build.props`

**Interfaces:**
- Consumes: refreshed `origin/develop` at merge `7f01f265d74e450c0797c99b28c3c43d12a354af`; PR #255 source `225d96e125f67f480c5b87310c7ab5b27be19dfa`; exact-head gate `38081518051`; develop gate `38081891541`.
- Produces: delivered PR #255 evidence in row 10; retirement of the completed `.69` plan; explicit rifle-shell release disposition and PG-004-B future capability; this committed `.70` plan.

- [ ] Reconfirm PR #255 merge/source and both successful hosted gate runs.
- [ ] Classify the `.69` plan against merged source and evidence; retire it in this successor and remove its stale roadmap link.
- [ ] Keep row 10 executing, link this `.70` plan, and record PR #255's source, merge and exact-head/develop gate evidence.
- [ ] Update PG-004 in the spec, feature matrix and inventory investigation to record rifle-shell retirement and the complete future reintegration contract without claiming the rest of PG-004 complete.
- [ ] Advance the authored product version once to `0.1.0-dev.70` in `Directory.Build.props` and verify the generated web identity through its owning check.
- [ ] Commit these planning and predecessor-retirement changes before implementation; the normal hook checks without mutating the staged candidate.

### Task 2: Prove unsupported item and catalog behavior fails before removal

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TownStoreCatalogResolverTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/InventoryTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiPurchaseTests.cs`
- Modify: the focused travel theft/ammunition scenarios in `tests/WildBunch.Domain.Tests/TravelResolverTests.cs` and `tests/WildBunch.Application.Tests/Handlers/ResolveJourneyEncounterHandlerTests.cs` as live call paths require.
- Modify only tests whose current assertion explicitly protects the rifle shell; locate them with `rg -n "Rifle|rifle" tests src`.

**Interfaces:**
- Consumes: current public town purchase request, catalog and inventory behavior, plus current event-backed travel resource flow.
- Produces: red tests for excluded Rifle/Rifle ammo offers, unsupported numeric purchases with no side effects, invalid retired inventory values, and revolver-only ammo resource behavior.

- [ ] Replace the old assertion that Rifle ammo is offered with catalog behavior asserting no store tier lists it; prove this fails against current source because Boomtown and Prosperous include Rifle ammo.
- [ ] Add an Inventory negative test using former enum values 8 and 9, proving retired/undefined values cannot be added; verify it fails against current permissive Inventory behavior.
- [ ] Change the HTTP purchase rejection behavior test to submit retired numeric kind 8 and assert unsuccessful purchase, unchanged wallet/inventory and no added event/log. Prove current HTTP behavior rejects it only because it is absent from the catalog, without relying on a now-removed named enum member.
- [ ] Replace any positive travel theft or ammo-count test that requires Rifle ammo with behavior using supported revolver ammo; retain meaningful revolver-spending and replay coverage.
- [ ] Run each focused test against unchanged implementation and observe the intended red failure; restore the test-only mutation before implementing.

### Task 3: Remove Rifle from Domain purchasing, inventory and travel

**Files:**
- Modify: `src/WildBunch.Domain/Inventory/ItemKind.cs`
- Modify: `src/WildBunch.Domain/Inventory/Inventory.cs`
- Modify: `src/WildBunch.Domain/Inventory/InventoryCapabilities.cs`
- Modify: `src/WildBunch.Domain/Inventory/InventoryCapabilityResolver.cs`
- Modify: `src/WildBunch.Domain/Economy/TownStoreCatalogModels.cs`
- Modify: `src/WildBunch.Domain/Game/StoreLoop.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `src/WildBunch.Domain/Game/JourneyLoopContexts.cs`
- Modify: `src/WildBunch.Domain/Game/JourneyLoop.cs`
- Modify: `src/WildBunch.Domain/Travel/JourneyEncounterResolutionEngine.cs`
- Modify: `src/WildBunch.Domain/Travel/TravelDiaryDayFactory.cs`
- Modify: owning Domain tests identified in Task 2 and `tests/WildBunch.Domain.Tests/InventoryCapabilityResolverTests.cs`.

**Interfaces:**
- Consumes: the red behavior tests and current revolver-only game path.
- Produces: a Domain model with only supported inventory kinds and revolver ammo; travel accounting, spending and theft no longer accept a Rifle ammo channel.

- [ ] Remove `Rifle` and `RifleAmmo` members while preserving all existing item numeric values 0 through 7; do not assign 8 or 9 to new items.
- [ ] Remove Rifle ammo from stackability and catalog tiers. Keep all other current goods, prices, ordering and prosperity availability unchanged.
- [ ] Remove `RifleUsable`; compute `FirearmThreatAvailable` and `GunfightCapable` solely from the revolver plus revolver ammo. Preserve existing knife and other capabilities.
- [ ] Remove rifle-specific travel context/runtime fields and branches. Ammo budgets, bribe theft selection, spent ammo and diary resource deltas use revolver ammo only.
- [ ] Keep the existing generic travel firearm outcome and tuning; do not add weapon selection, weapon-specific damage or new combat rules.
- [ ] Run focused Domain tests and relevant falsification checks; add no parallel mutation outside aggregate event application.

### Task 4: Make event and component retirement safe through migrations

**Files:**
- Create: a new `WildBunch.Persistence` migration after the current latest migration which deletes all rows from `GameSessions`; select an autogenerated timestamp later than the latest committed migration and verify EF registers it after `20261010184918_EnforceSingleActivePlaythrough`.
- Modify: `tests/WildBunch.Integration.Tests/MigrationTests.cs`.
- Inspect: `src/WildBunch.Persistence/GameSessions/GameSessionEntityConfiguration.cs`, `StartupOperationEntityConfiguration.cs`, stored-event/component/diary configurations, and the full replay/load pipeline.

**Interfaces:**
- Consumes: current FK cascade ownership from GameSessions to components, stored events, travel diary days and startup operations; numeric ItemKind event/snapshot payload shape.
- Produces: a forward-only pre-alpha data-discard migration and PostgreSQL integration proof that session-owned history is removed while the database and future session persistence remain healthy.

- [ ] Add a PostgreSQL migration test starting at the last migration before the new retirement migration. Insert a GameSession plus dependent component JSON containing former rifle values, a stored event with rifle item kind, a diary day, and a startup operation.
- [ ] Apply migrations and prove the session and every dependent session-owned row are deleted through the declared foreign-key cascade; verify the database still connects and can store/reload a new session with supported inventory.
- [ ] Prove the migration is registered after the latest existing migration and `Down` refuses to claim discarded playthroughs can be restored.
- [ ] Add a forward-only migration whose Up deletes only `GameSessions`; do not drop tables, recreate the database, manipulate the migration history table manually, rewrite event payloads, or add upcasters that silently erase historical facts.
- [ ] Run the migration test on PostgreSQL, then run `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api` and inspect the authored migration sequence.

### Task 5: Remove rifle capability from Application, API and browser contracts

**Files:**
- Modify: `src/WildBunch.Application/Games/Mapping/InventoryMapper.cs`
- Modify: `src/WildBunch.Application/Games/Models/GameDtos.cs`
- Modify: `src/WildBunch.Application/Projections/TravelDiaryDayProjector.cs`
- Modify: `src/WildBunch.Web/src/api/types.ts`
- Modify: `src/WildBunch.Web/src/ui/formatters.ts`
- Modify: `src/WildBunch.Web/src/components/travel/TravelActions.tsx`
- Modify: affected web test fixtures discovered by `rg -n "rifleUsable|Rifle|Rifle ammo" src/WildBunch.Web/src/tests`.
- Modify: `tests/WildBunch.Application.Tests/Handlers/PurchaseStoreItemHandlerTests.cs`, `tests/WildBunch.Application.Tests/Handlers/ResolveJourneyEncounterHandlerTests.cs`, `tests/WildBunch.Integration.Tests/GameApiPurchaseTests.cs` and `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedCatalog.cs` where contracts changed.

**Interfaces:**
- Consumes: Domain-only supported inventory/capability/read state.
- Produces: safe player DTOs and browser code that expose supported inventory only, without a permanently false Rifle capability.

- [ ] Remove `RifleUsable` from Application and Web DTOs/mappers and remove its unused formatter/fixture fields. Preserve other inventory capability meanings.
- [ ] Remove Rifle labels and Rifle ammo filtering from browser travel action code; render the supported inventory returned by the server without obsolete type branches.
- [ ] Update journal projector ammo tracking to count and subtract only revolver ammo for purchase, travel spend and theft events; keep event-derived narratives truthful.
- [ ] Keep strict CQRS: do not introduce aggregate reads or direct state updates to fill a removed DTO field.
- [ ] Run focused Application and Web behavior suites; verify actual supported revolver purchase and travel usage remain intact.

### Task 6: Review, validate and deliver

**Files:**
- Review: complete branch diff against PG-004/PG-007, ADR-0013 and ADR-0028, `.agents/doctrine/event-sourcing-integrity.md`, backend and code-review unslop profiles.
- Modify: this roadmap only in a successor worktree after this PR merges.

**Interfaces:**
- Consumes: committed `.70` plan; explicit pre-alpha discard migration; replay-safe revolver path; Application/API/Web cleanup; updated feature matrix and spec.
- Produces: reviewed, hosted-gate-green PR to `develop`; row 10 remains open until any other evidenced resource gaps are closed.

- [ ] Search production source for `Rifle`, `RifleAmmo`, `rifleUsable`, numeric item values, rifle-specific resource fields, and stale player-facing labels; resolve remaining live references. Tests may use numeric 8 only to prove retired requests fail.
- [ ] Verify no ADR change is needed and report why; feature/spec/investigation documents preserve the new scope disposition and PG-004-B future contract.
- [ ] Ensure PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`; run focused Domain, Application, Web and migration tests; restore .NET tools; verify the version identity; then run `py -3 tools/run.py ci --check` on the final candidate.
- [ ] Run a fresh whole-branch review against actual diff, plan, ADRs, event-sourcing doctrine and selected anti-slop profiles. Correct substantive findings with meaningful behavior tests and rerun affected checks.
- [ ] Commit with the check-only hook, publish PR against `develop`, verify exact head and hosted canonical gate success, then merge under the active goal.
- [ ] In the next fresh worktree, record actual PR/source/merge/gate evidence and retire this `.70` plan only after assessing its complete scope.

## Acceptance

- Current domain and player surfaces offer no Rifle or Rifle ammunition and cannot create or hold those unsupported item kinds.
- Revolver and revolver ammunition remain available through current prosperity-driven store rules; travel combat, ammo theft/spending, diary deltas and replay remain consistent.
- An HTTP request using former numeric kind 8 cannot mutate cash, inventory or event history.
- A forward PostgreSQL migration discards incompatible pre-alpha playthroughs and dependent history/cache rows without dropping the database or breaking future session persistence.
- Player read DTOs and browser code expose no `RifleUsable`, Rifle label or rifle-ammunition behavior.
- The complete future rifle feature is recorded in the matrix with acquisition, ammo, combat, event and player-read dependencies.
- Local and exact-head hosted canonical gates pass before merge; the successor records the develop push gate.

## Explicit Exclusions

No new rifle implementation, weapon-specific damage, weapon variants, different firearms, ammo availability tuning, other item/resource changes, store redesign, vendor distinctions, auth, user ownership, cloud deployment or infrastructure work.
