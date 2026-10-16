# N-92 Qualify one entry across full and journey journal views

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Qualify that one recorded trail encounter appears as the same player-facing entry in the full Journal and the journey-focused Travel diary after a fresh page load.

**Architecture:** Preserve the existing event-derived player journal and its two reader views. Extend the existing production-browser hostile-encounter journey to compare the same message, day and turn across a fresh full-journal read and the restored travel session, then verify both rendered views. Keep travel-day resolution details as supporting structured information; do not create another narration owner or change gameplay behavior.

**Tech Stack:** React, TypeScript, Playwright, Vite production build, ASP.NET API and PostgreSQL-backed isolated browser test database.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, the PG-008 decision that one event-derived player journal has full-playthrough and journey-focused views; current promise and evidence in `docs/features.md#pg-008`.

**Execution Strategy:** `executing-plans` - this is one coupled browser-proof and evidence slice, and inline execution keeps the seeded encounter, API payload, two rendered readers and truthful PG-008 update in one context. `subagent-driven-development` would add per-task handoffs without independent implementation or review boundaries; use one fresh whole-branch review at publication.

## Global Constraints

- Read and apply the PG-008, ADR-0005, ADR-0007, ADR-0027 and ADR-0047 contracts; state in the PR whether any durable decision changed.
- The event stream remains the sole authority for what happened; React displays server-projected facts and does not create journal entries.
- The same entry is identified by its actual message, game day and turn in `JournalDto.logEntries` and `GameSessionDto.travelDiary.journeyEntries`.
- Keep the existing Travel diary surface and structured day facts; do not add features, a second journal model, new endpoints or alternate gameplay paths.
- Automated browser proof uses the browser harness's uniquely named disposable PostgreSQL database; do not reset or truncate `wildbunch_dev` for automated tests.
- Advance the sole authored product version in `Directory.Build.props` to `0.1.0-dev.210` for this PR; generated web identity must come from the build.
- Keep row 18 partial; this one route does not qualify all of PG-008, deployment, hosted player behavior or the full release candidate.

## Review Focus

- If the journey filter omits a real recorded encounter entry, the restored Travel diary must fail its visible-entry assertion while the full Journal still reads the same server-owned event.
- If page reopen leaves the browser on stale client data, the test must fail because the new full-journal and game-session reads do not contain the resolved entry.
- If opening either reader repeats gameplay, the test must fail because the encounter-resolution POST count changes from one.

---

### Task 1: Prove one resolved trail entry in both journal views

