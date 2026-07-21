# Wild Bunch Local Skill Custody Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `wild-bunch-*` a protected, repo-local skill namespace; migrate the four current skills to local authorship; and enforce the local skill contract in automation.

**Architecture:** `.agents/skills/wild-bunch-*/` is the canonical inventory, discovered by prefix rather than a maintained name list. The marketplace installer continues to own only non-reserved projections and records only those in provenance. A focused validator enforces the local contract in CI and preflight, while the doctrine routes agents to the canonical policy and skill roots.

**Tech Stack:** Python 3.12, pytest, PyYAML, Markdown/YAML skill files, PowerShell CI preflight, GitHub Actions.

## Global Constraints

- Do not modify `.agents/plugins/marketplace-source` or advance its gitlink; marketplace retirement is a separate worker's responsibility.
- Treat every directory matching `.agents/skills/wild-bunch-*` as repo-local by naming rule. Do not create a second inventory of those skill names.
- Do not hand-edit marketplace-derived skills or provenance except by running `python scripts/install_agent_skills.py` after the installer protection exists.
- Keep `AGENTS.md` thin. Put durable skill law in `.agents/docs/skill-authoring-policy.md` and route to it from the project-doctrine reference.
- Use LF for authored text and run the whole mesh generator after adding or deleting tracked files.
- Preserve current product/domain behavior; this slice changes agent infrastructure only.
- Validate the migration with the current marketplace checkout and again after the separate marketplace-removal change is available.

---

## File Structure

| File | Responsibility |
| --- | --- |
| `.agents/docs/skill-authoring-policy.md` | Canonical Wild Bunch local-skill qualification, custody, shape, review, and validation contract. |
| `.agents/docs/mesh-policy.md` | Declares the permanent reserved-prefix custody rule and directory-discovered inventory. |
| `.agents/docs/repo-skills-policy.md` | Routes marketplace sync users to the local-skill contract and distinguishes generated provenance from local custody. |
| `scripts/install_agent_skills.py` | Excludes the reserved local prefix from marketplace copy, hash, projection, and stale-prune paths. |
| `scripts/tests/test_install_agent_skills.py` | Regression coverage for the exact marketplace-removal migration. |
| `scripts/validate_repo_local_skills.py` | Deterministic validator for every `wild-bunch-*` directory. |
| `scripts/tests/test_validate_repo_local_skills.py` | Unit coverage for valid local skills and every contract breach class. |
| `scripts/requirements.txt` | Declares `PyYAML` for frontmatter parsing. |
| `.github/workflows/ci.yml` | Installs script requirements and runs the local-skill validator in the index-mesh job. |
| `scripts/ci-preflight.ps1` | Runs the local-skill validator in the local index-mesh lane. |
| `.agents/skills/wild-bunch-*/` | Four canonical local skills, their current references, and no marketplace wrapper metadata. |
| `.agents/skills/.provenance.json` | Generated record of marketplace skills only; refreshed through the installer. |
| `.agents/docs/skills-catalog.md` | Removes the manual `wild-bunch-*` inventory and points readers to prefix discovery. |
| `.agents/skills/wild-bunch-project-doctrine/references/*.md` | Removes marketplace paths, stale commit snapshots, and unavailable skill routes. |

## Task 0: Create the execution workspace from current main

**Files:**
- Create: `Z:\_agent-worktrees\wild-bunch\local-skill-custody` (linked worktree)

**Consumes:** The committed plan and the current `origin/main` tip.

**Produces:** An isolated `codex/wild-bunch-local-skill-custody` branch for all implementation commits and the eventual PR.

- [x] **Step 1: Verify and create the worktree.**

  Run:

  ```powershell
  git -C Z:\wild-bunch fetch origin
  git -C Z:\wild-bunch rev-parse origin/main
  git -C Z:\wild-bunch worktree add Z:\_agent-worktrees\wild-bunch\local-skill-custody -b codex/wild-bunch-local-skill-custody origin/main
  ```

  Expected: the new linked worktree is clean and its branch starts at the fetched `origin/main` SHA. Do not perform implementation in the main checkout or this plan-only branch.

