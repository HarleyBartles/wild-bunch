# AGENTS.md

## Project
- Wild Bunch is a C#/.NET Western adventure game in `HarleyBartles/wild-bunch`.
- Workers branch from current `main` and publish work through a PR.
- Root index: `INDEX.md`
- Docs index: `docs/INDEX.md`
- Required working knowledge for architecture-sensitive work: `.agents/INDEX.md`, `.agents/architecture-hygiene.md`, `.agents/unslop/backend-architecture.md`
- Repo-local plugin marketplace: `.agents/plugins/marketplace.json` (default-installs `repo-worker-pack`, `superpowers-plus`, `wild-bunch-project-pack`, `game-studio`, `dotnet-kit`, `architecture-pack`, `frontend-pack`; sourced from `HarleyBartles/agent-asset-marketplace`).
- Required working knowledge for web UI/play-surface work: `src/WildBunch.Web/AGENTS.md`, `src/WildBunch.Web/.agents/unslop/play-surface-ui.md`
- Required working knowledge for dev overlay work: `.agents/dev-overlay/DOCTRINE.md`, `.agents/unslop/dev-overlay.md`

## Repo Skills

- Marketplace plugin skills are vendored into `.agents/skills/` so any agent working in this repo can invoke and discover them. Devin CLI discovers repo skills only from local `.agents/skills/<name>/SKILL.md` directories; it has no Codex-marketplace plugin install path.
- Source: `HarleyBartles/agent-asset-marketplace` pinned as a git submodule at `.agents/plugins/marketplace-source/`. Provenance (submodule HEAD SHA) is recorded in `.agents/skills/.provenance.json`.
- Do not hand-edit vendored skill files. Edit upstream in `agent-asset-marketplace` and re-sync.
- To refresh vendored skills after upstream plugin updates, run exactly these three commands in order:
  ```powershell
  git submodule update --remote .agents/plugins/marketplace-source
  .\scripts\sync-skills.ps1
  python scripts\generate_index_mesh.py
  ```
  `scripts\sync-skills.ps1` is idempotent: it no-ops when the submodule HEAD matches `.agents/skills/.provenance.json` (pass `-Force` to re-copy regardless). Do not look for or invent other skill-sync paths.
- Governing policy: `.agents/docs/mesh-policy.md` sections 6-7.

## Mesh Policy

The repo uses three separate documentation/navigation surfaces with different jobs.

Detailed mesh contract: [`.agents/docs/mesh-policy.md`](.agents/docs/mesh-policy.md). The summary below is binding; the detailed doc is the companion.

### Agents mesh (`AGENTS.md` files)
- Answer: what is lawful here, what differs from upstream law, and what upstream law still applies.
- Scoped node mesh — not every folder needs an `AGENTS.md`. Add or update only at meaningful law-boundary nodes.
- No `AGENTS.md` should be siloed; scoped nodes must be understandable from root agent law and the upstream nodes between here and root.

### Index mesh (`INDEX.md` files)
- Answer: what is here, where can I go, and how do I get back to root?
- All-or-nothing if installed. Must cover the whole folder/file tree except explicit documented exclusions (generated/build/cache/local output, dependency folders, canonical skill-shaped folders where `SKILL.md` is the entrypoint).
- Navigation surfaces, not doctrine. Orient traversal without duplicating source architecture.

### README files
- Human-facing. Not a mesh. Do not put operative agent law only in README.
- If a README is stale or contains agent law, repair it or move the law into the agents mesh.

### Self-healing rule
If you read stale or misleading `AGENTS.md`, `INDEX.md`, or README content, repair the relevant mesh in the same PR or return AMBER with the exact deferred repair.

