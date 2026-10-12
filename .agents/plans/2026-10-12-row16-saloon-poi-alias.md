# Row 16 Saloon Person-of-Interest Alias Retirement Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Remove the obsolete saloon wanted-suspect compatibility alias from the player response model and web API types, and keep the confrontation acceptance test on the canonical response contract.

**Architecture:** `GameSession` clears `ActiveSaloonPersonOfInterest` when it applies `SaloonPersonOfInterestConfronted`; the player response exposes that state through `GameSessionDto.ActiveSaloonPersonOfInterest`. The JSON-ignored `ActiveSaloonWantedSuspect` property and matching web type are redundant compatibility scaffolding, not a serialized wire contract. Preserve the event-sourced clearing behavior and the existing separate domain-state reload assertions.

**Tech Stack:** C#/.NET 10, ASP.NET Core, TypeScript, xUnit, PostgreSQL-backed API acceptance tests, Markdown investigation and roadmap artifacts.

**Spec and evidence:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, row 16 residual source dispositions; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 16; `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`, AP-18; `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`, AP-18 and the dated AP-15 disposition.

**Execution Strategy:** `executing-plans`. One canonical response alias and its only consumer are retired together; the focused PostgreSQL acceptance test proves the surviving behavior and a temporary mutation proves its assertion is sensitive.

## Global Constraints

- Remove only the Application `GameSessionDto.ActiveSaloonWantedSuspect` compatibility property, its DTO record, and the unused web declaration/field. Keep the canonical `ActiveSaloonPersonOfInterest` contract and internal Domain APIs, including legacy-named state helpers that remain in use.
- Update the real no-gun flee acceptance case to assert `result.CurrentSession.ActiveSaloonPersonOfInterest` is null after the event. Keep its existing reload assertions for suspect availability, cleared aggregate state, and absence of a wanted-suspect confrontation record.
- Prove the updated assertion can fail by temporarily bypassing `GameSession.Apply(SaloonPersonOfInterestConfronted)` clearing the active POI, running the focused acceptance test and observing the expected failure, then restoring the implementation and rerunning the test. Do not commit the mutation.
- Do not add a test that checks the removed member or type stays absent. Consumer search, C# build, TypeScript validation, and the behavior test are sufficient for deleting an ignored alias.
- Do not change the event, event replay, aggregate behavior, serialized JSON, feature promise, or turn-result diary response shape. Keep the separate `GameTurnResultFactory` question for a later JIT plan.
- The first substantive commit records PR #286 and its closeout in the row 16 roadmap, retires the completed `.101` plan, adds this successor, and advances `Directory.Build.props` once to `0.1.0-dev.102`. This implementation PR consumes `.102`.
- Review ADR-0014, ADR-0015, and ADR-0028 plus `docs/features.md` against the actual diff. No durable decision or product promise changes; keep those files unchanged and record that conclusion in the PR review evidence.
- Deliver one PR to `develop`. After exact-head hosted CI and the develop-push gate pass, fast-forward the primary checkout, remove this clean worktree first, then remove and verify its paired local and upstream branch refs.

## Review Focus

- Verify `SaloonPersonOfInterestConfronted` still clears the aggregate's active POI and that the acceptance test observes this through the returned canonical player DTO as well as its independent event-reloaded domain state.
- Verify the removed alias was ignored by JSON serialization and had no consumer beyond the corrected acceptance test; verify TypeScript had no consumer of the obsolete field/type.
- Keep the DTO cleanup separate from the still-open turn-result diary response-shape question and from Domain internal legacy naming.

## Task 1: Retire the redundant response alias and correct its behavior consumer

**Files:**
- Modify: `src/WildBunch.Application/Games/Models/GameDtos.cs`
- Modify: `src/WildBunch.Web/src/api/types.ts`
- Modify: `tests/WildBunch.Integration.Tests/Acceptance/SaloonConfrontationAcceptanceTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-application-mapping-facade.md`
- Keep through this PR: `.agents/plans/2026-10-12-row16-saloon-poi-alias.md`

