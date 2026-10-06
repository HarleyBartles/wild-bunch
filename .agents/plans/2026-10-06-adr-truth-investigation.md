# ADR truth investigation for stable 0.1.0

**Status:** Findings and proposed dispositions for discussion. No ADR or product remediation is authorized by this document. This is part of the [stable 0.1.0 investigation](2026-10-06-stable-0.1.0-investigation.md).

**Scope:** All 37 ADRs in `docs/decisions/`, read in full, plus their README and template. Assertions were compared with repository source, migrations, tests and current doctrine. This is a source assessment, not a runtime reproduction of the gameplay defects below. External issue state was not checked and is not evidence for these conclusions.

## Treatment agreed during the investigation

An ADR records a durable decision and why it was made. Collectively, the log must also explain how the repository reached its current shape, including abandoned designs, accidental removals and divergence from accepted decisions. Cleanup must not make the past appear more deliberate or compliant than it was. Class names, endpoint inventories, table inventories, test totals, implementation steps and worker closeout narratives usually belong in source, operational guidance or Git history. A few examples can explain a decision, but should not become a second implementation specification.

The user clarified that implementation-shaped ADRs should be rewritten around their actual durable decisions. That editorial treatment differs from supersession:

- **Retain:** The decision still holds and the historical context is legible.
- **Editorial rewrite:** The decision still holds, but implementation detail obscures it. Preserve the original decision date, motivation, alternatives and material tradeoffs. Add a dated editorial note; do not imply the decision was made again or replace historical rationale with today's rationale. Git retains the removed implementation narrative.
- **Partial supersession:** A later decision replaces a named part. Identify both the replaced scope and the surviving scope, and link the successor. Combine with an editorial rewrite where useful.
- **Full supersession:** The decision has been replaced as a whole. Preserve the historical decision with a clear successor pointer.
- **Dated correction:** An assertion was inaccurate or is unsupported. State the correction explicitly; do not describe an unjustified guarantee as a decision that merely became obsolete.
- **Implementation gap:** Source diverges from an accepted decision. Investigate and restore compliance, or deliberately make a successor decision. Source drift alone does not authorize changing the ADR to bless it.
- **Future decision:** Keep intentional non-implementation clear. A missing future feature is not evidence that its ADR is stale.

Material removals need dated history even when accidental. Use Git to establish the change and date where possible; record intent only when the evidence supports it. A removal without a recorded replacement decision is implementation drift, not retrospective supersession. If its reason cannot be established, say so. For example, the missing starting-town fallback must remain visible as an unaccounted-for removal until its history and intended disposition are settled. An editorial rewrite must preserve that distinction rather than silently deleting the old commitment.

This source audit establishes present contradictions. It does not yet establish the removal commit, date or intent for every contradiction. Before remediation closes a finding, inspect the relevant history and record those facts or their explicit uncertainty. The stable truthful base consists of current governing decisions, deliberate successors, known deviations and unresolved provenance; strict future practice starts from that honest base.

Status should distinguish an accepted decision from implementation completeness. Partial supersession must remain visible without implying that the surviving decision is retired. This audit proposes the distinction; the existing status parser and metadata contract have not been changed.

## Findings and proposed actions

### A-01: The template encourages implementation reporting

**Classification:** Editorial rewrite of the authoring convention, with a dated explanation of the change.

The [template](../../docs/decisions/TEMPLATE.md) requires an implementation status/plan, source inventory and proof section alongside many overlapping decision sections. These are not inherently wrong, but the log shows the resulting failure: milestone reports and volatile implementation descriptions become durable prose that later workers must reconcile. ADR-0027 records a 32-test result; ADR-0020 records `git diff --check`; ADR-0028 carries an extended migration chronicle and numbered implementation steps.

**Proposed action:** Simplify the convention around context, decision, rationale/alternatives, material consequences and dated amendments/successors. Permit sparse links to owning doctrine or source where they aid interpretation. Remove the expectation that every ADR is an implementation plan or validation receipt. Rewrite affected live ADRs editorially, without changing their original decisions. Leave already superseded records historical unless their successor metadata is misleading.

### A-02: Snapshot composition survived, but authoritative log/history storage changed