## Agent-Generated Outputs
- All agent-generated non-work outputs (plans, evidence, screenshots, doctrine notes, unslop profiles, session artifacts) must live under the `.agents/` subtree — never at repo root, under `docs/`, or in product source folders.
- Do not create loose files at repo root for agent use (no `COMMIT_MSG.txt`, `PR_BODY.md`, scratch notes, etc.). These are worker artifacts that pollute the tree and the generated index mesh.
- Superpowers plan records live under `.agents/superpowers/plans/`. Agent-facing superpowers material is consolidated under `.agents/superpowers/`, not at root `.superpowers/` or `docs/superpowers/`.
- Browser screenshots and other agent-generated evidence artifacts must be written under `.agents/superpowers/output/screenshots/` (or a coherent `.agents/superpowers/output/...` subfolder).
- Generated screenshot/image artifacts must NOT be committed to the repo. The `.agents/superpowers/output/screenshots/` folder is git-ignored via its local `.gitignore` (`*` with `!.gitignore` and `!INDEX.md` exceptions).
- PR/return notes may cite local evidence filenames/paths or attach screenshots through the review system if needed, but must not add them as repo files.
- If a worker finds screenshots or generated evidence committed elsewhere in the repo (e.g. under `docs/`), they should remove/move them to the git-ignored `.agents/superpowers/output/` area as part of self-healing.
- If a worker finds loose agent artifact files at repo root or in product folders, remove them as part of self-healing.

## Unslop Profiles
- Repo-wide unslop profiles live under `.agents/unslop/`.
- Project-local unslop profiles live under `{project}/.agents/unslop/`.
- Profile filenames are short lowercase kebab-case scope names. Do not include `unslop`, `profile`, or `unslop-profile` in the filename; the folder already says what it is.
- Human docs may point to these profiles, but profiles themselves are agent-facing review/filter material.
- Dev-overlay work should apply `.agents/unslop/dev-overlay.md` together with the backend and web unslop profiles where relevant.
- Unslop profiles are living documents. When a worker applies an unslop profile and slop still lands, the worker must postmortem whether the profile was effective. If the profile should have caught the drift but did not, the worker must strengthen the profile in the same PR when in scope, or return a precise deferred patch. "I read the unslop profile" is not enough; closeout must state what checks the profile forced and whether any gaps were found.
- When strengthening an unslop profile, the edit must name a reusable class of drift, not the one incident. Sharpen or replace existing guidance where possible instead of appending duplicates. Keep additions short enough to remain readable. Create a clear review failure condition (a test, a check, or a concrete reviewable assertion that would fail if the drift recurs). Do not turn profiles into a dumping ground for transient failures. Include a brief closeout note in the PR or return explaining why the profile change is durable — i.e. what class of future drift it now catches that it did not before.

## Branch + PR Workflow
- Workers branch from current `main`.
- Workers push a branch and open or return a PR.
- The PR is the normal publication surface.
- Direct pushes to `main` require explicit latest-turn authorization.
- `GREEN` means PR-ready with validation and evidence, not direct-main landing.
- Merge and landing verification are separate GPT or human steps after PR review and merge.

## Source of Truth
- Current repo state is the source of truth.
- Worker reports, issue comments, conversation summaries, and session notes are not proof.
- Always report exact branch, head commit, remote head, PR URL, and changed files.

## Connector / Tool Safety
- Read-only verification must stay read-only.
- Do not call GitHub mutation tools while inspecting repo state, reassessing an issue, or preparing a dispatch.
- Treat tools named `create_*`, `update_*`, `delete_*`, `add_*`, `remove_*`, `lock_*`, `unlock_*`, or low-level Git primitives such as `create_tree` / `create_commit` as mutation routes.
- `create_tree` is not a repo-listing tool. If a tree/listing read route is unavailable, use `fetch_file`, `fetch`, `search`, `compare_commits`, issue readers, and commit/status readers instead.
- Workers do not close GitHub issues; they only return source-backed closeout evidence and recommendations.

