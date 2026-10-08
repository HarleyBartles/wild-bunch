# ADR Governance and Discovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make durable decisions discoverable and maintainable through an authored catalogue, one shared decision-record procedure, and lifecycle routes that preserve truthful history.

**Architecture:** `docs/decisions/README.md` becomes the authored decision catalogue and human entrypoint. A single repository playbook owns decision selection, creation, correction, supersession, and history handling; lifecycle runbooks and relevant topical guides route to it. Remove the generated freshness table because its maximum history date does not prove semantic review.

**Tech Stack:** Markdown, Python 3 command bus, tracked Git hook, .NET/MSBuild and Vite-generated application version.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; roadmap outcome 02 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; source assessments `.agents/investigations/stable-0.1.0/2026-10-06-adr-truth-investigation.md` and `2026-10-06-docs-custody-investigation.md`.

**Execution Strategy:** `executing-plans` inline because the catalogue, authoring convention, lifecycle procedure, routes, generator retirement, and one-PR version identity form one coupled governance surface. The authorized roadmap goal selects inline execution; the nearest alternative, per-task subagent development, would add handoffs across documents whose correctness depends on a single shared procedure and review of the complete route.

## Global Constraints

- Start from refreshed `origin/develop` at `c34d688ba5c919703f739cfeca3452feedcb3db1`, in this fresh canonical worktree, and deliver by PR to `develop`.
- `develop` is the repository default branch and ordinary development PR base; `main` is reserved for explicit releases and hotfixes.
- This PR advances the merged `0.1.0-dev.2` identity to `0.1.0-dev.3` exactly once; `Directory.Build.props` remains the only authored application version.
- Retire the completed plan 01 and its stale roadmap link in this successor PR's first substantive commit; preserve this plan, the parent specification, and the roadmap through this PR.
- Preserve every ADR's original decision date and material change/removal history. Editorial cleanup must not make drift or accidental loss look deliberate.
- The ADR catalogue is authored, not generated. Do not create `INDEX.md` or restore a generated index mesh.
- This plan establishes decision-record governance and discovery only. It does not rewrite all 37 ADR bodies or consolidate the six non-ADR docs; those remain later JIT slices under roadmap row 02.
- The local hook and hosted CI remain check-only. Explicit maintenance may not mutate tracked decision files during validation.
- Do not change gameplay, APIs, persistence schemas, dependency versions, AOM subscriptions, or unrelated test behavior.

## Review Focus

- A history-entry date is not represented as a semantic review date; no agent is told that generating or refreshing a date proves a decision was reviewed.
- A partially superseded decision remains discoverable as partial, with the successor or surviving scope stated clearly.
- Each lifecycle entrypoint routes agents to the same decision procedure and supports selecting relevant records rather than loading every ADR.
- The check-only gate no longer invokes a retired freshness generator or expects generated decision metadata.
- The application version appears once in authored source and the production web artifact reports the generated `.dev.3` identity.

---

### Task 1: Commit this JIT plan and retire the completed predecessor

**Files:**
- Create: `.agents/plans/2026-10-07-adr-governance-and-discovery.md`
- Delete: `.agents/plans/2026-10-07-check-only-build-identity.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`

**Interfaces:** This plan consumes the merged row 01 implementation and PR #187. Its output is the committed, in-flight execution plan and an accurate roadmap state for row 01 and the bounded first row 02 slice.

- [x] **Step 1: Record successor-slice retirement and JIT boundaries.** Mark row 01 done using its merged PR and commit evidence, remove its stale plan link while retaining the PR link, and link row 02 to this plan. Explain that row 02 remains open after this slice: a later JIT PR applies the 37 ADR dispositions, and another JIT PR reconciles the six non-ADR docs. Do not author those plans or mark row 02 done.
- [x] **Step 2: Inspect the exact plan and roadmap candidate.** Verify the plan path, version floor, PR target, predecessor retirement, and successor boundaries. Run `git diff --check` and commit normally with `docs: plan ADR governance and retire completed predecessor`; the check-only hook validates the staged candidate.

### Task 2: Author the ADR convention and progressive catalogue

**Files:**
- Modify: `docs/decisions/README.md`
- Modify: `docs/decisions/TEMPLATE.md`
- Modify: `docs/decisions/ADR-0037-ambient-capabilities-opt-in-standards-and-decision-documentation.md`

