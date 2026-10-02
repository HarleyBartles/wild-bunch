# AOM Self-Certification Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Wild Bunch's deployed AOM v1 infrastructure with six pinned v2 subscriptions and honest repository-owned self-certification, deleting unused scaffolders and source dependencies.

**Architecture:** The repository owns its checks and operating guidance. Native plugin dependencies remain separate from immutable standard-definition pins. Cut over subscription, certification, runner, hook adapters, and obsolete resource removal together so the old dispatcher never reads a v2 record.

**Tech Stack:** Python 3.12 hosted CI, active Python 3 locally, Bash tracked hook, Git, Codex native plugin configuration, existing .NET 10 and Node 20 validation lanes.

**Spec:** [AOM self-certification migration](../specs/2026-10-02-aom-self-cert-migration.md).

**Execution Strategy:** `executing-plans`, because the authority record, runner, hook adapter, and source removals share one coupled cutover. Keep implementation inline and obtain one fresh whole-branch review.

## Global Constraints

- No game, API, persistence, asset, or web product behavior changes.
- No new standards, plugin payload copies, scaffolders, generated navigation mesh, or generic deployment framework.
- Preserve meaningful local policies; remove only boilerplate or obsolete obligations supported by the assessment.
- Do not create tautological, source-string, or change-detector tests. Add behavior tests only for new or changed gate behavior.
- Do not skip hooks. Normalize text without arbitrary Markdown wrapping.
- Certification distinguishes Windows proof, hosted Linux proof, native configuration validity, and installed runtime availability. Missing hosted or runtime evidence is explicit.

## Review Focus

- V2 structural success is mistaken for semantic certification: Task 2 records actual assessment and evidence limitations; malformed records and missing certification targets fail focused checks.
- Unstaged or ignored state masks a bad candidate: Task 2 tests hook preservation, failure propagation, and candidate-owned configuration; Task 3 exercises hosted parity on the committed counterpart.
- Root/router changes silently remove required discovery: Task 2 tests missing routes, broken local references, budget failures, and legitimate scoped routing.
- Removing the source dependency breaks fresh-clone validation: Task 3 runs the gate in a clean detached clone without ambient plugin access or the old submodule.
- Marker-only completion misses shipped work or deletes future work: Task 1 classifies full artifact scope from implementation and publication evidence; Task 2 revises lifecycle guidance without automatic deletion by marker.

## Task 1: Establish authority and successor custody

**Files:** Read `.agents/contracts/operating-standards.json`, `.agents/standards/provenance.json`, `.agents/doctrine/completed-artifacts.md`, `.agents/playbooks/completing-plans.md`, all active `.agents/plans/`, `.agents/specs/`, and `.agents/roadmaps/`. Retire only proven eligible predecessor artifacts and their live links.

**Interfaces:** Consumes the specification's exact old/new pins and selected six definitions. Produces a reviewed invariant-to-implementation assessment and a custody decision before cutover.

- [ ] Read all six proposed definitions with `git -C Z:/agent-asset-marketplace show b481f98ae90aa45e5271d10fe1f7aaeb6c7047aa:<definition>` using the spec's paths, after verifying the source origin. Compare historical resource/catalog obligations at `5fcfb473948fd0f32998ffce31d8e528f2261572`. Keep the historical record authoritative during preparation.
- [ ] Inspect predecessor artifact scopes, current implementation, and live GitHub completion facts. The three existing plans cover BUNCH-151, ambient opt-in standards, and native plugin subscriptions; a remaining unchecked item is not proof of incomplete scope. Retire only when the current historical custody policy permits it and durable content is promoted. If unmarked completion conflicts with that policy, preserve the file until Task 2 explicitly reconciles the new semantic rule. Do not infer completion solely from memory or a PR number.
- [ ] Record meaningful findings in the specification or durable owner documents; keep transient inspection material in `Z:/_agent-scratch/wild-bunch/codex-aom-self-cert-migration`. Commit any eligible retirement through the ordinary hook before implementation.

