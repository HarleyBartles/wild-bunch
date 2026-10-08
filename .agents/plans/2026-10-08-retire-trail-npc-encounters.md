# Retire Generated Interactive Trail NPC Encounters Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Remove the unfinished generated friendly trail-NPC interaction for 0.1.0 while preserving hostile encounters, shared encounter resolution, event-backed travel, and unrelated saloon citizens.

**Architecture:** Keep encounter generation deterministic and keep event application/replay authoritative. Remove only the NPC generation category, its exclusive messages/choices/dev option and stale fixtures. Preserve retained enum numeric values and generator version so existing serialized values are not renumbered and the seed hash is not changed wholesale. Validate developer category input at the API boundary; do not parse arbitrary strings in the command handler.

**Tech Stack:** C#/.NET Domain, Application and API; React/TypeScript developer overlay; xUnit, Vitest and PostgreSQL-backed integration tests; repository command bus.

**Spec:** Stable 0.1.0 baseline, the feature matrix PG-007, row 05 of the roadmap, and the dated DN-16 disposition added by this slice.

**Execution Strategy:** Use executing-plans inline. Generator, API validation, characterization fixtures and overlay option are one bounded retirement with shared behavior contracts; execute them in this worktree and request a fresh whole-branch review before opening the PR.

## Global Constraints

- Start at merged develop commit da88d5101f13745c094cd49d6055565e06a29c15 and deliver by PR to develop.
- Advance Directory.Build.props exactly once from 0.1.0-dev.11 to 0.1.0-dev.12.
- Do not rebalance retained encounter categories, add replacement interactions, or redesign travel.
- Preserve all retained TravelDayEncounterCategory numeric values; leave the retired value unused rather than shifting serialized values.
- Preserve TravelDayPlanGenerator.CurrentVersion at 1. TravelDayAdvanced records generated facts and replay applies those facts without rerunning the generator.
- Preserve real hostile Foe encounters, shared foe resolution, environmental and quiet travel outcomes, and unrelated saloon citizens.
- Do not delete generic shared encounter resolution merely because NPC generation used it.
- No migration, database reset, or historical playthrough retention work is required under the settled pre-alpha history boundary; preserve schema migration history.
- Tests must demonstrate an independently correct behavioral result. Do not add source-shape, enum-inventory, route-absence, or implementation-mirroring tests.
- Run inexpensive fail-fast validation before builds/tests; use the existing check-only hook and canonical command bus.

## Review Focus

- Generated interactive encounters are hostile Foe encounters with a valid foe profile; generated friendly stranger choices no longer appear.
- A stale developer request for the retired Npc category returns a client error and records no override or gameplay event; valid Foe forcing still works.
- Real PostgreSQL integration still proves a hostile encounter can be interrupted, resolved, and travel resumed with player-safe projection.
- Diary/state-machine/resource characterization retains its intent using real hostile encounters, not deleted friendly-stranger expectations.
- Feature and investigation records distinguish the retired generated interaction from retained hostile trail encounters and unrelated saloon citizens.

---

### Task 1: Bootstrap the JIT slice and retire the completed predecessor

**Files:** Create this plan; update .agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md and Directory.Build.props; delete .agents/plans/2026-10-08-retire-dev-salt-controls.md.

- [ ] Record PR #196 as merged to develop at merge commit da88d5101f13745c094cd49d6055565e06a29c15, source head 862c79b2c29f9e167f221023b80641e3a144c59b, hosted gate run 37824931027, and clean final whole-branch review.
- [ ] Classify the complete developer salt-control retirement as shipped and retire its completed plan in this successor slice.
- [ ] Update row 05 to link this JIT plan and record 0.1.0-dev.12 as its current checkpoint; keep row 05 executing for later exclusions.
- [ ] Advance Directory.Build.props once from 0.1.0-dev.11 to 0.1.0-dev.12.
- [ ] Stage only this plan, roadmap, version and completed predecessor retirement; inspect the staged diff and commit through the non-mutating hook before implementation.

### Task 2: Prove the retained generated encounter contract and remove the NPC generation path

**Files:** TravelDayPlanGenerator tests and Domain travel generator/factory/mapper/history code.

- [ ] Add a deterministic behavior test over fixed seeds and supported generation contexts that proves generated interactive encounters are hostile Foe encounters with a valid FoeProfile; run it against current behavior and witness the failing friendly-NPC case.
- [ ] Remove NPC category selection and its exclusive weights, adjustments, default choices, friendly-stranger message/title branches and mapper/history labels.
- [ ] Preserve Foe generation, other categories, shared encounter resolution, current generator version, and retained enum numeric values.
- [ ] Run focused Domain tests, including entropy variation, and adjust only expectations directly invalidated by the retired category.

### Task 3: Rebase retained characterization and persistence scenarios on hostile encounters

**Files:** Travel diary/state-machine/resource characterization; test factories; seed catalog and guardrails; GameApiTests and integration scenario catalog.

- [ ] Replace friendly-stranger assertions with an explicitly event-backed Foe override where the existing test's purpose is interruption, diary, resource tracking, or resume behavior.
- [ ] Remove the NPC-only seed fixture and its self-contained codec round-trip; keep unrelated deterministic seed coverage.
- [ ] Change the real PostgreSQL interrupted-travel scenario to force Foe and preserve its checks for hidden foe data, bribe resolution, travel resume, clock and diary.
- [ ] Run focused Domain and Integration tests using the local PostgreSQL helper; do not weaken independent assertions to make them pass.

### Task 4: Remove the developer option and reject retired category input safely

**Files:** TravelDevPanel, developer travel API/command mapping and DevTravelEndpointTests.

- [ ] Remove Npc from the developer forced-category choices.
- [ ] Validate that a requested category is a defined retained enum at the API boundary and pass a typed enum through the command; remove arbitrary string parsing from the handler.
- [ ] Add a negative endpoint behavior test: requesting retired Npc returns HTTP 400 and leaves override/gameplay state unchanged; retain the valid Foe forced-category test.
- [ ] Run the focused API and web tests, plus the required browser proof for the changed developer panel: capture compact, expanded and default panel states, confirm Npc is absent and Foe can still be forced, then observe a hostile encounter. Store screenshots only in branch-scoped scratch.

### Task 5: Reconcile feature and investigation records

**Files:** docs/features.md and the paired Domain source/test investigation records.

- [ ] Update PG-007 evidence to record that generated interactive friendly NPC encounters and their exclusive developer option were retired; hostile encounters and other travel remain, so PG-007 stays partial.
- [ ] Add dated resolution notes to DN-16 and the Domain test follow-up. Preserve the original investigation text as historical evidence rather than rewriting it as though the question was never open.
- [ ] Confirm ADR-0013 and ADR-0031 remain truthful: travel stays within GameSession and retained developer overrides remain event-backed. Record the no-ADR-change rationale in the PR.

### Task 6: Validate, review and deliver

- [ ] Run the focused suites, PostgreSQL integration, web checks and required developer-panel browser proof; then run the canonical repository gate.
- [ ] Inspect the final diff and version transition; confirm no Npc generation, developer option, or stale expectation remains in active paths.
- [ ] Request a fresh whole-branch review, resolve actionable findings, and rerun affected validation after each correction.
- [ ] Open a PR to develop with the plan and screenshots excluded from the commit, attach it to this task, and report exact source head and hosted gate evidence.
- [ ] Merge only after the reviewed head's hosted canonical gate succeeds. Keep this plan until the next successor slice records the merge and retires it.
