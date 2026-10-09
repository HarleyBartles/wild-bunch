# Wild Bunch Feature Matrix

**Status:** Current product-scope authority, reconciled on 2026-10-08. This matrix records what Wild Bunch promises, what the 0.1.0 baseline includes, and how capabilities depend on one another. It does not certify that an implementation works merely because a route, component, event, type or test exists.

Use the [feature-matrix playbook](../.agents/playbooks/feature-matrix.md) when a design, plan, implementation, review or release changes a product promise, capability boundary, dependency or evidence assessment. The [stable 0.1.0 baseline](../.agents/specs/2026-10-07-stable-0.1.0-baseline.md) owns the detailed settled rules; this matrix owns the current capability grouping, disposition and dependency map. Source investigations preserve audit evidence and are not a second feature authority.

## Reading the assessments

**Implementation assessment** describes current source and existing behavior-test evidence. `Coherent` means the assessed path matches its current promise; `partial` means only part of the lifecycle is implemented; `broken` means a required outcome or invariant is known to fail; `unreachable` means no supported player path reaches it; `disabled` means it is deliberately unavailable; `planned` means no current implementation is promised; `not assessed` means evidence has not been traced. A feature may have more than one status across its lifecycle.

**Release disposition** describes whether 0.1.0 retains and repairs the capability, consolidates it into another surface, retires it, or records it only for later work. A retained-but-broken feature remains a product promise and must be repaired; a partial capability is not automatically a shipped feature.