**Exit:** Both authorities and the exact subscription selection are understood; predecessor artifacts are classified without deleting active or ambiguous work.

## Task 2: Make one coherent repository-owned cutover

**Files:** Create `scripts/check_operating_standards.py`, `scripts/check_agent_routers.py`, `scripts/check_plugin_subscriptions.py`, `.agents/contracts/standards-certification.md`, and focused behavior tests under `scripts/tests/`. Modify `.agents/contracts/operating-standards.json`, `.agents/contracts/repo-standards-commands.json`, `tools/run.py`, `scripts/tests/test_run.py`, `scripts/tests/test_local_skill_registration.py`, `AGENTS.md`, `.agents/plugins/marketplace.json`, `.agents/doctrine/repo-skills-policy.md`, `.agents/doctrine/repo-runbook-policy.md`, `.agents/doctrine/completed-artifacts.md`, `.agents/doctrine/validation-policy.md`, `.agents/playbooks/completing-plans.md`, `.agents/playbooks/marketplace-generation.md`, `.agents/playbooks/testing.md`, `CONTRIBUTING.md`, `scripts/README.md`, and affected lifecycle routes. Remove `.agents/standards/`, `scripts/validate_repo_skill_scripts.py`, `.agents/plugins/marketplace-source`, its `.gitmodules` entry, and empty generated `.devin/config.json` if no independent settings exist. Adjust `.github/workflows/ci.yml` checkout to remove submodule acquisition. Preserve `githooks/pre-commit` candidate/parity behavior; correct its stale skill-generation comments.

**Interfaces:** Consumes Task 1's authority and custody assessment. Produces `py -3 scripts/check_operating_standards.py --check`, `py -3 scripts/check_agent_routers.py --check`, `py -3 scripts/check_plugin_subscriptions.py --check`, wired into the existing complete `tools/run.py ci --check` lane. `ci --apply` activates `githooks`, generates ADR freshness, and checks selected mechanical facts; it cannot certify or recreate standard assets.

