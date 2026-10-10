# Apply the settled saloon take-in rules

**Goal:** Make the retained saloon observation and take-in path obey PG-006: a player names the person they are challenging, citizens come quietly and are released with a capped fine, and a wanted suspect can be compelled only when the player owns a gun.

**Architecture:** Keep `GameSession` as the sole command/event boundary. `BountyLoop` decides from the encountered server-side person, known warrant, declared public identity and firearm ownership, then returns facts for `GameSession` to produce. The browser supplies only the declared wanted-identity handle. Event payloads record actual outcomes and wallet effects so cache reconstruction does not re-decide or reroll them. Do not model no-gun refusal as flight, a failed declaration as capture, or citizen cooperation as a wrong wanted identity.

**Specification and row:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, PG-006 and culprit identity/release; `docs/features.md`, PG-006 and PG-006-C; roadmap row 11. The previous row 11 plan is retired in this successor. The row 11 base is `655c3dbaaaf5ec18cdc42f307c104046c5436f3a`, `0.1.0-dev.72`.

**Decision records:** Read ADR-0002, ADR-0005, ADR-0025, ADR-0026, ADR-0028 and ADR-0038. The change preserves the session command root, public/hidden identity boundary, separation of warrant settlement from hunt resolution, immutable event facts and session-owned event production; no new architecture decision is expected. Reassess that conclusion against the final diff before review.

**Baseline corrections:** The linked saloon challenge contract currently describes resistance/escape and says its fine amount is unresolved; those assertions conflict with the settled PG-006 contract. The culprit contract says the threshold is undecided and gives a five-member example, while PG-006 now sets seven generated members and a five-of-six gate. Correct both investigation records with dated editorial clarifications, preserving their original dated history and explicitly making the specification the current product truth. Do not reinterpret either as permission to add resistance, gunfights, poster circulation or sheriff recognition.

## Boundaries

- Keep the Sheriff Office wanted-poster interaction as one clue. Do not restore the public noticeboard or private records.
- Retain saloon observation of distinguishing features and the current choice to declare a name from known wanted identities. The server validates the declaration against the actual person and known warrant; a feature match alone never authorizes settlement.
- A missing/blank identity declaration is rejected without consuming the observed person or producing a confrontation fact. A nonblank wrong identity is a wrong-name take-in attempt, not a successful bounty.
- A wanted person with a correctly named identity can be taken in only if the player owns a revolver. Ammo is irrelevant to take-in. Without a revolver the wanted person refuses; do not create a flight, health loss, capture, payout or roundup progress.
- A wanted person with a wrong declared identity is released by the sheriff, the player pays the existing capped $10 fine, and no bounty or roundup progress is awarded. Preserve public-target concealment; do not leak the actual wanted identity through the response or event narration.
- A citizen always comes quietly regardless of inventory or declared wanted identity, is released by the sheriff and incurs the same wallet-capped $10 fine. No citizen becomes a wanted suspect, receives a bounty, or advances roundup.
- Correctly named, gun-backed wanted take-in continues through the normal sheriff settlement, pays the warrant bounty once, and counts only through the existing lawful settlement event path.
- Keep saloon resistance, escape, injury, gunfight, death and lawman recognition deferred. Do not alter trail combat or the five-of-six culprit gate in this slice.
- No compatibility reader/upcaster is required for the retired pre-alpha histories. Confirm the established forward migration invalidates those sessions before replay; do not add new legacy support.

## Task 1: Prove and correct domain behavior

**Files:** `src/WildBunch.Domain/Game/BountyLoop.cs`, `BountyLoopContexts.cs`, `GameSession.cs`, saloon outcome/result/event types under `src/WildBunch.Domain/Cases` and `Events`; `tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs`, `GameSessionSaloonWantedSuspectLoopTests.cs`, `GameSessionBountyLoopCoordinatorTests.cs` and `EventSourcing/BountySaloonEventSourcingTests.cs`.

- [ ] Replace tests that protect fabricated flight or treat citizen cooperation as a wrong-identity result with independent behavioral cases for blank declaration, no gun, owned unloaded gun, correct identity, wrong identity, citizen and capped wallet.
- [ ] For each new behavior assertion, temporarily break the owning behavior and witness the assertion fail for that reason; restore the implementation before continuing. Do not add assertions that merely compare current state to its replayed copy.
- [ ] Separate firearm ownership from `FirearmThreatAvailable` (which means a usable loaded weapon for combat). Do not change that capability's meaning for travel combat.
- [ ] Reject an absent declaration without creating a result fact or consuming the active person. Produce the settled no-gun refusal as an explicit event-backed outcome without mutating wanted-presence to `GoneToGround`.
- [ ] Represent citizen cooperation/release and wanted wrong-name release with truthful outcomes and absolute wallet facts; apply/replay each wallet result exactly once and keep the original identity hidden when declaration is wrong.
- [ ] Preserve the successful correct-name sheriff settlement, bounty amount, one-time payment and event-derived roundup facts. Remove or update only tests that require the superseded no-ammo/no-name flight branch; keep valid identity, legality, privacy and replay negatives.
- [ ] Exercise the normal public player path for observation and take-in; developer overrides may control setup only where the test also demonstrates the player command and outcome rather than treating the override as player behavior.

