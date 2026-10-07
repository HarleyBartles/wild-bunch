# PG-009-R: Remove incomplete unrelated-criminal gameplay

**Work class:** Stable 0.1.0 feature retirement. **State:** Scope agreed in principle; detailed historical policy and implementation plan remain pending. This record owns the removal story and proposed work boundary. The [PG-009 assessment](2026-10-07-unrelated-criminal-feature-boundary.md) supplies source evidence; [PG-009-A](2026-10-07-unrelated-criminal-addition.md) preserves the separately deferred future feature. No product/test code or data was removed.

## Player story and intended result

As a player pursuing the Wild Bunch, I encounter eligible gang members, ordinary citizens or nobody in saloons, discover retained gang-member warrants, and use the supported gang confrontation/bounty loop. New hunts offer no independent non-gang bounty targets or their warrants. Citizen mistaken-identity consequences, gang bounties and hidden culprit rules remain.

The current saloon candidate pool already has that person scope. Removal principally stops new unrelated warrant generation and retires the incomplete ledger/settlement coupling. It must not delete the common wanted-poster, sheriff, citizen or bounty features simply because they share code with PG-009.

## Code points and proposed treatment

| Owner / code points | Retirement work | Boundary to preserve |
| --- | --- | --- |
| GameContent: SeedCaseBuilder.CreatePublicWarrants; CaseCharacterRoster unrelated pool/selectors | Stop adding unrelated warrants to new cases; remove unused live generation paths after caller review. Record a custody decision for reusable authored character/warrant material. | Gang/culprit warrants, deterministic supported generation and existing historical setup facts. |
| Domain: CaseWarrants.InvestigationTargetKind, WantedPosterResolver; InvestigationLoop context; GameSession.RetiredWarrantIds | Remove live unrelated eligibility/retired-pool plumbing where no retained behavior needs it. Reconcile supported old warrant payloads separately. | Common warrant terms, gang affiliations, culprit gating, investigation knowledge and source/visit rules. Persisted enum numbers must not be repurposed. |
| Domain: UnrelatedCriminalLedger; GameSession initialization/restoration and BuildUnrelatedCriminalLedger; BountyLoop field/Apply hooks | Retire active population, replacement, collection and parity state from new live sessions. Remove gang settlement's unrelated population side effect. | Gang settlement, payment, duplicate prevention and authoritative history. |
| Domain: GameSession.SettleUnrelatedCriminalTurnIn; unused UnrelatedCriminalTurnInContext | Remove the dormant direct turn-in capability after historical and consumer review. | Retained SuspectId-based gang turn-in and citizen/wrong-declaration outcomes. No production unrelated settlement caller was found. |
| Application/API: poster/case mapping; gang confrontation/turn-in handlers | Review common consumers for assumptions about unrelated data; preserve existing player commands and transport contracts. | No unrelated HTTP turn-in endpoint was found. Do not invent one during retirement or remove the shared sheriff settlement path. |
| React: SheriffPlace, SaloonPlace, WantedPosterSurface, CaseFileSurface and their DTO consumers | Reconcile new-hunt data/display and supported historical entries. Remove feature-specific copy/branches only where a caller trace proves them exclusive. | Sheriff records/posters, citizen/gang selection, wanted identity declaration and lawful player-safe information. No unrelated saloon branch or sheriff turn-in button currently exists. |
| Replay/projections: UnrelatedCriminalTurnInSettled; GameSession Apply/replay; HUD and diary wallet projectors; event deserializer | Stop new production of the feature event; implement the selected historical policy before retiring readers/effects. | Recorded wallet changes and readable historical streams. Live feature retirement and historical event support have different lifetimes. |
| Persistence: ledger component name; EF component write/read; PersistedPayloadLoader; ledger/session snapshot codecs | Retire new live ledger writes and reconstruction; handle old rows, snapshots and case data by explicit versioned policy. | Applied migrations and selected compatibility. No blanket database reset. |
| Tests: ledger unit/wiring/persistence/full-replay fixtures; shared poster/roster/component/migration expectations | Retire feature-only live assertions with the source. Correct shared fixture dependencies and keep selected compatibility proof. | Gang/citizen legality, hidden truth, actual payout/replay, source refresh and independent storage/transport boundaries. |
| Documentation and operating surfaces | Update PG-009 status/dependencies and material ADR history; route later implementation/review to the accepted matrix. | Preserve that the partial feature existed and why it was retired. Future addition remains separately recorded. |

Exact source links and mechanisms are in the [boundary assessment](2026-10-07-unrelated-criminal-feature-boundary.md#dependency-and-removal-impact). This is a work-shaping record, not a deletion list: a compatibility-only event/type/reader may remain after its gameplay producer is retired.

## Cross-feature dependencies

- **PG-001, setup:** changes which warrant data new hunts generate. Preserve established old setup facts; pool changes can alter selection results for identical seed/visit inputs.
- **PG-005, investigation:** stops producing unrelated poster knowledge; preserves gang warrants and source/visit behavior.
- **PG-006, confrontation:** removes only unrelated parity coupling from gang settlement; preserves citizens, gang identities, lawful confrontation and fine/payment semantics. The separately recorded missing gang presence transition remains a PG-006 correction, not an excuse to expand this retirement into rebuilding the whole loop.
- **PG-008, reference/history:** reconciles displays and historical wallet/warrant facts under the selected old-playthrough policy.
- **PG-002 and supporting persistence/replay:** historical handling determines whether affected existing hunts resume, migrate or are explicitly invalidated.

Retirement must settle these contracts before its implementation plan is accepted. An unavailable retained dependency is a recorded blocker or coordinated roadmap prerequisite, not silent permission to broaden this job.

## Acceptance and test direction

For newly generated hunts, independently verify that public warrants contain only the retained gang/culprit targets and no unrelated bounty population. Preserve lawful citizen/gang/nobody encounter selection, wrongful declaration behavior and culprit secrecy. Retained gang settlement still pays once and reconstructs its established effects; reducing the unrelated population is no longer a side effect. Poster exhaustion and return-visit behavior remain honest with the smaller pool.

Extend the existing content, investigation and settlement owners with meaningful outcomes/negatives. Retire ledger-only live tests instead of replacing them with missing-file/class tests. Shared tests must stop using unrelated-first-poster assumptions. Keep historical compatibility cases required by the chosen policy; do not preserve snapshot-only authority to keep those tests green. A pure code move or dead-code deletion needs no new regression by itself.

## Decisions before planning

Choose supported old-playthrough handling: versioned legacy support, migration preserving relevant history, or explicit pre-alpha invalidation. Determine treatment of already discovered unrelated warrants and settled bounty events, including projected wallet totals. Preserve applied migration history. Confirm reusable creative-content custody, and account for changed poster availability/selection without generating filler targets. These decisions are not resolved by the general permission to trash pre-alpha playthroughs.

## Later Linear shaping

Create a retirement item from this record during the separately requested Linear cleanup. Its scope is removal and preservation of retained/historical contracts, with links to the source assessment and PG-009-A. It must not close the future addition as delivered. Keep repository and actual Linear IDs linked when created; no Linear item has been created or changed here.
