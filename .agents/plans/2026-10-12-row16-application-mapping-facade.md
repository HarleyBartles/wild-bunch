# Row 16 Application Mapping Surface Cleanup Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Remove the unused Application narration façade and correct the historical record for already-resolved town-layout DTO ownership without changing player behavior.

**Architecture:** `GameSession` and the Domain `BeatNarration` helper own the narration fact; Application maps returned facts and does not need a second pass-through renderer. The existing player town-layout salt DTO is already owned by `Application.Games.Models` and remains part of the public rendering contract. Preserve the original audit findings and add dated dispositions for the changes that actually resolved them.

**Tech Stack:** C#/.NET 10, xUnit, Markdown investigation and roadmap artifacts.

**Spec and evidence:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, row 16 residual source dispositions; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, rows 15 and 16; `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`, AP-18 and AP-20; `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`, AP-18 and AP-20 proof dispositions.

**Execution Strategy:** `executing-plans`. The unused façade, its façade-only tests, and the two historical Application dispositions share one mapping-ownership cleanup; inline execution keeps the evidence together, and one fresh whole-branch review supplies the independent gate.

## Global Constraints

- Preserve narration produced by `WildBunch.Domain.Game.BeatNarration` through `GameSession` and the live saloon investigation/gossip response paths.
- Delete only the unreferenced `WildBunch.Application.Games.Mapping.BeatNarrationRenderer` façade and its tests; keep Domain narration and its behavior tests.
- Keep the player-facing `TownLayoutSaltsDto` in `WildBunch.Application.Games.Models`; salts remain renderer inputs in the ordinary town layout response.
- Do not alter the behavior, contract, or test policy of other AP-18 concerns such as `GameTurnResultFactory` overloads and saloon compatibility fields.
- Add no structural test for deleted files, namespaces, or investigation wording. Existing Domain behavior tests cover narration; deletion needs consumer search, compile, and focused execution.
- The first substantive commit retires the completed `.100` plan, records PR #285's merge, hosted gates, and paired worktree/ref cleanup in row 16, starts this successor plan, advances `Directory.Build.props` once to `0.1.0-dev.101`, and dates the already-landed AP-20 resolution. Record AP-18's façade retirement with the source deletion in the implementation commit.
- Review ADR-0014, ADR-0015, and ADR-0036 against the actual diff; no durable decision or product promise changes, so do not alter ADRs or `docs/features.md`.
- Deliver one PR to `develop`. After exact-head hosted CI and develop-push CI pass, fast-forward the primary checkout, remove the clean worktree first, then remove and verify the matching local and upstream branch refs.

## Review Focus

- Confirm the wrapper deletion does not remove actual saloon narration; retain the existing `BeatNarrationDomainTests` coverage through `GameSession`.
- Confirm the AP-20 disposition does not imply public renderer salts were removed; retain existing town-layout mapper and salt contract tests.

## Task 1: Retire the unused narration façade and reconcile source findings

**Files:**
- Delete: `src/WildBunch.Application/Games/Mapping/BeatNarrationRenderer.cs`
- Delete: `tests/WildBunch.Application.Tests/Renderers/BeatNarrationRendererTests.cs`
- Modify: `src/WildBunch.Domain/Game/BeatNarration.cs` to remove its stale reference to the Application façade.
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md` with the dated AP-18 proof disposition.
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-api-request-contracts.md`
- Keep through this PR: `.agents/plans/2026-10-12-row16-application-mapping-facade.md`

**Interfaces:**
- Consumes: Domain `BeatNarration`, `GameSession` narration results, current saloon Application handlers, current town-layout game read DTO, Domain narration behavior tests.
- Produces: no Application pass-through renderer; unchanged Domain-authored narration and public layout rendering contract; accurate dated disposition of AP-18 and AP-20.

- [x] Before editing, confirm `BeatNarrationRenderer` has no production consumer, confirm the existing tests only exercise the pass-through wrapper, and run `BeatNarrationDomainTests` to establish the retained behavior.
- [x] Add a dated AP-20 disposition explaining that commit `290b9cf4f30dce90d76c68ade12866c030f47a49` moved `TownLayoutSaltsDto` from Dev to Games on 2026-10-08, and that the subsequent developer-control retirement removed the dev mutation/query surface while retaining salts in the player renderer contract. Preserve the original finding as historical evidence.
- [x] Delete the Application façade and its two tests, remove the stale Domain XML-documentation reference to that façade, and add dated AP-18 dispositions to both investigation records in the same implementation commit. State that Domain narration and its callers remain; leave the other AP-18 factory/compatibility findings open for later JIT decisions. Do not create replacement tests for delegation or source shape; `BeatNarrationDomainTests` proves narration through real GameSession behavior.
- [x] In the first substantive commit, record PR #285's reviewed source `86bf1225ad139d1667b4f17d6e9bd4952caea8bb`, merge `8548ef8698111df35906ad7a736384cc05d26fcc`, fresh-review approval, exact-head hosted gate `38198520836`, develop-push gate `38198899235`, primary-checkout fast-forward, and verified worktree-first paired branch cleanup in row 16. Link this plan as the active successor, advance to `0.1.0-dev.101`, and retire the `.100` plan.
- [x] Verify the Application source and tests have no remaining `BeatNarrationRenderer` reference, while `GameSession` still calls Domain narration and the live saloon handlers still return the Domain result. Verify `TownLayoutDto` and its mapper still use the Games-owned `TownLayoutSaltsDto`.
- [x] Run the focused behavior tests and Application build through the command bus, then run `git diff --check`:

```powershell
py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~BeatNarrationDomainTests|FullyQualifiedName~TownLayoutMapperTests|FullyQualifiedName~TownLayoutMapperLayoutSaltsTests"
py -3 tools/run.py dotnet-build --check -- src/WildBunch.Application/WildBunch.Application.csproj
git diff --check
```

  Confirm the generated web build identity through the canonical CI gate.
- [ ] Let the tracked staged-snapshot pre-commit hook run `py -3 tools/run.py ci --check` once on the intended commit. Do not run the complete gate immediately before or after a successful hooked commit.
- [ ] Compare the final diff with ADR-0014, ADR-0015, ADR-0036, and `docs/features.md`; report why no durable decision or product promise changes.
- [ ] Obtain one fresh whole-branch review, publish the PR to `develop`, verify the exact PR head and both hosted gates before merge, then fast-forward the primary checkout and remove the worktree before its paired refs.

**Validation:** Domain narration behavior remains covered through actual `GameSession` behavior; Application build; reference and DTO ownership searches; full tracked pre-commit gate; generated identity `0.1.0-dev.101`; whole-branch review; exact-head PR and post-merge develop CI.
