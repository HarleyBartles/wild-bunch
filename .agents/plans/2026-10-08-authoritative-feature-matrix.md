# Authoritative Feature Matrix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish one truthful, authoritative feature matrix for Wild Bunch and make feature promises and dependencies a maintained part of design, implementation, review, and release work.

**Architecture:** Consolidate the approved feature discovery and source-audit evidence into `docs/features.md`, keeping product capabilities distinct from developer controls and platform requirements. Put the maintenance procedure in one feature-matrix playbook and route lifecycle stages to it; retire the provisional duplicate inventory after its unique decisions and evidence are either promoted or explicitly retained at their proper owners.

**Tech Stack:** Markdown documentation, repository runbooks and playbooks, existing command bus and repository validation.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially product boundary, feature ownership, source-of-truth split, dispositions, and row 01; [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 01 and per-plan delivery contract.

**Execution Strategy:** `executing-plans` with Native inline execution. This is one tightly coupled documentation and routing outcome: the matrix, its single maintenance procedure, lifecycle routes, predecessor retirement, and release-version update must describe the same current authority. The user authorized JIT plans executed inline under the active roadmap goal.

## Global Constraints

- Start from `origin/develop` at `4b3117777c5242aa9d338f4a3a4bb55f491ba0f2` in the fresh worktree on `codex/stable-0.1.0-row-01-feature-matrix`.
- Advance the sole authored application version in `Directory.Build.props` exactly once from `0.1.0-dev.8` to `0.1.0-dev.9` for this PR.
- Preserve feature IDs PG-001 through PG-011 and the separate retirement/addition work IDs already assigned in the approved records; update assessments from live source and the approved spec rather than copying a stale discovery table.
- Distinguish a product promise, implementation assessment, release disposition, evidence strength, and directed dependency meaning. Unknown or unassessed behavior stays explicitly unknown.
- `docs/features.md` is the one current product feature matrix. Do not keep a second independently maintained matrix in investigations, the roadmap, ADRs, or playbooks.
- Feature lifecycle guidance must be effective at design, planning, implementation, review, and PR publication. Keep the matrix as product truth and the playbook as its maintenance procedure.
- Preserve the adopted AOM playbook/runbook composition contracts and update their certification when changed.
- Do not add a checker or tests that only freeze headings, file existence, or table shape. This documentation change has no product behavior; validate its semantic coverage, references, staged candidate, and repository gate.
- The current merged AOM SemVer definition is at `12c86626f` in `HarleyBartles/agent-asset-marketplace`, PR #350. Row 18 must pin the accepted immutable definition current at its own JIT planning time; this epic does not define a future `1.0.0` compatibility contract.

## Review Focus

- The matrix reports incomplete and disabled capability honestly instead of treating endpoints, event types, UI labels, or existing tests as proof of a working feature; prove this through source-linked status and independent evidence-strength fields in each row.
- Dependency direction and conditions are explicit and do not imply coupling from shared imports or ordinary player sequence; check each edge against the feature assessments and settled spec.
- The removal/future-addition split for unrelated criminals, distinct service types, telegraph lawman clues, richer casebook inference, gunfights, and developer-only controls remains discoverable without presenting future work as shipped.
- A feature change routes through one procedure at every relevant lifecycle stage, and reviewers independently check the actual diff against the matrix; validate inbound links and read conditions in the authored routes.

---

### Task 1: Bootstrap the feature-matrix successor and retire the completed style plan

**Files:** Create `.agents/plans/2026-10-08-authoritative-feature-matrix.md`; modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/specs/2026-10-06-cloud-playability-and-releases.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-08-post-edit-formatting.md`.

- [x] Verify `develop` is still at `4b3117777c5242aa9d338f4a3a4bb55f491ba0f2`, the row 04 tree is clean and the reviewed/merged PR #193 evidence still identifies the delivered head and hosted canonical gate.
- [x] Classify the full post-edit-formatting plan against PR #193 and its merged source; record row 04 as delivered through PRs #192 and #193, mark it done, replace its active plan link with its successor-retirement status, and remove the completed plan.
- [x] Correct row 01 from done to executing because the required authoritative matrix is absent; link this plan and preserve PR #187's completed development-identity evidence separately from the outstanding matrix delivery.
- [x] Update the roadmap and release specification's SemVer note to cite merged AOM PR #350 at `12c86626f`, state that pre-1.0 adoption need not promise a future stable API, and keep future `1.0.0` contract definition at the point when that release is genuinely prepared.
- [x] Advance `Directory.Build.props` once to `0.1.0-dev.9` and commit the plan plus successor bookkeeping through the normal check-only hook before starting Task 2.

### Task 2: Reconcile and publish the authoritative product matrix

**Files:** Create `docs/features.md`; modify `.agents/investigations/stable-0.1.0/README.md`, `.agents/investigations/stable-0.1.0/2026-10-06-stable-0.1.0-investigation.md`, the hunt-creation, playthrough-lifecycle, saloon-challenge, store/inventory and unrelated-criminal contracts, and `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; retire `.agents/investigations/stable-0.1.0/2026-10-07-feature-inventory.md` after classifying its full content and promoting every live decision to the matrix or its proper current owner.

- [x] Reconcile player capability groups PG-001 through PG-011 against the approved spec, the investigation index and each linked application, persistence, domain, web, and test assessment. Preserve settled user stories and distinguish source assessment from browser/runtime evidence; status unknown paths as not assessed rather than inferring success.
- [x] Record PG-001 as name-required and quick-play with optional difficulty/randomness defaults and a fresh editable seed UUID each setup visit; Go creates the settled world, then the player reads the case lead in the prologue and chooses a starting town through a free first travel on the same map.
- [x] Record PG-002 as server-authoritative event-backed continuation; confirmed start-over archives immediately and persists; one active playthrough is the current scope, while cross-tab synchronization and account-backed discovery remain excluded.
- [x] Record PG-003 as one world/travel map, one persistent generated town layout per world, and town navigation as the shared route to services; accessible selection remains a repair obligation.
- [x] Record PG-004 as one store and common saloon, sheriff, store, and disabled telegraph services; prosperity may affect stock/prices; one horse and one canteen maximum, dead horse removal with saddle retained, town refill only for an owned canteen, unique non-stackable equipment, stackable consumables, and inventory floors at zero.
- [x] Record PG-005 as prologue lead, wanted public notices, saloon gossip and observation; sheriff records become the public noticeboard interaction; telegraph stays disabled for lawman intelligence; no automatic casebook deductions are promised in 0.1.0.
- [x] Record PG-006 as observe then name before a take-in attempt; citizens cooperate and are released with a small fine; wanted targets require a gun to compel but no ammunition; wrong identity yields the agreed fine without bounty or roundup; gunfight, death and sheriff poster-circulation features remain future work.
- [x] Record PG-007 as one legal travel lifecycle with retained hostile trail encounters, no generated interactive trail NPCs, zero-floored inventory, 25 HP per unfed trail day, and terminal death at 0 HP; arrival chooses the town once and begins play there.
- [x] Record PG-008 as accumulated casebook evidence that remains visible after capture with captured status, player-drawn deductions, and one event-derived in-world journal with full-playthrough and journey views; no second independent journal author or technical audit text is promised to players.
- [x] Record PG-009 as excluded from 0.1.0 with separate PG-009-R retirement and PG-009-A future addition records; record lawman pursuit/heat effects as future only and the first future telegraph scope as stale, sourced lawman-location intelligence rather than gang-identity clues.
- [x] Add the settled 0.1.0 inclusion, repair, consolidation, retirement, and deferral dispositions for all rows, plus directed dependencies and their relationship/conditions. Preserve future service types, unrelated criminals, saloon gunfights, sheriff poster circulation, richer inference, NPC systems, lawman pursuit, developer-only layout salt controls, account identity and hosting as separate future/support scope.
- [x] Add separate developer-capability and supporting-platform sections. Record the single salt-control slice, public/developer boundary and low priority accurately; keep deployment, identity and release support distinct from player features.
- [x] For every matrix row, identify the promise and entry point, availability prerequisites, meaningful outcomes/failure/resume boundary, implementation and evidence status, release disposition, directed dependencies and their relation/condition, and the owning source or evidence record. Link uncertainties and future additions instead of inventing answers.
- [x] Transfer the provisional inventory's unique current decisions and dependency descriptions to the matrix or their existing specific owner; retire the duplicate inventory only after every paragraph has a truthful destination or is explicitly superseded by the approved spec. Keep the source audits and PG-009-R/PG-009-A records intact.

**Inventory reconciliation record:** Discovery and evidence-strength rules move into the matrix reading guidance and feature-matrix playbook. PG-001, PG-002, PG-004 through PG-009 promises and dependencies move into their matrix entries and the existing approved contracts/specification; the distinct PG-004-R/A, PG-006-C/A/B and PG-009-R/A work boundaries remain in the matrix partition table and their feature-specific contracts. PG-010/011, DEV-001 and PLAT-001 through PLAT-004 move into their own matrix rows. Open source/test questions remain in their linked layer and test investigations. F-019 records the inventory's historical role; no current feature truth or audit evidence is deleted with the provisional table.

### Task 3: Route matrix maintenance through feature lifecycle work

**Files:** Create `.agents/playbooks/feature-matrix.md`; modify `AGENTS.md`, `README.md`, `CONTRIBUTING.md`, `REVIEW.md`, `.agents/doctrine/repo-runbook-policy.md`, `.agents/runbooks/design.md`, `.agents/runbooks/planning.md`, `.agents/runbooks/implementing.md`, `.agents/runbooks/code-review.md`, `.agents/runbooks/pr.md`, and `.agents/contracts/standards-certification.md`.

- [x] Write the playbook as concern guidance: when a product promise, capability boundary, dependency, release disposition, or evidence assessment changes, read and update `docs/features.md`; distinguish product truth from implementation plans, ADR history and issue backlogs; preserve unknowns and evidence limits.
- [x] Route relevant lifecycle stages to the playbook, including an independent reviewer check of the actual diff and PR-author responsibility to include the matrix update or explain why no matrix truth changed.
- [x] Add one concise root-agent route and a human README entry to the matrix and maintenance procedure; keep the root router within its existing budget and avoid duplicating the matrix itself into agent guidance.
- [x] Update repo-runbook policy and playbook-composition certification to name the new concern owner, relevant routes, and semantic upkeep; do not add a heading-count or existence test.

### Task 4: Validate and deliver row 01

**Files:** All Task 1-3 files; update this plan and the roadmap with actual delivery evidence before publication.

- [x] Review the matrix row by row against the approved spec and investigation records, verify every local link, ensure the provisional inventory no longer competes as live truth, and compare the changed routes with the pinned AOM composition standards.
- [x] Run the canonical fail-fast `py -3 tools/run.py ci --check` and `git diff --check`; manually verify every changed local Markdown link and do not add a tautological document-shape test.
- [x] Compare the diff with relevant ADRs through the decision-record playbook; update a record only if a durable decision actually changes. No durable ADR decision changes in this documentation slice; ADR-0024 and ADR-0029 were checked against the matrix claims.
- [ ] Commit through the normal check-only hook, obtain one fresh whole-branch review, fix and re-review actionable findings, then publish a Draft PR to `develop`; verify exact head and hosted canonical checks before marking it Ready and merging under the active goal.
- [ ] In the completing PR, mark row 01 done and record its actual PR/merge proof only after the matrix and lifecycle route are complete; keep this plan active until that completing PR is merged.
- [ ] Keep this plan and the governing spec through the completing PR; after merge, the next fresh substantive slice retires this completed plan and records actual row 01 delivery evidence.