---

## Task 1: Establish the local-skill authoring contract and routing

**Files:**
- Create: `.agents/docs/skill-authoring-policy.md`
- Modify: `.agents/docs/mesh-policy.md`
- Modify: `.agents/docs/repo-skills-policy.md`
- Modify: `.agents/skills/wild-bunch-project-doctrine/references/policy-references.md`
- Modify: `.agents/docs/skills-catalog.md`

**Consumes:** The Adventures policy model and the marketplace skill standards, adapted to Wild Bunch local custody.

**Produces:** A single canonical policy with the reserved namespace and discovery rule used by the installer and validator.

- [x] **Step 1: Write the contract’s acceptance examples before changing policy text.**

  Add these executable examples to the policy under `Authoring gate`:

  ```markdown
  - A candidate that merely repeats a project rule is reclassified as doctrine or a reference.
  - A `wild-bunch-*` skill with `source-path: sources/first_party/...` fails local validation.
  - A local skill that disappears from a marketplace plugin remains after a refresh.
  - Renaming or adding a `wild-bunch-*` directory requires no inventory edit; discovery follows the prefix.
  ```

- [x] **Step 2: Write `.agents/docs/skill-authoring-policy.md`.**

  Require: qualification before creation; `name` matching the directory; a `Use when...` trigger-only description; local `metadata.status`, `metadata.scope`, `metadata.use_when`, and `metadata.do_not_use_when`; a compact body with references for detailed facts; no marketplace `source-id`, `source-path`, `provenance-name`, `source-category`, or `owner` identity fields; and no `agents/openai.yaml` unless a future task explicitly prepares a marketplace projection.

  Define the custody rule exactly once in policy prose:

  ```markdown
  The `wild-bunch-` prefix is reserved for Wild Bunch repository-local skills.
  The canonical inventory is the matching directories under `.agents/skills/`.
  Do not maintain a parallel list of their names.
  ```

  Include the Adventures-style disposition record: source/revision, local use case, disposition, overlapping owner, stale claims, pressure scenario, and final local path or retirement reason.

- [x] **Step 3: Replace temporary custody language with permanent namespace custody.**

  In `mesh-policy.md`, replace section 7’s temporary/uncovered-skill rule and its `_None_` inventory with a rule that `wild-bunch-*` is permanently repo-local, is discovered from the directory prefix, is excluded from marketplace provenance, and is protected by both installer and validator. Keep the existing single-source rule for `.agents/plugins/marketplace.json` unchanged.

- [x] **Step 4: Route to the contract without adding root doctrine.**

  Add `.agents/docs/skill-authoring-policy.md` to the doctrine reference map and the repo-skills policy. Replace the four manual Wild Bunch rows in `skills-catalog.md` with one sentence directing agents to discover `.agents/skills/wild-bunch-*/SKILL.md`; retain the root `AGENTS.md` requirement for `/wild-bunch-project-doctrine` unchanged.

- [x] **Step 5: Review policy scope.**

  Confirm the contract does not prescribe marketplace authoring, plugin membership, a worker workflow, or a duplicate skill list. It must govern only local Wild Bunch skills and their interaction with the refresh boundary.

- [x] **Step 6: Commit.**

  ```powershell
  git add .agents/docs/skill-authoring-policy.md .agents/docs/mesh-policy.md .agents/docs/repo-skills-policy.md .agents/docs/skills-catalog.md .agents/skills/wild-bunch-project-doctrine/references/policy-references.md
  git commit -m "docs: define Wild Bunch local skill custody"
  ```

## Task 2: Protect the reserved namespace during marketplace refresh

**Files:**
- Modify: `scripts/install_agent_skills.py`
- Modify: `scripts/tests/test_install_agent_skills.py`

