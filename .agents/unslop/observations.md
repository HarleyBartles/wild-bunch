# Distinct slop observations

Records below describe concrete incidents and guard outcomes. They are durable inputs to the feedback loop, not a log of work performed. Unknown recurrence, reading and effect remain unknown.

## U-009: Cleanup discovery drifted into feature design and execution-plan custody

**Work and evidence:** During the stable 0.1.0 investigation on 2026-10-07, the user stopped the discussion because feature-boundary discovery had become a continuing feature-design interview. The user also identified that investigation findings stored under `.agents/plans/` could be mistaken for executable plans. The [investigation records](../investigations/stable-0.1.0/README.md) preserve the source assessments and supplied product decisions; neither their detail nor their former location authorized implementation.

**Recognition and correction:** Keep the release outcome visible: included features must work, and incomplete excluded features must get out of their way. Record discovered future work without making it a cleanup prerequisite. Distinguish evidence, programme specification, roadmap and accepted execution plan at their entrypoints. Move the 23 session investigation/working-decision records to their named investigation home, preserve links and route planning through the [baseline specification](../specs/2026-10-07-stable-0.1.0-baseline.md). Keep the existing release-foundation plan explicitly blocked until reconciliation. This applies the existing writing/custody and scope discipline; it does not require every investigation in every repository to adopt this directory.

**Reach and effect:** The user's interventions established the failure and corrected the scope. This is one session incident, not evidence of independent recurrence. The specification and custody correction are present for review; future planning behavior, release delivery and prevention of renewed drift remain unproven.

## U-010: Placeholder-only sections padded agent guidance

**Work and evidence:** On 2026-10-08, the user noticed empty capabilities and repository-skill sections in the PR runbook. A read of the authored runbooks and topical playbooks found 39 sections whose only content was `- None.` across 15 files.

