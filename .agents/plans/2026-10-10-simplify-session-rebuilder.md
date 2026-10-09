# Simplify Session Rebuilder

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Remove the unused serializer parameter and stale planning comment from `SessionRebuilder`, simplify its call sites, and retire the test that freezes an unsupported placeholder-ID/component-JSON shape.

**Architecture:** Persistence owns reconstruction from ordered, upcast events through `GameSession.RehydrateFromEvents`. The rebuilder does not serialize components; the `PersistedPayloadLoader` owns component loading and uses a rebuilt aggregate only to produce a requested current component cache. Keep known-session-ID reconstruction and the event-only component-cache callback distinct. Preserve behavior tests for stale component recovery, fail-closed history, and repository reads.

**Tech Stack:** .NET 10, C#, xUnit.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery” and “History and migration boundary”; row 07 of `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; PS-15 and PS-17 in the persistence investigation and the PS-15 test follow-up.

**Execution Strategy:** `executing-plans`, inline and sequential. First commit this JIT plan and retire the completed `.47` predecessor; then simplify the persistence helper and call sites, remove the unsupported test, and validate the real cache-recovery consumer. Run the canonical gate, review the full branch diff, and use the documented self-review fallback because this runtime does not permit reviewer-agent dispatch.

## Rulings

- Remove `GameSessionJsonSerializer` from both `SessionRebuilder.RebuildFromEvents` overloads. The parameter is unused; serializers remain where `PersistedPayloadLoader` actually needs them.
- Preserve the known-ID replay path and event-only component-cache rebuild callback. The callback may construct an ephemeral session identity because events carry no envelope ID; that identity is not a persisted game fact and must not become a tested component format promise.
- Replace lambdas that exist only to forward events and an unused serializer with the callback method group where it binds unambiguously. Keep unrelated serializer instances required to construct payload loaders or serialize actual test output.
- Remove the stale “Plan C” helper comment. Describe current event reconstruction and the limited callback use in direct terms.
- Retire `SessionRebuilder_ComponentJson_IsIndependentOfSessionId`: it passes explicit IDs to the known-ID overload, does not test the no-ID callback, and freezes equality of only five component encodings. Add no replacement test for an internal placeholder identity or serializer signature.
- Retain `LoadComponentPayload_StaleVersion_TriggersRebuildFromEvents` and existing PostgreSQL/replay behavior coverage. Do not change event application, upcasting, component versions, current read behavior, or serialized component payloads.
- No ADR or feature-matrix update: this removes a dead parameter and unsupported test promise without changing durable event authority or player behavior.
- Leave unbounded status loads and populated historical migration policy outside this slice.
- Advance `Directory.Build.props` once from `0.1.0-dev.47` to `0.1.0-dev.48`.

## Global Constraints

- Work from the clean `develop` merge `5fac46181d2a6f9622f9081fa57ad726756ba8c0` in this fresh linked worktree and target `develop`.
- Commit this plan before source edits. Retire the completed `.47` plan only after recording PR #232's source/merge identities and exact hosted gate evidence.
- Preserve full event replay, event upcasting, cache fallback, fail-closed behavior, component formats, and production dependency registration.
- Keep the roadmap live through PR merge. The next substantive successor retires this plan after verifying delivery evidence.

## Review Focus

- Every rebuilder call site uses the correct overload; no source call or lambda retains the unused serializer argument.
- `PersistedPayloadLoader` still receives its serializer and rebuild callback, and the stale-version cache behavior test still demonstrates event-backed component reconstruction.
- No test remains that treats the ephemeral identity or a partial list of component encodings as a supported persistence contract.
- No event names, payloads, versions, replay state, migration history, database schema, or player-facing behavior changed.
- ADR and feature-matrix review confirms no durable or player-facing truth moved.

---

### Task 1: Record PR #232 and commit this `.48` successor plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-09-retire-unused-persistence-schema-artifacts.md`; create this plan.