**Consumes:** The `wild-bunch-` reserved-prefix rule from Task 1 and the existing provenance schema.

**Produces:** Marketplace projection logic that never copies, hashes, validates, records, overwrites, or prunes reserved local skills.

- [x] **Step 1: Write a failing regression test for the real removal sequence.**

  Add `test_sync_cli_does_not_overwrite_reserved_local_skill_from_marketplace`. Its temporary fixture must include both a marketplace `wild-bunch-project-doctrine/SKILL.md` containing `# Marketplace doctrine` and a destination `wild-bunch-project-doctrine/SKILL.md` containing `# Local doctrine`. Run `main()` and assert the destination is unchanged and the name is absent from `syncedSkillNames`.

  Add `test_sync_cli_preserves_reserved_local_skill_after_marketplace_removal`. Its temporary fixture must contain:

  ```python
  source_skill = plugins_root / "marketplace-plugin" / "skills" / "vendored-skill"
  local_skill = skills_root / "wild-bunch-project-doctrine"
  local_skill.joinpath("SKILL.md").write_text("# Local doctrine")
  provenance = {
      "sha": "marketplace-sha",
      "syncedPlugins": ["marketplace-plugin"],
      "syncedSkillNames": ["vendored-skill", "wild-bunch-project-doctrine"],
      "syncedSkills": 2,
      "syncedSkillHashes": {
          "vendored-skill": "old-vendored-hash",
          "wild-bunch-project-doctrine": "old-marketplace-hash",
      },
  }
  ```

  Run `main()`, then assert the local `SKILL.md` remains `# Local doctrine`, `syncedSkillNames == ["vendored-skill"]`, and a following `--check` returns `0`.

- [x] **Step 2: Run the focused test to verify RED.**

  Run:

  ```powershell
  python -m pytest scripts/tests/test_install_agent_skills.py -k reserved_local_skill -q
  ```

  Expected: FAIL because the current installer overwrites a reserved name while it still exists upstream, then stale pruning removes it once the marketplace copy disappears.

- [x] **Step 3: Add one prefix predicate and apply it at every marketplace boundary.**

  In `scripts/install_agent_skills.py`, add:

  ```python
  REPO_LOCAL_SKILL_PREFIX = "wild-bunch-"

  def _is_repo_local_skill_name(skill_name: str) -> bool:
      return skill_name.startswith(REPO_LOCAL_SKILL_PREFIX)
  ```

  Use the predicate to exclude reserved names from `_expected_skill_names`, `_expected_skill_hashes`, `_projection_matches_source`, source-copy iteration/progress counts, and `stale_dirs`. Preserve normal behavior for every non-reserved skill. Do not add a list of individual Wild Bunch skill names.

- [x] **Step 4: Run focused GREEN and the installer suite.**

  Run:

  ```powershell
  python -m pytest scripts/tests/test_install_agent_skills.py -q
  ```

  Expected: PASS. The new test proves a formerly marketplace-recorded reserved skill survives both normal refresh and the next check.

- [x] **Step 5: Refresh generated provenance through the script.**

  Run:

  ```powershell
  python scripts/install_agent_skills.py
  python scripts/install_agent_skills.py --check
  ```

  Expected: the four `wild-bunch-*` names are absent from `.agents/skills/.provenance.json`; the second command reports current projections. Do not edit `.provenance.json` directly.

- [x] **Step 6: Commit.**

  ```powershell
  git add scripts/install_agent_skills.py scripts/tests/test_install_agent_skills.py .agents/skills/.provenance.json
  git commit -m "fix: preserve Wild Bunch local skills during refresh"
  ```

## Task 3: Enforce the local-skill contract in CI and preflight

**Files:**
- Create: `scripts/validate_repo_local_skills.py`
- Create: `scripts/tests/test_validate_repo_local_skills.py`
- Modify: `scripts/requirements.txt`
- Modify: `.github/workflows/ci.yml`
- Modify: `scripts/ci-preflight.ps1`