**Interfaces:**
- Consumes: the canonical saloon POI in `GameSessionDto`, event-backed aggregate clearing, the real flee acceptance flow, and the browser's canonical `activeSaloonPersonOfInterest` type.
- Produces: no JSON-ignored wanted-suspect alias in the Application DTO and no unused matching browser type; unchanged saloon confrontation outcome and serialized contract.

- [ ] Before editing, verify the compatibility member's only C# consumer is the named acceptance assertion, the TypeScript member/type have no consumers, and `[JsonIgnore]` prevents the alias from changing serialized JSON. Read the relevant ADRs and feature-matrix entry.
- [ ] Run the focused `SaloonConfrontationAcceptanceTests` against PostgreSQL before changing code. Then temporarily bypass the `ClearActiveSaloonPersonOfInterest()` call in `GameSession.Apply(SaloonPersonOfInterestConfronted)`, run the focused no-gun flee acceptance test, confirm it fails at the returned-response assertion, restore the source exactly, and rerun the test green. Do not commit the mutation.
- [ ] Remove the ignored C# property and now-unused DTO record, and remove the unused import if it has no remaining use. Remove the optional TypeScript field and its interface. Replace the stale acceptance assertion with `Assert.Null(result.CurrentSession.ActiveSaloonPersonOfInterest)` while preserving all independent reloaded-state and event assertions.
- [ ] Add dated AP-18 dispositions to both investigation records. Preserve the original observation as historical; record that PR #286 removed the Application narration façade and wrapper tests, that AP-15 had already retired `GameSessionDtoProjectionFieldsTests` in commit `0849f16781c34cbaba29154ab177ed92836afb62` on 2026-10-10, and that this slice retires the JSON-ignored alias while preserving the canonical response behavior. State that the separate `GameTurnResultFactory` response-shape finding remains unresolved for a later JIT plan. Correct the existing AP-18 test-follow-up text that incorrectly calls the already-retired projection test file live and says alias removal is still pending.
- [ ] In the first substantive commit, record PR #286's reviewed source `9cfd22880068493e168dfef0d17e67d93b741804`, merge `34d636df0f7a2e467870fffb411c0babb19a64b0`, fresh-review approval, exact-head hosted gate `38200656675` (passing attempt 2) and develop-push gate `38201121660`, primary-checkout fast-forward, and verified worktree-first paired local/upstream ref cleanup in row 16. Link this `.102` plan, retire the `.101` plan, and advance `Directory.Build.props` to `0.1.0-dev.102`.
- [ ] Verify no Application or web source/type consumer remains for `ActiveSaloonWantedSuspectDto` or the DTO property; keep Domain internal helpers outside this retirement. Confirm API JSON uses the canonical `activeSaloonPersonOfInterest` field and has no observable contract change from deleting the `[JsonIgnore]` property.
- [ ] Ensure PostgreSQL is running with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`, then run the focused test and type/build checks through the command bus:

```powershell
py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~SaloonConfrontationAcceptanceTests"
py -3 tools/run.py dotnet-build --check -- src/WildBunch.Application/WildBunch.Application.csproj
py -3 tools/run.py web --check
git diff --check
```

- [ ] Let the tracked staged-snapshot pre-commit hook run `py -3 tools/run.py ci --check` once on the intended implementation commit. Do not run the complete gate immediately before or after a successful hooked commit.
- [ ] Compare the final diff with ADR-0014, ADR-0015, ADR-0028, and `docs/features.md`; confirm that the ignored alias retirement preserves current decisions and product promises, and do not edit those documents.
- [ ] Obtain one fresh whole-branch review, publish the PR to `develop`, verify the exact PR head and both hosted gates before merge, then fast-forward the primary checkout and remove the worktree before its paired refs.

**Validation:** Sensitivity-proven PostgreSQL no-gun flee acceptance behavior; Application build; web lint, typecheck, tests and build; alias consumer and serialization review; full tracked pre-commit gate; generated identity `0.1.0-dev.102`; whole-branch review; exact-head PR and post-merge develop CI.