## Validation
- Run `dotnet build`.
- Run `dotnet test`.
- Run `dotnet tool restore` before EF validation commands when the repo-local tool manifest is used.
- Run `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api` when persistence may be affected, or as standing validation unless clearly irrelevant.
- Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent tests or validation to reuse the shared local service (idempotent: no-op when already healthy).
- Run `.\scripts\postgres-dev.ps1 validate` for the repo-local PostgreSQL-backed validation lane; it provisions the persistent cluster, exports the repo-local connection string for child `dotnet` commands, restores tools, and runs the EF and test checks together.
- For targeted PostgreSQL-backed tests, use `.\scripts\postgres-dev.ps1 test -- <dotnet test args>` so the script sets `ConnectionStrings__WildBunchPostgresDb` in the same process before invoking `dotnet test`; do not rely on a standalone `$env:` assignment in a separate command.
- Use `.\scripts\postgres-dev.ps1 status` to check whether the lane is already running, `setup` or `validate` to provision it, and `reset` for the destructive local app-database reset path. `stop` and `reset` are manual/destructive; do not stop the shared service during normal worker cleanup.
- If PostgreSQL port `5434` is closed or connection setup fails, report the exact command and output after running the repo-local setup/status lane instead of treating it as a product regression.
- Report warnings separately from failures.

### Index Mesh CI Failures

The "Index mesh + plugin manifest" CI job runs `python scripts/generate_index_mesh.py --check` on a clean Linux checkout. It fails when the committed INDEX.md files don't match what the generator produces from the CI tree. Common causes and fixes:

- **Stale INDEX.md after file rename/add/delete:** Regenerate with `python scripts/generate_index_mesh.py` and commit the updated INDEX.md files. The generator walks the live tree, so any renamed/added/deleted file or directory needs an index refresh.
- **`TestResults` directory (gitignored test output):** `TestResults/` is a gitignored directory created by `dotnet test` runs. It contains dynamic GUID-named subdirectories. The generator must exclude it (it is in `EXCLUDED_DIR_NAMES` in `scripts/generate_index_mesh.py`). If a new gitignored output directory appears, add it to `EXCLUDED_DIR_NAMES` and `EXCLUDED_ROOT_NAMES` in the generator script, then regenerate. Do NOT commit INDEX.md files inside gitignored output directories.
- **PowerShell pipe encoding corrupts `git cat-file` output:** When debugging blob contents on Windows, do NOT pipe `git cat-file -p` through PowerShell `|` or `>` — PowerShell converts stdout to UTF-16LE, adding a `\xff\xfe` BOM and wide characters that look like file corruption. Use `git cat-file -p <sha> | python -c "import sys; ..."` with `sys.stdin.buffer.read()` to inspect raw bytes, or write to a file with `git cat-file -p <sha> -o <file>`.
- **`core.autocrlf=true` on Windows:** The repo uses `autocrlf=true` on Windows. Git stores INDEX.md blobs as LF (the generator writes with `newline="\n"`), and autocrlf normalizes on checkout. This is fine — the generator's `normalize_text` strips CRLF before comparing. The CI check is not a line-ending issue; it is a content/tree-structure mismatch.

## Testing Posture
- New or updated real application behavior should normally include test coverage in the same slice.
- If coverage is skipped, state the reason explicitly and keep the gap narrow and deliberate.
- Debug-only or temporary prototype surfaces, including the current cockpit/debug shell, may use lighter-weight coverage while they remain debug-only.

## GREEN Standard
- `GREEN` requires implementation, validation, a clean worktree, branch head proof, PR publication, issue-goal conformance, and complete worker-owned cleanup proof when validation touched local workspace resources.
- Passing tests alone is not `GREEN`.
- A commit existing is not `GREEN`.
- If the worker started long-running helpers for validation or browser checks, `GREEN` also requires stopping or explicitly accounting for those worker-owned processes and browser sessions before return.
- If validation touched `C:/WORK/**`, `GREEN` requires a post-cleanup proof block that accounts for worker-owned helpers, used ports, and repo/file-lock risk before the return. Missing or partial cleanup proof is `AMBER` or `BLOCKED`, not `GREEN`.