**Consumes:** The prefix and required/forbidden frontmatter rules from Task 1.

**Produces:** A deterministic Python validation lane that discovers the local skill roots and fails on contract drift.

- [x] **Step 1: Write failing unit tests for valid and invalid local skill roots.**

  Test `validate_repo_local_skills(skills_root: Path) -> list[str]` with temporary directories. Cover a valid `wild-bunch-valid/SKILL.md`, then assert failures for: a name/directory mismatch; a description not beginning `Use when`; missing each required local metadata field; marketplace provenance fields; `agents/openai.yaml`; malformed YAML; and a broken relative Markdown reference. Include a control `other-skill/` directory and assert it is ignored.

- [x] **Step 2: Run RED.**

  Run:

  ```powershell
  python -m pytest scripts/tests/test_validate_repo_local_skills.py -q
  ```

  Expected: collection failure until the validator module exists.

- [x] **Step 3: Implement the validator with parsed YAML, not string heuristics.**

  Add `PyYAML>=6.0` to `scripts/requirements.txt`. Implement:

  ```python
  REPO_LOCAL_SKILL_PREFIX = "wild-bunch-"
  REQUIRED_METADATA_KEYS = {"status", "scope", "use_when", "do_not_use_when"}
  FORBIDDEN_MARKETPLACE_METADATA_KEYS = {
      "source-id", "source-path", "provenance-name", "source-category", "owner",
  }

  def validate_repo_local_skills(skills_root: Path) -> list[str]:
      """Return stable contract errors for every reserved local skill root."""
  ```

  Discover only immediate directories beginning with the prefix and containing `SKILL.md`. Parse the first YAML frontmatter block with `yaml.safe_load`; verify the name, trigger description, metadata mapping, required keys, forbidden keys, body word limit (500), absence of `agents/openai.yaml`, and local links in Markdown. Return sorted errors; `main()` prints each error and exits `1`, otherwise prints the validated skill count and exits `0`.

- [x] **Step 4: Run GREEN and repository-local validation.**

  Run:

  ```powershell
  python -m pytest scripts/tests/test_validate_repo_local_skills.py -q
  python scripts/validate_repo_local_skills.py
  ```

  Expected: tests pass; the repository validator initially identifies every current marketplace-custody violation, which Task 4 resolves.

- [x] **Step 5: Wire the same deterministic check into both CI routes.**

  Add this CI step after Python setup and requirements installation in the `index-mesh` job:

  ```yaml
  - name: Install Python script requirements
    run: python -m pip install -r scripts/requirements.txt

  - name: Validate repo-local skills
    run: python scripts/validate_repo_local_skills.py
  ```

  In `scripts/ci-preflight.ps1`, invoke `python "$ScriptDir/validate_repo_local_skills.py"` in the existing index-mesh block and pass its result through `Assert-LastExitCode 'repo-local skill validation failed'`.

- [x] **Step 6: Commit.**

  ```powershell
  git add scripts/validate_repo_local_skills.py scripts/tests/test_validate_repo_local_skills.py scripts/requirements.txt .github/workflows/ci.yml scripts/ci-preflight.ps1
  git commit -m "test: enforce Wild Bunch local skill contract"
  ```

## Task 4: Rewrite and certify the four canonical local skills