**Affected:** ADR-0003 and ADR-0028.

**Classification:** Partial supersession of ADR-0003 by ADR-0028, plus editorial rewrites of both.

ADR-0003 says dedicated log-entry and diary rows preserve session history, and its proof names `GameSessionLogEntries`. The current [DbContext](../../src/WildBunch.Persistence/WildBunchDbContext.cs) has session envelopes, component snapshots, diary projections and stored events. The [drop migration](../../src/WildBunch.Persistence/Migrations/20260624104903_DropGameSessionLogEntries.cs) removes the log table. [JournalLogProjector](../../src/WildBunch.Application/Projections/JournalLogProjector.cs) derives log output from events. The composed JSONB cache and domain/persistence separation remain real decisions; the former history authority does not.

**Proposed action:** Mark the log/history authority portion superseded by ADR-0028. Preserve the composed storage decision as surviving. Do not keep the old table inventory as current guidance, and do not replace it with a new exhaustive inventory.

### A-03: Internal child authority is still described as multiple aggregate event owners

**Affected:** ADR-0002, ADR-0005, ADR-0013, ADR-0020 and ADR-0028.

**Classification:** Editorial rewrite of the surviving root/child decisions; partial supersession of the autonomous child event-emission and cross-aggregate protocol in ADR-0020/0028.

The current [architecture guardrails](../doctrine/architecture-guardrails.md) name `GameSession` as the command consistency boundary, event-production owner and apply-dispatch owner. Children receive narrow context and return outcomes or events to produce. [ActionContextTracker](../../src/WildBunch.Domain/Game/ActionContextTracker.cs), [BountyLoop](../../src/WildBunch.Domain/Game/BountyLoop.cs), [JourneyLoop](../../src/WildBunch.Domain/Game/JourneyLoop.cs), [InvestigationLoop](../../src/WildBunch.Domain/Game/InvestigationLoop.cs) and [StoreLoop](../../src/WildBunch.Domain/Game/StoreLoop.cs) follow that internal-child direction. ADR-0020 instead speaks of each aggregate owning event emission and routing facts between other aggregate public APIs. ADR-0028's historical note acknowledges that future sub-aggregate splits were replaced by child extraction, but the main protocol still carries the old framing.

**Proposed action:** Record a successor decision for the current consistency boundary and internal child protocol, partially superseding the conflicting portions. Preserve cohesive child ownership of rules, the prohibition on reach-through mutation, and the distinction between domain ownership and persistence layout. Do not mechanically rename every occurrence of aggregate or infer that all child invariants now belong in the root.

### A-04: The seed decision has materially changed

**Affected:** ADR-0021; vocabulary dependency on ADR-0023.

**Classification:** Partial supersession by a new seed/setup ownership decision, plus editorial rewrite of the surviving UUID contract.

ADR-0021 rejects semantic UUID bitfields in favour of a labelled mixer and says a single descriptor owns difficulty, entropy, starting town and resources. The current [SeedWorldResolver](../../src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs) explicitly uses direct bit packing and identifies its contract as `resolver-v17`. [SeededNewGameFactory](../../src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs) takes difficulty and entropy separately. [Seed doctrine](../doctrine/game-content-seed-pipeline.md) separates seed-owned map, pressure-owned difficulty/resources, entropy policy and player-selected starting town. Old codec/mixer/descriptor paths in the ADR no longer identify the implementation. Its entropy `Standard` name also predates ADR-0023's `classic` vocabulary.

**Proposed action:** Preserve the UUID-shaped public seed and hidden-truth boundary. Supersede the mixer algorithm, broad descriptor ownership and seed-selected starting-town decisions explicitly. Record the new ownership boundaries and the reason for choosing a reversible codec, without copying its bit positions into an ADR. Do not restore the old implementation merely because the ADR lacks a successor.

### A-05: The locked difficulty vocabulary disagrees with source

**Affected:** ADR-0023; dependent setup/source guidance in ADR-0021 and ADR-0024.

**Classification:** Implementation/decision conflict requiring a user decision. Editorial rewrite alone is insufficient.

