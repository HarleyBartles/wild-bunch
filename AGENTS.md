# AGENTS.md

## Project
- Wild Bunch is a C#/.NET Western adventure game in `HarleyBartles/wild-bunch`.
- Workers branch from current `main` and publish work through a PR.
- Docs index: `docs/INDEX.md`
- Required working knowledge for architecture-sensitive work: `.agents/INDEX.md`, `.agents/architecture-hygiene.md`, `docs/unslop/backend-architecture.md`
- Repo-local skills index: `.agents/skills/INDEX.md`
- Required working knowledge for web UI/play-surface work: `src/WildBunch.Web/AGENTS.md`

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
- The game-start UUID is the single encoding of all starting world state: towns, trails, world variant, difficulty, entropy, loadout, cash, culprit identity, and later additions (gang members, warrants, etc.).
- `StartingWorldDescriptorResolver.Resolve(Guid)` decodes UUID → world descriptor. `StartingWorldDescriptorResolver.CreateRepresentativeSeedCode(descriptor)` encodes world descriptor → UUID via round-trip search.
- Both directions must stay in sync. When you add a new field to the starting world state (new town, new trail, new loadout option, new difficulty, new entropy level, new world variant, new case-file parameter, anything that changes what a player starts with):
  1. Add the field to `StartingWorldDescriptor` and the codec in `GameSetupSeedCodec.cs`.
  2. Add the field to the descriptor signature in `StartingWorldDescriptorSeedMixer.CreateDescriptorSignature` so `CreateRepresentativeSeedCode` can round-trip it.
  3. Update `SeedWorldCatalog` if the field is a new town or trail.
  4. Update `SeedWorldBuilderTests` snapshot assertions to include the new town/trail/field.
  5. Update `SeededNewGameFactoryTests` count assertions if town/trail counts changed.
  6. Run the round-trip guardrail test to verify the codec still resolves both ways.
- Do NOT store UUIDs in test fixtures or libraries. Store descriptors and derive UUIDs on the fly via `CreateRepresentativeSeedCode`. Stored UUIDs go stale when the codec evolves; descriptors are compile-time checked.
- Do NOT create test sessions by bypassing the seed system with hand-built worlds unless the test is specifically about resource mechanics (canteen math, horse exhaustion). For encounter, trail-event, and journey tests, go through the seed system. Deterministic foe-encounter seed profiles for travel tests are tracked in BUNCH-87.
- The UUID has 128 bits of bandwidth. As fields are added, fewer UUIDs map to each descriptor shape — this is expected and fine. `CreateRepresentativeSeedCode` searches until it finds a match.

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
- Known caveats or next recommended slice
- Landing verification if and when the PR is merged to `main`
