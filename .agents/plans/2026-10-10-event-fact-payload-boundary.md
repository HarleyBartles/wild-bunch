# Record the Event Fact Payload Boundary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Resolve the remaining PS-14 question about typed event payload snapshots with a source-backed disposition, without duplicating immutable Domain facts into a blanket Persistence DTO layer.

**Architecture:** Persistence continues to own event serialization, event envelopes, version checks, and registered upcasting. The current typed event records and their explicitly named immutable fact snapshots remain the payload model; review found no serialized aggregate roots or computed event metadata beyond the three timestamp properties corrected in the preceding slice. Record this boundary and its evidence in the PS-14 investigation and test follow-up without changing runtime behavior.

**Tech Stack:** Markdown, .NET 10, C#, EF Core, PostgreSQL, event-sourcing upcasters.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; row 07 of `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; PS-14 of `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md`.

**Execution Strategy:** `executing-plans` inline and sequentially because the successor handoff and its evidence correction are tightly coupled documentation changes; a second implementation context would add no independent behavioral review value.

## Global Constraints

- Start from `develop` at verified PR #240 merge `d692b8f6bc617f9c40a02b958c6cdabf8a0c011e`; target `develop`.
- Record PR #240 source `be2af352007424662e712c006e32eeac63e87f3b`, merge `d692b8f6bc617f9c40a02b958c6cdabf8a0c011e`, PR-head gate `38023685917`, develop push gate `38023977521`, and delivered `0.1.0-dev.55`.
- Retire the completed `.55` plan only after its complete scope and delivery are verified; preserve the current row 07 investigation and roadmap.
- Advance the single authored application version in `Directory.Build.props` to `0.1.0-dev.56` in the committed successor-plan handoff.
- Do not add event DTOs, upcasters, migrations, tests, event fields, or product behavior when the reviewed event shapes and replay behavior already satisfy the accepted contract.
- A future change to a persisted event fact or its serialized shape must preserve historical rows through the registered event-upcaster chain and retain supported literal history evidence.
- Keep changes scoped to row 07. Do not alter feature claims, ADR status, supported-playthrough policy, or migration history.
- Publish a PR to `develop`; require hosted canonical CI success on the exact PR head and exact develop merge commit before retiring the worktree and branch.

## Review Focus

- **Event fact ownership:** Confirm the event catalog persists typed fact records, named immutable snapshots, and value objects rather than live aggregate roots; preserve the current event facts without introducing a parallel storage model.
- **Historical-shape evolution:** Keep occurrence time in the envelope and ensure the dated disposition does not imply historical payload rows were rewritten or that event schema changes can bypass registered upcasters.
- **Test claims:** Update only the test follow-up's assessment of payload ownership; do not claim new runtime coverage or add a structural test that only detects serializer implementation details.

---

### Task 1: Commit the `.56` successor handoff

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-10-event-payload-occurrence-metadata.md`; create this successor plan.

- [ ] Verify PR #240 is merged to `develop` at `d692b8f6bc617f9c40a02b958c6cdabf8a0c011e`, source `be2af352007424662e712c006e32eeac63e87f3b`, and exact-head/develop gates `38023685917`/`38023977521` succeeded.
- [ ] Record `.55` delivery in row 07 and select this PS-14 disposition as the `.56` successor; keep the row marked executing.
- [ ] Set `Directory.Build.props` to `0.1.0-dev.56`, remove the completed `.55` plan, and add this committed successor plan.
- [ ] Commit the planning handoff before editing the investigation or test follow-up.

**Expected:** The roadmap reflects the exact completed PR #240 evidence, points to this successor, and the version is `.56` in one authored location.

### Task 2: Resolve the remaining PS-14 event-payload assessment

**Files:** Modify `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md` and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`.

- [ ] Review the concrete event payload serializer, event-type resolver, all Domain event records, the nested `WorldSnapshot`, `CaseFileSnapshot`, and `TravelJourneySnapshot` types, and the registered event upcasters at the committed plan base.
- [ ] Confirm that the current event payload path serializes immutable typed event facts and named snapshot/value types; no live aggregate root is serialized, and the only computed time property found in the event catalog was the three-property occurrence-time leak corrected in PR #240.
- [ ] Add a dated PS-14 disposition stating why the current Domain fact/snapshot types remain the payload model and why copying them into Persistence DTOs would duplicate the same event contract without closing a demonstrated runtime or history defect.
- [ ] Update the test follow-up to state that current event evolution remains governed by registered upcasters and production-loader history tests; distinguish existing coverage from new coverage and do not add tests for this documentation-only disposition.
- [ ] Re-read ADR-0028 and the baseline event-payload boundary; leave both unchanged because this disposition preserves their current decision and makes no new persistence or product promise.

**Expected:** The investigation no longer leaves typed event snapshots as an unbounded open DTO-mapping task, while the existing event-version/upcaster requirement and historical compatibility boundary remain explicit.

### Task 3: Review, validate, and publish

**Files:** All changed planning and investigation paths in Tasks 1–2.

- [ ] Review the entire diff against PS-14, the baseline spec, ADR-0028, the event-sourcing doctrine, and completed-artifact custody; verify the plan is retired only because its entire timestamp-removal scope is merged.
- [ ] Confirm a documentation-only disposition did not change product claims, feature dependencies, event schemas, tests, migrations, or runtime behavior; no new behavioral test is warranted.
- [ ] Stage the intended files and let the normal check-only pre-commit hook run the canonical fail-fast gate. Do not run the canonical gate immediately before or after a successful hooked commit.
- [ ] Push and open a PR to `develop`; verify the exact PR head SHA and hosted canonical gate. Merge as authorized by the epic and verify the exact develop push gate.
- [ ] Record the actual source/merge SHAs and hosted gate evidence in the roadmap through the next successor handoff, fast-forward `Z:\wild-bunch`, then remove only this verified merged worktree and its local/remote branch.

**Expected:** The `.56` documentation disposition merges to `develop` with hosted CI green on the exact PR head and merge commit; row 07 remains open for its remaining supported persistence findings.