ADR-0023 explicitly locks `easy`, `normal`, `hard`, `brutal`. The current [GameDifficulty](../../src/WildBunch.Domain/Travel/GameDifficulty.cs) defines `Standard`, `Easy`, `Challenging`, `Brutal`; [SetupHuntStep](../../src/WildBunch.Web/src/components/start-flow/SetupHuntStep.tsx) and [formatters](../../src/WildBunch.Web/src/ui/formatters.ts) expose Standard and Challenging. [GameEntropy](../../src/WildBunch.Domain/Travel/GameEntropy.cs) does match the accepted entropy ladder. The separation of challenge pressure, volatility and fixed mystery truth remains valid.

**Proposed action:** Decide whether to restore Normal/Hard or accept Standard/Challenging through a partially superseding vocabulary decision. Do not silently rewrite the locked names to match source. Keep fairness and axis separation live whichever naming decision wins.

### A-06: UI shell records describe a retired route topology

**Affected:** ADR-0011, ADR-0016, ADR-0019 and ADR-0027.

**Classification:** ADR-0011 is already superseded correctly. Partial supersession of ADR-0027's cockpit route by ADR-0030; a further successor is needed for the current player flow. Editorial rewrites of surviving stack/client/shell decisions.

ADR-0027's Camp/Hunt/Case/Wanted/Trail/Debug diagram is not the live [router](../../src/WildBunch.Web/src/shell/router.tsx). Current routes are pre-session, town hub, town places, trailhead and trail; long-form surfaces are also owned by shell overlays. ADR-0030 replaces `/debug` with the dev overlay, but neither ADR gives a complete supersession chain for the current player-flow shape. ADR-0016's unsettled cockpit/routing commentary and ADR-0019's hook inventories should not be current route guidance.

**Proposed action:** Keep React/Vite, TanStack Router/Query, typed manual transport, server authority and React-owned UI state as surviving decisions. Record the durable reason for phase/place navigation and shell overlays in a successor, rather than preserving a route catalogue. Leave ADR-0011's superseded cockpit experiment intact.

### A-07: ADR-0028 mixes several incompatible points in the migration

**Classification:** Editorial rewrite with a dated explanation; partial supersession where A-02/A-03 identify changed decisions; an explicit bounded exception and defect follow-up for A-12.

Its dated history records travel migration, removal of aggregate logs, replay fallback and versioning. Its body still says travel is not migrated, `AddLogEntry` remains, `LegacyLogProjector` is not implemented, and implementation is planned/doctrine-only. Current [repository](../../src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs), [payload loader](../../src/WildBunch.Persistence/Versioning/PersistedPayloadLoader.cs), [JournalLogProjector](../../src/WildBunch.Application/Projections/JournalLogProjector.cs) and [event-sourcing doctrine](../doctrine/event-sourcing-integrity.md) contradict those present-tense descriptions. The root no longer has aggregate `LogEntries` or `AddLogEntry` members, while a compatibility-shaped log DTO/projection output remains.

**Proposed action:** Retain the actual durable choices: event history authority, typed domain facts, infrastructure envelopes, optimistic append, one staged repository/UoW write path, rebuildable safe projections, snapshot caches and transport independence. Remove campaign steps, call-site counts and test receipts from the live explanation. Retain dated material changes and removals, including unapproved drift where established, rather than an exhaustive chronology of implementation work. Do not claim the prep exception is solved merely by making the ADR concise.

### A-08: Archive atomicity is mistaken for concurrency exclusion

**Affected:** ADR-0034 and its invariant summary in ADR-0028.

**Classification:** Dated correction of an unsupported guarantee; implementation gap; later partial supersession for user ownership.

[CompletePlayerSetupHandler](../../src/WildBunch.Application/Games/Commands/CompletePlayerSetupHandler.cs) reads all globally Active sessions and stages archive/create. [EfGameSessionUnitOfWork](../../src/WildBunch.Persistence/GameSessions/EfGameSessionUnitOfWork.cs) opens its transaction only at commit. [GameSessionEntityConfiguration](../../src/WildBunch.Persistence/GameSessions/GameSessionEntityConfiguration.cs) has no active-session uniqueness rule, and [StoredEventEntityConfiguration](../../src/WildBunch.Persistence/GameSessions/StoredEventEntityConfiguration.cs) protects sequences within a stream. Two requests can both read an empty active set, create different session IDs and commit without sharing a protected event sequence. Therefore a single commit provides archive/create atomicity, not the claimed concurrent global uniqueness guarantee. This is a source-derived counterexample, not an executed race test.