**Evidence** links the source and test investigations that assess the existing behavior. Those assessments are static evidence unless they explicitly report a runtime run. They do not claim a browser playtest, deployed environment or external player validation. The source of game truth is the event history; saved state and projections are reconstructible under the [baseline's history and recovery contract](../.agents/specs/2026-10-07-stable-0.1.0-baseline.md#history-and-migration-boundary).

Dependency edges use these meanings: **requires** means the target is a necessary precondition; **consumes** means the source uses the target's output; **changes** means the source changes the target's state or availability; **conditional** means the edge applies only under the stated game state or choice. Shared imports and normal player sequence do not by themselves establish a dependency.

## Player capabilities

### PG-001 - Start a hunt and choose the first town

**Promise and entry:** A new player names the character and starts a hunt from the setup surface. Optional difficulty offers Easy, Standard, Challenging and Brutal, defaulting to Standard; optional randomness offers Boring, Classic, Adventurous and Wild, defaulting to Classic. Difficulty owns pressure/resource policy, randomness owns volatility, and neither changes settled mystery facts. Each visit to setup presents a fresh editable seed UUID, and a supplied seed is validated and used. Starting the game settles the world, gang, identities and culprit. The player then reads the prologue, which contains the case lead, and chooses a starting town on the same world map. Choosing the town is one command outcome and the first free arrival; there is no second choice or journey-time charge. See the [hunt creation contract](../.agents/investigations/stable-0.1.0/2026-10-07-hunt-creation-contract.md).

**Authority, failure and history:** The server and event history establish setup and phase. The world is settled before the prologue is read; only player location is selected afterward. Acknowledgement advances the current prologue phase and cannot regenerate the case or substitute a different clue. A repeated town selection after arrival is illegal; an actual transport retry may return the original result without reapplying it.

**Dependencies:** `PG-003` is **consumed** for the starting-town choice. A started session supplies identity and settled world state to `PG-002`, `PG-005`, `PG-006`, `PG-007` and `PG-008`.

**Assessment and evidence:** Partial across the complete player lifecycle. Backend genesis now has an acceptance test proving that normal setup persists generated world and case facts, that production event decoding and replay restore them, and that the legal prologue acknowledgement advances the same history without rerolling salt. Browser seed submission, setup/resume and the full start journey remain unproven or broken. See the [player genesis replay acceptance test](../tests/WildBunch.Integration.Tests/Acceptance/PlayerSetupReplayAcceptanceTests.cs), [Application source audit](../.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md), [Application test audit](../.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md), [Web source audit](../.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md) and [Web test audit](../.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md). The acceptance test proves backend behavior, not a browser journey.

**0.1.0 disposition:** Retain and repair. Do not bypass the prologue or first-town choice for quick play. A complete hunt-resolution/win flow is not established and is not required to call this deliberately incomplete game 0.1.0.

### PG-002 - Resume, archive and start over

**Promise and entry:** A player can leave and return to the selected game, resume from the server's saved phase, and choose Start Over in Game Settings. One game is active in the current player flow; selecting among multiple retained playthroughs is not a 0.1.0 feature.

**Authority, failure and history:** Browser storage is not game authority. Confirming Start Over commits an archive event before returning to setup; that decision persists if the browser closes, the user signs out, or no replacement game is created. Cancelling changes nothing. Retain archived history; restoration and multi-playthrough discovery are later work. See the [playthrough lifecycle contract](../.agents/investigations/stable-0.1.0/2026-10-07-playthrough-lifecycle-contract.md).

**Dependencies:** **Requires** an established game from `PG-001`; archival **changes** availability of all surfaces tied to that game. Resume **consumes** event-backed state and persistence/recovery support in `PLAT-001`.

**Assessment and evidence:** Partial. Archive and session-selection surfaces exist, but saved-phase resume, identity changes and pending/failure recovery have known gaps. See the [Application](../.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md), [Persistence](../.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md) and [Web test](../.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md) assessments. They are source/test reviews, not a browser resume run.

**0.1.0 disposition:** Retain and repair saved-phase continuation and event-backed confirmed archive. Cross-tab push, account-backed game discovery and restoring archived games are excluded.

### PG-003 - Consult one world map and navigate towns

**Promise and entry:** One world map supports the explicit starting-town choice and later travel, each under its own legal phase rules. Every town visibly contains a Saloon, Sheriff, Store and Telegraph. `TownLayout` is the authority for building presence; available actions are the authority for usability. Saloon, Sheriff and Store actions are available in every town, while the Telegraph remains visible and disabled. Generated town layouts persist between visits; refreshing or replaying does not generate different geometry.

**Authority, failure and history:** The server owns map destinations and legal selection; the browser renders and reports the selected destination. The starting choice is one-shot under `PG-001`; later movement uses `PG-007`. Actual keyboard and pointer interaction, eligible destinations and recoverable read states are part of the player contract.

**Dependencies:** Supplies map selection to `PG-001` and `PG-007`. Town navigation provides entry points to `PG-004`, `PG-005` and `PG-006`; entry does not vary by town service flags or make later travel depend on setup.

**Assessment and evidence:** Partial and broken at accessible interaction boundaries. The one-map design is settled, but keyboard selection was lost and several existing tests assert counts or implementation structure rather than independently expected map behavior. See the [Web architecture spike](../.agents/investigations/stable-0.1.0/2026-10-07-web-architecture-spike.md), [Web source audit](../.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md), [Web test audit](../.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md) and [Domain test audit](../.agents/investigations/stable-0.1.0/2026-10-07-domain-test-followup.md). No full keyboard browser flow is recorded.

**0.1.0 disposition:** Retain and repair one map and usable town navigation. Do not keep separate start and travel map authorities or add a town-layout editor.

### PG-004 - Buy supplies and manage player resources

**Promise and entry:** A player buys the currently supported supplies from the one Store available in every town. The store's stock and the price of each offered item vary with town prosperity. Equipment is unique by item type; consumables can stack. A player cannot acquire a second horse or canteen without benefit. When a horse dies, it leaves inventory and the saddle remains. Arrival refills a canteen only when the player owns one.

**Authority, failure and history:** Purchases and losses change event-backed wallet/inventory state. Counts and remaining charges never fall below zero; loss records the actual quantity removed. Invalid purchase intent is rejected. Detailed stock, price and fine values remain in the [baseline specification](../.agents/specs/2026-10-07-stable-0.1.0-baseline.md#product-boundary) and [store/inventory contract](../.agents/investigations/stable-0.1.0/2026-10-07-store-and-inventory-boundaries.md).

**Dependencies:** **Requires** an active game and a town Store. Purchase output **changes** inventory and wallet state consumed conditionally by `PG-006` and `PG-007`. Town prosperity **changes** store stock/prices; distinct vendor identity does not.

**Assessment and evidence:** Partial. The one-store catalog, prosperity-tier item/price matrix and item-only purchase contract are implemented across Domain, Application, HTTP and browser boundaries. PG-004 remains partial because horse/canteen lifecycle and other resource ownership/consumption contracts have not all been certified, and `Rifle` still has neither a current offer nor a starting grant. See the [store/inventory boundary](../.agents/investigations/stable-0.1.0/2026-10-07-store-and-inventory-boundaries.md) and linked layer/test investigations for remaining evidence and gaps.

**0.1.0 disposition:** Consolidate to one Store and repair. Preserve coherent existing item/pricing behavior by prosperity; distinct stable, gunsmith and other vendor services are future features.

### PG-005 - Gather public investigation knowledge

**Promise and entry:** The player can learn from the prologue lead, wanted posters and public notices, reachable saloon gossip, and saloon observation. The sheriff-office records interaction is public-noticeboard content in disguise; the player reads the public board, not privileged case records. An observed person is evidence about a distinguishing feature, not a solved identity. The player draws conclusions from the facts collected.

**Authority, failure and history:** Learned claims preserve their actual text, source, identity only when explicitly established, and recorded time/place details. Unknown subjects remain unknown. Reading a source records only the knowledge/outcome the player actually received. The opening culprit clue is sourced to the Prologue. Telegraph remains visible but disabled and has no 0.1.0 command, route or generated clue source.

**Dependencies:** **Requires** an active game and the relevant town/source context. Produces player-known facts **consumed by** `PG-006` and `PG-008`; source availability and return-visit refresh are affected conditionally by `PG-007`.

**Assessment and evidence:** Partial and partly unreachable. Saloon gossip is implemented; the opening clue provenance and removal of Telegraph clues/actions are covered by generated-case, Domain and API behavior tests. Sheriff records and noticeboard surfaces still do not match the accepted public-board story; row 11 owns that surface consolidation. See the [Domain](../.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md), [Application](../.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md), [Web](../.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md) and corresponding test follow-ups. Browser evidence for the retained source journey is not recorded.

**0.1.0 disposition:** Retain and repair the prologue, public noticeboard, wanted information, saloon gossip and observation. Retire privileged-record access and the two redundant telegraph gang clues; leave telegraph unavailable pending the lawman feature. Do not add an automatic deduction engine. ADR-0024 clarifies that earlier newspaper/source forecasts are historical exploration, not a current promise; no newspaper interaction is included in 0.1.0.

### PG-006 - Name and take in a saloon person

**Promise and entry:** In a saloon, the player may look around and see a person described by distinguishing features. A take-in attempt requires the player to name the person; suspicion or a matching clue alone is not enough. A citizen comes quietly, is released by the sheriff, and incurs the existing $10 fine capped at available cash. A wanted person requires the player to own a gun to compel a take-in; ammunition is not required. A wrong name releases the person, pays no bounty and gives no gang-roundup credit. A correct name can pay the bounty and count that distinct gang member toward the five-of-six culprit-release gate in the seven-member gang.

**Authority, failure and history:** The server validates current saloon presence and naming. Outcomes, fine and bounty are recorded through the event-backed game session and reconstruct through replay. The culprit's identity is settled at game start; lawful settlement of five distinct other members of the seven-member gang releases the already-known culprit into encounter eligibility. Killing the culprit would fail the hunt while leaving corpse bounty payment possible, but no kill flow is implemented in 0.1.0.

**Dependencies:** **Consumes** relevant learned identity/public information from `PG-005`. Lawful outcomes **change** wallet, case and culprit-release state consumed by `PG-008`; player equipment from `PG-004` is a conditional prerequisite for taking in wanted people. The interaction does not require or establish `PG-009`.

**Assessment and evidence:** Partial and broken at lawful reachability and outcome boundaries. Current tests include seeded presence and expectations inconsistent with named identity, firearm, citizen, wrong-name and culprit-gate rules. See the [saloon challenge contract](../.agents/investigations/stable-0.1.0/2026-10-07-saloon-challenge-contract.md), [culprit contract](../.agents/investigations/stable-0.1.0/2026-10-07-culprit-identity-and-release-contract.md), [Domain source audit](../.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md), [Domain test audit](../.agents/investigations/stable-0.1.0/2026-10-07-domain-test-followup.md) and [Web test audit](../.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md). No complete browser take-in journey is recorded.

**0.1.0 disposition:** Retain and repair named take-in, citizen/wanted distinction, correct/wrong-name settlement and the five-of-six release gate. Resistance rolls, escape/injury, saloon gunfights, sheriff poster circulation and murder consequences from killing an innocent are future features.

### PG-007 - Travel between towns

**Promise and entry:** A player previews a legal route, departs, advances trail days, resolves available encounters and resources, completes the journey, and acknowledges arrival in the selected destination. Existing hostile trail encounters remain. Generated interactive friendly trail-NPC encounters are not part of 0.1.0.

**Authority, failure and history:** The server owns route progress, actions, resource outcomes and arrival. Remaining resources floor at zero; daily food upkeep consumes a meal when available, otherwise costs 25 HP. Any cause reaching zero health records terminal death in the same committed operation and stops later rewards/arrival. Pending choices, refresh and retry cannot charge a day or effect twice. The full [travel and resource contract](../.agents/specs/2026-10-07-stable-0.1.0-baseline.md) owns exact outcomes.

**Dependencies:** **Requires** an active game, a legal route from `PG-003`, and resources/equipment as applicable from `PG-004`. Journey completion **changes** town/visit/context state used conditionally by `PG-004`, `PG-005` and `PG-006`. `PG-008` consumes the recorded account of journey events.

**Assessment and evidence:** Partial with confirmed reachable failures in route distance, low-resource generated outcomes and transition/replay boundaries. Existing hostile encounters are retained. See the [Domain](../.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md), [Persistence](../.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md), [Application](../.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md), [Web](../.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md) and linked test follow-ups. Audits identify meaningful existing and missing test scenarios; they are not a complete runtime journey proof.

**0.1.0 disposition:** Retain and repair legal travel, hostile encounters, resource settlement, death and arrival. Generated interactive friendly trail-NPC encounters and their exclusive choices/weights/developer option are retired; friendly strangers are not converted into enemies and no replacement interaction is added. Hostile encounters remain a retained travel path, so PG-007 remains partial.

### PG-008 - Keep a casebook and journal

**Promise and entry:** The player can open the casebook to review accumulated clues, notes and public wanted information, including details about people marked as captured. Capture changes the person's status; it does not tear out or hide the page. The player draws conclusions. The player can also read one in-world journal: a full-playthrough view and a journey-focused view of the same authored history.

**Authority, failure and history:** Both views derive from the same authoritative event stream. Casebook records include only learned facts and explicit identity associations; unknowns stay unknown, and an unnamed observation does not become a named sighting from a feature match. Journal narration reports established player actions and effects without inventing provenance, success or technical audit text. Repeated reads do not create history. Developer audit is a separate audience projection, not another source of truth.

**Dependencies:** **Consumes** established world/session facts from `PG-001`, knowledge from `PG-005`, outcomes from `PG-006` and `PG-007`, and the selected `PG-002` history. Opening either view does not require a completed investigation.

**Assessment and evidence:** Partial and broken. Caseboard inference currently invents conclusions, captured-warrant filtering hides learned detail, and journal/diary paths duplicate authorship and invent some occurrence facts. See the [baseline casebook and journal decisions](../.agents/specs/2026-10-07-stable-0.1.0-baseline.md#product-boundary), [Application](../.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md), [Persistence](../.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md), [Web](../.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md) and related test follow-ups. Existing UI tests do not by themselves prove safe, truthful serialized responses or a complete in-world account.

**0.1.0 disposition:** Retain and repair an organized, truthful casebook and one event-derived journal with full and journey views. Retire automatic identity/deduction assistance as a separate future feature; keep full developer audit out of player surfaces.

### PG-009 - Independent unrelated-criminal bounties

**Promise and entry:** Retired from the 0.1.0 product. The former shell generated independent warrants and ledger state without a complete discover/encounter/capture/settlement player loop. Saloons continue to choose eligible gang members, citizens or nobody.

**Authority and history:** The one-time `20261008180000_InvalidatePreAlphaPlaythroughs` migration deletes all existing `GameSessions` rows and dependent event/cache rows through existing foreign-key cascades when applied. It preserves schema and applied migration history; it does not drop or recreate a database or migration chain. The migration has been tested against isolated integration databases; no claim is made that any other database has already been migrated. See [PG-009-R](../.agents/investigations/stable-0.1.0/2026-10-07-unrelated-criminal-removal.md). The distinct future addition remains [PG-009-A](../.agents/investigations/stable-0.1.0/2026-10-07-unrelated-criminal-addition.md).

**Dependencies:** The former shell shares generic warrant/poster, identity, wallet and settlement concepts with `PG-005`, `PG-006` and `PG-008`; those shared behaviors remain. The future feature would require lawful discovery, active encounter, identity, secured-person settlement and replay/persistence; its independent addition record owns those details.

**Assessment and evidence:** The live unrelated roster, ledger, settlement event, replay/projection effects, codecs and feature-only tests have been removed. The [dependency assessment](../.agents/investigations/stable-0.1.0/2026-10-07-unrelated-criminal-feature-boundary.md) records the pre-retirement source audit; the [Domain](../.agents/investigations/stable-0.1.0/2026-10-07-domain-test-followup.md) and [Persistence](../.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md) test audits record the disposition of tests that protected that shell.

**0.1.0 disposition:** Retired. Generic gang/citizen/warrant/poster behavior remains. No former authored unrelated roster is an approved reusable candidate pool; prior content is recoverable from Git history only, and PG-009-A must revalidate and re-author any future content.

### PG-010 - Lawman pursuit and pressure

**Promise and entry:** No active lawman pursuit feature is promised in 0.1.0. The displayed heat value is not a promise of tracking, interception, route danger or encounter difficulty.

**Authority and dependencies:** The current ADR defines heat as town/lawman attention, not trail danger; it currently has no lawman effect. Future pursuit **requires** a separately designed lawman, recorded movement and lawful player-facing information. It is not a prerequisite for retained hostile trail combat in `PG-007`.

**Assessment and evidence:** Planned only for the pursuit lifecycle. Existing heat state/display does not constitute one. See [ADR-0029](decisions/ADR-0029-heat-is-future-lawman-pressure-not-trail-danger.md) and the [Domain investigation](../.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md). No pursuit runtime or test proof is claimed.

**0.1.0 disposition:** Excluded; retain truthful heat meaning and record lawman pursuit as future work.

### PG-011 - Telegraph information

**Promise and entry:** The telegraph is present as a core town service but unavailable in 0.1.0. Sending a telegram and following a lead are distinct claims; action vocabulary or clue types alone do not establish a working player interaction.

**Future boundary and dependencies:** The first future telegraph capability is lawman intelligence: lawfully bribe a clerk to read a report of the lawman's recorded location, movement and timing, with source and age visible. It **requires** `PG-010`'s lawman and event-backed movement/report facts. A later iteration may add other telegraph clue types. A stale report must not become an omniscient current-position query.

**Assessment and evidence:** Disabled/unreachable. Current code has disabled navigation and dormant or misleading gang-clue/send vocabulary. See the [Domain](../.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md), [Application](../.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md), [Web](../.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md) and linked test follow-ups. No working telegraph browser path is evidenced.

**0.1.0 disposition:** Keep unavailable; remove misleading live promises and redundant gang identity clues. First future addition is lawman intelligence only.

## Partitioned feature work

These linked records keep an agreed 0.1.0 retirement or repair distinct from a later feature addition. They preserve user stories and integration obligations for future planning; they do not authorize that work or replace the feature-specific design records.

| Work ID | Capability | Work boundary | Current record |
| --- | --- | --- | --- |
| PG-004-R | PG-004 store boundary | Consolidate current offers into one Store and remove distinct vendor identities from the 0.1.0 path while retaining prosperity-based price and stock behavior. | [Store and inventory boundary](../.agents/investigations/stable-0.1.0/2026-10-07-store-and-inventory-boundaries.md). |
| PG-004-A | PG-004 store boundary | Add distinct vendor types and vendor-specific availability/economics when their player-facing lifecycles are implemented. | [Store and inventory boundary](../.agents/investigations/stable-0.1.0/2026-10-07-store-and-inventory-boundaries.md). |
| PG-006-C | PG-006 saloon take-in | Repair named take-in, citizen cooperation, wanted-person firearm requirement, correct/wrong-name settlement, and the five-of-six culprit-release gate. Resistance rolls are outside this current repair. | [Saloon challenge contract](../.agents/investigations/stable-0.1.0/2026-10-07-saloon-challenge-contract.md) and [culprit identity/release contract](../.agents/investigations/stable-0.1.0/2026-10-07-culprit-identity-and-release-contract.md). |
| PG-006-A | PG-006 saloon challenge | Add a named gunfight choice and outcome: winning requires a gun and compatible ammunition; accepting the challenge without them can kill the player. The player must win and have named the wanted person correctly; a wrong identity can lead to sheriff arrest for murdering an innocent. Killing the culprit fails the hunt even if the body earns a bounty. | [Saloon challenge contract](../.agents/investigations/stable-0.1.0/2026-10-07-saloon-challenge-contract.md) and [culprit identity/release contract](../.agents/investigations/stable-0.1.0/2026-10-07-culprit-identity-and-release-contract.md). |
| PG-006-B | PG-006 sheriff settlement | Add town-specific sheriff recognition based on wanted-poster circulation; correct player naming remains required in 0.1.0. | [Saloon challenge contract](../.agents/investigations/stable-0.1.0/2026-10-07-saloon-challenge-contract.md). |
| PG-009-R | PG-009 unrelated criminals | Retired for 0.1.0. The live roster and ledger loop are removed; the migration invalidates existing pre-alpha sessions and dependent rows while preserving schema and migration history. | [Retirement outcome and code scope](../.agents/investigations/stable-0.1.0/2026-10-07-unrelated-criminal-removal.md). |
| PG-009-A | PG-009 unrelated criminals | Add a fully integrated independent bounty-target lifecycle later, including lawful discovery, encounter, identity, settlement and replay/persistence. | [Future story and integration obligations](../.agents/investigations/stable-0.1.0/2026-10-07-unrelated-criminal-addition.md). |

## Developer capabilities

### DEV-001 - Prepare real salted decisions

**Audience and promise:** Local developer tooling may adjust a salt immediately before a real generator/decision consumes it, so a developer can inspect deterministic variation without changing the player's authority. It is one vertical developer-control capability; town-layout salts are a concern within it, not a separate town-layout editor.

**Authority and dependencies:** A changed input affects only decisions made after it is supplied. Existing world, town layout, culprit identity or other settled facts do not reroll. Chosen input and gameplay outcomes remain event-backed; replay reconstructs recorded facts and never samples entropy. It may help investigate `PG-001`/`PG-003`, but is not required for player play.

**Assessment and evidence:** Excluded from stable 0.1.0. This slice removes the manual town-layout override panel and its exclusive prep/start backend, along with the unregistered session RNG lock/clear paths and their event mutations. Normal player Go still settles the world; generated layout facts remain in the world/map contract and survive persistence and event replay. Other current developer tooling includes session/audit/context reads, next-saloon and next-travel preparation/clear operations, and difficulty/entropy changes; those are separate controls, not a promise that this salt-control feature works. See the [developer control boundary](../.agents/specs/2026-10-07-stable-0.1.0-baseline.md#product-boundary), [Domain](../.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md), [Application](../.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md) and [Persistence](../.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md) assessments.

**Disposition:** Retired from 0.1.0. Independently useful developer tools may remain only where they already work; they are not a 0.1.0 repair obligation. Preserve normal generation, recorded layouts, shared renderer, normal salt derivation and replay behavior. No developer controls appear in the public game. A future preview using alternate town-layout salts is separately deferred and must leave the authoritative layout unchanged.

## Supporting platform requirements

### PLAT-001 - Event history, saved state and recovery

**Purpose and scope:** Event history is the only authority for what happened. The continuously maintained cache supplies current game state; when invalid, supported history rebuilds it. Snapshots and projections are caches, and read models never create gameplay facts. Preserve the event/schema migration chain and upcasters for retained facts. No pre-0.1.0 random playthrough needs retention; any initial discard is explicit and scoped to playthrough data/dependent events/caches, never a database reset or migration-history deletion.

**Assessment and dependency:** Required by `PG-001`, `PG-002` and every feature that persists/resumes or projects an outcome. Row 06 established replay-complete normal genesis and recorded-salt restoration. The first row 07 slice rebuilds player/journal reads when `SnapshotVersion` trails `StreamVersion`, and reads envelope, components, ordered events and diary rows from one PostgreSQL `RepeatableRead` snapshot. The current Player-cache slice now recovers current-version null `wallet`, `inventory` and `inventory.items` shapes from production-decoded events through aggregate, player-read and journal-read paths. PostgreSQL tests prove the reads preserve the malformed cache, component version, stream position and event count; a later legal purchase writes recovered state through the normal unit of work, while unsupported event history fails closed. PLAT-001 remains partial: the current travel-diary cache now records its applied stream version and projected day count; missing trailing/interior rows, orphan rows and legacy null watermarks rebuild from the decoded event stream without query writeback, and the next legal command save repairs sequence-keyed rows in its existing unit of work. PostgreSQL tests cover both aggregate and read paths, persistence preservation during recovery, a later save and fresh reads. Malformed shapes outside the covered Player fields, other missing/partial projection recovery, command-load consistency, retry classification and other production restoration still need row 07 work. The [row 07 roadmap](../.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md#07-persistence-restoration-and-retries), [cache-state spike](../.agents/investigations/stable-0.1.0/2026-10-07-cache-state-architecture-spike.md), [Persistence audit](../.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md) and [test audit](../.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md) describe evidence and remaining limits.

**Disposition:** Required supporting capability for stable 0.1.0; not a player feature by itself.

### PLAT-002 - Public identity and session isolation

**Purpose and scope:** A future public browser deployment needs a player identity associated with saved playthroughs, Google sign-in as the selected starting provider, and server-side ownership checks so one player cannot read or act on another player's game. The pre-alpha local baseline has no public login or multi-user isolation and cannot be exposed as the public game until those boundaries exist.

**Assessment and dependency:** Not implemented in the 0.1.0 local release. This is a prerequisite for public deployment and account-backed resume in `PG-002`; browser session expiry must not become game-state authority.

**Disposition:** Excluded from this cleanup roadmap; owned by the separate [cloud playability specification](../.agents/specs/2026-10-06-cloud-playability-and-releases.md).

### PLAT-003 - Environment isolation and cloud delivery

**Purpose and scope:** A future cloud release requires containerized application delivery, infrastructure-as-code or an equivalent reproducible deployment definition, separate developer/preprod and public-production access, and a deliberate release path. Kubernetes is not currently justified. Hosting provider choice is not settled; a small VPS or another suitable low-player-count host is under consideration.

**Assessment and dependency:** Not implemented by this local baseline. It depends on user isolation in `PLAT-002`, and preprod must not expose developer controls to public play. The developer playtest environment is a preproduction environment, not a second public audience.

**Disposition:** Excluded from this cleanup roadmap; the [cloud roadmap](../.agents/roadmaps/2026-10-06-cloud-playability.md) owns the later deployment investigation.

### PLAT-004 - Versioned integration and release identity

**Purpose and scope:** Gitflow uses `develop` for ordinary integration PRs and `main` for qualified releases/hotfixes. One authored application version lives in `Directory.Build.props`; every merge to `develop` advances one `0.1.0-dev.N` checkpoint, and release candidates/stable releases bind to verified source and derived artifacts.

**Assessment and dependency:** In progress in this epic. It supports delivery of every included capability but does not change player gameplay. Pre-1.0 development makes no public API or gameplay-compatibility promise; this product's current `0.y.z` policy is recorded in the [release specification](../.agents/specs/2026-10-06-cloud-playability-and-releases.md).

**Disposition:** Required release support. Roadmap row 18 adopts and certifies the then-current accepted immutable Gitflow/SemVer definitions and qualifies the 0.1.0 source. This epic does not define a future `1.0.0` contract.

## Cross-cutting boundaries

CQRS and event sourcing apply at the implementation boundary: commands validate and produce facts through the game aggregate; queries return safe projections; replay restores recorded facts without rerolling randomness. These are system obligations, not dependency edges between every pair of features. The [backend architecture unslop profile](../.agents/unslop/backend-architecture.md), [architecture guardrails](../.agents/doctrine/architecture-guardrails.md) and [event-sourcing integrity doctrine](../.agents/doctrine/event-sourcing-integrity.md) own their implementation constraints.

The game can be a stable, intentionally incomplete `0.1.0`. An unfinished hunt, deferred lawman, disabled telegraph, removed unrelated-criminal loop, or future developer tool does not become a hidden promise because the code contains a matching enum, type or test. Update this matrix when those promises or their dependencies change, and keep future additions separate from the retirement work that clears the 0.1.0 boundary. Container delivery, infrastructure-as-code and environment isolation remain in the separate [cloud roadmap](../.agents/roadmaps/2026-10-06-cloud-playability.md).