**Interfaces:** The README is the stable human entrypoint and authored catalogue. The template describes durable decisions and dated amendments, not implementation plans, receipts, or a second feature roadmap. Existing ADR status and successor relationships remain readable until the later disposition slice updates every applicable record.

- [x] **Step 1: Replace the flat title list and generated-date table with the authored catalogue.** Include every existing ADR exactly once in numeric order, link its stable file, and write one concise sentence describing the decision recorded there. Show the recorded status and successor/surviving scope where known. Correct ADR-0037 to partial supersession by the current standards contract/certification while preserving its decision-log and retired-mesh decisions. Do not create a separate INDEX or claim every entry is implemented.
- [x] **Step 2: Rewrite the template around decision history.** Retain context, decision, rationale or alternatives, consequences, related decisions, and dated history. State that decision status describes whether the decision remains authoritative, not whether implementation is complete. Support an explicit partial-supersession status. Remove mandatory implementation status/plan, source inventories, validation/proof receipts, and boilerplate future-work sections. Require dated editorial notes to preserve the original decision date and identify that an edit is editorial rather than a newly made decision.
- [x] **Step 3: Review the catalogue against all 37 current ADRs and the investigation disposition table.** Verify the one-sentence summaries and the retained, partial, full, planned, and rejected relationships against the live records. Keep unresolved provenance explicitly unknown; do not infer intent or move detailed dispositions into the catalogue.

### Task 3: Remove the misleading generated freshness mechanism

**Files:**
- Modify: `tools/run.py`
- Delete: `scripts/update_adr_freshness.py`
- Delete: `scripts/tests/test_adr_freshness.py`
- Modify: `CONTRIBUTING.md`
- Modify: `.agents/runbooks/implementing.md`
- Modify: `.agents/runbooks/pr.md`
- Modify: `.agents/playbooks/testing.md`
- Modify: `.agents/playbooks/completing-plans.md`

**Interfaces:** `ci --apply` configures the repository hook and runs selected checks; `ci --check` remains the canonical read-only validation command. Neither command generates a semantic review claim or refreshes generated metadata. Authored catalogue content is reviewed with its ADR changes.

- [x] **Step 1: Remove freshness apply/check wiring.** Delete the freshness command helpers and `decision-freshness` CI lane from `tools/run.py`; remove the apply invocation from `_ci_apply`. Keep all unrelated check and apply behavior unchanged.
- [x] **Step 2: Retire only the generator's own behavior test.** Delete the tests that exercise table sorting and rewriting because the generated table and its contract are removed. Do not replace them with source-string, filename, or catalogue-shape tests.
- [x] **Step 3: Correct maintenance guidance.** Remove the claim that the hook refreshes decision freshness output. State that the hook checks the staged candidate and leaves decision files unchanged; route semantic decision updates through the decision-record playbook. Correct generic `ci --apply` metadata-refresh instructions in implementing, testing, completing-plans, and PR guidance to name the owning maintenance command, while retaining `ci --apply` for hook setup and its actual selected checks.
- [x] **Step 4: Run the affected Python lanes.** Run `py -3 -m pytest scripts/tests -q` and `py -3 -m pytest tools/tests -q`. Search tracked source/guidance for `update_adr_freshness`, `decision-freshness`, `Last reviewed`, and `Decision Status and Review Dates`; no obsolete generator, test, or claim may remain.

### Task 4: Establish one decision-record procedure and route lifecycle obligations

**Files:**
- Create: `.agents/playbooks/decision-records.md`
- Modify: `AGENTS.md`
- Modify: `REVIEW.md`
- Modify: `CONTRIBUTING.md`
- Modify: `.agents/doctrine/repo-runbook-policy.md`
- Modify: `.agents/doctrine/architecture-guardrails.md`
- Modify: `.agents/doctrine/event-sourcing-integrity.md`
- Modify: `.agents/runbooks/design.md`
- Modify: `.agents/runbooks/planning.md`
- Modify: `.agents/runbooks/implementing.md`
- Modify: `.agents/runbooks/code-review.md`
- Modify: `.agents/runbooks/pr.md`
- Modify: `.agents/playbooks/code-style.md`
- Modify: `.agents/playbooks/dev-overlay.md`
- Modify: `.agents/playbooks/seeded-game-setup.md`
- Modify: `.agents/playbooks/testing.md`
- Modify: `.agents/playbooks/security.md`
- Modify: `.agents/playbooks/ui-browser-check.md`
- Modify: `.agents/playbooks/marketplace-generation.md`

