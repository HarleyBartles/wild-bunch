# ADR Dispositions and Historical Truth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply the approved 37-record ADR disposition table, preserve the repository's real decision history, and make each current or planned decision accurately discoverable without turning ADRs into implementation reports. Make ADR and unslop guidance effective at lifecycle entrypoints, with PR authors and reviewers as the final stale-decision safeguards.

**Architecture:** Preserve each original decision and its date, add a successor only where an accepted choice changed, and use dated editorial notes for factual corrections or material removals. Keep implementation status, code inventories, test receipts, and feature backlogs in their existing owners. Route lifecycle and topical guidance through central unslop and decision-record playbooks, and require authors and reviewers to check the actual diff against relevant ADRs. Keep durable runbooks and playbooks scope-stable: campaign-specific delivery rules belong in the active roadmap or plan, and empty placeholder sections are removed.

**Tech Stack:** Markdown ADRs, Git history for provenance, repository command bus validation, `Directory.Build.props` as the sole authored application version.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially “Reconcile durable decisions with retained behavior” and “History and migration boundary”; [approved cloud-playability specification](../specs/2026-10-06-cloud-playability-and-releases.md), which governs planned cloud successors; [ADR disposition investigation](../investigations/stable-0.1.0/2026-10-06-adr-truth-investigation.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 02.

**Execution Strategy:** `executing-plans` — user-selected inline execution; sequential, interdependent editorial and successor changes are reviewed as one ADR-history slice.

## Global Constraints

- Target `develop`; this PR advances the merged `0.1.0-dev.3` identity exactly once to `0.1.0-dev.4` in `Directory.Build.props`.
- Preserve each ADR's original decision date, actual motivation, alternatives and material tradeoffs; new editorial dates identify the actual correction date and never imply a new decision.
- Preserve material removals, including accidental or unexplained ones; state only what Git or another authoritative source proves and leave unsupported intent unknown.
- ADR status describes decision authority, not implementation completion; use `planned` only for accepted decisions not yet in force as implemented behavior.
- The baseline specification governs settled choices; source inspection verifies implementation facts but cannot override those choices or silently bless drift.
- A change may not silently remove or contradict behavior protected by an ADR; a durable changed choice requires an explicit ADR amendment or successor. New durable architecture requires consulting relevant ADRs and recording the decision.
- PR authors resolve the decision-record check before requesting review. Reviewers independently perform the same semantic check against the actual diff; neither gate treats the presence of an ADR edit as sufficient by itself.
- Do not re-open settled choices for the UUID seed, difficulty/randomness vocabulary, GameSession root/child protocol, setup-to-arrival flow, excluded developer layout preparation, archive behavior, or cloud boundary.
- Do not change game code, tests, migrations, or feature inventory. Agent-guidance changes are limited to the central ADR/unslop routing and PR/review gates in Task 6; leave the six non-ADR documents for their separate row-02 slice.
- Do not add source inventories, test totals, proof receipts, future-work lists, or route diagrams to ADRs.
- Keep this plan, the baseline spec, and the roadmap through this PR; retire the prior completed plan in this PR's first substantive commit.

## Review Focus

1. A successor preserves the historical decision and names precisely what changed and what survives; it does not rewrite the predecessor as if today's choice were original.
2. A source contradiction is recorded as an implementation gap or dated correction unless the approved spec explicitly changed the decision; source drift alone never supersedes an ADR.
3. Git archaeology establishes material removal commits and dates where possible; absent evidence is recorded as unknown rather than inferred intent.
4. ADRs state durable choices and rationale, not implementation shape; catalogue summaries and status/successor links match the records without treating status as a code-completion claim.
5. Cloud decisions remain `planned` and distinguish accepted future boundaries from current 0.1.0 behavior.
6. Central playbooks own profile and decision-record selection, while each applicable work surface supplies an effective route; PR and review gates independently check that the ADR log remains true after the change.

## Scope and File Ownership

The first substantive commit creates this plan, updates row 02 with PR #188's actual merge evidence and this successor plan, removes the completed predecessor plan, and advances the authored version to `0.1.0-dev.4`. The decision-history work then creates the successors listed below, edits only records whose accepted disposition requires an edit, and updates `docs/decisions/README.md` as the authored catalogue. Before publication, a bounded guidance task makes the ADR and unslop playbooks the central routers and gives both PR authors and reviewers explicit independent decision-log gates.

Create the following records, using the titles and decision scopes below. Preserve the predecessor records and add reciprocal relationship links.

| New ADR | Decision scope | Predecessor relationship |
| --- | --- | --- |
| ADR-0038, GameSession Consistency Boundary and Internal Child Protocol | GameSession owns command consistency, event production/application, and persistence coordination; cohesive children own narrow rules/state and return outcomes without independent event or persistence authority. Preserve no reach-through mutation and avoid mechanically renaming all historical uses of “aggregate.” | Partially supersedes ADR-0020 and ADR-0028; clarifies ADR-0002, ADR-0005, and ADR-0013. |
| ADR-0039, Seeded Game Setup and Free First Arrival | Preserve the public UUID seed and versioned reversible world codec; keep difficulty and randomness separate; Go settles world and case truth; the player then reads the prologue and selects one starting town, with that selection performing the one free arrival. There is one world map for setup and travel. The stream records resolved facts and replay never re-rolls randomness. Do not retain the superseded mixer/descriptor/start-town ownership or the exclusive zero-event developer prep route. | Partially supersedes ADR-0021, the obsolete setup-flow portion of ADR-0027, and the exclusive prep/start portion of ADR-0036; records the accepted setup lifecycle. |
| ADR-0040, Difficulty and Randomness Vocabulary | Retain Easy, Standard, Challenging, Brutal (Standard default) and Boring, Classic, Adventurous, Wild (Classic default); difficulty governs pressure/resource policy, randomness governs volatility, and neither rerolls already-recorded mystery facts. | Partially supersedes the vocabulary portion of ADR-0023; ADR-0024's compatible fairness and axis-separation decision survives. |
| ADR-0041, Public and Preprod Developer Capability Boundary | Public admits any verified Google account and has player APIs/UI only; preprod admits only the owner's verified stable identity and may expose player APIs, existing dev APIs/UI, with separate databases and volumes. This accepted boundary is planned, not implemented in 0.1.0. | Partially supersedes the future public developer-role direction in ADR-0030; sourced from the approved cloud-playability specification. |
| ADR-0042, User-Owned Playthrough Lifecycle | A verified Google identity resolves to an immutable internal user; a user may own many sessions over time but at most one active session; confirmed start-over immediately and durably archives that user's current session, independently of replacement creation; ownership and active selection are event-derived. Keep this planned for later cloud work and do not imply current 0.1.0 account enforcement. | Partially supersedes ADR-0034's unsupported global uniqueness/concurrency and replacement-transaction claims while preserving event-backed archival and archive-is-not-deletion. |

These successors express decisions already settled in the approved specifications. Do not add another successor or broaden a decision because a current implementation detail seems inconvenient. If a live-source fact contradicts a settled decision, preserve the decision and record the gap for its owning roadmap row.

## Complete ADR Disposition Matrix

Every ADR from 0001 through 0037 is assessed below. `Retain` means no edit unless source/history review finds a factual contradiction; `editorial` keeps the decision and removes volatile implementation narration; `partial` or `superseded` requires a successor link and an explicit surviving/replaced scope; `correction` adds a dated factual clarification; `gap` stays a gap until its owning implementation row closes it. Do not force edits to records whose current text already expresses the outcome accurately.

| ADR | Required treatment in this slice |
| --- | --- |
| 0001 | Retain the durable Markdown decision-log choice and historical relocation chain; editorially clarify only if its current wording conflicts with the authored catalogue or new status semantics. |
| 0002 | Editorial rewrite around GameSession as the sole top-level command consistency boundary and cohesive session-owned children; remove extraction milestones and current class/test inventories. |
| 0003 | Partially supersede the dedicated log/diary-row authority with event history and projections; preserve composed JSONB snapshot storage; remove the removed-table inventory as current guidance. |
| 0004 | Preserve PostgreSQL and real-provider validation; add a dated correction for the shared `Z:/pg` lane replacing repo-local cluster ownership; route operational commands to their owner. |
| 0005 | Preserve CaseFile as session-owned with no independent command repository; describe ownership without freezing component APIs or a second aggregate protocol. |
| 0006 | Retain the knowledge-not-gang-pressure rule and clarify that source use may still advance clock/heat; do not imply investigation has no other session effect. |
| 0007 | Retain the hidden-truth/player-knowledge boundary and the explicitly guarded dev exception; remove implementation inventories if they obscure that rule. |
| 0008 | Editorial rewrite around town-visit source refresh and repeatability; remove object-shape inventories. |
| 0009 | Retain structured clue anchors and player-known plausibility; remove stale implementation proof or inventories while preserving the durable decision. |
| 0010 | Retain the accepted lawman-evidence derivation constraint as a planned decision; keep current telegraph scope lawman-only and do not turn the ADR into a feature backlog. |
| 0011 | Retain as a historical, already-superseded cockpit experiment and preserve its ADR-0027 successor link. |
| 0012 | Retain code-backed GameContent as current; preserve the database-backed alternative only as historical context, not a future-work assignment or external issue status claim. |
| 0013 | Editorial rewrite around travel as a session-owned subtree, not an independent command root; remove snapshot member inventories. |
| 0014 | Retain Onion/DDD/CQRS, repository ports and Unit of Work; preserve the distinct read and write ports without implementation inventory. |
| 0015 | Retain Minimal APIs as a thin HTTP boundary; clarify the handler/domain ownership distinction if required by the current wording; remove endpoint lists. |
| 0016 | Retain React/Vite/TanStack Query/styled-components; partially supersede obsolete cockpit/routing assumptions through ADR-0027/0030 and ADR-0039; remove old route proposals. |
| 0017 | Retain xUnit/Vitest/Testing Library/jsdom and real PostgreSQL validation; clarify that historical smoke-test skipping does not make current PostgreSQL integration optional. |
| 0018 | Retain the .NET target, nullable and implicit-using choices; remove unnecessary project inventories. |
| 0019 | Retain the manual typed frontend client and its generation threshold; remove hook and endpoint inventories. |
| 0020 | Partially supersede autonomous child event ownership and cross-aggregate protocol through ADR-0038; preserve cohesive legality, narrow ownership and the prohibition on reach-through mutation. |
| 0021 | Partially supersede mixer, broad descriptor and seed-owned starting-town choices through ADR-0039; preserve the UUID public contract and hidden-truth boundary. |
| 0022 | Retain manual browser evidence as distinct from automated tests; move operational instructions to the existing browser playbook and remove stale route lists. |
| 0023 | Partially supersede obsolete difficulty labels through ADR-0040; preserve randomness names, axis separation, fairness and fixed mystery truth. |
| 0024 | Retain source variation and fairness constraints; remove backlog assignments while preserving the current town/public source distinction. |
| 0025 | Retain the legal/warrant versus hidden-truth boundary; make historical future framing explicit and remove live backlog language. |
| 0026 | Preserve settlement, turn-in and case-resolution distinctions; correct stale claims that turn-in/payout do not exist without claiming the full murder-case loop is implemented. |
| 0027 | Partially supersede the retired route catalogue and old setup flow through ADR-0039; retain routed shell, query ownership and server authority; remove route diagrams. |
| 0028 | Editorially rewrite around immutable event-history authority and typed facts; healthy caches are maintained from committed events and serve normal state reads, while invalid caches rebuild from ordered history at a coherent stream position; replay reconstructs recorded facts and never re-rolls randomness. Preserve infrastructure envelopes, optimistic append, one staged repository/UoW path, rebuildable projections and transport independence; partially supersede old log-row and child protocols through ADR-0038; correct migration-era present-tense claims without restoring removed tables. |
| 0029 | Retain the lawman-pressure heat rule; correct zero-based turn notation as a representation change, not a gameplay-rule change. |
| 0030 | Editorial rewrite around contextual dev overlay and separate dev namespace; remove dimensions/private-field/path inventories; partially supersede the public developer-role direction through ADR-0041. |
| 0031 | Retain event-backed travel override setup/consumption; remove private-field, factory and snapshot-restoration recipes. |
| 0032 | Retain saloon override lifecycle, gate-aware eligibility and explicit dev truth; remove DTO/private-field recipes. |
| 0033 | Retain as historical, fully superseded by ADR-0037; keep its then-current mesh decisions as history, not live navigation. |
| 0034 | Correct the unsupported database-global uniqueness/concurrency guarantee and replacement atomicity claims; preserve archive event/history/terminality; partially supersede to the later per-user lifecycle through ADR-0042. |
| 0035 | Editorial rewrite around React-owned state and Phaser as render/input adapter; add a dated factual note for changed transport/setup shape; retain accessible equivalent town selection as an implementation gap for row 15. |
| 0036 | Partially supersede the exclusive snapshot-only preparation/start pattern through ADR-0039; retain the original rationale and clean player-API boundary as historical context; clearly distinguish the still-present code gap from the accepted retirement. |
| 0037 | Preserve the PR #188 clarification: partially superseded, with ADR location and retirement of generated index navigation still authoritative; do not rewrite its already-corrected subscription history. |

## Exclusions and Guardrails

- Do not implement the source repairs or accessible map control implied by ADR gaps; their roadmap rows own those outcomes.
- Do not delete a historical ADR, rewrite original dates, or erase removed behavior because current source differs.
- Do not leave `Last reviewed` metadata or treat an editorial date as a decision review date.
- Do not state the original reason for a removal unless a contemporaneous decision, commit, or other authoritative source supports it.
- Do not treat cloud successor ADRs as proof of authentication, authorization, deployment isolation, or ownership in the current application.
- Do not edit the six non-ADR documents; their independent custody reconciliation remains the third row-02 slice.
- Do not add a test that asserts ADR filenames, headings, status strings, summaries, or an expected list of links. This is authored decision content, not executable behavior; use the existing canonical repository gate and human semantic review.

## Task 1: Commit the JIT plan and retire the completed predecessor

**Files:** Create `.agents/plans/2026-10-08-adr-dispositions.md`; modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-07-adr-governance-and-discovery.md`.

- [x] Confirm PR #188 is merged to `develop` at merge commit `67646cb53a8604c85972f0697daafb96c55249ef`, and classify the predecessor's entire scope from that PR, its final branch and validation evidence; its remaining row-02 ADR and six-document work is expressly successor scope, not predecessor scope.
- [x] Update roadmap row 02 to record row 01 as complete with PR #187, PR #188 as merged with range `c34d688..67646cb`, and this plan as the active ADR-disposition successor; preserve row 02 as executing and keep the separate six-document slice pending.
- [x] Set `Directory.Build.props` to `0.1.0-dev.4`; verify it remains the only authored application version and do not modify package or lockfile versions.
- [x] Remove the predecessor plan and its stale roadmap link only after the merged PR and the full-scope classification above are verified.
- [x] Run `git diff --check`, inspect the staged candidate, and make a normal hooked commit before beginning ADR edits.

## Task 2: Reconcile current source and provenance before editing ADR history

**Files:** Read all `docs/decisions/ADR-0001` through `ADR-0037`, `docs/decisions/README.md`, the approved baseline/cloud specifications, and the relevant implementation/doctrine sources named by each finding; use Git history for material removals.

- [x] Refresh `origin/develop` and confirm the worktree still starts at merge `67646cb`; if another develop PR merged, rebase/refresh safely, advance the planned version to the next unused value, and revalidate the changed base.
- [x] For each matrix row, classify the live record as retain, editorial rewrite, partial/full supersession, dated correction, implementation gap, or planned decision; compare its status and successor links with the catalogue.
- [x] Trace source contradictions and material removals through their owning code, tests, doctrine and Git history, including the log-row removal, seed codec/setup ownership, missing starting-town behavior, difficulty labels, event/replay migration, turn-in/payout, prep/start flow, archive concurrency and map accessibility.
- [x] Record the exact commit/date and known reason where evidence exists; distinguish missing intent from evidence that a removal was deliberate. Keep any unsupported provenance explicitly unknown.
- [x] Confirm each proposed successor's accepted scope against the stable baseline or cloud specification. If a settled decision does not support a proposed clause, omit that clause and record the evidence-backed plan ruling; do not invent a sixth decision.

## Task 3: Create the accepted successor decisions

**Files:** Create `docs/decisions/ADR-0038-gamesession-consistency-boundary-and-internal-child-protocol.md`, `ADR-0039-seeded-game-setup-and-free-first-arrival.md`, `ADR-0040-difficulty-and-randomness-vocabulary.md`, `ADR-0041-public-and-preprod-developer-capability-boundary.md`, and `ADR-0042-user-owned-playthrough-lifecycle.md`.

- [x] Write ADR-0038 from the approved root/child decision, preserving domain-owned cohesive rules while assigning external command consistency and event authority to GameSession; link only the affected predecessors and state exactly which old scopes survive.
- [x] Write ADR-0039 from the approved seed and flow decisions: Go settles world/case truth; prologue follows; one selected first town performs a one-shot free arrival; the same world map supports setup and travel; no exclusive snapshot-only prep route is retained. Preserve the UUID codec choice without bit-layout details.
- [x] Write ADR-0040 with the exact current difficulty/randomness names and defaults and the durable separation of pressure from volatility; state that recorded mystery facts are not re-rolled.
- [x] Write ADR-0041 from the approved cloud specification with public player-only capabilities and owner-only preprod access; set status to `planned` and state explicitly that this is not current 0.1.0 deployment behavior.
- [x] Write ADR-0042 from the approved cloud lifecycle with immutable user ownership, at most one active playthrough per user, and immediate durable archival on confirmed start-over; set status to `planned` and distinguish it from current unowned local playtests.
- [x] Use the current ADR template, preserve only rationale supported by the source record, add no implementation inventories or proof receipts, and link each successor to its predecessors with clear relationship labels.

## Task 4: Apply every required predecessor disposition

**Files:** Modify only those `docs/decisions/ADR-*.md` files whose matrix treatment requires a change; leave accurately retained records unchanged.

- [x] Editorially rewrite surviving ADRs around their historical durable decision, rationale, alternatives and material consequences; preserve the original decision date and add a dated editorial note where the rewrite materially changes how the record reads.
- [x] Add partial/full supersession histories to the affected predecessors, linking ADR-0038 through ADR-0042 and identifying the exact replaced scope and surviving scope.
- [x] Add dated factual corrections for the removed `GameSessionLogEntries` authority, current PostgreSQL lane, implemented turn-in/payout, heat turn notation, current setup/map transport and unsupported archive guarantees where the evidence supports them.
- [x] Keep the ADR-0035 accessible town-selection decision even though the current interaction does not meet it; state the gap truthfully and leave the implementation to row 15.
- [x] Preserve ADR-0011 and ADR-0033 as historical superseded records and preserve ADR-0037's already-correct partial status; do not rewrite historical decisions just to match current code.
- [x] Remove implementation status/proof sections, mutable source/test lists, endpoint/table/class inventories and future backlog prose from edited records only after preserving any genuine historical decision or material removal they contain.
- [x] Ensure ADR-0034 no longer claims database-global uniqueness, supported concurrency, or atomic replacement; do not weaken the approved per-user future rule or imply it already exists.
- [x] Re-read each edited predecessor and successor together and check all reciprocal links and status history before proceeding.

## Task 5: Update the authored catalogue and validate the complete history

**Files:** Modify `docs/decisions/README.md`; retain `docs/decisions/TEMPLATE.md` unless evidence shows an actual uncovered authoring rule.

- [x] Add ADR-0038 through ADR-0042 exactly once in numeric order with one-sentence summaries that name decisions, not implementation state.
- [x] Update every status and successor summary changed by Tasks 3-4; verify all 42 records appear exactly once and every catalogue link resolves.
- [x] Verify every `superseded by`, `supersedes`, and `partially superseded` relationship is reciprocal and scope-specific; old superseded records remain in the catalogue.
- [x] Search edited records for stale implementation/future-work headings, `Last reviewed`, unlinked successor references, removed source paths presented as current, and any unsupported reason or date; manually classify each result rather than blanket-replacing terms.
- [x] Confirm no code, tests, migrations, six non-ADR docs, or generated artifacts changed. Do not add tautological documentation tests.
- [x] Confirm `tools/run.py` exposes no ADR-specific validation target; manually verify each local ADR/catalogue/successor link and the catalogue's one-entry-per-record coverage, then stage the complete candidate and make a normal commit; the check-only hook runs the canonical gate, including the web build and generated-version identity check.
- [x] Read the hook result, review the committed diff against this plan, the baseline/cloud specs and all 37 matrix rows, and correct any failure with a focused edit followed by another normal hooked commit; do not run a duplicate canonical check on an unchanged committed tree.

## Task 6: Establish central ADR and unslop routes with PR/review gates

**Files:** Create or update the central unslop playbook; update `.agents/unslop/README.md`, `.agents/unslop/writing.md`, `.agents/unslop/observations.md`, `.agents/playbooks/decision-records.md`, applicable lifecycle runbooks and topical playbooks, root `AGENTS.md`, `CONTRIBUTING.md`, `REVIEW.md`, `.agents/runbooks/pr.md`, and `.agents/runbooks/code-review.md` as required by the live route audit.

- [ ] Make one unslop playbook the authoritative selector for applicable profiles and their observation loop. Keep profile content in `.agents/unslop/`; remove duplicate selection rules from its README or other routers once the central playbook owns them.
- [ ] Route root agent, contributor, reviewer, lifecycle-runbook, and topical-playbook entrypoints to both the central unslop playbook and the decision-record playbook, with each work surface naming when the guidance applies.
- [ ] Keep selection scoped: agents consult relevant ADRs through the authored catalogue and follow successors; agents select applicable unslop profiles rather than loading every profile indiscriminately.
- [ ] State in the decision-record playbook that removing or contradicting an ADR-protected behavior requires a deliberate decision and matching amendment/successor, and that introducing durable architecture requires consulting the log and recording the decision. State that routine implementation preserving existing decisions does not require a new ADR.
- [ ] Make the PR runbook require authors to compare their proposed diff with relevant ADRs and include the required decision work, or explain why no durable decision changes.
- [ ] Make the review runbook require reviewers to repeat that check independently against the actual diff; a PR-author assertion or an ADR file change alone is not proof that the decision log remains true.
- [ ] Remove campaign-specific instructions from durable runbooks and topical playbooks when they belong to an active roadmap or plan; keep only reusable operating procedure. In particular, remove the stable-0.1.0 per-plan worktree/version contract from the general PR runbook after verifying its durable `develop`/release guidance remains.
- [ ] Remove empty or placeholder-only headings such as capability/skill sections containing only `None.` from runbooks and topical playbooks; add a concrete guard and distinct observation in the writing unslop profile so this boilerplate does not recur. Preserve empty fields only where a validator or external schema explicitly requires them.
- [ ] Do not add a heading-existence or empty-heading checker; review document meaning manually because structural assertions would not protect reader behavior.
- [ ] Audit durable guidance for other active-epic or temporary-plan references and classify each against its actual owner; move campaign requirements to the current roadmap/plan rather than leaving stale routes in permanent runbooks.
- [ ] Trace direct entry routes and remove duplicated or stale profile-selection guidance without weakening scoped profile selection, lifecycle stages, or topical ownership. Include `REVIEW.md` and `CONTRIBUTING.md` because reviewer and implementer harnesses may enter through them.
- [ ] Validate the routes by following them from each declared entrypoint and checking their applicability language and links. Confirm changed durable guidance contains no active-epic instructions or empty placeholder sections. Do not add source-string or link-existence tests as a proxy for effective routing.

## Task 7: Review, publish, merge, and retain successor artifacts

- [ ] Complete one fresh whole-branch review against the plan, both governing specifications, the decision-record and unslop playbooks, relevant profiles, and the protected-decision gates; fix Critical and Important findings with focused evidence and normal hooked commits.
- [ ] Confirm the PR targets `develop`, advances the latest merged development version exactly once, leaves package versions unchanged, and contains no out-of-scope code or generated evidence; publish as Draft and attach it to the current task.
- [ ] Promote to Ready only after local canonical validation and whole-branch review pass; read hosted checks on the exact PR head and merge only after they pass.
- [ ] Verify merge ancestry and the merge commit on refreshed `origin/develop`; retain this plan and the parent spec/roadmap in the completing PR. Row 02 remains open for its six-document custody slice.
- [ ] Remove only the verified merged branch/worktree and its branch-scoped scratch; if the host reports a lock, preserve the path and report the limitation rather than forcing deletion.

## Acceptance Evidence

- All 37 predecessor ADRs receive the treatment in the matrix, including explicit unchanged/retained decisions; the five new successor ADRs are accurate to their approved source specifications and no implementation status is overstated.
- Material removals and corrections are represented with known provenance or explicit uncertainty, and no past drift is made to look intentional.
- Applicable runbooks and topical playbooks route through central ADR and unslop playbooks; PR authors and reviewers each have an explicit decision-log check, and the review is independent of the author's claim.
- The authored catalogue contains ADR-0001 through ADR-0042 once each with accurate statuses and successor links.
- The repository canonical gate and production web identity check pass on the exact PR head; the branch review is clean of Critical and Important findings; the PR is merged into `develop`.
