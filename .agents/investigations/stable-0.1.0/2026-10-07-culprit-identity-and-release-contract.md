# Culprit identity and release contract

**Status:** Historical investigation and product-decision record. The dated clarification below settles the current 0.1.0 threshold; earlier statements that the threshold remained undecided are historical.

## 2026-10-10 current 0.1.0 clarification

The user settled the threshold: the generated gang has seven members, one culprit and six other members; five distinct other members must be brought to justice through successful lawful settlement to release the culprit into encounter eligibility. Clue discovery, duplicates, citizens, failed attempts and wrong-name outcomes do not count. Wanted-poster identity remains discoverable before release through the Sheriff Office. No separate town noticeboard or private sheriff-records surface exists in 0.1.0. PG-006 and the culprit identity/release rows in the baseline specification and `docs/features.md` own the current contract.

The original example below (five gang members and three settlements) and its undecided-threshold statements preserve the state of the investigation when authored; they are not the current threshold or an implementation option.

## Two independent capabilities

Investigation allows the player to identify the culprit before that person becomes encounterable. The prologue provides a distinguishing characteristic; public wanted information can associate a name and gang affiliation with that characteristic. For example, a poster can describe Elzy Lay as a Wild Bunch member with no left ear while the case book records that the killer belongs to the gang and has no left ear. Preserve the player's ability to make that inference without disclosing an internal culprit flag.

Gang roundup flushes the culprit out of hiding. For a gang of y members including the culprit, initially y - 1 members are at large and the culprit is hidden. Bringing x distinct other gang members to justice, dead or alive, opens the release gate. The remaining uncollected gang members stay at large, and the culprit joins the eligible encounter population. This does not create or replace a gang member, change the culprit's identity, or guarantee an immediate encounter.

The user's example is five gang members total, four initially at large, and release after three are brought to justice. It illustrates the rule; it does not select five/three as universal constants or define difficulty-dependent threshold policy. Choose the supported x/y configuration explicitly and ensure the required roundup is achievable.

## Event authority and negative rules

Successful gang settlement is the progression source. Reading clues, viewing the prologue, reading a poster, spotting someone, declaring an identity, failing a confrontation or turning in an unrelated criminal must not advance this gate. A previously settled member cannot count twice. Dead and alive successful outcomes count toward the same roundup requirement. Replay must reproduce the count and release eligibility from authoritative facts; direct counter mutation or an unrecorded command-side unlock cannot establish game truth.

Identity discovery and encounter release have different dependencies: investigation produces player knowledge, while successful gang settlement produces release progress. Neither is a substitute for the other. Public wanted information about the culprit must be eligible before release; hiding from encounters does not remove the person's public identity. Preserve existing knowledge across the release transition.

## Corrections against source when investigated

CaseFile currently associates release advancement with an optional clue-discovery flag that production callers do not enable. SeedCaseBuilder sets NormalReleaseThreshold to five. BountyLoop excludes the culprit from saloon eligibility until release, which matches the product rule. WantedPosterResolver also excludes the culprit's warrant until release, which conflicts with independent early identity discovery. Separate these policies rather than either deleting the gate or enabling progress for every clue.

Trace the successful gang-turn-in event/application path, distinct settlement identity, projections/snapshots and full replay before implementing the roundup counter. SheriffTurnInSettled already carries target identity and alive/disposition facts; decide whether existing settlement facts sufficiently derive progress or whether a new release fact is warranted. Do not add an event solely because the counter needs a setter. Review historical progress semantics and use explicit compatibility handling if changing their meaning.

## Behavioral coverage

Prove that the culprit's public identity can become known while encounter eligibility remains locked; unrelated clues/poster reads do not advance release; x - 1 unique gang settlements leave the gate closed; the xth opens it with both alive and dead qualifying outcomes; duplicate settlement cannot advance it again; other remaining gang members retain eligibility; and replay reproduces the correct release state. Use independent expected outcomes rather than tests that merely compare command state with equally wrong replay state. Fix the normal saloon presence path identified in DN-02 so tests do not bypass encounter legality to demonstrate this feature.

## Culprit death and losing the hunt