- [ ] Establish narrow behavior tests before new gate logic. Use temporary repository fixtures: malformed immutable pin or unsafe path fails; a missing certification file/fragment fails; a valid record passes without declaring semantic success. A root lacking either record route, a broken local route, an oversized router, or a scoped router lacking its scope/read condition fails. Valid concise root/scoped fixtures pass. For native dependencies, test supported Git selectors and valid declarations, invalid path/selector and dangling activation failures, and acceptance of inactive catalog entries. Assert observable exit/diagnostic behavior, not source strings or exact implementation commands.
- [ ] Adapt only the pinned `skills/repo-standards/scripts/subscriptions.py` and `skills/repo-agent-assets/assets/check_plugin_subscriptions.py` as small repository-owned structural helpers, recording origin in their headers. Implement router policy locally using tracked files, the spec's 40/15 line budgets, scope/read-condition requirement, and local route checks. Include focused Python behavior tests in the canonical check lane, which currently does not run `scripts/tests`.
- [ ] Remove exact `repo.local_skills` inventory and the registration-based test after replacing its useful custody assertion with direct authored-skill/frontmatter validity. Preserve the six authored skills byte-for-byte. Native Git plugin declarations remain unchanged otherwise; preserve Codex main catalog binding. Supported repository plugin activation is Codex; Devin plugin activation remains explicitly deferred rather than falsely certified.
- [ ] Replace `_repo_standards_cmd` and the old apply/check dispatch in `tools/run.py` with the three repository-owned read-only checks. Remove the no-op external skill-script lane. Preserve diagnostic failure collection, complete .NET/web coverage, ADR freshness, and shared-checkout mutation guard. Activate `core.hooksPath=githooks` explicitly in `ci --apply`, since the removed runtime owned that setup. Reduce `generated_paths` to actual generated output, `docs/decisions/README.md`; do not add certification or subscriptions to generation ownership.
- [ ] In one pending cutover, write six v2 entries using the exact source/pin/definition table in the spec and certification references `.agents/contracts/standards-certification.md#<id>`. Explicitly retire the two obsolete standard IDs. Remove the full deployment tree and then remove the tooling gitlink through Git's submodule removal procedure in this worktree, checking path containment and main-checkout ownership before any filesystem cleanup. Remove the empty Devin file only after inspecting its contents. Do not delete shared submodule metadata or the primary checkout.
- [ ] Assess every selected pledge against observed files. Certification identifies useful runbooks/playbooks, routes, reference upkeep, capability source/availability, authored skill custody, router policy/checks, hook candidate preservation, ignored input rebuilding, hosted prerequisites, and next-slice artifact classification. Record pending evidence honestly. Revise the marker-dependent completed-artifact doctrine to the new semantic rule. Remove obsolete five-section router, fixed inventories, installed projection synchronization, scaffold provenance, exact-name registration, and no-op validator claims. Keep useful authored guidance and repair the existing validation-doctrine link to missing `.agents/runbooks/testing.md` by routing to the actual testing playbook.
- [ ] Run `py -3 -m pytest scripts/tests -q` and the three new checker commands. For changed hook integration, use a disposable temporary Git fixture to prove an invalid staged candidate fails even with an unstaged repair, unrelated unstaged edits survive success/failure, configuration comes from the candidate, and normalization affects only staged/declared generated paths. Keep existing hook behavior when it already meets these conditions; do not replace it with the optional new starter.
- [ ] Inspect all live callers with `rg -n 'standards/|marketplace-source|repo_standards|scaffold|local_skills' tools scripts .agents .github AGENTS.md CONTRIBUTING.md README.md`, distinguishing this plan/spec and historical evidence from executable or authoritative routes. Check no deleted dependency is invoked or reconstructed. Stage the coherent cutover, ensure PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`, then commit normally. Do not run the full canonical gate immediately before the same hooked commit.

**Exit:** The ordinary hook passes with the new record and checks, the retired framework and submodule are absent, and retained standards have truthful certification. No intermediate cutover commit invokes the old dispatcher with v2 data.

## Task 3: Prove independent operation and hand off

**Files:** Review the full branch, update `.agents/contracts/standards-certification.md` with actual evidence, and mark this plan/spec completion only when their whole scope is finished. Follow `.agents/runbooks/code-review.md` and `.agents/runbooks/pr.md` for review and any authorized publication.

**Interfaces:** Consumes Task 2's committed migration. Produces fresh-clone and candidate/committed parity evidence, fresh review, and an accurate completion report.

- [ ] Make a clean detached local clone of the branch under its branch-scoped scratch root, with no ambient plugin dependency and no Marketplace source submodule. Install `scripts/requirements.txt`, provide existing PostgreSQL/.NET/Node prerequisites, and run `REPO_STANDARDS_HOSTED_COMMIT=HEAD githooks/pre-commit` through Bash there. This deliberate parity exercise is separate from the successful normal hook. Report Windows/local parity separately from actual hosted Linux evidence.
- [ ] Inspect native Codex declarations and installed representative plugin skills in the task checkout without editing global plugin settings. Configuration validity does not establish runtime installation or authentication. If a Codex fresh-process field test is unavailable, identify the limitation and keep runtime evidence pending rather than claiming success. No change to the four plugin payloads is required.
- [ ] Obtain fresh whole-branch review against the spec and `.agents/runbooks/code-review.md`; correct actionable findings and obtain fresh review after corrections. Finish certification evidence and mark these artifacts `completed-awaiting-retirement` for the successor slice only when the migration acceptance criteria are met. Commit final corrections through the normal hook.
- [ ] Return exact branch/head, selected source pin, retired paths, focused and complete gate results, review findings, and any pending hosted/runtime evidence. Publication and merge follow the user's authorized scope; do not count a local parity fixture as hosted Linux proof or claim a PR exists without GitHub evidence.

**Exit:** The migration is reviewable with independent clone/parity and fresh-review evidence, honest certification status, and no claim beyond observed systems.
