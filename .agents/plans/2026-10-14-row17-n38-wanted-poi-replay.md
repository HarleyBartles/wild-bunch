# N-38 Require a Reached Wanted POI in Saloon Replay

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Make the saloon event-replay test require a specific wanted person of interest to be spotted and reconstructed, so equality cannot pass when both command and replay contain no POI.

**Architecture:** The canonical session history is `GameSession.AllEvents`; replay consumes one copy of that ordered stream. A deterministic developer override selects an eligible non-culprit suspect through the existing command path. Typed events remain the only source for replayed active-POI state.

**Tech Stack:** C# 14, .NET 10, xUnit, Wild Bunch Domain event sourcing.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), PG-006 and row 17 of the [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), N-18 and the test dispositions, and ADR-0028.

## Constraints

- N-37 merged as `0.1.0-dev.155`; advance the sole authored application version in `Directory.Build.props` to `0.1.0-dev.156` exactly once.
- Preserve current saloon behavior, event contracts, event-sourced mutation, strict CQRS, immutable event history and exact replay from ordered facts.
- Keep the implementation within the Domain test, feature-evidence and test-investigation surfaces; do not change production behavior or player capability.
- Retire the completed N-37 plan only in this successor's first substantive commit; keep N-38 through its completing PR.
- Use the fresh worktree from `origin/develop`, target the PR at `develop`, obtain independent review and passing exact-head hosted CI before merge, verify the develop-push gate, then remove the worktree and paired local/upstream refs.

## Task 1: Record N-37 delivery and commit the N-38 plan

**Files:**
- Create: `.agents/plans/2026-10-14-row17-n38-wanted-poi-replay.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-14-row17-n37-original-event-history.md`

- [x] Verify PR #340 merged reviewed source `54cb222de3788b72ab80a97b511280e482b8a5c2` as `62d495851eb58d1174d4a0f404598e926e1c1248`, source and merge trees match, the exact-head and develop-push canonical gates passed, and the primary checkout and paired N-37 refs were cleaned.
- [x] Retire N-37 after classifying its complete replay-fixture scope as shipped; record PR #340's review, gate, merge and cleanup evidence in row 17; select N-38 as the successor; set `Directory.Build.props` to `0.1.0-dev.156`.
- [x] Commit this transition and the N-38 plan before editing tests or evidence.

## Task 2: Require the wanted-person replay branch

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/EventSourcing/BountySaloonEventSourcingTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `docs/features.md`

- [ ] Inspect the current saloon replay setup and use the existing canonical confrontable-suspect fixture with its evented `DevSaloonOverride.ForSuspect` command to force a known eligible non-culprit suspect; avoid reproducing a parallel game-start fixture.
- [ ] Replace setup-event concatenation with one post-command `session.AllEvents.ToArray()` replay input; assert the emitted spotted fact has the expected suspect ID and `WantedSuspect` kind, require that exact ID and kind in command state and replay state, preserve context and turn checks, and compare replayed and command versions.
- [ ] Remove the local setup helper if it has no remaining caller; do not add a test for helper absence.
- [ ] Prove the assertion is behavior-sensitive by temporarily disabling forced wanted-suspect selection in the owning Domain path, observing the test fail because the expected wanted person is missing, restoring production byte-for-byte, and confirming the focused test passes.
- [ ] Update the test disposition and N-18 with the precise closed gap while keeping unrelated travel-effect, completion and generator findings open; add a concise PG-006 dated evidence note for deterministic wanted-POI replay proof without implying broader feature completion.
- [ ] Compare the diff with ADR-0028 and the stable spec; retain both because the event and replay decision is unchanged. Do not add a feature dependency, release disposition or gameplay promise.
- [ ] Run the focused test, the full Domain test project, and `py -3 tools/run.py ci --check` on the committed candidate.

## Task 3: Review and deliver N-38

- [ ] Independently review the whole committed branch with the required Domain, test-quality and event-sourcing lenses; compare the actual diff with ADR-0028, PG-006, N-18 and the feature matrix.
- [ ] Open a Draft PR to `develop`, verify its exact head and description, then mark it ready after review so hosted CI runs on the reviewed head.
- [ ] Wait for exact-head hosted CI to pass, squash-merge N-38, verify its merge identity and successful develop-push gate, then fast-forward the primary checkout.
- [ ] Remove the clean N-38 worktree, delete only its matching local and upstream branch refs, and verify all three are absent before starting another plan.

## Review focus

- The test must force and assert a real wanted POI, so nullable equality cannot silently compare two absent values.
- Replay must consume the actual complete event history exactly once and restore the same active suspect, kind, context, turn and event version.
- The forced suspect must be eligible through the current domain rules; no direct state mutation or fabricated event may set the POI.
- Test-audit and feature-matrix changes must describe only the newly proven behavior and preserve open findings and partial feature status.