## Issue-Goal Conformance
- Restate the task as observable repo state.
- Run falsification checks.
- Compare claim vs observed state.
- Do not close or claim closure unless explicitly asked and fully proven.

## Architecture Guardrails
- `GameSession` is the live-play aggregate root.
- Game mutations should flow through `GameSession` or the established aggregate route.
- Travel, journey, and encounter DTO mapping should live in `TravelMapper`; `GameSessionMapper` should delegate rather than duplicate that shape.
- Wallet and Inventory are concrete player state; avoid generic supplies.
- Hidden culprit truth remains internal.
- The culprit is always a gang member. Any gang member can be the culprit. Which one is encoded in the UUID seed. Do not mark gang members as culprit-ineligible unless they are associated characters who are not part of the gang.
- Clue, journal, and wanted-poster flows stay stable unless directly in scope.
- Horse and saddle are separate inventory concepts.
- Mounted travel requires a living/non-lame horse plus saddle.
- Travel advances one trail day at a time; do not reintroduce instant multi-day travel.
- Keep temporary cockpit/debug-shell UI light; do not spend architecture cleanup effort polishing it for its own sake.

## Persistence / Model Posture
- POCO domain models are fine when they keep the domain plain, composable, and naturally serializable.
- Do not couple domain models to EF/table shape.
- Runtime session persistence is JSON snapshot-oriented today.
- Snapshot codecs belong in `WildBunch.Persistence` and should be split by coherent domain area when they get unwieldy.
- Do not normalize runtime session state into many DB tables unless explicitly directed.
- Persistence adapters may map the domain to JSON now and tables later without forcing domain refactors.
- In this greenfield repo, current mainline model correctness wins over old-save or legacy internal compatibility.
- Dev database drop/recreate is allowed when a current snapshot or schema shape changes and a reset is the cleanest path.
- Do not add compatibility shims for obsolete old saves or internal models unless Harley explicitly asks for one.
- Serializer optionality should exist only for current-domain reasons, not as a default legacy-save support layer.
- When a task calls for replacement, fully replace the old internal model instead of layering a compatibility adapter over it.
- Repo-local database artifacts should live under repo-root `.local/`, never under `src/`.

## Scope Discipline
- Do only the requested slice.
- No opportunistic broad refactors.
- No unrelated feature work.
- If a needed design decision is missing, return `BLOCKED` or `AMBER` rather than inventing broad architecture.

## UUID Seed Codec
- The game-start UUID encodes the seed-owned world/map layer: world variant, selected town IDs, trail graph (with baseline terrain/water/distance), accusation/default culprit candidates, and seed-derived cash bonus.
- `SeedWorldResolver.Resolve(Guid)` decodes UUID → `SeedWorld`. `SeedWorldResolver.CreateRepresentativeSeedCode(SeedWorld)` encodes `SeedWorld` → UUID via round-trip search.
- The seed does NOT encode difficulty, entropy, loadout, horse/saddle, final starting town, or final cash — those are pressure-owned (`DifficultyEnvelope`), entropy-owned (`EntropyPolicy` + `MysteryTruthResolver`), or player/setup-owned (`StartingTownPolicy`).
- The starting town is NOT a seed-owned fact. The player can start in any town that exists in the generated world. `StartingTownPolicy` validates the choice and provides a safe default (pinecross). Future seam: difficulty may constrain eligibility.
- The seed deterministically derives the world map from the full town catalog: town count (6-8), which towns are selected (anchor towns pinecross/redmesa/holloway always included, rest seed-selected), and the trail graph (catalog trails where both endpoints are selected, with terrain/water/distance from the catalog indexed by world variant). This is NOT a pair of canned named sets — it is true seed-derived town selection.
- `SeedWorld` holds `SelectedTownIds` and `Trails` (list of `SeedWorldTrail` with terrain/water/distance). The seed owns the default terrain and trail distances. Later difficulty can modify those values downstream of the seed codec.
- Design boundary: SeedWorld owns the candidate/generated map. Same seed + same difficulty should produce the same resolved map. Difficulty may later influence map pressure/layout realization (distance bands, terrain harshness, connectivity constraints) downstream of the seed codec, not by hiding difficulty inside the seed. Longer term, `SeedWorld + DifficultyEnvelope` may produce the final resolved world/map, while `StartingTownPolicy` validates the player's start choice against that world.
- Both directions must stay in sync. When you add a new seed-owned field:
  1. Add the field to `SeedWorld` and the codec in `SeedWorldResolver.Resolve`.
  2. Add the field to the seed world signature in `StartingWorldDescriptorSeedMixer.CreateSeedWorldSignature` so `CreateRepresentativeSeedCode` can round-trip it.
  3. Update `SeedWorldCatalog` if the field is a new town or trail.
  4. Update `SeedWorldBuilderTests` snapshot assertions to include the new town/trail/field.
  5. Update `SeededNewGameFactoryTests` count assertions if town/trail counts changed.
  6. Run the round-trip guardrail test to verify the codec still resolves both ways.