## Task 2: Carry the corrected outcome through persistence and player API

**Files:** `src/WildBunch.Application/Games/Commands` and mapping for the saloon command, `src/WildBunch.Api/Games` saloon endpoint/contract, `tests/WildBunch.Application.Tests/Handlers/ConfrontSaloonWantedSuspectHandlerTests.cs`, `tests/WildBunch.Integration.Tests/Acceptance/SaloonConfrontationAcceptanceTests.cs`, and focused persistence/event-replay tests.

- [ ] Keep the existing player command and its declared identity input; do not add client-supplied target identity or outcome selection.
- [ ] Prove PostgreSQL save/reload and event replay for refusal, citizen fine, wrong-name fine and correctly named bounty using event payload facts as independent expected values. Verify retry/re-entry cannot charge the same fine or bounty twice.
- [ ] Keep the command result truthful for the established statuses and avoid leaking `TrueCulpritId`, hidden release state, or actual wanted identity after a wrong declaration.
- [ ] Update or retire only API/application tests that assert the old invalid no-gun flight semantics; do not create source-shape/change-detector tests.

## Task 3: Make the player surface express the command contract

**Files:** `src/WildBunch.Web/src/flow/places/SaloonPlace.tsx`, its owned styles or hooks only if needed, saloon action/query mapping and a focused player behavior test under `src/WildBunch.Web/src/tests/`.

- [ ] Preserve observation as a feature-description display and provide only known wanted identities as declarations.
- [ ] Make blank identity unavailable to submit (server rejection remains authoritative); render refusal, citizen release/fine, wrong-name release/fine and successful bounty outcomes from the returned server result without inventing capture or flight narration.
- [ ] Test the player-visible observation-to-declaration-to-result journey, including refusal and a citizen result, with focused behavior assertions. Do not rely on broad `AppShell` smoke coverage or assert exact implementation markup beyond the interaction contract.

## Task 4: Truthful planning/source records and delivery

- [ ] Correct the two dated investigation-contract clarifications identified above without rewriting historical claims; update `docs/features.md` PG-006 assessment to describe the delivered boundary and remaining five-of-six gate work.
- [ ] Reconfirm the ADR comparison against the final diff. Leave ADRs unchanged unless implementation uncovers a genuinely new or corrected durable decision; if so, amend/supersede it in this PR.
- [ ] Confirm the row 10 pre-alpha history invalidation boundary already removes incompatible playthroughs before replay. Add no migration unless this check finds that the selected event/contract changes can reach persisted history despite that boundary.
- [ ] Advance the authored application version to `0.1.0-dev.73`; build generated projections through the repository command bus and verify generated web identity. Do not hand-edit generated mirrors.
- [ ] Run focused domain/application/API/persistence/web lanes in fail-fast order, then `py -3 tools/run.py ci --check`. Ensure local PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure` before the canonical gate.
- [ ] Review the whole branch against the PG-006 contract, relevant ADRs, unslop profiles and changed-concern guidance. Resolve substantive findings with behavior proof and re-review the corrected diff.
- [ ] Commit with the check-only hook, publish a PR to `develop`, require exact-head hosted canonical CI to pass, merge under the active goal, and verify the post-merge develop gate.
- [ ] In the next fresh worktree, record PR/source/merge/exact-head/develop-gate evidence in row 11 and retire this plan after checking its complete scope.

## Acceptance

- A player observes physical features, names a known wanted identity, and only the server's actual encountered person and warrant determine the result.
- No declaration is rejected without losing the opportunity; a wanted person refuses without a gun and stays unrecorded as captured or fled; an owned unloaded revolver permits take-in.
- Correctly named armed wanted suspects settle once through the sheriff; wrong-name suspects and citizens are released with a capped fine and no bounty or roundup credit.
- Event replay and PostgreSQL reload reconstruct the exact wallet, active saloon encounter, confrontation status and lawful settlement without re-deciding outcomes.
- The player sees truthful saloon outcomes through the normal browser path, and the complete local and exact-head hosted canonical gates pass before merge.

## Explicit exclusions

No public noticeboard or fake sheriff records, saloon resistance/escape/injury, saloon gunfight, kill flow, lawman or telegraph, poster circulation, local sheriff recognition, new clue source, culprit-release threshold change, additional vendors, trail combat change or cloud deployment work.
