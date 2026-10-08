# PG-009-R: Remove incomplete unrelated-criminal gameplay

**Work class:** Stable 0.1.0 feature retirement. **State:** Retired from live code and tests. The [PG-009 assessment](2026-10-07-unrelated-criminal-feature-boundary.md) preserves the pre-retirement source evidence; [PG-009-A](2026-10-07-unrelated-criminal-addition.md) preserves the separately deferred future feature. This record preserves the accepted player boundary, data policy and creative-content custody.

## Player story and intended result

As a player pursuing the Wild Bunch, I encounter eligible gang members, ordinary citizens or nobody in saloons, discover retained gang-member warrants, and use the supported gang confrontation/bounty loop. New hunts offer no independent non-gang bounty targets or their warrants. Citizen mistaken-identity consequences, gang bounties and hidden culprit rules remain.

The live saloon candidate behavior remains eligible gang members, ordinary citizens or nobody. Retirement removed unrelated warrant generation and the incomplete ledger/settlement coupling. It did not remove the common wanted-poster, sheriff, citizen or gang bounty features.

## Code points and retirement outcome

| Owner / code points | Retirement work | Boundary to preserve |
| --- | --- | --- |
| GameContent: seed case builder; case character roster | Unrelated roster selection and warrant generation are removed. | Gang/culprit warrants and deterministic retained generation remain. Former authored content is recoverable from Git history, not a promised candidate pool; PG-009-A must revalidate/re-author it. |
| Domain: warrant target kind, poster resolver, investigation context, retired-warrant tracking | Unrelated target eligibility and retired-pool plumbing are removed; no old-playthrough compatibility is retained. | Common warrant terms, gang affiliation, culprit gating, investigation knowledge and source/visit rules remain. Persisted enum values were not repurposed. |
| Domain: unrelated ledger, session restoration/replay and bounty loop | Ledger population/collection/parity state and its gang-settlement side effect are removed. | Gang settlement, one-time payment and event authority remain. |
| Domain: direct unrelated turn-in method/context | Dormant direct settlement path is removed. | Suspect-ID gang turn-in and citizen/wrong-declaration outcomes remain. |
| Application/API and React consumers | No unrelated-specific command or UI branch required removal; shared consumers now operate on retained case/warrant data. | Existing player commands, sheriff/poster surfaces and transport contracts remain. |
| Replay/projections: unrelated settlement event, HUD and diary effects, event deserialization | Event type and feature-specific replay/projector effects are removed. | Retained events continue to project authoritative wallet and journal facts. |
| Persistence: ledger component, repository read/write, serializer and payload loading | Ledger persistence and snapshot codecs are removed. A SQL-only migration deletes existing `GameSessions` rows, relying on existing FK cascades for dependent components, events and diary rows. | Schema and `__EFMigrationsHistory` remain. The migration is irreversible because discarded playthroughs cannot be restored. It has only been exercised against isolated integration databases. |
| Tests: ledger unit/wiring/persistence/full-replay fixtures | Tests that asserted the retired ledger's live behavior are removed; shared fixtures and component inventories are corrected. Migration behavior proves invalidation and the ability to store a new session. | Retained gang/citizen legality, hidden truth, payout/replay, source refresh and independent storage/transport behavior remain covered. |
| Documentation and operating surfaces | Update PG-009 status/dependencies and material ADR history; route later implementation/review to the accepted matrix. | Preserve that the partial feature existed and why it was retired. Future addition remains separately recorded. |

The [boundary assessment](2026-10-07-unrelated-criminal-feature-boundary.md#dependency-and-removal-impact) is a historical source audit and its source references describe the pre-retirement tree. Git history records exact removed implementations. No compatibility-only unrelated event, type or reader remains in live code.

## Cross-feature dependencies

- **PG-001, setup:** changes which warrant data new hunts generate. Preserve established old setup facts; pool changes can alter selection results for identical seed/visit inputs.
- **PG-005, investigation:** stops producing unrelated poster knowledge; preserves gang warrants and source/visit behavior.
- **PG-006, confrontation:** removes only unrelated parity coupling from gang settlement; preserves citizens, gang identities, lawful confrontation and fine/payment semantics. The separately recorded missing gang presence transition remains a PG-006 correction, not an excuse to expand this retirement into rebuilding the whole loop.
- **PG-008, reference/history:** reconciles displays and historical wallet/warrant facts under the selected old-playthrough policy.
- **PG-002 and supporting persistence/replay:** historical handling determines whether affected existing hunts resume, migrate or are explicitly invalidated.

These boundaries were settled for retirement. The migration invalidates every pre-existing pre-alpha playthrough when applied; this does not claim that any non-test database has already been migrated. Existing schema and migration history remain intact.

## Acceptance and test direction

For newly generated hunts, independently verify that public warrants contain only the retained gang/culprit targets and no unrelated bounty population. Preserve lawful citizen/gang/nobody encounter selection, wrongful declaration behavior and culprit secrecy. Retained gang settlement still pays once and reconstructs its established effects; reducing the unrelated population is no longer a side effect. Poster exhaustion and return-visit behavior remain honest with the smaller pool.

Extend the existing content, investigation and settlement owners with meaningful outcomes/negatives. Retire ledger-only live tests instead of replacing them with missing-file/class tests. Shared tests must stop using unrelated-first-poster assumptions. Keep historical compatibility cases required by the chosen policy; do not preserve snapshot-only authority to keep those tests green. A pure code move or dead-code deletion needs no new regression by itself.

## Later Linear shaping

Create a retirement item from this record during the separately requested Linear cleanup. Its scope is removal and preservation of retained/historical contracts, with links to the source assessment and PG-009-A. It must not close the future addition as delivered. Keep repository and actual Linear IDs linked when created; no Linear item has been created or changed here.