- Do NOT store UUIDs in test fixtures or libraries. Store `SeedWorld` records and derive UUIDs on the fly via `CreateRepresentativeSeedCode`. Stored UUIDs go stale when the codec evolves; `SeedWorld` records are compile-time checked.
- Do NOT create test sessions by bypassing the seed system with hand-built worlds unless the test is specifically about resource mechanics (canteen math, horse exhaustion). For encounter, trail-event, and journey tests, go through the seed system. Deterministic foe-encounter seed profiles for travel tests are tracked in BUNCH-87.
- The UUID has 128 bits of bandwidth. As fields are added, fewer UUIDs map to each seed world shape — this is expected and fine. `CreateRepresentativeSeedCode` searches until it finds a match.

## Modular Excitement Doctrine
- Modular player excitement is achieved through boring implementation.
- Build player-facing surprise, variety, and authorship from composable, validated primitives rather than from bespoke adventure chaos.

## Coding Discipline
- Keep slices small and mainline-friendly; if a file is getting bulky, extract the pure helper, factory, or renderer before it becomes a god object.
- Avoid letting aggregate roots, endpoint files, React panels, and builders accumulate unrelated responsibilities.
- Do not move gameplay mutation out of `GameSession` just to satisfy SOLID; extract pure helpers around it instead.
- Prefer one canonical algorithm or formatter over duplicate versions that can drift.
- When you touch a surface, leave it cleaner or explicitly report why the cleanup is deferred.
- Backend remains authoritative for gameplay state; React renders server state instead of inventing it.
- For deterministic seed, world, or travel behavior, prefer characterization tests before refactoring.
- Current cockpit/debug shell UI sections are temporary scaffolding; do not over-refactor them for their own sake while they remain temporary.
- Real replacement UI/screens should follow the decomposition rules from the cleanup track: focused hooks, small components, backend-authoritative mutation paths, clear command/state boundaries, and reducers only when coupled command-legality state truly warrants them.

## ADR Log Freshness
- The ADR log at `docs/adr/` is the durable record of architecture and gameplay decisions. It must represent the system as it exists today, not as it existed when each ADR was written.
- **If you read the ADR log, you check the whole log for freshness.** Reading any ADR creates a responsibility to verify that the rest of the log is not stale against current source. If you find a stale ADR, update it or mark it `superseded` and create its replacement in the same pass.
- `docs/adr/INDEX.md` carries a per-ADR "Last checked" freshness table. When you complete a freshness check, update the timestamp for each ADR you verified so the next worker can infer which files are likely fresh and which may need re-checking. A file with a stale timestamp (weeks old, or older than the last merge to `main`) should be re-read before trusting it.
- Staleness means: the ADR describes behavior, identifiers, fields, or mechanics that no longer match current source. Historical status entries (dated `live` entries that record what happened at a point in time) are not stale — they are the audit trail. The current `Status` line and `Decision` section must match the system today.
- When you change behavior that an ADR documents, update that ADR in the same PR. Do not leave the ADR log behind the code.

