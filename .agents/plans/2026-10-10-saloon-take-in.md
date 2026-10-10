# Apply the settled saloon take-in rules

**Goal:** Make the retained saloon observation and take-in path obey PG-006: a player names the person they are challenging, citizens come quietly and are released with a capped fine, and a wanted suspect can be compelled only when the player owns a gun.

**Architecture:** Keep `GameSession` as the sole command/event boundary. `BountyLoop` decides from the encountered server-side person, known warrant, declared public identity and firearm ownership, then returns facts for `GameSession` to produce. The browser supplies only the declared wanted-identity handle. Event payloads record actual outcomes and wallet effects so cache reconstruction does not re-decide or reroll them. Do not model no-gun refusal as flight, a failed declaration as capture, or citizen cooperation as a wrong wanted identity.

**Specification and row:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, PG-006 and culprit identity/release; `docs/features.md`, PG-006 and PG-006-C; roadmap row 11. The previous row 11 plan is retired in this successor. The row 11 base is `655c3dbaaaf5ec18cdc42f307c104046c5436f3a`, `0.1.0-dev.72`.

**Decision records:** Read ADR-0002, ADR-0005, ADR-0025, ADR-0026, ADR-0028 and ADR-0038. The change preserves the session command root, public/hidden identity boundary, separation of warrant settlement from hunt resolution, immutable event facts and session-owned event production; no new architecture decision is expected. Reassess that conclusion against the final diff before review.

**Baseline corrections:** The linked saloon challenge contract currently describes resistance/escape and says its fine amount is unresolved; those assertions conflict with the settled PG-006 contract. The culprit contract says the threshold is undecided and gives a five-member example, while PG-006 now sets seven generated members and a five-of-six gate. Correct both investigation records with dated editorial clarifications, preserving their original dated history and explicitly making the specification the current product truth. Do not reinterpret either as permission to add resistance, gunfights, poster circulation or sheriff recognition.

## Boundaries

- Keep the Sheriff Office wanted-poster interaction as one clue. Do not restore the public noticeboard or private records.
- Retain saloon observation of distinguishing features and the current choice to declare a name from known wanted identities. The server validates the declared handle against player-known warrants, then resolves the encountered suspect's actual warrant from server-held known and unread public warrants; a feature match alone never authorizes settlement.
- A missing/blank identity declaration is rejected without consuming the observed person or producing a confrontation fact. A nonblank wrong identity is a wrong-name take-in attempt, not a successful bounty.
- A wanted person with a correctly named identity can be taken in only if the player owns a revolver. Ammo is irrelevant to take-in. Without a revolver the wanted person refuses; do not create a flight, health loss, capture, payout or roundup progress.
- Only a handle in the server's known-warrant set is a valid declaration. A fabricated or stale handle is rejected without consuming the active person or recording a fine; a different known identity is a wrong-name attempt and follows the settled fine/release outcome.
- A suspect's unread public poster still identifies their actual warrant to the server. Comparing the declared known identity against that server-held target warrant yields the wrong-name outcome without exposing the unread identity. If no warrant corresponds to the encountered suspect, reject without an event or consuming the person.
- A wanted person with a wrong declared identity is released by the sheriff, the player pays the existing capped $10 fine, and no bounty or roundup progress is awarded. Preserve public-target concealment; do not leak the actual wanted identity through the response or event narration.
- A citizen always comes quietly regardless of inventory or declared wanted identity, is released by the sheriff and incurs the same wallet-capped $10 fine. No citizen becomes a wanted suspect, receives a bounty, or advances roundup.
- Correctly named, gun-backed wanted take-in continues through the normal sheriff settlement, pays the warrant bounty once, and counts only through the existing lawful settlement event path.
- Retire the obsolete `ConfrontSaloonWantedSuspect` command/method if a fresh production-reference search confirms it has no route or dispatcher. It has no public-name input and must not remain as an alternate take-in contract; keep observation coverage that still protects the shared saloon selection behavior.
- Keep saloon resistance, escape, injury, gunfight, death and lawman recognition deferred. Do not alter trail combat or the five-of-six culprit gate in this slice.
- No compatibility reader/upcaster is required for retired pre-alpha histories. The earlier rifle-retirement migration only removed sessions present when it ran, so this slice adds a later forward migration to delete sessions created under the intermediate event contract; do not add legacy support.

## Task 1: Prove and correct domain behavior

**Files:** `src/WildBunch.Domain/Game/BountyLoop.cs`, `BountyLoopContexts.cs`, `GameSession.cs`, saloon outcome/result/event types under `src/WildBunch.Domain/Cases` and `Events`; `tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs` and the applicable event-sourcing, settlement and presence-cache behavior tests.