**Files:** Modify `src/WildBunch.Web/browser-tests/player-startup.spec.ts`, `docs/features.md`, `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, and `Directory.Build.props`; delete `.agents/plans/2026-10-16-row18-n91-casebook-capture-browser-qualification.md` in this first substantive implementation commit.

**Consumes:** The existing `a player can resolve a hostile trail encounter and restore its journal result` production-browser route, its deterministic multi-day journey fixture, the current `JournalDto.logEntries` and `TravelDiaryDto.journeyEntries` projections, and PG-008's one-journal/two-view contract.

**Produces:** A real-browser assertion that the same event-derived encounter entry survives page reopen in both the full Journal and the journey-focused Travel diary, truthful bounded PG-008 evidence, the N-91 successor-plan retirement, and product version `0.1.0-dev.210`.

- [x] Extend the existing hostile-encounter browser test's response shapes to include full `GameLogEntryDto` identity (`kind`, `message`, `day`, `turn`) in both `logEntries` and `travelDiary.journeyEntries`; after the real fight response, find the known resolution message and assert its exact record is present in both projections.
- [x] On page reopen, wait for a new successful `GET /api/games/{id}` and a new successful `GET /api/games/{id}/journal`; compare the fresh travel and journal responses to the one resolved record, not to React Query cache retained from before navigation.
- [x] Before opening the Journal overlay, assert the restored Travel diary visibly presents the resolution message and its existing structured encounter details, including the chosen fight and actual ammo spent; then open the full Journal and assert the same message is visible there. Close it and verify the read did not issue another encounter-resolution POST or travel-day POST.
- [x] Run the focused browser test and demonstrate it can fail for the reason it exists: temporarily suppress the actual `travelDiary.journeyEntries` passed to `JournalEntryTimeline` in `TravelDiaryNotebook`, run the focused test, and confirm the restored Travel diary assertion fails. Restore that component byte-for-byte and rerun the focused test successfully.
- [x] Run the manual browser check with this worktree's paired API and web servers using the same deterministic seed, Brutal difficulty, Boring randomness, longest connected trail and real hostile encounter route; after resolving the encounter, close/reopen the page and inspect both the Travel diary and full Journal. Record the actual URLs, prove the web build calls this worktree's API, and report console/network errors separately from the automated result. The development database may be restarted or its active playthrough replaced if needed; its playthroughs are the agent's own.
- [x] Add dated PG-008 evidence that names the exact full and journey view, event record and fresh-load behavior proven, and retains the limits on other entries, journeys, browsers, hosting and deployment. Keep the assessment partial.
- [x] In the roadmap, record N-91's PR #394 merge `b747b92ca9206fce4b6552a4bb5fe6678e1acf24`, source `130505a69e2b83c86b4457b372079cc0f58ab385`, passing exact-head gate run `38843716087`, passing develop-push gate run `38844330834`, independent review result and paired worktree/branch cleanup; make N-92 the active row-18 plan and record this bounded scope.
- [x] Set the authored product version to `0.1.0-dev.210`; compare the actual diff with ADR-0005, ADR-0007, ADR-0027 and ADR-0047 and preserve them unchanged if no durable decision changed.
- [x] Ensure PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`, build the production web app with `npm --prefix src/WildBunch.Web run build`, run `npm --prefix src/WildBunch.Web run test:browser -- browser-tests/player-startup.spec.ts --grep "hostile trail encounter and restore its journal result"`, perform the separate manual browser route, and pass the normal check-only commit hook for `py -3 tools/run.py ci --check`.

### Task 2: Review, publish, merge and clean N-92

**Files:** Review the committed N-92 diff, this plan, the row-18 roadmap entry, PG-008 and its evidence, the applicable ADRs, and the browser route.

**Consumes:** The focused red witness and passing restored-browser assertion, manual paired-server observation, bounded feature evidence, N-91 plan retirement and version `0.1.0-dev.210`.

**Produces:** Independent whole-branch review, a reviewed PR to `develop`, passing exact-head hosted CI and develop-push CI, a verified fast-forwarded primary checkout, and paired worktree/local/upstream-branch retirement.

- [ ] Prepare a fresh-context whole-branch review package from current `origin/develop` to `HEAD`, including this plan, the PG-008 and ADR comparison, the failed-behavior witness, automated and manual browser proof, and the canonical local hook result. Use the standing authorization for one independent reviewer without asking again; use gpt-6-luna. Resolve findings and repeat review after any correction.
- [ ] Follow the PR runbook, push the branch and open a Draft PR targeting `develop`; read back the PR body, exact head SHA, draft state and hosted checks. Mark it ready for review only after self-review and the committed local gate pass; do not merge before exact-head hosted CI succeeds.
- [ ] Merge the reviewed PR to `develop`; verify the develop-push gate passes on the exact merge commit, then fast-forward the primary checkout while preserving unrelated user changes.
- [ ] Use the finishing helper in check/apply mode, verify the PR head, merge result and clean worktree, then delete and verify absence of the exact local and upstream branch refs together with the worktree. Retain the N-92 plan through its completing PR for semantic retirement in the next substantive slice.

## Validation Boundaries

This browser route covers one real hostile-trail encounter entry in a fresh full-journal read and the active journey-focused Travel diary. It does not prove every entry category, multiple journey filtering, all casebook behavior, other browser engines, deployed behavior, hosted play or complete PG-008 qualification.
