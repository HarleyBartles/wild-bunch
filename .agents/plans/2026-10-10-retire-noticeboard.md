# Retire the unbuilt noticeboard and fake sheriff records

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Remove the thin noticeboard and fake sheriff-records feature from stable 0.1.0 while preserving wanted-poster reading in the Sheriff Office.

**Architecture:** The player-facing sheriff interaction is wanted posters, read in the Sheriff Office; one poster read reveals one clue. There is no second noticeboard, public-square or private-records interaction in this release. Remove the noticeboard and local-record clue sources, authored public-notice clue, action/API/UI paths, and tests that protect those shells. Keep the existing `ReadWantedPosters` Domain/Application/API/Web flow and its truthful Sheriff Office context. Do not add a Town Square context or relocate the removed clue onto wanted posters. `InvestigationPerformed` keeps its current single-clue shape; no event upcaster or compatibility reader is needed for retired pre-alpha playtest data.

**Data boundary:** The baseline specification already settles that pre-0.1.0 playthroughs are disposable and schema migration history must remain intact. Inspect the existing forward migration delivered for row 10 and verify it invalidates incompatible pre-alpha event histories before application replay. Reuse that migration coverage when sufficient; otherwise extend the established versioned migration to discard disposable pre-0.1.0 playthrough data and its dependent events/caches. Preserve the database schema, applied migration history and unrelated data. Do not retain obsolete source/event replay support to save these playtests, and do not drop or rebuild the database.

**Specification and row:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, PG-005; `docs/features.md`, PG-005; roadmap row 11. This is the first row 11 slice; later row 11 work repairs saloon observation/take-in and culprit release.

**Execution strategy:** Inline, as one removal slice across Domain, generated content, Application, HTTP, persistence migration, browser and their behavior tests. Inspect current source and tests JIT, write a failing behavioral proof first, then remove the unsupported path without changing wanted-poster behavior.

## Constraints

- Keep wanted posters in the Sheriff Office and preserve the existing one-poster/one-clue interaction and player-facing result.
- Retire `InspectNoticeBoard`, `CheckSheriffRecords` and the `NoticeBoard`/`LocalRecords` clue sources where they exist only for those interactions; remove the authored public-notice clue from new games.
- Do not create or imply a Town Square context, move public notices to wanted posters, or introduce another clue source to compensate for the removal.
- Do not change the singular clue event contract, add event upcasters, or retain removed enum/event behavior solely for disposable pre-0.1.0 playtests.
- Apply the established forward-migration policy before removing any persisted type required to deserialize old sessions; prove fresh sessions and the retained poster flow still work after migration.
- Leave saloon gossip, saloon observation, prologue knowledge, telegraph exclusion, poster circulation and all later row 11 gameplay decisions unchanged.
- Do not add private records, new clues, automatic deductions, time/cost rules or unrelated gameplay.

## Task 1: Retire Domain, generated content and HTTP paths

**Files:** Modify the current Domain investigation/action/source owners, generated new-game case builder, Application registrations/handlers, API route mapping, feature matrix and Sheriff Office asset guidance; update Domain, GameContent, Application, integration and Web tests at their existing behavior owners.

- [x] Add the intended negative assertion to the actual available-actions API behavior test and run it first; the RED must show that a valid current game offers `InspectNoticeBoard` or `CheckSheriffRecords`.
- [x] Remove the `InspectNoticeBoard` and `CheckSheriffRecords` Domain interactions and their action availability, source-spent rules, exclusive results, commands, handlers and routes.
- [x] Remove `NoticeBoard` and `LocalRecords` source definitions when no retained behavior consumes them; remove the authored public-notice clue from newly generated cases.
- [x] Remove the unused roster clue-source tags that described the retired noticeboard and fake records surfaces; do not preserve dead source metadata.
- [x] Preserve `ReadWantedPosters` in the Sheriff Office, its one-clue discovery behavior, source provenance, command, route, event and player result. Keep `InvestigationPerformed` at its current single-clue shape.
- [x] With a valid persisted game, prove the retired HTTP operations cannot append events or alter reads. Prove the retained poster action still surfaces one clue and its existing player-safe result.
- [x] Verify the row 10 `RetirePreAlphaRiflePlaythroughs` forward migration deletes disposable pre-0.1.0 sessions and dependent rows before replay; reuse its existing populated PostgreSQL coverage if it meets this contract. Do not add a noticeboard-specific migration, compatibility reader or legacy fixture if this migration already invalidates those sessions.
- [x] Add a dated clarification to ADR-0010 because its context still describes public notices as current; preserve its planned lawman evidence decision and record the 0.1.0 removal without rewriting history.
- [x] Update PG-005 in `docs/features.md` from planned removal to the delivered 0.1.0 boundary, and align Sheriff Office asset guidance with wanted posters rather than a separate noticeboard cue.
- [x] Demonstrate a generated game retains prologue, saloon gossip, observation and wanted-poster paths without the authored public-notice clue. Do not add tests that assert declarations are absent; test the available actions, route behavior, generated player knowledge and persisted results.

## Task 2: Retire browser controls and source copy

**Files:** Modify the town/sheriff surface, API client, TanStack Query mutations and affected Web behavior tests.

- [x] Add the player-surface assertion first and witness RED: the Sheriff Office shows wanted posters alongside the unsupported local-record action/content.
- [x] Remove noticeboard and sheriff-record controls, copy, mutation paths and unavailable-action labels from the player UI.
- [x] Keep wanted posters available only through the Sheriff Office interaction and preserve its pending, error and affected-read refresh behavior.
- [x] Exercise the routed Sheriff Office poster journey and prove a player cannot see or invoke a noticeboard/records interaction.

## Task 3: Review and deliver

- [x] Confirm migration runs before application replay can encounter removed pre-alpha source values; where its existing PostgreSQL behavioral test does not prove an essential part of the settled data boundary, strengthen that behavior test before changing migration code.
- [x] Review the complete branch against PG-005, the history/migration boundary, relevant ADRs, event-sourcing doctrine, unslop profiles and changed-concern/code-review guidance.
- [x] Confirm wanted-poster behavior remains in the Sheriff Office; the noticeboard, fake records and authored public-notice clue are gone from new play; removed pre-alpha history has no compatibility machinery; no unrelated knowledge source changed.
- [x] Advance the single authored application version to `0.1.0-dev.72` for this merged PR; verify generated web identity and the complete canonical gate against that final candidate.
- [x] Run an independent whole-branch review. Resolve substantive findings with meaningful behavior proof and re-review the corrected diff.
- [ ] Commit with the check-only hook, publish a PR to `develop`, verify exact-head hosted canonical CI success and merge under the active goal.
- [ ] In the next fresh worktree, record PR/source/merge/exact-head/develop-gate evidence and retire this `.72` plan after checking its complete scope.

## Acceptance

- A player can read one wanted-poster clue in the Sheriff Office through the existing flow.
- Stable 0.1.0 exposes no noticeboard, public-square notice or fake sheriff-records interaction, and new games generate no public-notice clue.
- Event sourcing retains its existing single-clue event shape; disposable pre-0.1.0 sessions are invalidated by the established explicit migration policy rather than preserved through legacy replay code.
- Migration leaves the schema and applied migration history intact and permits fresh sessions; the retained poster path works after migration.
- Local and exact-head hosted canonical gates pass before merge; the successor records the develop push gate.

## Explicit exclusions

No Town Square context, public notices, private sheriff records, new clue sources, newspaper, telegraph, lawman, automatic deduction, saloon encounter, named take-in, fine, bounty, culprit release, inventory, travel or infrastructure feature.