The later [PrepGameSessionHandler](../../src/WildBunch.Application/Games/Commands/PrepGameSessionHandler.cs) and [StartGameSessionHandler](../../src/WildBunch.Application/Games/Commands/StartGameSessionHandler.cs) also provide a creation/activation path without delegating to the archive flow. That is the ADR's own stated review trigger.

**Proposed action:** Preserve the archive-is-not-deletion and atomic restart decisions. Add a dated correction explaining the concurrency limitation and open a bounded behavior investigation. The approved cloud spec will replace the global invariant with one active playthrough per user, including setup, but that future decision is not implemented today. Select its actual exclusion mechanism during that feature's design; do not assume per-stream optimistic concurrency provides it. Remove the DB-drop/recreate aside from the durable retention rationale without weakening the migration strategy.

### A-09: Legal outcome decisions remain valid; non-implementation statements are historical

**Affected:** ADR-0025 and ADR-0026.

**Classification:** Editorial rewrite with a dated implementation note, not supersession of the legal/murder boundary.

ADR-0026 repeatedly says no turn-in, payout or wallet mutation exists. Current [GameSession](../../src/WildBunch.Domain/Game/GameSession.cs) has `SettleSheriffTurnIn`, emits `SheriffTurnInSettled`, and applies bounty cash; [BountyLoop](../../src/WildBunch.Domain/Game/BountyLoop.cs) and settlement policies provide outcome logic. ADR-0025's deferred seam discussion is similarly overtaken by implemented bounty behavior. These facts do not disprove the durable distinction between legal eligibility, handoff outcome, settlement, murder-case resolution and hidden truth.

**Proposed action:** Keep those distinctions and the alive-confession design constraint unless consciously replaced. Make original non-implementation context explicitly historical and remove live backlog ownership from the decision text. Do not claim the full murder-case resolution loop exists merely because bounty settlement exists.

### A-10: Developer overlay details and hosting plans need different treatments

**Affected:** ADR-0030, ADR-0031 and ADR-0032.

**Classification:** Editorial rewrites for implementation detail; future partial supersession for public/preprod capability isolation.

The dev namespace, separate DTOs, environment guard and event-backed consume-once controls remain implemented. ADR-0030 fixes drawer dimensions at 45/85vh, while [DevOverlay](../../src/WildBunch.Web/src/dev/DevOverlay.tsx) uses 40/80dvh adjusted for shell position. ADR-0031/0032 explain root-private override fields and reflection restoration, whereas current state ownership/restore seams have moved into the extracted children. ADR-0030/0032 also retain old `.agents/docs/` and index-mesh doctrine locations in implementation history.

**Proposed action:** Preserve contextual controls, ordinary gameplay consumption, replay-safe dev events, explicit hidden-truth separation and centralized access enforcement. Remove dimensions, field ownership and exhaustive DTO inventories from the durable explanation. Keep historical path mentions historical. The cloud spec's public deployment without dev capabilities and Harley-only preprod is a successor to the planned public developer-role direction, not evidence that today's `IsDevelopment()` guard has already changed.

### A-11: Phaser's durable adapter decision survives; its map contract and accessibility claim do not

**Affected:** ADR-0035.

**Classification:** Editorial rewrite and dated factual note for transport changes; implementation gap for keyboard/screen-reader selection.

The React-owned state/API and Phaser intent-only adapter remain visible in [PhaserMapHost](../../src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx). The ADR describes a global map endpoint, DTO `Selectable` flag and single `POST /api/games` confirmation. Current [endpoints](../../src/WildBunch.Api/Games/GameSessionEndpoints.cs), [map handler](../../src/WildBunch.Application/Games/Queries/GetStartingTownMapHandler.cs) and [map DTO](../../src/WildBunch.Application/Games/Models/StartingTownMapDto.cs) instead use a session-derived map and staged setup/start flow, with no `Selectable` member.

