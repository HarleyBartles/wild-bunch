# Consolidate public board knowledge

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** One public noticeboard interaction surfaces wanted posters and public notices. Remove the fake sheriff-records interaction and the duplicate poster-reading action without granting access to private records. Preserve event-sourced historical truth while new games use the public-board source.

**Architecture:** The Domain owns which public knowledge a player learns and records that fact through `InvestigationPerformed`. Application handlers, API routes and browser controls expose that one interaction. Current setup content marks public notices as `NoticeBoard`; wanted-poster clues retain their truthful poster provenance. Legacy `LocalRecords` and `SheriffWarrants` source enum values and old events remain readable for replay, but new board interactions record `NoticeBoard`. Do not rewrite or upcast old events to claim the player used a different source. No database migration is needed while the legacy enum values remain valid.

**Spec and row:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, PG-005; `docs/features.md`, PG-005; roadmap row 11. The preceding row 10 boundary is complete at merge `f87fa3581ebc885b329d10e8e320953f02121b58`.

**Execution strategy:** Native inline execution. The interaction combines clue/warrant selection, town source state, an event, poster and journal reads, an API result, and browser navigation. Keep the transition coherent and validate its real persisted result instead of splitting into temporary public interfaces.

## Constraints

- One interaction at a public noticeboard surfaces current wanted posters and public notices. There is no distinct sheriff-records interaction, private case-record access, or second poster-reading command.
- Preserve the established event-owned clue/warrant discovery and town-visit behavior. A board read must not invent identity knowledge or bypass source eligibility.
- New public notice clues use `InvestigationSourceKind.NoticeBoard`. Preserve numeric legacy values and replay behavior for `LocalRecords` and `SheriffWarrants`; never emit `LocalRecords` for a new board read and never rewrite old event facts.
- Keep saloon gossip, saloon observation, prologue knowledge and the disabled telegraph behavior unchanged.
- Do not add automatic culprit deductions, new clues, sources, content, or time/cost rules. Preserve the existing eligible poster and clue resolution policy unless a defect is demonstrated by the actual player path.
- Do not expand named take-in, citizen/wanted settlement or culprit-release behavior in this slice. Those remain row 11 follow-ups.
- No new ADR is expected: this implements the already-settled public-board product boundary. Verify the applicable ADRs remain truthful.

## Review focus

Confirm a player can learn the authored public notice and wanted information through one ordinary board interaction, and the recorded event and independently reloaded journal/poster reads agree. Confirm a player cannot invoke a sheriff-records interaction or a duplicate wanted-poster interaction through available actions, API or Web controls. Confirm historical `LocalRecords` and `SheriffWarrants` events still deserialize and replay with their original meanings, current source selection remains legal, and no private case-file state is exposed.

---

### Task 1: Commit the successor plan and retire row 10

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `docs/features.md`
- Delete: `.agents/plans/2026-10-10-retire-rifle-shell.md`
- Create: `.agents/plans/2026-10-10-public-noticeboard.md`
- Modify: `Directory.Build.props`

**Interfaces:**
- Consumes: `origin/develop` merge `f87fa3581ebc885b329d10e8e320953f02121b58`; PR #256 source `0955c07c12a71c4144dfdf4fa2cbb09ec09f6135`; exact-head gate `38084909604`; develop push gate `38085225926`.
- Produces: row 10 marked complete with PR/source/merge/review/gate evidence; row 11's first JIT slice and public-board rule; version `0.1.0-dev.71`.

- [ ] Record the completed row 10 slices and verified PR #256 gates, then retire the `.70` plan under completed-artifact custody.
- [ ] Record the single public-board interaction and legacy-event preservation rule in the specification and feature promise. Keep the feature assessment truthful until implementation and exact-head evidence exist.
- [ ] Link this plan as the first row 11 slice, keep row 11 open for saloon observation/take-in and culprit release, and advance the authored version once to `0.1.0-dev.71`.

### Task 2: Establish the board behavior at the Domain owner

**Files:**
- Modify: `src/WildBunch.Domain/Game/InvestigationLoop.cs`, `GameSession.cs`, `InvestigationContext.cs` or the current context owner, `Actions/AvailableActionKind.cs`, `Actions/InvestigationSources.cs`, `World/TownSourceModels.cs`, and `Cases/InvestigationSourceKind.cs` only if required by the settled compatibility boundary.
- Modify: `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`.
- Modify: `tests/WildBunch.Domain.Tests/GameSessionInvestigationActionsTests.cs`, `GameSessionWantedPostersTests.cs`, and the existing investigation event/replay behavior owner.
- Modify: `tests/WildBunch.GameContent.Tests` at the existing generated-case behavior owner.

**Interfaces:**
- Consumes: source-owned public clues, warrant/poster resolution, town visit state and legacy event values.
- Produces: one aggregate board-read transition that records only the public knowledge actually surfaced.

