# Make startup seed and prologue reflect the settled playthrough

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Make a player's visible setup seed the exact seed used to create the playthrough, give each fresh setup visit a new UUID, and keep the prologue bound to the case that setup settled, including after refresh.

**Architecture:** The web setup draft owns one seed string. A fresh setup visit initializes it once with a UUID; user edits update that same value; submit validates and normalizes it before calling the existing setup command. The API validates resolved request facts rather than silently accepting a missing seed or unsupported enum values. Successful setup persists the supplied seed through the existing event-backed path. The prologue read is session-scoped and derives its player-safe culprit descriptor from the settled session case file; it never regenerates a case from browser setup values.

**Tech Stack:** React, TypeScript, TanStack Query, ASP.NET minimal API, existing `CompletePlayerSetupHandler`, xUnit, Vitest and repository command bus.

**Spec:** [Stable 0.1.0 baseline, product boundary](../specs/2026-10-07-stable-0.1.0-baseline.md#product-boundary), including PG-001 hunt creation and PG-002 lifecycle.

**Execution Strategy:** `executing-plans` inline. The setup form, request validation and persisted seed constitute one behavior from visible input to authoritative playthrough fact.

## Global Constraints

- `Directory.Build.props` remains the only authored application version source; this PR uses `0.1.0-dev.66`.
- Name is required; Standard difficulty and Classic randomness remain defaults; players may start after entering only a name.
- Every fresh visit to setup gets a new UUID, retained while settings are edited; a valid edited UUID is the value sent and persisted.
- Invalid seed, blank name, or unsupported difficulty/randomness does not create a playthrough. No silent canonical-seed fallback is allowed for the resolved setup request.
- Preserve the semantic sequence setup -> Go creates the settled playthrough -> prologue -> explicit starting-town choice -> free first arrival. Keep the settled case lead unchanged and bind its read to the created session; do not change town selection in this slice.
- Do not add auth, account ownership, idempotency mechanisms, new events, event-payload changes, world generation changes, or persistence schema changes.
- Follow the event-sourcing, DDD, API validation, feature-matrix and testing playbooks. Do not preserve tautological seed truthiness tests.
- Each epic PR uses its own fresh worktree, targets `develop`, advances the development version, and leaves this plan in Git history until a successor retires it.
- Use the canonical fail-fast command bus and check-only commit hook; do not run the full gate immediately before the hooked implementation commit.

## Review Focus

- The seed visible in the editable field is the exact normalized seed in the setup request and persisted session.
- A new setup visit is randomized once; rerenders and settings changes do not silently replace the seed.
- Invalid intent is rejected at the web and API boundaries without creating a session.
- Name-only quick start still uses Standard, Classic and the visit's generated UUID.
- The prologue query reads the existing session through `IGameSessionReadRepository` and returns the same established case lead after refresh; the browser does not supply setup facts to that read.
- Do not change `PrologueViewed` event shape or its historical consumers in this slice. The acknowledgement path remains separately governed by its current event contract.

---

### Task 1: Advance the roadmap and commit this JIT plan

**Files:**
- Create: `.agents/plans/2026-10-10-startup-seed-submission.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Retire: `.agents/plans/2026-10-10-action-availability-read-model.md`

**Interfaces:**
- Consumes: merged PR #251 source `7024113a5b49563fdc05a516ddb23026ebebdd98`, merge `66718c4ff2af8b30f7748f6ec8412ef2b24b6a0e`, exact-head hosted gate `38064117790`, and develop-push gate `38064444334`.
- Produces: a committed `.66` plan from `develop` at `66718c4ff2af8b30f7748f6ec8412ef2b24b6a0e`, row 08 completed with evidence, and row 09 executing with this plan as its current JIT slice.

- [x] Record PR #251's source, merge and both hosted gate runs in row 08; confirm no player query under `Games/Queries` remains on `IGameSessionRepository`; mark row 08 complete and retire the `.65` plan in this first substantive commit.
- [x] Set the sole authored version to `0.1.0-dev.66`, advance row 09 to executing, and commit the plan, roadmap, version and predecessor retirement before source changes.

### Task 2: Prove and fix setup seed authority from field to persisted session

**Files:**
- Modify: `src/WildBunch.Web/src/hooks/useStartGameSeed.ts`, `src/WildBunch.Web/src/hooks/useStartFlow.ts`, `src/WildBunch.Web/src/flow/PreSessionSurface.tsx`, `src/WildBunch.Web/src/components/start-flow/SetupHuntStep.tsx`, and `src/WildBunch.Web/src/ui/gameSetupSeedCodec.ts` as inspection requires.
- Test: `src/WildBunch.Web/src/tests/StartFlow.test.tsx`, `src/WildBunch.Web/src/tests/SetupHuntStep.test.tsx`, plus a focused hook test only if the real UI path cannot prove visit initialization behavior.
- Modify: `src/WildBunch.Api/Games/Validation/RequestValidation.cs` if required to reject missing/unsupported resolved request facts.
- Modify: `src/WildBunch.Api/Games/GameSessionEndpoints.cs` and the existing prologue query path to make that read session-scoped.
- Modify: `src/WildBunch.Application/Games/Queries/GetPrologueHandler.cs` and `GetPrologueQuery.cs` to resolve the player-safe descriptor from the settled session case.
- Modify: `src/WildBunch.Web/src/api/wildBunchApi.ts` and `src/WildBunch.Web/src/components/start-flow/StorySoFarStep.tsx` to query with the active session identity, not setup drafts.
- Test: `tests/WildBunch.Integration.Tests/GameApiValidationTests.cs` and the setup acceptance test that can independently prove the persisted seed.
- Test: `tests/WildBunch.Integration.Tests/PrologueHiddenTruthTests.cs` and focused prologue application/API behavior.

**Interfaces:**
- Consumes: `SetupGameRequest(PlayerName, GameDifficulty, SeedCode, GameEntropy)`, current setup-flow reset lifetime, and the existing `PlayerSetupCompleted` persistence/replay contract.
- Produces: one setup draft seed whose UUID-shaped value is validated and normalized before request submission; invalid request facts produce validation failure before session creation.

- [x] Replace the truthiness-only seed assertion with behavior asserting the exact expected UUID submitted after editing the visible field; first run it against current code and confirm it fails because the request still uses `seedState` rather than `seedDraft`.
- [x] Add rendered StartFlow tests proving a fresh setup visit starts with a UUID and name-only submission uses Standard and Classic; prove editing the optional difficulty does not change the visit seed and a later setup reset receives a new UUID with a focused hook test.
- [x] Add a negative browser scenario: invalid edited seed is visible as a useful field error, setup API is not called, and the player remains in setup. Add an API negative for missing/malformed UUID and unsupported difficulty or entropy proving no session is created.
- [x] Consolidate `seedState`, `seedDraft`, `seedDirty` and inert decode-error plumbing into the smallest truthful draft contract. Generate a UUID once on a new setup visit and on an explicit setup reset, not during render or when unrelated settings change. Submit the validated normalized draft value.
- [x] Require and validate name, UUID seed, supported `GameDifficulty`, and supported `GameEntropy` at the API boundary; preserve request defaults only where callers intentionally omit optional settings, not for the resolved seed. Do not create a session on invalid input.
- [x] Preserve `CompletePlayerSetupHandler` event flow. Verify the accepted request seed is the `GameSession.SeedCode` reconstructed from the event-backed setup facts, using an independent expected UUID. Do not add an event just to echo browser state.
- [x] Prove a refresh at the prologue phase requests the saved session's prologue and returns its established clue; keep the response player-safe and do not load a command aggregate in the query.
- [x] Run `npm --prefix src/WildBunch.Web test -- --run src/tests/StartFlow.test.tsx src/tests/SetupHuntStep.test.tsx`; after `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`, run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~GameApiValidationTests|FullyQualifiedName~PlayerSetupReplayAcceptanceTests"`. Falsify exact seed submission by temporarily submitting the prior seed state and falsify API validation by allowing the invalid request; each owning behavior test must fail, then restore and rerun. The session-scoped prologue behavior was also witnessed RED against the old route and passed with the corrected read path; the final focused tests include `StorySoFarStep` and `PrologueHiddenTruthTests`.

### Task 3: Reconcile decisions and deliver the setup-input slice

**Files:**
- Review: [ADR-0014](../../docs/decisions/ADR-0014-use-ddd-onion-cqrs-repositories-and-first-class-unit-of-work.md), [ADR-0028](../../docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md), and [feature matrix](../../docs/features.md) through `.agents/playbooks/feature-matrix.md`.
- Preserve: `.agents/investigations/stable-0.1.0/2026-10-07-hunt-creation-contract.md` and `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`; add a dated disposition only for WB-05 if this slice resolves it.
- Record: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` in the next successor after merge evidence exists.

**Interfaces:**
- Consumes: the setup input, API validation and event-backed session behavior; produces: an independently reviewed `.66` PR to `develop` with hosted exact-head and develop-push gates.

- [x] Confirm this corrects submitted start facts and session-bound prologue truth without changing archive semantics, ownership or product feature dependencies; compare ADR-0014, ADR-0028 and ADR-0039 and retain them unchanged because the slice implements their existing CQRS, event-replay and settled-prologue decisions. Update PG-001 evidence to distinguish rendered UI behavior tests from browser-backed journey evidence.
- [x] Add a dated finding disposition only for behavior actually corrected, preserving the investigation's historical observation.
- [x] Run focused behavior tests then `py -3 tools/run.py ci --check`; commit source through the canonical check-only hook. Obtain an independent whole-branch review and resolve any actionable findings with focused behavior proof.
- [ ] Publish the reviewed PR to `develop`, verify the hosted gate passes on the exact source SHA, merge it, and verify develop-push CI passes on the merge SHA. Keep this plan in-tree through its PR; its successor records evidence and retires it in the successor's first substantive commit.

## Acceptance

- A player can enter a name and submit without changing optional defaults; a new visit's UUID is fresh and stays stable while that draft is edited.
- The actual visible valid UUID, normalized, is submitted and persisted as the setup seed.
- Invalid/missing seed, blank name and unsupported option values fail validation without creating a playthrough; invalid web input remains recoverable on setup.
- Go retains the established setup -> prologue -> starting-town -> free-arrival flow.
- Focused browser and API behavior tests, full local canonical CI, exact-head hosted CI and develop-push hosted CI pass; no persistence migration is generated.

## Explicit Exclusions

Do not change the `PrologueViewed` event shape, implement backend creation idempotency, browser command-pending lifecycle, start-over recovery, account/OIDC ownership, multiple simultaneous playthroughs, starting-town/map composition, or unrelated web composition cleanup. If any becomes a prerequisite for the visible seed behavior, stop and revise the plan rather than expanding silently.
