# AP-12 Setup Projections and AP-13 Public Poster Knowledge Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline.

**Goal:** Make the stable-baseline contract and investigation records reflect truthful unknown setup state and the public knowledge a wanted poster may disclose, with a behavioral guard against deriving public gang affiliation from hidden warrant fields.

**Architecture:** Setup sessions have no `GameStarted` fact, so player identity/name known from setup may remain while location, health, inventory and HUD remain absent; the HUD endpoint returns no content until the start event exists. A wanted poster may identify a target as gang-affiliated only when its public `GangAffiliations` terms establish that fact. Internal target kind and gang-pressure routing remain private. Current source already implements AP-12 and this AP-13 rule; the slice records that truth, adds the missing negative mapper behavior proof, and preserves the historical audit findings with dated dispositions.

**Tech Stack:** C#, xUnit, ASP.NET Core integration tests, Markdown specification, ADR log and investigation records.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, PG-005 Knowledge; feature matrix `docs/features.md`, PG-005; AP-12/AP-13 in `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md` and `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`.

**Execution Strategy:** Execute inline. The slice has one focused negative behavior test whose public-knowledge contract must be recorded with it, plus dated audit corrections for an already implemented setup boundary; splitting these dependent edits across implementers would add handoffs without independent proof.

## Global Constraints

- Preserve the setup-to-prologue-to-starting-town flow and all current event, persistence and response shapes.
- Keep setup-known player name while location, health, inventory and HUD remain unknown before `GameStarted`; retain the existing `204 No Content` HUD route behavior.
- A poster's public affiliation is known only when its public warrant terms include that affiliation. Do not infer player knowledge from `TargetKind`, `AdvancesGangPressureFor`, culprit identity or other internal truth.
- Preserve the existing positive public poster/API behavior and hidden-field exclusion checks.
- Do not add source-shape or DTO-field absence tests, modify behavior that already satisfies AP-12, or create a new compatibility path.
- ADR-0007 remains live. Add a dated editorial clarification because the record's general player-knowledge boundary needs the now-settled poster-affiliation example; do not change its original decision or date.
- No new feature is added. Update PG-005 wording only to make its existing truthful-poster contract precise.
- Advance `Directory.Build.props` exactly once to `0.1.0-dev.106` for this PR.
- In the first substantive commit, record PR #290 source `02fd3e6669f6931ca56459a6c604e65b2b0dd875`, merge `928370538d50b1b1cb913d2b4e82ffa190571b7c`, successful exact-head hosted gate run `38212971942`, review approval and paired worktree/local/upstream branch cleanup; retire the `.105` plan and route this `.106` successor from the roadmap.
- Deliver by PR to `develop`; require fresh whole-branch review and exact-head hosted CI success before merge. After merge, fast-forward the primary checkout, remove this clean worktree, delete its matching local and upstream branch refs, and verify all three are absent before another plan.

## Review Focus

- The existing setup endpoint scenario proves unknown fields stay unknown before start and become established after the `GameStarted` event is committed.
- The negative poster test distinguishes public warrant affiliation from hidden gang membership/progression fields, while the existing positive API scenario protects explicit public affiliation.
- ADR-0007's dated note clarifies which public fact may be disclosed without weakening hidden culprit or progress boundaries.
- AP-12 is recorded as already corrected by the current implementation, not described as a new change in this PR.
- No source behavior or feature assessment is claimed beyond the focused mapper boundary and existing setup/API scenarios.

---

### Task 1: Commit the JIT plan before implementation

**Files:**
- Add: `.agents/plans/2026-10-12-row16-ap12-ap13-projection-truth.md`

- [ ] Commit this plan by itself before changing tests, specification, ADR, audit records, roadmap, or version.

### Task 2: Prove public poster affiliation does not come from hidden fields

**Files:**
- Modify: `tests/WildBunch.Application.Tests/Mappers/WantedPosterMapperTests.cs`

- [x] Add a warrant with `GangAffiliations` empty but `TargetKind` set to a gang member/culprit and `AdvancesGangPressureFor` set; assert the public classification is `wanted criminal`.
- [x] Make the existing positive mapper fixture's summary neutral so its gang classification is supported by the explicit public affiliation term rather than repeating gang membership in prose.
- [x] Temporarily change the mapper to derive the classification from an internal target/progression field, run the focused test, and verify it fails specifically on the public classification; restore the source before continuing.
- [x] Run the focused test and retain the existing positive gang-affiliation assertion; do not duplicate the positive API test or assert source structure.

### Task 3: Record the truthful setup and public-knowledge contracts

**Files:**
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `docs/features.md`
- Modify: `docs/decisions/ADR-0007-hidden-culprit-truth-and-hidden-progress-boundaries.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`

- [x] State in PG-005 that an explicit public warrant affiliation may identify a wanted person as gang-affiliated; internal target kind, culprit identity and hidden pressure routing are not public knowledge.
- [x] Add a dated editorial clarification to ADR-0007 preserving its original decision and establishing that only affiliation stated by public warrant terms may appear on the player poster.
- [x] Add dated AP-12 dispositions that cite the current nullable setup DTO, pre-start HUD `204`, the real setup-to-start API test and the prior `.10` setup-projection correction; preserve the original audit-time finding.
- [x] Add dated AP-13 dispositions recording the settled public-affiliation rule, existing positive API behavior and the new negative mapper proof; preserve that hidden target fields remain private.
- [x] Do not add a runtime test for AP-12 because the existing PostgreSQL HTTP scenario already exercises the relevant setup and post-start behavior.

### Task 4: Retire `.105` and route the `.106` successor

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-ap11-diagnostic-metadata.md`

- [x] Record PR #290 source, merge, review, exact-head hosted run and verified paired worktree/branch cleanup; link this `.106` plan as the executing row-16 slice.
- [x] Advance the product version from `.105` to `.106` exactly once and retire the `.105` plan in this successor.
- [x] Review `git diff --check`, run the focused mapper test, and pass the normal check-only commit hook's complete canonical gate before whole-branch review and publication.

**Validation:** The current `develop` tree matches PR #290's reviewed tree. Baseline focused command `py -3 tools/run.py dotnet-test --check -- --filter 'FullyQualifiedName~ProjectionEndpointTests|FullyQualifiedName~WantedPosterMapperTests'` passes on the worktree baseline. After implementation, rerun the focused command; verify the new mapper test's red/green result; then use the staged-snapshot commit hook, fresh whole-branch review and exact-head hosted canonical CI.