More seriously, the promised selectable DOM fallback is absent from [StartingTownStep](../../src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx) and `PhaserMapHost`: the host exposes a labelled image container, and scene selection uses pointer callbacks. No town-choice buttons or equivalent keyboard selection path exist in those components. A DOM fallback was an accepted accessibility decision, not incidental implementation trivia.

**Proposed action:** Keep React/Phaser ownership and accessible equivalent interaction as durable decisions. Strip endpoint and object-shape snapshots. Investigate restoring an equivalent accessible selection path; do not delete the accessibility promise to match the code. Browser verification is needed when that correction is implemented.

### A-12: The dev-enabled action ADR describes public phases that are dev-only and incompletely replayable

**Affected:** ADR-0036, with ADR-0028's event-sourcing claim.

**Classification:** Partial supersession or explicit correction of the public-phase decision, after intent is settled; implementation gap for event-backed setup/start; editorial rewrite of the remaining prep/inject/act principle.

ADR-0036 specifies public prep and act endpoints around optional dev injection. Current [DevEndpoints](../../src/WildBunch.Api/Dev/DevEndpoints.cs) maps both `/games/prep` and `/games/{id}/start` beneath `/api/dev/` with the guard. Normal `/api/games/setup` remains a separate flow.

`GameSession.StartPrepped` creates a zero-event snapshot. Current event-sourcing doctrine acknowledges that limited exception. However, `StartFromPrepped` directly sets world, case, seed, salt and Active status before producing `WorldGenerated`; it does not emit `PlayerSetupCompleted` or `GameStarted`. [RehydrateFromEvents](../../src/WildBunch.Domain/Game/GameSessionEventReplay.cs) requires one of those events. Thus the prep/inject/start stream before further lifecycle events cannot meet the ordinary full-replay constructor contract. `StartGameSessionHandler` also resolves world/reads dev salts before entering its retry closure, so a retry can reuse stale preparation. These are source-supported risks requiring behavior proof, not runtime failures reproduced by this audit.

**Proposed action:** Decide whether the durable pattern is a shared public action with optional injection or an explicitly dev-only preparation path. Preserve clean player API parameters and backend-owned override consumption. Investigate event-backed setup and generation inside the retry boundary before claiming full replay/retry compliance. Do not make the zero-event exception a blanket exemption for the later event-backed transition.

### A-13: Partial supersession is incorrectly displayed as full retirement

**Affected:** ADR-0037, ADR-0001's successor reference and the decision README.

**Classification:** Metadata correction and editorial clarification. ADR-0033 is already fully superseded and should remain historical.

ADR-0037's status is `superseded`, while its dated history says superseded in part and retains ADR location and retirement of generated indexes. Its successor is a contract/certification pair, not a new ADR. The current [operating standards contract](../contracts/operating-standards.json) now also includes the retained unslop adoption from this worktree, so neither its historical seven-item deployment list nor its October 2 six-reference note is a current inventory. Historical counts need not be rewritten each time a subscription changes.

**Proposed action:** Make the surviving decisions and replaced deployment mechanism explicit in status/relations or a dated scope note. Route readers to current contract-owned subscription truth. Preserve the old inventory as historical context or remove it during an editorial rewrite, without substituting a perpetually updated inventory. Do not reopen ADR-0033's generated mesh merely because ADR-0037's status appears fully retired.

### A-14: Review dates and evidence links are weaker than their labels suggest

**Affected:** The README/updater and several live ADRs, especially ADR-0016/17/19/21/22/27/30/35/36.

**Classification:** Editorial/metadata convention correction, not architecture supersession.

[update_adr_freshness.py](../../scripts/update_adr_freshness.py) computes Last reviewed from the maximum date in Dated Status History. That proves neither a semantic review nor the currency of the body. ADR-0002's July implementation note does not appear in that history; ADR-0028's July review date coexists with contradictory main-body statements. Several source pointers reference removed descriptors, relocated tests or retired index/doctrine paths. A missing evidence path alone does not establish that its decision was replaced.

ADR-0017's skippable PostgreSQL description applies to its original guarded smoke tests, but newer fixture-backed integration suites require the configured database. Current [validation doctrine](../doctrine/validation-policy.md) and [runner](../../tools/run.py) describe the real provider lane. [PostgreSqlTestDatabase](../../tests/WildBunch.Integration.Tests/TestInfrastructure/PostgreSqlTestDatabase.cs) fails when its required connection is absent; the original `PostgreSqlPersistenceTests` still explicitly skip. This mixed setup is not a single universally skippable lane.

