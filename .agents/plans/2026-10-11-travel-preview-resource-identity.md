# Keep travel previews bound to the current ride

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** A travel confirmation only presents and submits a preview produced for the active game, current origin, selected eligible destination, and current game-session snapshot.

**Architecture:** TanStack Query owns preview reads. The selected destination is local player intent bound to its game and origin. A preview query key includes those identities and the session query's `dataUpdatedAt` freshness token, so a session refresh cannot reuse a preview computed from older resources or travel mode. Accept only successful previews whose origin and destination match the query identity. Keep backend travel legality authoritative.

**Tech Stack:** React, TypeScript, TanStack Query, Vitest, React Testing Library.

**Specification:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, Workstream E.

**Investigation:** `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, WB-04; `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`, World-map selection and travel preview.

**Decision records:** Preserve ADR-0035 and ADR-0039: the shared world map remains one renderer/resource with travel owning eligible destinations and confirmation. No durable decision, endpoint, event, or backend legality changes in this plan.

**Execution Strategy:** Native inline execution. The change couples one travel selection, its query identity, confirmation rendering, and one production-provider test scenario; one implementer can keep that complete boundary coherent, followed by an independent whole-branch review.

## Global Constraints

- Do not retain preview data in component state or copy it into another client authority.
- Keep selection scoped to the game and origin that offered the destination; if the identity changes, stale confirmation is unavailable and only a destination still legal from the new origin may be previewed again.
- A session query update refreshes the preview identity even when game, origin, and destination IDs remain unchanged, because inventory and travel mode affect the preview.
- Do not display a cached or late preview unless its origin and destination match the active selection.
- Show pending, domain-rejected, and transport-failure states at the travel interaction; allow the player to return to destination selection or retry a failed read.
- Enable travel only when the active query has a successful, matching preview. The server remains authoritative for the eventual travel command.
- Preserve the existing single shared map and its current pointer behavior; keyboard town selection is ADR-0045 work in roadmap row 15.
- Do not expand transport-wide cancellation/error abstractions, change travel rules, or redesign the map.
- Advance the product version only in `Directory.Build.props`, from `0.1.0-dev.84` to `0.1.0-dev.85`; generated mirrors remain build-owned.
- This worktree owns the plan commit, completed-plan retirement, version advancement, Draft PR to `develop`, exact-head hosted gate, merge, develop-push gate, primary-checkout fast-forward, and verified cleanup.
- Retain `.agents/plans/2026-10-11-explicit-saloon-identity-selection.md` through this plan's plan-only commit; retire it in this plan's first substantive implementation commit after verifying its shipped scope against PR #269.

## Review Focus

- Select destination A, return to the map, then select B; resolve A after B. The confirmation must remain bound to B and its preview.
- Update the active session while the destination ID stays the same. The old preview must not authorize travel or remain displayed as current while the fresh preview is pending.
- Change current town while a preview is pending. The old origin's result must not appear or enable a trip from the new origin.
- Return an unsuccessful preview or reject the HTTP request. The player must see the failure, have a retry/back path, and be unable to start travel.
- A response whose origin/destination contradicts the request must never enable travel.

---

### Task 1: Bind travel selection and preview to the live session

**Files:**
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Modify: `src/WildBunch.Web/src/flow/TravelPrepSurface.tsx`
- Add: `src/WildBunch.Web/src/state/travelPreviewQuery.ts`
- Modify: `src/WildBunch.Web/src/tests/TravelPrepSurface.test.tsx`

- [x] Add `sessionUpdatedAt` to the session hook's returned read contract from the subscribed `sessionQuery.dataUpdatedAt`; treat it only as client cache freshness metadata, never as domain state.
- [x] Add a named travel-preview query key/options owner keyed by game ID, current origin town ID, destination town ID, and `sessionUpdatedAt`; keep retries disabled under the existing UI policy.
- [x] Replace `TravelPrepSurface`'s effect-driven `preview` state and cancellation boolean with the keyed query. Bind selected-destination intent to game and origin identity, and refuse to render confirmation when it no longer matches the live context.
- [x] Check that the selected destination is still connected from the current town before using it. If game/origin changes, clear or make the old selection unavailable; do not render an old confirmation during the reconciliation render.
- [x] Accept a preview only when the query result reports success, contains a preview, and both `originTownId` and `destinationTownId` match the active query. Keep an unsuccessful domain result's message available to the player.
- [x] Render a useful pending state, a retryable transport-error state, and an unsuccessful/mismatched-preview state with a back path to destination selection. Do not route preview errors through unrelated global notices.
- [x] Keep the Start the ride command unavailable until the current query has a valid matching preview; preserve server-side command authority and existing pending behavior.
- [x] Add a meaningful provider-backed RED/GREEN test for out-of-order A/B responses and another for a session update while the destination ID stays the same. The latter must prove the previous preview is unavailable until the refreshed preview resolves.
- [x] Add behavioral coverage for a current-town change during an in-flight preview, an unsuccessful preview response, a transport rejection with retry, and a mismatched-origin/destination response. Assert no travel command is sent from invalid preview states.
- [x] Assert a successful matching preview enables the existing action and submits exactly the selected current destination.
- [x] Prove the new race test fails against the effect/local-state implementation, then restore the implementation and show it passes.
- [x] Update the web test follow-up with a dated row-13 disposition describing the concrete behavior and evidence; do not convert the investigation into a development receipt.
- [x] Retire `.agents/plans/2026-10-11-explicit-saloon-identity-selection.md` in this first substantive commit after confirming the plan's scope shipped in merged PR #269; keep this plan live.
- [x] Update roadmap row 13 with PR #269 reviewed source `98f0169723793697086827163367aece8fae03c2`, merge `57b1f48929307f223611bed24a3a777b612253a1`, matching tree `dfc8706a71b3e44854a022dc3f06d19dedda96c7`, exact-head gate `38128754214`, develop-push gate `38129072476`, and this plan as the active slice.
- [x] Advance `Directory.Build.props` from `0.1.0-dev.84` to `0.1.0-dev.85` and record the roadmap's next development version.
- [x] Run focused `TravelPrepSurface` tests and web typecheck; then run the full `py -3 tools/run.py ci --check` gate on the committed implementation state.
- [ ] Request an independent whole-branch review against the plan, feature matrix, ADRs, unslop profiles, frontend standards and tests; resolve findings and re-review any changed diff.
- [ ] Open a Draft PR targeting `develop`, verify the body and exact head, mark ready, and merge only after the exact-head hosted canonical gate passes.
- [ ] In the next JIT successor plan/roadmap update, record this PR's reviewed source, merge/tree and both hosted gate runs.
- [ ] After merge, verify the develop-push gate, source/merge tree equality and primary-checkout fast-forward; remove only this verified clean worktree and branch.

## Out of Scope

- Changing server-side travel legality, preview calculations, journey events, travel rules, or API shape.
- Building a generalized cancellation framework or changing `requestJson` error policy.
- Adding a second map, a town list, keyboard map selection, or a Phaser framework.
- Reworking unrelated pending/error handling in store, startup, overlay, journal, casebook, or developer surfaces.
- Broadly decomposing `GameSessionProvider` or reorganizing the React folder tree.