- [x] Replace tests that protect fabricated flight or treat citizen cooperation as a wrong-identity result with independent behavioral cases for blank declaration, no gun, owned unloaded gun, correct identity, wrong identity, citizen and capped wallet.
- [x] Prove behavior assertions against the old behavior and with a focused provider mutation; restore the implementation before continuing. Do not add assertions that merely compare current state to its replayed copy.
- [x] Separate firearm ownership from `FirearmThreatAvailable` (which means a usable loaded weapon for combat). Do not change that capability's meaning for travel combat.
- [x] Reject an absent declaration without creating a result fact or consuming the active person. Produce the settled no-gun refusal as an explicit event-backed outcome without mutating wanted-presence to `GoneToGround`.
- [x] Represent citizen cooperation/release and wanted wrong-name release with truthful outcomes and absolute wallet facts; apply/replay each wallet result exactly once and keep the original identity hidden when declaration is wrong.
- [x] Reject declared handles that are not in the session's known-warrant set before resolving either a wanted person or citizen; prove unknown client-supplied handles cannot consume the active person or produce a fine.
- [x] Resolve the encountered suspect against both server-held known and unread public warrants so a player may make a wrong-name accusation about a gang member whose poster they have not read; prove the fine/release outcome and hidden-identity boundary.
- [x] Preserve the successful correct-name sheriff settlement, bounty amount, one-time payment and event-derived roundup facts. Remove or update only tests that require the superseded no-ammo/no-name flight branch; keep valid identity, legality, privacy and replay negatives.
- [x] Exercise the normal public player path for observation and take-in; developer overrides control deterministic setup only, while player commands determine outcomes.

## Task 2: Carry the corrected outcome through persistence and player API

**Files:** Existing saloon command and API contracts, `src/WildBunch.Application/Projections/JournalLogProjector.cs`, `tests/WildBunch.Integration.Tests/Acceptance/SaloonConfrontationAcceptanceTests.cs`, and focused PostgreSQL repository/event-replay tests. The unused `ConfrontSaloonWantedSuspect` command and its tests are retired under Task 1 after confirming they have no production route.

- [x] Keep the existing player command and its declared identity input; do not add client-supplied target identity or outcome selection.
- [x] Prove PostgreSQL save/reload and event replay for refusal, citizen fine, wrong-name fine and correctly named bounty using event payload facts as independent expected values. Verify retry/re-entry cannot charge the same fine or bounty twice.
- [x] Add a forward migration deleting playthroughs created under the intermediate pre-alpha event contract after the previous retirement migration; test cascade cleanup and refusal to reverse the history deletion. Do not add enum upcasters or event compatibility.
- [x] Keep the command result truthful for the established statuses and avoid leaking `TrueCulpritId`, hidden release state, or actual wanted identity after a wrong declaration.
- [x] Update or retire only API/application tests that assert the old invalid no-gun flight semantics; do not create source-shape/change-detector tests.

## Task 3: Make the player surface express the command contract

**Files:** `src/WildBunch.Web/src/flow/places/SaloonPlace.tsx`, `src/WildBunch.Web/src/state/GameSessionProvider.tsx`, and focused player behavior tests under `src/WildBunch.Web/src/tests/`.

- [x] Preserve observation as a feature-description display and provide only known wanted identities as declarations. Resolve a selected poster only when its handle is actually selected; do not imply the first poster is a player's choice.
- [x] Make blank identity unavailable to submit (server rejection remains authoritative); render refusal, citizen release/fine, wrong-name release/fine and successful bounty outcomes from the returned server result without inventing capture or flight narration.
- [x] Test the player-visible observation-to-declaration-to-result journey, including refusal and a citizen result, with focused behavior assertions. Do not rely on broad `AppShell` smoke coverage or assert exact implementation markup beyond the interaction contract.

## Task 4: Truthful planning/source records and delivery

- [x] Correct the two dated investigation-contract clarifications identified above without rewriting historical claims; update `docs/features.md` PG-006 assessment to describe the delivered boundary and remaining five-of-six gate work.
- [x] Reconfirm the ADR comparison against the final diff. Leave ADRs unchanged because implementation preserves the consulted durable decisions.
- [x] Confirm the earlier row 10 retirement protects only rows created before it ran; delete subsequent pre-alpha playthroughs in this slice because outcome enum values and event replay facts now change. Preserve the existing migration history and use no compatibility reader/upcaster.
- [x] Advance the authored application version to `0.1.0-dev.73`; build generated projections through the repository command bus and verify generated web identity. Do not hand-edit generated mirrors.
- [x] Run focused domain/application/API/persistence/web lanes in fail-fast order, then `py -3 tools/run.py ci --check`. Ensure local PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure` before the canonical gate.
- [x] Review the whole branch against the PG-006 contract, relevant ADRs, unslop profiles and changed-concern guidance. Resolve substantive findings with behavior proof and re-review the corrected diff.
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
