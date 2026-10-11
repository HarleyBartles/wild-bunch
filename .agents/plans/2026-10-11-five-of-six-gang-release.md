# Implement the five-of-six gang release gate

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the culprit become encounter-eligible only after five distinct other Wild Bunch members have successful sheriff settlements, while keeping the culprit's public identity discoverable beforehand.

**Architecture:** `SheriffTurnInSettled` is already the immutable successful-settlement fact, and `CaseFileGenerated` supplies the suspect and warrant identities needed to classify its target. Derive roundup progress from distinct recorded settlements for other Wild Bunch members; do not advance it from clues or add a second progress event. Keep poster discovery and culprit encounter eligibility as separate policies, and keep release counters and culprit identity out of player-facing reads.

**Tech Stack:** C#/.NET domain aggregate and replay, Application projections, EF Core/PostgreSQL persistence and tests, React player behavior tests where affected.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, sections `Saloon take-in, PG-006`, `Culprit identity and release`, and `Knowledge, PG-005 and PG-008`; `docs/features.md`, PG-005, PG-006 and PG-006-C.

**Execution Strategy:** `executing-plans` - settlement progress, encounter selection, poster discovery, and replay share the same aggregate facts and must be verified as one inline integration slice.

## Global Constraints

- The generated gang has seven members, including the culprit; five distinct other gang members must be successfully settled alive or dead to release the culprit.
- Clue reads, poster reads, citizens, the culprit's own settlement, wrong-name outcomes, refusals, failed attempts and duplicate settlements do not advance the gate.
- The culprit's public identity can be learned before release; release changes encounter eligibility, not identity.
- Wanted posters are read only in the Sheriff Office context, one clue per read; there is no town noticeboard, public-square interaction or private sheriff-records feature.
- `SheriffTurnInSettled` remains the authoritative settlement fact. Do not introduce a release-progress event, infer progress from browser state, or expose hidden release state through player DTOs or journal text.
- The application version advances once for this PR to `0.1.0-dev.74`; `Directory.Build.props` is authoritative and generated web identity is build output.

## Review Focus

- Four distinct other-gang settlements leave the gate closed; the fifth opens it through normal player progression.
- Dead-or-alive successful settlements count, but settling the culprit itself never counts as one of the five.
- A public wanted poster can reveal the culprit's identity while the culprit remains ineligible for saloon encounters.
- Clue discovery, a citizen, wrong-name settlement, refusal, and duplicate settlement cannot manufacture roundup progress.
- Event replay and PostgreSQL reload preserve the same release eligibility without leaking the hidden culprit flag or counter.

---

## Task 1: Replace clue-based progress with settlement-derived roundup state

**Files:** `src/WildBunch.Domain/Cases/CaseFile.cs`, `src/WildBunch.Domain/Cases/CaseProgress.cs`, `src/WildBunch.Domain/Cases/CaseFileSnapshot.cs`, `src/WildBunch.Domain/Cases/BountySettlementPolicy.cs`, `src/WildBunch.Domain/Game/GameSession.cs`, `BountyLoop.cs`, `BountyLoopContexts.cs`; `src/WildBunch.Persistence/GameSessions/CaseFileGenerationCacheRecovery.cs` and `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`; `tests/WildBunch.Domain.Tests/CaseProgressTests.cs`, `CaseFileTests.cs`, `GameSessionSheriffTurnInTests.cs`, `SaloonPoiSelectionTests.cs`, `Events/GameSessionEventSourcingTests.cs`, and `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`.

**Interfaces:**

- Consumes: generated case suspects and public/known warrants, `SheriffTurnInSettled.TargetSuspectId`, and distinct `SheriffTurnInSettlementState` records.
- Produces: release state whose progress is the count, capped at the configured threshold, of settled suspect IDs matched to a generated warrant with `TargetKind == GangMember` and `AdvancesGangPressureFor == OutlawGangIds.WildBunch`, excluding `TrueCulpritId`; every `GameSession.Apply(SheriffTurnInSettled)` reconstructs this state from the recorded event fact.

- [ ] Add behavior tests proving four qualifying settlements keep the culprit locked and the fifth unlocks saloon eligibility. Make the setup settle each suspect through the normal command/event path; do not set an arbitrary progress integer or force final eligibility as the asserted behavior.
- [ ] Add negative behavior tests showing clue discovery cannot advance progress; a non-gang settlement, the culprit's own settlement, a refused/wrong-name attempt and a repeated settlement cannot advance it. Preserve existing duplicate-settlement rejection and assert the independent expected count.
- [ ] Prove both alive and dead successful settlements for other gang members count, and that encounter selection continues to admit remaining non-culprit gang members before release.
- [ ] Remove the optional clue-discovery progress parameters and dead clue-counter advancement method. Derive roundup progress from stored unique settlement facts and the generated warrant metadata, preserving the configured threshold of five.
- [ ] Rename the release state's `RequiredPublicClues` member to a settlement-based name and make its locked explanation refer to remaining gang members to bring in; update all domain callers and behavior tests so no surviving code claims that clues advance the gate.
- [ ] Rebuild this state deterministically when `CaseFile` is reconstructed from cached settlements; do not trust a stale cached progress integer over its event-derived settlement facts. Preserve the existing event and JSON payload shapes because this slice changes behavior over existing facts, not the serialized contract; remove the generated-fact cache comparison that incorrectly equates a later dynamic progress projection with the initial `CaseFileGenerated` value. The settlement-cache recovery already compares settlement facts with events and remains the authoritative repair trigger. Do not add a migration or legacy compatibility path because the serialized contract is unchanged.
- [ ] Run the focused Domain tests and confirm each new negative test fails for its named invalid source before applying the implementation, then passes afterward.

