# Persistence Read-Path Claims Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline.

**Goal:** Correct stale comments about Persistence payload loading and upcaster completeness, and record what the current source does without changing runtime behavior.

**Architecture:** `PayloadUpcasterRegistry` registers real event upcasters, and its completeness assertion runs as a test rather than during compilation. `PersistedPayloadLoader` centralizes event upcasting/deserialization and version-aware component/diary payload recovery, but component reads return JSON for downstream codecs and do not all return domain objects. API startup continues to own the current synchronous migration call; release privilege, failure and rollback posture remain assigned to row 18.

**Tech Stack:** C#, .NET, xUnit, Markdown investigation and roadmap.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; row 16 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; PS-17 in `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md`.

**Execution Strategy:** `executing-plans` inline. The three stale claims share one narrow Persistence read-path boundary, their corrections are documentation-only, and a fresh implementer/reviewer handoff per comment would add overhead without an independent behavior proof.

## Global Constraints

- Do not change payload formats, upcaster registration, replay, component recovery, API composition or migration execution.
- Keep `ApplyWildBunchMigrations` as the current API-startup owner; deployment privilege, failure and rollback policy remain for row 18.
- Do not add tests that assert comment wording or class/file structure; compile and run the required repository gate after comment-only edits.
- Keep the original PS-17 audit finding as historical evidence and add a dated disposition that distinguishes resolved comment claims from the open release-policy question.
- Advance `Directory.Build.props` exactly once to `0.1.0-dev.107` for this PR; retire `.106` planning artifacts only in this successor PR.
- Record PR #291 source `a83251ee15e6a968a8e7e1697f5bdaec23b330e4`, merge `fa31b373893916a8848665d1d462ac59db31ebe3`, successful exact-head gate `38216421352`, successful develop push gate `38217005434`, clean review, and verified worktree/local/upstream ref cleanup in the roadmap.
- Deliver by reviewed PR to `develop`; verify exact head and hosted gate before merge. After merge, fast-forward the primary checkout, remove this worktree, delete its paired local/upstream branch refs and verify all three absences before the next plan.

## Review Focus

Re-read each changed comment beside its actual registration, load, serialization or startup call path. Confirm the dated PS-17 note does not turn the deferred row-18 migration policy into a decision or imply that API startup migration behavior changed.

---

### Task 1: Commit the JIT plan before implementation

**Files:**
- Add: `.agents/plans/2026-10-12-row16-persistence-truth.md`

- [x] Commit this plan by itself before changing Persistence comments, investigation records, roadmap or version.

### Task 2: Correct Persistence read-path descriptions

**Files:**
- Modify: `src/WildBunch.Persistence/DependencyInjection.cs`
- Modify: `src/WildBunch.Persistence/Versioning/PersistedPayloadLoader.cs`

- [x] Describe upcaster completeness as a test-suite assertion, not build-time enforcement; the runtime registration remains the list returned by `CreateDefaultUpcasters`.
- [x] Describe the loader by its actual responsibilities: event upcasting/deserialization and version-aware component/diary payload reads, with stale components reconstructed through the shared event replay callback.
- [x] Remove the inaccurate claim that the loader is the one funnel turning every persisted row into a domain object or the only serializer call site. Production repository and read-store loaders also use the serializer inside the Persistence adapter; retain only that supported production boundary.
- [x] Do not change `UpcasterCorrectnessTests`' test-only example: its comment describes a synthetic transition that demonstrates the pattern without changing production event history.
- [x] Do not change `ApplyWildBunchMigrations`; it still runs synchronously through the API startup composition.

### Task 3: Record the bounded PS-17 disposition

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md`

- [x] Add a dated partial disposition stating that the stale “Plan C” rebuilder comment was already removed in the row-07 disposition, current source has no claim that the first production upcaster is future work, and the upcaster example test remains accurate.
- [x] Record the corrected completeness/loader descriptions and that no runtime behavior or payload contract changed.
- [x] State that `ApplyWildBunchMigrations` remains the API-startup migration owner; its production privilege, failure and rollback policy is unresolved here and stays with row 18. Preserve the original finding and timing uncertainty.
- [x] Do not create or revise an ADR: no durable architecture choice changes in this slice.

### Task 4: Retire `.106` and route the `.107` successor

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-ap12-ap13-projection-truth.md`

- [x] Record PR #291's reviewed source, merge, exact-head gate, develop push gate and verified paired cleanup; route this `.107` plan as the executing row-16 slice.
- [x] Advance the product version from `.106` to `.107` exactly once and retire the `.106` plan in this successor.
- [x] Re-read each changed comment against current code, run `git diff --check`, then pass the normal check-only commit hook's complete canonical gate before whole-branch review and publication.

**Validation:** Comments do not alter runtime behavior, so no new behavioral test is justified. Before implementation, check `git diff --check`; the staged-snapshot hook and `py -3 tools/run.py ci --check` provide compile, test, formatting and build proof for the final committed tree. Persistence composition and migration discovery are out of scope, so no separate migration command is needed.
