# Design: Skill-based preflight and repo-resident script cleanup

## Goal

Replace the generic repo-resident scripts that duplicate bundled marketplace
skills with the skill-based mechanics, and move Wild Bunch-specific concerns
into the extension points the skills now provide.

## Background

The `agent-asset-marketplace` plugin packs now ship:

- `refreshing-installed-skills` with `github`/`local` plugin source support,
  correct `marketplace-source` HEAD provenance, and a
  `scripts/validate_local_skills_extra` hook for repo-local skill validation.
- `generating-agent-mesh` with a `scripts/generate_index_mesh_extra` hook for
  repo-specific `INDEX.md` post-processing.
- `repo-standards` with a `ci-preflight` template that calls
  `scripts/ci-preflight-extra` for repo-specific preflight checks.

This lets Wild Bunch drop its local duplicates of skill mechanics and keep
only repo-specific extension scripts and operational helpers.

## Scope

1. Replace `scripts/ci-preflight.sh` and `scripts/ci-preflight.ps1` with the
   `repo-standards` bundled template.
2. Add `scripts/ci-preflight-extra.sh` and `scripts/ci-preflight-extra.ps1`
   that run the Wild Bunch backend/frontend build and test lanes.
3. Add `scripts/generate_index_mesh_extra.py` and `.sh`/`.ps1` wrappers to
   append the ADR freshness table to `docs/adr/INDEX.md`.
4. Add `scripts/validate_local_skills_extra.py` and `.sh`/`.ps1` wrappers to
   validate the `wild-bunch-*` repo-local skills.
5. Remove the now-redundant generic scripts:
   - `scripts/install_agent_skills.py`, `.ps1`, `.sh`
   - `scripts/validate_marketplace_plugin_sync.py`
   - `scripts/generate_index_mesh.py`, `.ps1`, `.sh`
   - `scripts/validate_repo_local_skills.py`
6. Update `scripts/README.md` and `scripts/AGENTS.md` to reflect the new
   script catalog.
7. Update `.agents/docs/mesh-policy.md` and `.agents/docs/validation-policy.md`
   to reference the bundled skills instead of the removed repo scripts.
8. Regenerate the `INDEX.md` mesh with the bundled `generating-agent-mesh` skill
   and the new extra hook.
9. Commit the rolled `marketplace-source` submodule pointer and the refreshed
   `.agents/skills/` tree.

## Non-goals

- No C# gameplay or domain changes.
- No changes to `.agents/plugins/marketplace.json` plugin inventory.
- No removal of operational helpers (`scripts/dev-servers.*`,
  `scripts/postgres-dev.*`, `scripts/image_asset_pipeline.*`).

## Contracts

### `scripts/generate_index_mesh_extra.sh` / `.ps1`

Invocation:

```text
scripts/generate_index_mesh_extra.sh [--check] <repo-root>
```

- In write mode, post-process `docs/adr/INDEX.md` and append a freshness table
  for every `ADR-*.md` file in the directory. The table uses the same parsing
  rules as the old `scripts/generate_index_mesh.py`:
  - `## Status` heading gives the status column.
  - `## Dated Status History` section gives the most recent `YYYY-MM-DD`
    date for the "Last checked" column.
- In `--check` mode, verify the table is present and current. Exit non-zero
  with an error line if stale or missing.
- Print **nothing** on success. The bundled `generating-agent-mesh` hook
  treats any stdout/stderr as an error.

### `scripts/validate_local_skills_extra.sh` / `.ps1`

Invocation:

```text
scripts/validate_local_skills_extra.sh [--check] <skills-root> <prefix> ...
```

- For each directory under `<skills-root>` whose name starts with one of the
  supplied prefixes, enforce the same rules as the old
  `scripts/validate_repo_local_skills.py`:
  - Directory name is lowercase-kebab.
  - `SKILL.md` exists with YAML frontmatter.
  - Frontmatter `name` matches the directory name.
  - `description` begins with "Use when".
  - `metadata` contains `status`, `scope`, `use_when`, `do_not_use_when`
    and does not contain marketplace-only keys (`source-id`, `source-path`,
    `provenance-name`, `source-category`, `owner`).
  - `agents/openai.yaml` is not present.
  - Body is 500 words or fewer.
  - Relative Markdown references resolve within the skill directory.
- `--check` is read-only; the script does not mutate.
- Print nothing on success; print one error line per failure on exit.

### `scripts/ci-preflight-extra.sh` / `.ps1`

- Receives `--check` / `-Check` and optional `--changed-from` / `-ChangedFrom`.
- If `--check` is set, skip heavy backend/frontend builds and only run checks
  that are safe in the pre-commit hook.
- In full mode run:
  - `dotnet restore WildBunch.sln`
  - `dotnet build WildBunch.sln --configuration Release`
  - `dotnet tool restore`
  - `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`
  - `dotnet test WildBunch.sln --configuration Release`
  - `npm ci`, `npm run typecheck`, `npm run test`, `npm run build` in
    `src/WildBunch.Web`

## Validation

- `bash scripts/ci-preflight.sh --check` passes.
- `powershell -NoProfile -File scripts/ci-preflight.ps1 -Check` passes.
- `py -3 .agents/skills/repo-standards/scripts/repo_standards.py --check` passes.
- `py -3 .agents/skills/generating-agent-mesh/scripts/validate_agent_mesh.py --check` passes.
- `py -3 .agents/skills/repo-standards/scripts/scaffold_guides.py --check` passes.
- `dotnet build WildBunch.sln --configuration Release` succeeds.

## Risks and notes

- The bundled `generating-agent-mesh` extra hook returns all captured
  stdout/stderr as errors, even when the hook exits 0. The extra scripts must
  be silent on success and only emit lines on failure.
- `scripts/generate_index_mesh.py` had an `ALWAYS_EXCLUDED_DIR_NAMES` fallback
  for directories that `.gitignore` might miss. The bundled skill relies on
  `.gitignore` alone. Wild Bunch's `.gitignore` must be correct; if a new
  untracked output directory appears, it should be added to `.gitignore`, not
  to the skill.
- The `marketplace-source` submodule is pinned at the new upstream main commit
  that contains the extension hooks. `refreshing-installed-skills` records that
  commit in `.agents/skills/.provenance.json`.