The user explicitly establishes that killing the actual culprit makes clearing the player's name impossible: the only person who could clear them is dead, so the player loses. The body can still be taken to the sheriff and the bounty collected. A successful bounty transaction is therefore not a successful hunt outcome. Dead/alive roundup eligibility for other gang members must not erase this distinction for the culprit.

Loss follows culprit death, not merely body turn-in or reading their identity. The implementation design must reconcile irreversible loss with the permitted subsequent corpse settlement; do not add a blanket failed-session command guard that prevents the specified bounty collection. Do not defer the failure fact until turn-in just to reuse its endpoint. Record/replay the death, failed hunt and permitted settlement consistently, and expose the outcome without depending on whether the player had correctly inferred the culprit's identity before killing them.

This outcome had not been established in the preceding release-gate assessment. The inspected WantedSuspectConfronted application records confrontation state, and SheriffTurnInSettled application pays the bounty and records settlement; those paths alone do not establish implemented terminal hunt behavior. Assess terminal production, projection, command availability and historical reconstruction before claiming this rule works. GameStatus.Failed's existence is insufficient evidence.

Behavioral coverage must distinguish killing a non-culprit from killing the culprit, failed hunt despite legitimate corpse bounty payment, no duplicate payment, and resumed/replayed loss. Capturing the culprit alive must not be treated as equivalent to killing them; its precise clear-name/win transition remains a separate product contract to settle.

## Inventory dependencies

Telegraph leads are not retained gang-identity investigation. The user defines their future purpose as paid access to recent messages about the pursuing lawman's whereabouts; the feature is paused while lawman gameplay is absent. Reconcile current TelegraphLead gang/culprit clue content with retained public notices/posters/gossip and solvability during cleanup. Do not rely on enabling telegraph to provide a required identity clue.

PG-005 investigation supplies identity evidence; PG-008 case/poster displays preserve it; PG-006 successful gang settlement advances the newly identified roundup/release capability. That capability controls later PG-006 culprit encounter eligibility. Record release as its own mechanic/capability boundary rather than folding it into a generic investigation action. See the [feature matrix](../../../docs/features.md).

## 2026-10-08 implementation outcome: town sources and Prologue provenance

The 0.1.0 town-service slice keeps the opening culprit clue in the settled case and marks its source as `Prologue`. Its text remains the distinguishing feature the player reads before choosing a starting town. The two generated gang clues formerly attributed to `TelegraphLead` are removed; public wanted identities remain available through the retained notices and warrants. Telegraph stays visible in every town layout but has no investigation source, player action, API route or browser affordance. This changes clue-source provenance and removes redundant clues without changing the independent identity-discovery and gang-release rules above.

## 2026-10-11 implementation outcome: settlement-derived five-of-six release

`CaseFile.KillerReleaseProgress` is now derived from unique `SheriffTurnInSettlementState` facts whose target matches a generated warrant classified as a Wild Bunch gang member that advances Wild Bunch pressure. The culprit's own settlement is excluded. The generated seven-member gang has six other members, and five qualifying settlements release the culprit into encounter eligibility. Clue discovery no longer has a progress flag or counter mutation. The event schema is unchanged: `CaseFileGenerated` establishes suspect/warrant identity and each successful `SheriffTurnInSettled` establishes a settlement; replay reconstructs the same progress. The cached progress field is ignored in favor of the settlement list, while settlement-cache recovery continues to compare that list with event history.

Wanted-poster eligibility is independent of the release gate, so a poster in the Sheriff Office can identify the culprit while saloon candidate selection still excludes them. Existing knowledge remains visible after capture. Domain tests settle four then five distinct gang members through normal command/event paths, prove clue/non-gang/culprit/citizen/wrong-name/refusal/repeat outcomes do not advance progress, and replay the fifth settlement. PostgreSQL tests reload after four, continue with a fifth, verify the gate opens, and verify that a stale cached progress field cannot override intact settlements. Player response boundaries continue to keep the hidden culprit flag and release counter private.

ADR review found no existing durable decision contradicted by the change: the event-sourcing decision and dev saloon guard still describe the implemented behavior. No ADR amendment or successor was needed. This implementation does not add a new release event, alter serialized event shapes or promise compatibility for disposable pre-alpha playthroughs.