**Files:**
- Modify: `.agents/skills/wild-bunch-browser-game/SKILL.md`
- Modify: `.agents/skills/wild-bunch-browser-game/references/browser-game-stack.md`
- Delete: `.agents/skills/wild-bunch-browser-game/agents/openai.yaml`
- Delete: `.agents/skills/wild-bunch-browser-game/assets/icon.svg`
- Modify: `.agents/skills/wild-bunch-domain-modeling/SKILL.md`
- Modify: `.agents/skills/wild-bunch-domain-modeling/references/domain-model.md`
- Delete: `.agents/skills/wild-bunch-domain-modeling/agents/openai.yaml`
- Delete: `.agents/skills/wild-bunch-domain-modeling/assets/icon.svg`
- Modify: `.agents/skills/wild-bunch-dotnet-architecture/SKILL.md`
- Modify: `.agents/skills/wild-bunch-dotnet-architecture/references/dotnet-architecture.md`
- Delete: `.agents/skills/wild-bunch-dotnet-architecture/agents/openai.yaml`
- Delete: `.agents/skills/wild-bunch-dotnet-architecture/assets/icon.svg`
- Modify: `.agents/skills/wild-bunch-project-doctrine/SKILL.md`
- Modify: `.agents/skills/wild-bunch-project-doctrine/references/difficulty-entropy-seeded-world-setup.md`
- Modify: `.agents/skills/wild-bunch-project-doctrine/references/policy-references.md`
- Modify: `.agents/skills/wild-bunch-project-doctrine/references/repo-posture.md`
- Modify: `.agents/skills/wild-bunch-project-doctrine/references/skill-routing.md`
- Modify: `.agents/skills/wild-bunch-project-doctrine/references/working-knowledge.md`
- Delete: `.agents/skills/wild-bunch-project-doctrine/agents/openai.yaml`
- Delete: `.agents/skills/wild-bunch-project-doctrine/assets/icon.svg`
- Modify: generated `INDEX.md` files from whole-mesh regeneration

**Consumes:** The contract validator from Task 3 and live source from `src/WildBunch.Domain`, `src/WildBunch.Persistence`, `src/WildBunch.Web/package.json`, `AGENTS.md`, and `.agents/docs/`.

**Produces:** Four concise, valid, repo-owned skills with current routes and reference facts.

- [x] **Step 1: Record pressure scenarios and run them against the existing skills.**

  Use four prompts: (1) a domain travel command change, (2) a persistence/event-snapshot change, (3) a Phaser HUD/playtest change, and (4) a repo-sensitive planning task. Record the existing ambiguity: marketplace custody claims, stale unavailable names (`cqrs-event-sourcing`, `ef-core`), and the old seed-identity assertion that makes difficulty/entropy part of the seed. Reclassify nothing: all four have active, distinct, recurring owned decisions.

- [x] **Step 2: Rewrite frontmatter and trigger descriptions.**

  For each skill, retain only local metadata required by the contract and use a description shaped like:

  ```yaml
  ---
  name: wild-bunch-domain-modeling
  description: Use when Wild Bunch work changes GameSession boundaries, gameplay invariants, player state, investigation truth, or trail-day travel rules.
  metadata:
    status: active
    scope: Wild Bunch gameplay domain decisions.
    use_when:
      - Use when a task changes live-play domain rules or aggregate ownership.
    do_not_use_when:
      - Do not use for generic C# structure without Wild Bunch gameplay rules.
  ---
  ```

  Apply the same concise, trigger-only pattern to browser delivery, .NET architecture, and project doctrine. Remove every marketplace provenance field and all plugin-wrapper files listed above.

- [x] **Step 3: Refresh factual content from live source.**

  Keep the current verified facts: React 18, Phaser 3, TypeScript, and Vite in the browser skill; `GameSession` as aggregate root; typed event streams plus component snapshot cache; and the existing `BountyLoop`, `JourneyLoop`, `InvestigationLoop`, `StoreLoop`, and `ActionContextTracker` ownership model.

  In the doctrine references, remove the `a65ca6c2` snapshot claims and unavailable route names. Route architecture discovery through currently installed `ddd`, `cqrs`, `event-sourcing`, `event-driven-architecture`, `clean-architecture`, `dotnet`, and the three `wild-bunch-*` specialists. Replace the marketplace `sources/first_party/...` placement target with the local repository path. Reconcile seeded-world wording with the current difficulty/entropy implementation before retaining it; do not leave the assertion that difficulty and entropy are part of seed identity if current source disproves it.

