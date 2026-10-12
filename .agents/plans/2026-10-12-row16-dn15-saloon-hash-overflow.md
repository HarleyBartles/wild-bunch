# Saloon Hash Overflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep the ordinary saloon look-around command playable when a valid runtime-shaped salt makes its deterministic hash equal `int.MinValue`.

**Architecture:** Preserve the existing deterministic hash and modulo mapping for every hash except `int.MinValue`, which currently throws through `Math.Abs`. Map that one value to `int.MaxValue` before returning the hash so the existing pool selection remains valid and all other rolls remain unchanged. Prove the public `GameSession.LookAroundSaloon` path with a 32-character uppercase hexadecimal salt produced by the normal runtime salt source.

**Tech Stack:** C#/.NET 10, xUnit, Wild Bunch Domain, repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; row 16 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; DN-15 in `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`.

**Execution Strategy:** `executing-plans` inline. The reproduced defect, public command test, deterministic mapping fix, investigation disposition, roadmap evidence and version change are one tightly coupled slice. Independent implementation handoffs would add context reconstruction without a separable review boundary; a fresh whole-branch review will assess the completed PR.

## Global Constraints

- Keep `Directory.Build.props` as the sole authored application-version authority and advance exactly once to `0.1.0-dev.112` in the first substantive commit.
- Preserve saloon pool outcomes for every hash other than `int.MinValue`; do not change candidate ordering, pool size, salt, events or replay behavior.
- Keep event facts authoritative; the selected saloon result remains recorded by the existing domain event and replay does not reroll it.
- Keep `CitizenCast.StableHash` open in DN-15 because this plan has not established a concrete production-shaped input for that path.
- Keep the active spec, roadmap and this plan through their completing PR; retire the `.111` plan and record PR #296 merge, hosted-gate and paired worktree/ref cleanup evidence in this successor's first substantive commit.
- Deliver through a reviewed PR to `develop`; require exact-head hosted CI to pass, fast-forward the primary checkout after merge, then remove the clean worktree before deleting and verifying its local and upstream branch refs.

## Review Focus

- A valid runtime salt that hashes to `int.MinValue` must complete normal saloon selection and record its result instead of throwing; the test uses the ordinary public command path without a dev override.
- Existing deterministic saloon outcomes must remain unchanged for all other hash values; use the single-value fallback and do not rewrite the hash algorithm or modulo mapping.
- The adjacent `CitizenCast.StableHash` observation remains unresolved rather than being mislabeled fixed by this bounded slice.

---

### Task 1: Commit the JIT plan before implementation

**Files:**
- Add: `.agents/plans/2026-10-12-row16-dn15-saloon-hash-overflow.md`

- [x] Commit this plan by itself before editing source, tests, investigation, roadmap or version files. Keep the version at `0.1.0-dev.111` in this plan-only commit.

### Task 2: Prove and fix the runtime-shaped saloon hash overflow

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/SaloonPoiSelectionTests.cs`
- Modify: `src/WildBunch.Domain/Game/BountyLoop.cs`

**Interfaces:**
- Consumes: `TestSessionFactory.CreateWithConfrontableSaloonSuspect(string salt)`, `GameSession.LookAroundSaloon()`, and the existing `SaloonPersonOfInterestSpotted` event.
- Produces: A regression test showing a valid 32-character uppercase-hex salt can traverse ordinary saloon selection when its computed signed hash is `int.MinValue`.

- [x] Add a test using salt `000000000000000000000000DEA475D9` and the normal `CreateWithConfrontableSaloonSuspect(salt)` fixture. Call `LookAroundSaloon()` without a developer override; assert success, exactly one `SaloonPersonOfInterestSpotted` event, and equality between the event's `SuspectId`/`PersonOfInterestKind` and the active state. The salt is 32 uppercase hex characters, matching `SaltSource.CreateRuntime()` output, and the fixture's `current` town/day/turn/visit values (`1/1/1/1` after saloon context entry) produce `int.MinValue`.
- [x] Run only the new test with `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~LookAroundSaloon_RuntimeShapedSaltWhoseHashIsIntMinValueStillSelectsAndRecordsPoi"` and confirm it fails because `Math.Abs(int.MinValue)` throws `OverflowException`. The hash folds the numeric day, turn and visit values as integers; they are not the ASCII digit characters in the input string.
- [x] Change `StableSaloonRollHash` to return `int.MaxValue` only when the unchecked hash equals `int.MinValue`; otherwise return `Math.Abs(hash)` as today. Do not change hash inputs or the caller's modulo operation.
- [x] Re-run the focused test and `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~SaloonPoiSelectionTests"`. Confirm both pass.
- [x] Keep the implementation and focused test ready for the first substantive commit. Do not commit them separately: Task 3's predecessor retirement, roadmap evidence, investigation disposition and `.112` version change must be in this PR's first substantive commit.

### Task 3: Record the bounded disposition and successor delivery

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `docs/features.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-dn14-refresh-policy-retirement.md`

**Interfaces:**
- Consumes: Task 2's passing public-command proof and PR #296's verified merge/CI/cleanup evidence.
- Produces: A dated partial DN-15 disposition, matching PG-006 evidence for the newly proven normal-command edge, an accurate row-16 successor entry at `0.1.0-dev.112`, and retirement of the completed `.111` plan.

- [x] Add a dated DN-15 note that the valid runtime-shaped salt reproduced `int.MinValue` in `BountyLoop.StableSaloonRollHash`, that the single-value fallback preserves all other saloon hash outcomes, and that `CitizenCast.StableHash` remains open pending a concrete production-shaped input or stronger source contract.
- [x] Do not change the Domain test follow-up: the new command test is the minimal behavioral proof for this newly reproduced failure, while the existing file already records conditional test recommendations and does not assert this overflow path.
- [x] Update PG-006's evidence assessment to record that the runtime-shaped `int.MinValue` case completes through the normal player command path and records matching event/state facts; leave the product promise, dependencies and disposition unchanged.
- [x] Retire the `.111` plan and update row 16 to point to this `.112` plan. Record PR #296 source `199b31404f730e5a9c24cf6e31d1094ce38272e`, merge `6d417431d6af3067c99c5690c9dd8e194f98a40f`, exact-head gate `38239274921`, develop-push gate `38239928829`, and that the clean worktree was removed before both branch refs and all three absences were verified. These records are part of the same first substantive commit as the source fix and version change.
- [x] Change `Directory.Build.props` exactly once from `0.1.0-dev.111` to `0.1.0-dev.112`; ensure the production web build identity still derives from this shared authority.
- [x] Run `py -3 tools/run.py ci --check` and the canonical staged-snapshot pre-commit hook, resolve any failure through its designated repair command, then commit the successor retirement, dated dispositions, roadmap delivery proof and version change together.
