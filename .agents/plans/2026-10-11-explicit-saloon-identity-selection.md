# Require an explicit saloon identity declaration

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Keep known wanted posters in the canonical game-session Query resource and require the player to deliberately select the identity they declare for a saloon person of interest.

**Architecture:** The session DTO is the sole client source of wanted posters. The declared identity is local presentation intent, initially empty, preserved only while that poster remains in the current session's known posters, and cleared by reset/archive. The server continues to decide confrontation legality and outcome.

**Tech Stack:** React, TypeScript, TanStack Query, TanStack Router, Vitest, React Testing Library.

**Specification:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, section E and PG-006.

**Decision records:** Preserve ADR-0006, ADR-0016, ADR-0027, ADR-0035, ADR-0039 and server authority. ADR-0045 now requires one shared world map to offer equivalent pointer and semantic keyboard/screen-reader town selection; this plan records that settled scope but does not implement the map interaction, which remains in roadmap row 15. No gameplay promise, event, or endpoint changes in this plan.

**Execution Strategy:** `executing-plans` (Native inline execution) because the Query-backed session, local declaration state, reset/archive lifecycle, provider derivation and saloon interaction form one coupled behavior. A separate implementer context would have to reconstruct the same React Query/provider identity chain, while one whole-branch review can assess the complete scenario.

## Global Constraints

- Wanted posters are server-known session state; never maintain a second client copy or treat a mutation result as a competing authoritative resource.
- A player has not named a person merely because a poster was loaded; the declared identity starts empty and changes only through explicit player selection.
- A selected identity is valid only while its poster remains in the active session's known poster list; clear stale selection rather than choosing another person.
- Reset and confirmed archive clear local declaration intent and remove the associated Query resources as they do today.
- Keep server-side confrontation legality, event facts, outcomes, and command paths unchanged.
- Do not introduce saloon gunfight, resistance, clue matching, automatic deductions, or new player-facing behavior beyond requiring explicit identity selection.
- Update product version only in `Directory.Build.props`, from `0.1.0-dev.83` to `0.1.0-dev.84`; generated mirrors remain build-owned.
- This worktree owns the plan commit, successor-plan retirement, version increment, Draft PR to `develop`, exact-head hosted gate, merge, develop-push gate, primary-checkout fast-forward, and verified cleanup.
- Keep `.agents/plans/2026-10-11-store-offers-route-ownership.md` until the first substantive implementation commit retires it with this plan's delivery.

## Review Focus

- A real `GameSessionProvider` scenario must fail before implementation when session Query data gains a wanted poster and the player has not selected it; the combobox remains blank and confrontation stays disabled.
- Explicitly selecting a currently known poster enables the existing action; removing that poster from the session resource clears the stale selection and disables the action.
- Reset/archive continue to clear selection; poster reads update displays through the session Query rather than local mirrored writes.
- ADR-0045 supersedes ADR-0044 without rewriting either record's historical decision; the spec, roadmap, ADR catalogue and test follow-up consistently state the shared accessible map contract.
- State why `docs/features.md` is unchanged: PG-006 already requires the player to name the person; this corrects an implementation that implicitly chose one.

---

### Task 1: Prove explicit declaration through the production provider

**Files:**
- Modify: `src/WildBunch.Web/src/tests/SaloonPlace.test.tsx`
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionState.ts`
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionMutations.ts`
- Keep: `src/WildBunch.Web/src/state/GameSessionProvider.tsx`
- Keep: `src/WildBunch.Web/src/state/gameSessionQuery.ts`

- [x] Add a provider-backed behavior test that mounts `GameSessionProvider` and `SaloonPlace` with a real `QueryClient`, the existing mocked navigation boundary, and API-boundary mocks for session, actions and journal.
- [x] Start from session data with an active saloon person and no known posters, then update `gameSessionQueryKey(gameId)` with a session containing one poster; wait for the identity combobox and assert it remains on the blank option and the take-in action remains disabled.
- [x] Run the new test against the current code and confirm it fails because the provider's poster synchronization selects the first poster without player input.
- [x] In the same scenario, explicitly select the poster and assert the existing action becomes enabled; replace the Query session with one that no longer contains the chosen poster and assert the choice clears and the action disables.
- [x] Remove `wantedPosters` and `setWantedPosters` from `useGameSessionState`; remove its first-poster auto-selection effect.
- [x] Derive `wantedPosters` from `session?.wantedPosters ?? []` in `useCurrentGameSession`; remove effect-driven poster mirroring and setter writes from poster-read, reset and archive paths.
- [x] Keep the declared poster handle local and initially empty; clear it only when its poster disappears from the active session, on reset, or after confirmed archive.
- [x] Ensure confrontation requires a currently resolved selected poster rather than any stale nonempty handle; derive the displayed poster from the same session Query data.
- [x] Run `npm test -- src/tests/SaloonPlace.test.tsx src/tests/GameSessionProvider.test.tsx src/tests/SheriffPlace.test.tsx` and `npm run typecheck` from `src/WildBunch.Web`.