- [x] **Step 4: Run the local contract validator to verify GREEN.**

  Run:

  ```powershell
  python scripts/validate_repo_local_skills.py
  ```

  Expected: `OK: validated 4 repo-local Wild Bunch skill(s)` with no marketplace metadata, stale wrapper files, invalid references, or body-limit violations.

- [x] **Step 5: Regenerate navigation and run focused infrastructure validation.**

  Run:

  ```powershell
  python scripts/generate_index_mesh.py
  python scripts/generate_index_mesh.py --check
  python -m pytest scripts/tests/test_install_agent_skills.py scripts/tests/test_validate_repo_local_skills.py scripts/tests/test_validate_marketplace_plugin_sync.py -q
  python scripts/validate_marketplace_plugin_sync.py
  python scripts/install_agent_skills.py --check
  git diff --check
  ```

  Expected: mesh is current; the local contract and installer tests pass; marketplace provenance validates while excluding the four local skills; refresh check is clean; and no whitespace errors appear.

- [x] **Step 6: Run the full relevant preflight and post-removal handoff check.**

  Run:

  ```powershell
  .\scripts\ci-preflight.ps1
  ```

  After the marketplace worker’s removal is available, update only the submodule pointer, then run:

  ```powershell
  python scripts/install_agent_skills.py
  python scripts/install_agent_skills.py --check
  python scripts/validate_repo_local_skills.py
  ```

  Expected: all four local skills remain byte-for-byte local, none re-enter provenance, and the refresh converges without special-case name edits.

- [x] **Step 7: Commit.**

  ```powershell
  git add .agents/skills .agents/docs/skills-catalog.md
  git commit -m "docs: localize Wild Bunch project skills"
  ```

## Task 5: Review and publish the custody migration

**Files:**
- Modify: `.agents/superpowers/plans/2026-07-21-wild-bunch-local-skill-custody.md` (check boxes only, after verified delivery)

**Consumes:** Tasks 1-4 and the marketplace worker’s independent removal PR/commit.

**Produces:** A reviewable draft PR whose final head is validated against the compatible marketplace state.

- [x] **Step 1: Perform a fresh-eyes review focused on custody failure modes.**

  Check that no `wild-bunch-*` directory remains in `.provenance.json`; the installer has no list of individual local names; every local skill passes the validator; no deleted `agents/openai.yaml` or icon is referenced; and all direct references point to installed/current names.

- [ ] **Step 2: Verify exact-head checks.**

  Run the Task 4 validation set after the final review fix. Record the branch SHA, submodule SHA, local-skill count, and provenance count in the PR body; do not claim marketplace removal until its gitlink is actually updated here.

- [ ] **Step 3: Publish a draft PR.**

  ```powershell
  git push -u origin codex/wild-bunch-local-skill-custody
  gh pr create --draft --base main --title "Localize Wild Bunch project skills" --body "Makes wild-bunch-* repository-local skills, protects them from marketplace refresh, validates their authoring contract, and refreshes local doctrine. Marketplace retirement remains a separate change."
  ```

  Expected: draft PR points only at the local-custody contract, installer, validator, four skills, derived mesh/provenance, and any later explicit submodule-pointer update.

## Execution Confidence Assessment

- **Direct execution confidence:** 9/10. The affected installer/provenance paths, existing pytest style, CI job, and exact four skills are verified.
- **SDD confidence:** 8/10. Tasks 1 and 2 may run independently after sharing this plan; Tasks 3 and 4 are sequential because the validator must exist before it can certify the rewrites.
- **Gap closure:** Verified the current installer’s stale-prune path, provenance schema, existing Python test harness, current browser stack, current aggregate/event-snapshot posture, current doctrine routing drift, current mesh rules, and the Adventures local-skill policy model.
- **Open question:** The separate marketplace worker’s removal must be merged or otherwise supplied as an updated submodule commit before the final post-removal proof. This plan deliberately does not make that marketplace change.