**Proposed action:** Label generated dates as last recorded status/amendment dates, or introduce an actual review convention if there is a useful owner for it. Avoid adding ceremonial review dates to every record. Keep ADR references sparse and route executable validation details to the testing playbook. Preserve the real-provider and deterministic-test choices; retire the blanket skip assumption through a dated clarification or partial successor decision.

## Every ADR: disposition and surviving decision

This table records recommendations, not changed ADR statuses. E means editorial rewrite around the surviving decision; P means partial supersession; C means a dated correction/clarification; G means an implementation/decision gap; R means retain. Combined codes name different affected parts.

| ADR | Proposed treatment | Assessment |
|---|---|---|
| 0001 | R / light E | Markdown decision log still exists. Its dated relocation note already explains `docs/adr` becoming `docs/decisions`; historical location alone is not a defect. Clarify the partial successor chain under A-13/A-14. |
| 0002 | E | GameSession remains the command/root consistency boundary. Preserve child rule ownership; remove extraction milestones and current class inventories. A-03. |
| 0003 | P + E | Composed JSONB cache remains; dedicated authoritative log rows are replaced by event history/projections. A-02. |
| 0004 | E + C | PostgreSQL and explicit real-provider validation remain. The existing August 24 note records the shared `Z:/pg` lane replacing repo-local cluster ownership; make that partial scope clear, route commands to operations guidance and retain historical rationale. |
| 0005 | E | CaseFile remains session-owned with no independent command repository. Describe the ownership boundary without freezing the component API or persisting a second aggregate protocol. A-03. |
| 0006 | R / light E | Investigation reveals knowledge rather than awarding gang pressure. Keep the scope clear: entering a source context can still affect clock/heat; knowledge-only is not a promise of no other session effects. |
| 0007 | R / light E | Hidden culprit/progress and player knowledge remain distinct. Guarded diagnostic truth is the explicit dev exception in ADR-0030/32, not a reason to weaken player projections. |
| 0008 | E | Town visit source refresh and town-local availability remain implemented through TownAggregate/TownVisitState. Remove object-shape inventories; retain visit repeatability. |
| 0009 | R / light E | Structured subject/location/time clue anchors remain in the domain and public mapper. Preserve player-known plausibility rather than a frontend-invented solution. |
| 0010 | R, future | Planned event-derived lawman evidence is explicitly unimplemented. Heat bookkeeping is not that future system. |
| 0011 | R, already superseded | ADR-0027 already replaces cockpit-hosted player surfaces. Preserve this historical experiment and its successor pointer. |
| 0012 | R / light E | GameContent remains code-backed; DB-backed content is expressly future. Do not infer a need to buy a managed database or claim issue #34 is currently open. |
| 0013 | E | Travel is still session-owned; JourneyLoop extraction does not make it an independent command root. Preserve journey/replay boundary, remove snapshot member inventories. A-03. |
| 0014 | E | Onion, DDD, CQRS, repository ports and first-class UoW remain. Both IGameSessionRepository and IGameSessionReadRepository exist; ADR-0028's single event-write port does not supersede the separate read-model port. |
| 0015 | E + C | Minimal APIs remain the HTTP adapter. Clarify orchestration versus domain gameplay ownership if rewriting the loose phrase about handlers containing game logic; endpoint lists are not the decision. |
| 0016 | E + P | React/Vite/Query/styled-components remain in package.json. Old cockpit/routing assumptions are replaced by ADR-0027/30 and the later flow shape. A-06. |
| 0017 | E + C | xUnit/Vitest/Testing Library/jsdom and real PostgreSQL remain; original smoke-test skipping is not the entire current integration setup. A-14. |
| 0018 | R / light E | Source and test project files still target net10.0 with nullable and implicit usings enabled. No contrary runtime decision found. |
| 0019 | E | Manual typed client remains. Keep the generation threshold; remove fetching-hook and endpoint inventories that do not define that choice. |
| 0020 | P + E | Preserve cohesive domain legality and no reach-through mutation. Autonomous child event ownership/cross-aggregate protocol conflicts with current internal-child posture. A-03. |
| 0021 | P + E | UUID public contract survives; labelled mixer, all-in-one descriptor and seed-owned starting town do not. A-04. |
| 0022 | R / light E | Manual browser evidence remains distinct from automated component/API validation. Old index routes are stale evidence; move operational instructions to the browser playbook. |
| 0023 | G, possible P | Difficulty names conflict; entropy names, two-axis separation and fixed mystery truth survive. Naming intent must be resolved. A-05. |
| 0024 | R / light E | Source variation/fairness map is intentionally partly future, including newspaper. Current town/public source distinction survives; remove deferred work assignments rather than assert those mechanics are implemented. |
| 0025 | E + C | Legal/warrant vocabulary and hidden culprit truth remain separate. Follow-on outcome work now exists; original future framing is historical. A-09. |
| 0026 | E + C | Keep outcome/settlement/case resolution distinctions. The repeated present-tense absence of turn-in/payout is stale. A-09. |
| 0027 | P + E | Query/Router/shell/server authority survive. Debug route is replaced by ADR-0030; player route/overlay flow also changed without a clear successor. A-06. |
| 0028 | E + P + G | Event sourcing decisions survive; main-body migration claims conflict with dated history/current source. Child protocol and setup/replay scope require explicit treatment. A-02/A-03/A-07/A-12. |
| 0029 | R / light E + C | Town rollover increases heat, journey start resets it, trail generation does not consume heat as danger. Source uses zero-based Turn, rolling 3 to 0; ADR's 4 to 1 wording needs a dated representation clarification, not a changed heat rule. |
| 0030 | E, future P | Contextual overlay/dev namespace/access seam survive. Dimensions and mesh paths are historical implementation detail; public/preprod isolation is a later successor. A-10. |
| 0031 | E | Event-backed forced/cleared/consumed travel setup survives. Remove private-field, factory and snapshot-restoration recipes; preserve once-only consumption through normal play. A-10. |
| 0032 | E | Saloon override lifecycle, gate-aware eligibility and explicit dev truth survive. Remove expanded DTO inventories and root-private-field recipes. A-10. |
| 0033 | R, already superseded | Generated index mesh is explicitly retired by ADR-0037. Its obsolete index counts and paths are legitimate historical context. |
| 0034 | C + G, future P | Archive/history/atomicity survive. Concurrent uniqueness is unsupported; additional start path is unaccounted for; global scope must later become per-user. A-08. |
| 0035 | E + C + G | React shell/Phaser adapter survives. Session map/start contract changed; promised accessible town selection is absent. A-11. |
| 0036 | P or C + E + G | Public phases are actually dev-only; prep/start replay and retry claims need behavior investigation. Preserve clean player parameters/backend override ownership. A-12. |
| 0037 | C + E | Mark partial replacement accurately. ADR location, optional standards/capability distinction and retired generated indexes survive; current subscription inventory belongs to the contract. A-13. |

## Recommended remediation order for discussion

1. Agree the ADR convention from A-01, including how partial supersession is represented. This prevents every rewrite from recreating the same implementation-report pattern.
2. Resolve actual intent conflicts: difficulty names, seed/setup ownership, internal-child protocol, current navigation ownership and dev-enabled prep/act scope. Write successor decisions only where a decision genuinely changed.
3. Bound behavior investigations for restart concurrency, accessible starting-town choice and prep/start replay/retry. An ADR cleanup must not hide these source gaps.
4. Inspect Git provenance for changed and removed commitments, then rewrite the surviving live decisions editorially and apply explicit successor/correction/deviation notes. Preserve material removals even if accidental; never invent approval, dates or rationale. Keep 0011 and 0033 historical. Clarify 0037's surviving scope.
5. Route operational recipes to their existing owners, remove development receipts rather than migrating them into another durable report, and update the template/README/updater only after the convention is settled.

The accepted cloud specification describes intended account/environment behavior, not today's implementation. No ADR should be marked implemented or superseded merely because a future feature has been specified. No bulk code repair, ADR rewrite or status change has been performed as part of this investigation.