**Interfaces:** The decision-record playbook owns the shared process. Root contributor/reviewer surfaces and lifecycle runbooks state the applicable trigger and link to it; topical playbooks identify when their concern should select relevant records. Do not copy the full procedure into each surface.

- [x] **Step 1: Write the shared procedure.** Explain when a durable decision warrants an ADR; how to discover current records through the catalogue, follow successors, and compare the decision with source; how to record a successor, partial/full supersession, dated correction, deviation, or material removal; and how to leave unsupported intent unknown. Require a plan to name governing decisions and planned history edits, implementation to record discovered divergence/removal, review to check the diff against current decisions, and PR publication to confirm required changes or state why none are needed. Make scope-based selection explicit; never require full-folder reading for every change.
- [x] **Step 2: Route lifecycle entrypoints.** Add concise direct links and stage-specific read/create/update/review obligations to `CONTRIBUTING.md`, `REVIEW.md`, `design.md`, `planning.md`, `implementing.md`, `code-review.md`, and `pr.md`. Keep root `AGENTS.md` a thin pointer and the runbook policy a router; no duplicated detailed procedure.
- [x] **Step 3: Route topical decisions.** Add direct, concern-scoped composition pointers from the listed architecture and topical playbooks. Replace the doctrine's brittle claim that ADR-0028 alone is the current decision authority with a route through the catalogue and successor chain while retaining the doctrine as the implementation rule owner.
- [x] **Step 4: Follow each route from its work point.** For contributor, reviewer, design, planning, implementation, PR, and each changed topical playbook, verify the trigger identifies when the decision-record playbook must be read and that its link resolves. Do not add a route merely to satisfy a link count.

### Task 5: Advance this PR's version and validate the complete candidate

**Files:**
- Modify: `Directory.Build.props`

**Interfaces:** The production web build generates `src/WildBunch.Web/dist/version.json` from the effective MSBuild version. `tools/versioning.py` validates that artifact; no duplicate authored npm version is added.

- [x] **Step 1: Advance the only authored version.** Set `Directory.Build.props` to `0.1.0-dev.3`. Confirm `package.json` and the npm lockfile remain unchanged and the web artifact is generated by the existing build step.
- [x] **Step 2: Inspect the candidate and commit the completed slice normally.** Run the affected focused lanes, `git diff --check origin/develop...HEAD`, and review the exact staged diff. Commit with `docs: establish ADR catalogue and lifecycle routing` or an accurate final-scope equivalent; the normal hook runs the canonical check-only gate.
- [x] **Step 3: Verify version and candidate preservation.** Confirm `Directory.Build.props` is the only authored application version, the production artifact reports `0.1.0-dev.3`, the hook did not mutate or stage decision files, and unrelated candidate/worktree contents remain intact.

### Task 6: Review, publish, merge, and retire this worktree

- [ ] **Step 1: Complete the whole-branch review.** Review `origin/develop..HEAD` against this plan, the baseline specification, the ADR/document investigations, the review runbook, and applicable unslop guards. Correct all Important findings with focused proof and normal hooked commits; report unresolved Minor findings without broadening scope.
- [ ] **Step 2: Set the repository default and publish a Draft PR to `develop`.** Set and read back GitHub's default branch as `develop`, push this branch, open a Draft PR to `develop`, attach it to the task, and read back the PR head/base/status. Keep the parent spec and roadmap and this plan through the completing PR.
- [ ] **Step 3: Enable hosted validation after local proof and review are clean.** Mark the PR ready, read back hosted checks on the exact head, and merge to `develop` only after they pass.
- [ ] **Step 4: Verify integration and clean the branch.** Confirm the PR is merged, its merge commit is in refreshed `origin/develop`, and the feature head is integrated. Remove only this merged worktree/branch and its branch-scoped scratch after preserving the review outcome and rulings in the execution ledger; stop if the host reports a locked worktree.

## Completion Evidence

Report the `develop` base and merge commit, the `.dev.3` identity, changed files, focused and canonical checks, the final review verdict, the Draft/ready/merged PR state, and any hosting or worktree-cleanup limitation. Row 02 remains executing until its later JIT ADR-disposition and six-document custody slices are complete.