## Worker Environment
- The worker environment uses PowerShell, so do not use `&&` for command chaining.
- Run commands separately or use PowerShell-safe sequencing when multiple commands are needed.
- The local PostgreSQL dev service (`localhost:5434`) is a shared, long-lived developer service owned by the persistent main checkout. Do not stop it during normal worker cleanup. `.\scripts\postgres-dev.ps1 stop` and `reset` are manual/destructive and only for explicit service lifecycle ownership or when Harley asks. Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent tests; it reuses a healthy service and only starts one when down.
- Background shell circuit breaker — do not poll a "still running" shell indefinitely. If a backgrounded command that should have completed (setup, ensure, build, test) reports "still running" with no new output for several consecutive `get_output` polls, stop waiting and verify the expected outcome directly. For service-provisioning scripts (`postgres-dev.ps1 ensure`, `dev-servers.ps1 ensure`, etc.), run `status` in a separate foreground shell to check whether the service is already up and healthy. If it is, the background shell is hung (a known Windows handle-inheritance class of bug where a spawned daemon holds the parent pipe open) — kill it and proceed. Never let a single hung shell block an entire session; the expected end state is observable through `status` or other read-only checks, independent of the shell that started the service.
- Dev servers (API and Vite) are **worktree-owned**, not shared like the PostgreSQL service. Before starting API or Vite, check whether a healthy server is already running for the current worktree. If it is, reuse it and leave it up. If the canonical ports (`5275` for API, `5173` for Vite) are occupied by a **different worktree's** server, do not kill it — allocate a non-conflicting port pair and report the actual URLs. Browser proof must exercise the code in the worker's current worktree; a server from a different worktree is not proof for this branch. Use `.\scripts\dev-servers.ps1 ensure` to automate this (it records PIDs, ports, URLs, worktree path, and branch in a worktree-local state file). See `.agents/ui-browser-check-playbook.md` for the binding topology, port-conflict resolution, worktree-identification procedure, and evidence-invalidity rules.
- When you start worker-owned API servers, Vite dev servers, test servers, browsers, watch processes, or other long-running helpers, record what you started and clean them up before returning `GREEN` unless you explicitly return `AMBER` or `BLOCKED` with exact process/port evidence.
- When validation touches `C:/WORK/**`, verify cleanup from the workspace perspective before returning `GREEN`: account for likely worker-owned server, browser, watcher, and test-helper processes; include process id, process name, and command line for anything stopped or left running; check every port used during validation, including alternate Vite preview/dev ports; and state repo/file-lock posture. If handle tooling is unavailable, say so and provide the process/command-line fallback proof.
- A later user finding a worker-owned helper from the validation run after `GREEN` falsifies the cleanup lane, even if the product slice itself was correct.

## Return Format
- Status: `GREEN` | `AMBER` | `RED` | `BLOCKED`
- Branch
- Head commit hash
- Remote branch head hash
- PR URL
- Changed files
- Validation commands and results
- Clean worktree status
- Cleanup proof when validation touched `C:/WORK/**` or started worker-owned helpers: started helpers, stopped helpers, post-cleanup process scan, post-cleanup port scan, repo/file-lock posture, remaining known worker-owned processes
- Issue-goal conformance notes
- Unslop profile evidence: which profiles were applied, what checks they forced, and whether any gaps were found (strengthen the profile in-PR or return a precise deferred patch when slop landed despite the profile)
- Known caveats or next recommended slice
- Landing verification if and when the PR is merged to `main`