**Interfaces:** Record PR #232 source `338aed0fe1e02af22d212423587b93bf2f544418`, merge `5fac46181d2a6f9622f9081fa57ad726756ba8c0`, exact-head PR gate run `38003358699`, develop push gate run `38003779646`, and delivered identity `0.1.0-dev.47`. Row 07 remains executing and points to this `.48` plan.

- [x] Verify PR #232 is merged to `develop` at the stated source and merge SHAs and both hosted gates passed on those exact commits.
- [x] Compare the completed `.47` plan with merged source and delivery evidence; record its scope and disclosed self-review outcome in row 07 and append PR #232 to the merged PR list.
- [x] Select the dead rebuilder serializer argument and unsupported placeholder-ID test as this successor's bounded PS-15 slice; leave unbounded status loads and historical migration disposition separate.
- [x] Advance `Directory.Build.props` to `0.1.0-dev.48`, retire the completed `.47` plan and its stale row pointer, and commit this plan before source edits.

**Expected:** Delivery evidence is accurate, the completed predecessor is retired, and the committed `.48` plan is the live row 07 pointer.

### Task 2: Simplify the rebuilder contract and remove the unsupported test

**Files:** Modify `src/WildBunch.Persistence/GameSessions/SessionRebuilder.cs`, `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`, `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`, `src/WildBunch.Persistence/DependencyInjection.cs`, and call sites in `tests/WildBunch.Integration.Tests`; remove only the helper identity test from `tests/WildBunch.Integration.Tests/Versioning/VersionMismatchBehaviorTests.cs`; append dated PS-15/PS-17 dispositions to the persistence investigation and PS-15 test follow-up.

- [ ] Recheck every `SessionRebuilder.RebuildFromEvents` caller and the `PersistedPayloadLoader` callback type. Distinguish calls with a real envelope ID from the event-only component-cache callback.
- [ ] Remove the unused serializer parameter and its now-unused namespace import. Preserve the callback's serializer dependency on `PersistedPayloadLoader` itself.
- [ ] Replace event-only forwarding lambdas with the rebuilder callback method group where the delegate signature resolves the one-argument overload. Update known-ID callers to pass only the ID and events.
- [ ] Remove the stale “Plan C” comment and clarify the current event reconstruction/callback boundary without making claims about a product identity.
- [ ] Remove only `SessionRebuilder_ComponentJson_IsIndependentOfSessionId`. Preserve `LoadComponentPayload_StaleVersion_TriggersRebuildFromEvents`, missing-history failure behavior, repository cache recovery, and full replay tests. Do not add source/API absence detectors.
- [ ] Append dated investigation/test dispositions stating why the five-component identity test was retired, which actual behavior proof remains, and that this change does not alter event or cache semantics.
- [ ] Recheck the decision catalogue and `docs/features.md`; leave them unchanged because no durable or player-facing truth moved.

**Expected:** Reconstruction has no unused dependency or obsolete planning language, real cache rebuild remains behaviorally covered, and the unsupported test no longer freezes an incidental helper shape.

### Task 3: Validate, review and publish to `develop`

- [ ] Run the focused stale-component version recovery test and related event replay tests, then the canonical fail-fast `py -3 tools/run.py ci --check` gate on the exact staged candidate.
- [ ] Inspect the whole branch diff, all changed call sites, helper comments, removed test, retained behavior tests, investigation dispositions, applicable ADRs, feature matrix and unslop profile. Record the self-review fallback due to runtime reviewer-agent restrictions.
- [ ] Publish a Draft PR to `develop` and verify the PR head matches local `HEAD`. PR jobs run only after the PR is marked ready, so require hosted canonical CI to pass on that exact head before merge.
- [ ] After merge, verify the develop push gate passes on the merge SHA. Leave the plan and roadmap for the next successor to retire after verifying evidence.

**Expected:** `.48` is merged to `develop` with exact-head review and passing hosted PR and develop gates; event-authoritative rebuild and the real cache recovery behavior remain intact.