The test must exercise production provider/query ownership; the current `StatefulSaloon` context fake begins with an empty declaration and cannot detect the provider's automatic first-poster assignment. Keep its focused display/outcome assertion if the new provider test does not make it redundant.

### Task 2: Record the successor design and delivery, then retire the predecessor

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`
- Modify: `docs/decisions/README.md`
- Modify: `docs/decisions/ADR-0035-react-shell-with-phaser-as-renderer-input-adapter-for-playfield-surfaces.md`
- Modify: `docs/decisions/ADR-0039-seeded-game-setup-and-free-first-arrival.md`
- Modify: `docs/decisions/ADR-0044-pointer-operated-world-map-selection-for-0.1.0.md`
- Add: `docs/decisions/ADR-0045-accessible-town-selection-on-the-shared-world-map.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-11-store-offers-route-ownership.md`
- Keep: `.agents/plans/2026-10-11-explicit-saloon-identity-selection.md`

- [x] Record PR #268's reviewed source `91b52ca0c7af9b55ec1a4511e90dc4365c2428d9`, merge `0c7b47696bbb9f32a625c76459bef0709c641470`, matching tree `d802b3310d17a47a346607c750c11796beceff15`, exact-head gate `38126111561`, and develop-push gate `38126452305` in row 13.
- [x] Link this plan as the live row-13 work item, advance the product version to `0.1.0-dev.84`, and preserve only the remaining row-13 scope after the deliberate-identity behavior ships.
- [x] Record ADR-0045's accessible shared-map decision in the specification and row 15; mark ADR-0044 superseded, preserve its dated historical decision, update the ADR catalogue and surviving scope in ADR-0035/0039, and reconcile the map test follow-up.
- [x] Record that the same `WorldMapScene` already serves starting-town selection and travel with distinct legal eligible-town inputs; the earlier starting-town DOM button path was removed, travel has no separate keyboard town chooser, and Town Hub's keyboard building controls are a different interaction.
- [x] Retire `.agents/plans/2026-10-11-store-offers-route-ownership.md` only in the first substantive implementation commit after checking its complete scope against PR #268 and the current source.
- [x] Leave `docs/features.md` unchanged because the player's explicit declaration is already promised by PG-006; leave event, backend and API contracts unchanged.

Before the first implementation commit, format the changed web sources with `py -3 tools/run.py format --apply src/WildBunch.Web/src/hooks/useGameSessionState.ts src/WildBunch.Web/src/hooks/useCurrentGameSession.ts src/WildBunch.Web/src/hooks/useGameSessionMutations.ts src/WildBunch.Web/src/tests/SaloonPlace.test.tsx` and lint those same paths with `py -3 tools/run.py lint src/WildBunch.Web/src/hooks/useGameSessionState.ts src/WildBunch.Web/src/hooks/useCurrentGameSession.ts src/WildBunch.Web/src/hooks/useGameSessionMutations.ts src/WildBunch.Web/src/tests/SaloonPlace.test.tsx`. Stage source, tests, planning/decision updates, the sole version source and predecessor retirement together so the implementation PR carries its successor-artifact and development-version obligations.

**Acceptance:** wanted posters are derived from the canonical session Query, no identity is implied by their arrival, a deliberate current selection alone enables the existing saloon action, stale declarations cannot survive poster removal, row 13 records PR #268 and the remaining scope, ADR-0045 supersedes pointer-only ADR-0044, and `Directory.Build.props` is the only authored product-version source at `0.1.0-dev.84`.

### Task 3: Validate, review, publish and merge

- [x] Run the focused tests and typecheck from Task 1; the normal staged-candidate commit hook runs the complete fail-fast repository gate.
- [ ] Review the complete diff against the specification, this plan, ADR-0006/0016/0027/0035/0039/0045, the feature-matrix and decision-record playbooks, frontend standards, testing doctrine and scoped web unslop profile.
- [ ] Open a Draft PR targeting `develop`, verify the body and exact head, mark it ready to trigger the hosted canonical gate, and merge only after that exact-head gate passes.
- [ ] Record the resulting merge and both hosted runs in the next JIT successor plan/roadmap update.
- [ ] After merge, verify the develop-push gate, source/merge tree equality and primary-checkout fast-forward; remove only the verified clean worktree and branch.

## Out of Scope

- Implementing the keyboard/screen-reader world-map controls in this slice; roadmap row 15 owns that implementation after its declared predecessors.
- Changing saloon or server gameplay, event history, API contracts, feature scope or case deductions.
- Broad provider decomposition, cross-tab synchronization, multi-playthrough selection or generalized Query architecture.
- Resurrecting unrelated criminal features, NPC behavior, poster circulation, saloon gunfights or resistance mechanics.