## Task 2: Separate public poster discovery from culprit encounter release

**Files:** `src/WildBunch.Domain/Cases/WantedPosterResolver.cs`, `tests/WildBunch.Domain.Tests/WantedPosterResolverTests.cs`, `tests/WildBunch.Domain.Tests/GameSessionWantedPostersTests.cs`, `tests/WildBunch.Domain.Tests/SaloonPoiSelectionTests.cs`; `tests/WildBunch.Application.Tests/Handlers/ReadWantedPostersHandlerTests.cs`; `tests/WildBunch.Integration.Tests/Acceptance/WantedPosterAcceptanceTests.cs`.

**Interfaces:**

- Consumes: the public warrant pool and settlement-derived culprit release state from Task 1.
- Produces: wanted-poster reads eligible to reveal the culprit's warrant before release; saloon candidate selection remains gated until five qualifying settlements.

- [ ] Add a behavior test that reads the culprit's poster while release progress is below five and receives the public identity clue while saloon selection still excludes the culprit.
- [ ] Remove the release-state filter from poster eligibility without changing the one-clue-per-read rule or allowing clue discovery itself to alter the gate.
- [ ] Verify after the fifth successful other-member settlement that the same culprit is eligible for encounter selection and existing known/public warrant movement remains truthful.
- [ ] Verify player/API responses disclose only public warrant and clue facts; they do not disclose `TrueCulpritId`, settlement progress or release-state internals.
- [ ] Run focused resolver, handler and acceptance behavior tests; leave the browser unchanged because this slice does not alter the existing Sheriff Place interaction contract.

## Task 3: Verify replay, persistence, and current feature records

**Files:** `src/WildBunch.Persistence/GameSessions/CaseFileSettlementCacheRecovery.cs`; `tests/WildBunch.Domain.Tests/EventSourcing/BountySaloonEventSourcingTests.cs`, `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs` and acceptance coverage; `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-culprit-identity-and-release-contract.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `Directory.Build.props`.

**Interfaces:**

- Consumes: the settlement-derived release behavior and independent public-poster behavior from Tasks 1-2.
- Produces: replay and PostgreSQL reload proof for the four-to-five boundary; truthful feature evidence and delivery history; one `0.1.0-dev.74` identity.

- [ ] Prove replay from a `CaseFileGenerated` event followed by four or five real `SheriffTurnInSettled` events reconstructs the closed/open gate exactly, without rerunning generation or deriving progress from clues.
- [ ] Prove PostgreSQL save/reload preserves the event-derived settlement set and the four-to-five release boundary. Mutate the persisted settlement cache and confirm the existing event recovery path rebuilds progress; separately mutate only its serialized progress projection and confirm domain reconstruction derives the same value from the intact settlement cache without query-side writeback.
- [ ] Check applicable ADRs against the actual diff. Preserve ADRs that remain true; add a dated clarification or successor only if the implementation makes a durable decision false. Do not add an ADR merely to record implementation status.
- [ ] Update PG-006/PG-006-C assessment and row 11 evidence to state that wanted posters are Sheriff Office-only, one clue per read, and public culprit identity can precede encounter release; no noticeboard is restored.
- [ ] Retire `.agents/plans/2026-10-10-saloon-take-in.md` in the first substantive implementation commit after confirming all its scope merged in PR #258 and its durable decisions remain in the spec, feature matrix and dated source-contract clarification. Keep this new plan through its own completing PR.
- [ ] Set `Directory.Build.props` to `0.1.0-dev.74`; run `npm --prefix src/WildBunch.Web run build` to regenerate `src/WildBunch.Web/dist/version.json`, then verify identity with `py -3 tools/run.py ci --check` without committing generated output.
- [ ] Ensure PostgreSQL using `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`; run focused Domain, Application, Integration and web lanes in fail-fast order, then `py -3 tools/run.py ci --check`.
- [ ] Review the whole branch against the plan, current ADRs, applicable unslop profiles and review runbook. Use the normal check-only commit hook; publish a PR to `develop`, require exact-head hosted canonical CI to pass, merge under the active goal and verify the post-merge develop gate.

## Acceptance

- Five distinct, successful settlements of non-culprit Wild Bunch members open the encounter gate; four do not.
- Public wanted identity can be read in the Sheriff Office before that gate opens, while saloon encounters still exclude the culprit.
- Event replay and PostgreSQL reload reconstruct the gate from the event-established world and settlement facts.
- Invalid attempts, clue reads, citizens, repeated targets and the culprit's own settlement do not count.
- Player surfaces retain public/hidden truth boundaries; the noticeboard remains removed.
- The complete canonical gate and exact-head hosted gate pass before merge.

## Explicit exclusions

No new release event, difficulty-dependent threshold, unrelated-criminal capability, noticeboard, sheriff-records surface, saloon resistance/gunfight, culprit murder outcome, telegraph/lawman feature, automatic deduction engine, remote deployment, or compatibility promise for disposable pre-alpha sessions.