**Recognition and correction:** A heading that only says nothing adds scanning cost without guiding the reader. Remove empty or placeholder-only sections unless a validator or external schema requires the field. The [writing profile](writing.md#remove-empty-sections) now records this guard; it does not add a structural checker.

**Reach and effect:** The affected sections were removed from the current runbooks and playbooks. The user prompted the correction; independent recurrence and future effectiveness remain unknown.

## U-011: Campaign instructions leaked into durable guidance

**Work and evidence:** On 2026-10-08, the user identified stable-0.1.0 worktree and version rules embedded in the general PR runbook, with those same requirements already owned by the active roadmap and plan.

**Recognition and correction:** Durable runbooks and playbooks state reusable operating procedure. Put temporary epic sequence, worktree, and version requirements in the active roadmap or plan, and leave stable repository policy in the runbook. The [writing profile](writing.md#keep-durable-guidance-scope-stable) now guards this boundary.

**Reach and effect:** The campaign-specific sentence was removed from the PR runbook, a duplicate was removed from `CONTRIBUTING.md`, and an active-epic reference was removed from the unslop playbook. General `develop` and release-line guidance remains. This is one corrected incident, not proof of recurrence or future effectiveness.

## U-001: Existing profiles with weak work-point routes

**Work:** Unslop adoption ahead of the interactive stable-0.1.0 audit, 2026-10-06. Treat the discovered gaps together as one incident, not multiple independent recurrence examples.

**Evidence:** At repository commit `4eef2d6fb5592b5125686c6fe8dcf42e27c2787d`, `.agents/contracts/unslop/backend-architecture.md` declared use before backend design, implementation and review, but `.agents/doctrine/architecture-guardrails.md` and the design/implementation runbooks did not link it. `.agents/runbooks/code-review.md` routed to a directory of profiles without naming the profile for review itself. `.agents/contracts/unslop/dev-overlay.md` required backend and web profiles without linking their paths. Inspect those files at that commit to distinguish this incident from later reports.

**Failure and correction:** Existing guidance was available on disk but weakly routed at the decision points that needed it. Move canonical profiles to `.agents/unslop/`, link them directly from stage and concern guidance, and require scoped full reading from the root/contributor selector. [Routing guard](routing.md) makes that correction reusable.

**Reach and effect:** The paths and missing direct routes were inspected during this adoption. Whether earlier agents discovered, read or ignored the profiles is unknown; no historical code defect or independent recurrence is inferred. The adopting agent read the profiles and revised the routes. Future audit work must assess whether those routes and guards change decisions; successful link checks alone do not establish effectiveness.

## U-002: Obsolete profile-placement detector

**Work and evidence:** The same adoption encountered a distinct validation obstacle: `scripts/tests/test_repo_guidance_contracts.py` at commit `4eef2d6fb5592b5125686c6fe8dcf42e27c2787d` required `.agents/unslop/` to contain no Markdown, required the former contracts directory and asserted a fixed web profile filename. It tested neither an agent's route nor a validator's behavior. The newly adopted immutable standard instead requires `.agents/unslop/` as canonical custody.

**Correction and guard:** Remove this obsolete location-only detector; do not replace its expected filenames with the new layout. Retain behavior fixtures for the subscription and AGENTS validators and inspect actual route reachability. [Code-review guards](code-review.md#location-only-change-detectors) distinguish a change detector from a legitimate structural-validator contract.

**Reach and effect:** The adopting agent read the review profile, encountered the incompatible test during validation and applied the existing prohibition on change-detector tests. This observation makes the corrective distinction explicit for subsequent agents. It is one near miss, not established recurrence. Whether future agents reach and follow this guard remains unproven.

## U-003: Decision records used as implementation chronicles

**Work and evidence:** The interactive ADR truth investigation on 2026-10-06 read all 37 records. ADR-0028's main body still describes travel migration, log removal and replay wiring as future work while its dated history records those changes as complete. ADR-0027 embeds a test-total receipt and a retired route diagram; the template requires implementation plans and proof sections. [The active investigation](../investigations/stable-0.1.0/2026-10-06-adr-truth-investigation.md) records the affected decisions and source evidence. Treat this as one audit observation, not proof of independent agent incidents or historical intent.

**Recognition and proposed correction:** An ADR accumulates campaign steps, private fields, DTO/table inventories and test counts, then requires readers to reconcile incompatible points in implementation history. Rewrite around the actual durable decision where it survives; use explicit successors where it changed, corrections for unsupported assertions and deviation notes where source broke the accepted decision. Preserve material removals and their known or unknown provenance, including accidents. Do not silently rewrite the record to bless source drift or make the past look deliberate.

**Reach and effect:** Writing, backend, UI, dev and review profiles were consulted for the audit. The user sharpened the distinction between editorial rewriting and preserving truthful history. Findings are recorded; the ADR convention and profiles have not yet been amended to enforce this proposed guard. Remediation and future guard effectiveness remain unproven.

## U-004: Test titles and green paths can overstate behavioral proof

**Work and evidence:** The 2026-10-06 [test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md) identified independent mechanisms within one bounded audit: `types.test.ts` reads its own object-literal assignments; `routingConventions.test.ts` accepts any empty-object return as route validation; `GameSessionEventSourcingTests.RehydrateFromEvents_Reconstructs_Investigation_State` seeds the replay from an already-investigated case file; `HeatSemanticGuardrailTests.StartingJourney_ResetsHeatToZero` heats a different session from the one it starts. The tests can satisfy their assertions without establishing their named behavior. This records one investigation, not a count of independent historical agent incidents.

**Recognition and proposed correction:** Identify the production operation, the independent precondition and oracle, the branch that must actually execute, and the observable failure the test would detect. Remove compiler/initializer echoes and historic layout detectors. Strengthen nonadversarial absence checks, conditional assertions and already-mutated replay fixtures. Consolidate fragments of one happy-path scenario while preserving real failure, reachability, persistence, serialization and independently owned validator boundaries. Do not replace weak proof with a larger inventory of similarly weak tests.

**Reach and effect:** Applicable review, backend, web and dev profiles were consulted. The existing review guard distinguishes obsolete placement detectors from validator behavior; the user added explicit TDD-sprawl assessment. Findings and proposed corrections are recorded, but no test remediation or new profile guard is implemented by this observation. Past agent reading, recurrence and future guard effectiveness remain unknown.

## U-005: Architecture comments and derived output overstate established facts

**Work and evidence:** The 2026-10-07 [Application investigation](../investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md) found comments naming nonexistent `ExecuteAsync` and `GameSession.ThrowIfSetupPhase` methods, a full-audit projector calling `DateTime.UtcNow` while claiming pure event derivation and returning occurrence times, and HUD/diary contracts filling unknown setup fields with default values. The prepped developer workflow also emits events without a replay-complete genesis. These are mechanisms found in one source audit, not proof of independent historical agent incidents.

**Recognition and proposed correction:** Follow a claimed invariant, extension point or event-derived fact through its actual command, storage and read paths. A comment, method name, event count or successful snapshot load cannot establish the claimed authority or replay guarantee. Preserve unknown values until authoritative facts exist, distinguish projection time from event occurrence, and require fresh-state retry/replay proof where the flow claims it. Remove stale scaffolding only after identifying consumers; do not wire incomplete reference code into production to justify its presence.

**Reach and effect:** Backend, developer and writing profiles informed the audit. Their existing authority, mutation-honesty and projection guards helped separate definite source contradictions from legitimate CQRS repetition, audience-specific diagnostics and unresolved knowledge policy. Findings are recorded only; no application remediation or new profile guard was implemented, and historical agent reading/intent and future guard effectiveness remain unknown.

## U-006: Cache version and presence checks mistaken for authoritative state validity

**Work and evidence:** The 2026-10-07 [Persistence investigation](../investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md) traced all 55 tracked files. `PersistedPayloadLoader.LoadComponentPayload` accepts matching-version JSON without validation and treats missing optional rows as absent state. The read loader omits snapshot/stream freshness, while the diary loader trusts any nonempty current-version row set. The repository substitutes `SaltSource.CreateRuntime()` when a stored salt component is absent. These are mechanisms in one audit, not independently counted historical agent incidents.

**Recognition and proposed correction:** A current schema version, row name or valid JSON syntax does not establish complete, coherent state at a stream position. Follow cache trust through stream freshness, component validity, optional presence, partial row loss and a consistent read boundary. Recover derived state from intact authoritative events; reject unrecoverable history and preserve proven historical compatibility through explicit transitions. A load must not invent new authoritative randomness. Use actual malformed/partial-cache and concurrent-read scenarios instead of source-string, constants or filename detectors.

**Reach and effect:** The [backend profile](backend-architecture.md) and event-sourcing doctrine were consulted; their event authority and snapshot/cache distinction supplied the assessment. Findings are recorded, with no new profile guard, implementation or runtime reproduction. Whether past agents read those routes and whether future remediation prevents recurrence remain unknown.

**Test follow-up:** The [related test assessment](../investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md) found expectations that directly preserve new randomness and erased presence state after synthetic field removal, plus a stale-snapshot test that edits only the envelope while leaving correct cache contents. Correct the expectations with the source contract and make cache/replay tests falsifiable using independently observable damaged cache facts. This is follow-up evidence within the same bounded investigation, not another independent recurrence or a claim that tests have been repaired.

## U-007: Partial event integration mistaken for complete feature authority

**Work and evidence:** The 2026-10-07 [Domain investigation](../investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md) found `DevSaltSourceCleared` resampling authoritative entropy inside Apply, saloon spotting without the availability transition needed for confrontation, unrelated-criminal turn-ins applied to an empty replay ledger before reconstruction omits them, and travel theft undone by an absolute reserve snapshot. These are mechanisms within one bounded audit, not independently counted historical agent incidents.

**Recognition and proposed correction:** Typed events, snapshots and named domain methods do not establish complete feature authority. Trace initialization, legal reachability, effect application and reconstruction from independent history. Two paths can agree on the same wrong result. Decide whether the feature belongs in the baseline before repairing it: unfinished non-compliant shells can be removed and deferred instead of completed opportunistically. Preserve truthful decision history and explicitly handle retained or invalidated old playthroughs when removing a feature.

**Reach and effect:** Backend/developer/review profiles, repository domain/.NET/seed skills and event-sourcing doctrine informed the assessment. The user explicitly introduced retain/fix versus remove/defer triage, naming unrelated criminals as a candidate. Findings and alternatives are recorded only; no feature was repaired or removed, no new guard was implemented, and historical reading/intent and future guard effectiveness remain unknown.

**Test follow-up:** The [Domain test assessment](../investigations/stable-0.1.0/2026-10-07-domain-test-followup.md) found direct presence seeding hiding unreachable confrontation, snapshot-only ledger persistence fixtures, horse-loss assertions preserving the old countdown and a cash-or-food theft assertion satisfied by bribe payment. Replay equality must accompany an independent correct outcome, not compare two paths applying the same wrong effects. Strengthen existing behavioral families after agreeing retained scope, or retire feature-only tests with deliberate removal and explicit historical policy. This is follow-up evidence within the same bounded investigation, not another independent recurrence or a claim of remediation.

## U-008: Local UI success mistaken for authoritative game state

**Work and evidence:** The 2026-10-07 [Web investigation](../investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md) traced ignored seed drafts, resumed prologue inputs reset to local defaults, duplicate session/map caches, stale travel previews, command rejections displayed as successful notices and diary/case prose inventing facts beyond returned data. StartingTownStep and travel already share one map renderer, and both endpoints invoke the same handler; separate naming and cache ownership obscure the actual game concept. These are mechanisms in one bounded audit, not independently counted historical agent incidents.

**Recognition and proposed correction:** Trace an input through the transmitted intent, accepted result, authoritative resource update and resumed display. A local step transition, resolved promise, refreshed cache name or plausible narrative cannot establish that the server accepted the action or that the displayed facts are true. Give one server resource one coherent ownership/key policy; keep selection and draft state with the interaction that owns it. Compose vertical slices around game capabilities, including one world map with distinct setup/travel interactions, rather than copying resource ownership into every screen. Components own their display contract; parents arrange supported interfaces rather than styling internal descendants.

**Reach and effect:** The web, developer, review and writing profiles, repository frontend guidance, React/Feature-Sliced Design skills and primary-source research informed this assessment. Existing authority, interaction and display guards supplied review criteria. The user explicitly chose vertical slices and clarified one map per playthrough. Findings and proposed behavioral test families are recorded only; no product/test repair, new profile guard or browser reproduction is claimed, and historical agent reading/intent and future guard effectiveness remain unknown.

**Test follow-up:** The [Web test assessment](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md) traced empty-map/loading expectations, implementation/copy detectors, invalid start-phase fixtures, isolated hydration and sprite doubles that omit actual scale semantics. Backend map alias equality checks counts while duplicate handler suites exercise the same query. Correct or retire these expectations with accepted source contracts, preserve useful negatives and test independently changed facts through real lifecycle/read/display boundaries. This is follow-up evidence within the same bounded investigation, not another independent recurrence or proof of test remediation.