- [ ] First write or strengthen a behavioral test showing the current board and sheriff-record reads split a player-visible public notice from the wanted-board result; witness the expected red failure before correcting production behavior.
- [ ] Make the single board transition resolve the eligible wanted poster and public notice under the existing surfacing rules and record one coherent `InvestigationPerformed` fact. Avoid double action cost/turn advancement.
- [ ] Generate public notices as `NoticeBoard` clues; keep wanted-poster provenance truthful and leave other investigation sources intact.
- [ ] Remove current Domain availability and command paths for `ReadWantedPosters` and `CheckSheriffRecords`. Keep old source enum values stable and ensure historical events still replay instead of remapping old facts.
- [ ] Prove one board read changes player-known knowledge through the event path, repeat behavior respects the board's visit rules, and old legacy source events remain reconstructable where an existing history test owner supports it.

### Task 3: Align Application and HTTP with the aggregate transition

**Files:**
- Delete or retire the now-unused `ReadWantedPosters` and `CheckSheriffRecords` Application commands/handlers and registrations.
- Modify: `src/WildBunch.Application/Games/Commands/InspectNoticeBoardHandler.cs`, relevant read mapping, `src/WildBunch.Api/Games/InvestigationEndpoints.cs`, and Application/API tests at the existing behavior owners.

**Interfaces:**
- Consumes: one Domain board-read result.
- Produces: one HTTP command path and truthful player-safe poster/journal reads.

- [ ] Make the existing public-board HTTP command return the current read model containing the newly surfaced public notice and wanted information.
- [ ] Remove the two duplicate read routes and handlers without creating endpoint-shape detectors; prove an ordinary board request works and removed operations cannot mutate state.
- [ ] Exercise a persisted read and independent subsequent session/journal/poster read; assert authored facts, source provenance, turn/effect count and no private record contents.

### Task 4: Present one public board interaction in the browser

**Files:**
- Modify: `src/WildBunch.Web/src/api/wildBunchApi.ts`, `api/types.ts`, `hooks/useGameSessionMutations.ts`, `hooks/useCurrentGameSession.ts`, the sheriff/public-board surface, and the existing town action surface as source inspection determines.
- Modify: affected Web tests and fixtures that currently expose `ReadWantedPosters` or `CheckSheriffRecords`.

**Interfaces:**
- Consumes: the single board interaction and its player-safe server reads.
- Produces: one visible public-board affordance; posters and notices are available from the authoritative response.

- [ ] Remove duplicate read controls and local-records labels from player surfaces while retaining the wanted poster and public notice content the player actually learned.
- [ ] Route the surviving interaction through TanStack Query, preserve pending/error feedback and refresh only affected reads.
- [ ] Test the player interaction through the real routed surface: one read reveals the expected board content, repeat/empty outcomes remain honest, and a transport failure remains recoverable.

### Task 5: Review and deliver

**Files:**
- Review: complete branch diff against PG-005, relevant ADRs, event-sourcing integrity doctrine, changed-concern and code-review profiles.
- Modify: this roadmap only in the next fresh worktree after merge.

**Interfaces:**
- Consumes: committed `.71` plan and one public-board command/read contract.
- Produces: reviewed, hosted-gate-green PR to `develop`; row 11 remains open for lawful saloon interaction work.

- [ ] Confirm no current code advertises a distinct local-records or wanted-poster read command, and no other knowledge source was changed accidentally.
- [ ] Verify historical enum/event values are preserved and exact replay retains original event source semantics; no migration or event rewrite is introduced without new evidence.
- [ ] Run focused Domain, GameContent, Application, API, PostgreSQL integration and Web behavior checks, verify version identity, then run `py -3 tools/run.py ci --check` on the final candidate.
- [ ] Run an independent whole-branch review. Resolve substantive findings with meaningful behavior proof and re-review the corrected diff.
- [ ] Commit with the check-only hook, publish a PR to `develop`, verify exact-head hosted canonical CI success, and merge under the active goal.
- [ ] In the next fresh worktree, record PR/source/merge/exact-head/develop-gate evidence and retire this `.71` plan after checking its complete scope.

## Acceptance

- One public-board interaction reveals current wanted posters and public notices; no separate sheriff-records or poster-reading action remains.
- New public notices are sourced from the public board, and events truthfully record the interaction and learned facts.
- Legacy LocalRecords and SheriffWarrants event/source numeric values remain readable and old streams replay without rewriting history.
- Journal, casebook and wanted-poster reads after reload agree with the event stream and expose no private case records.
- Local and exact-head hosted canonical gates pass before merge; the successor records the develop push gate.

## Explicit exclusions

No new clues, newspaper, telegraph, lawman, automatic deduction, private-record access, saloon encounter, named take-in, fine, bounty, culprit-release, inventory, travel or infrastructure feature.
