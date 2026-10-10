# Move HUD query orchestration behind the Application read boundary

## Goal

Move the player-facing HUD query out of the API transport layer and into an Application query handler with an explicit read-side port. Preserve current HTTP behavior and derive the HUD from the persisted event stream without loading the mutable command aggregate.

## Parent and delivery

- Roadmap: [Stable 0.1.0 cleanup, row 08](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md)
- Parent: `8bcf9991abc8ce9fe5055a50a4f6e2c8de68662b` (`0.1.0-dev.62`), merged PR #247.
- This plan is implementation slice `0.1.0-dev.63`; `Directory.Build.props` remains the single version authority.
- Use a fresh worktree from latest `origin/develop`, commit this plan before implementation, open a PR to `develop`, require the exact-head hosted CI gate to pass before merge, then verify the develop-push gate.
- In the first substantive commit, retire the fully delivered `.62` plan and update the row 08 roadmap with its delivery evidence and this active plan.

## Current behavior and intended contract

`ProjectionEndpoints.GetHudProjectionAsync` currently loads a `GameSession` through `IGameSessionRepository`, separately reads its full event stream, checks whether `GameStarted` exists, and invokes `HudProjector` in the API. The API therefore owns query orchestration and uses the command aggregate repository for a player read.

The route contract remains: an unknown session returns 404; an existing setup-phase session without `GameStarted` returns 204 with no body; a started session returns 200 with the same safe `HudProjection`, including the requested session id. Projection output remains event-derived. Reads must not append events, alter the aggregate, repair/write caches, or expose raw events. Command handlers may continue using `HudProjector` as they do today.

## Implementation tasks

1. **Confirm the boundary and tests.** Re-read ADR-0028 and the backend architecture guidance against this diff. Inspect the existing `IGameSessionReadRepository`, `GameSessionReadStoreLoader`, event payload loading funnel, `HudProjector`, API DI, and HUD endpoint integration tests. Keep the persisted payload loader as the only event decoding/version-upcasting funnel. Do not change gameplay rules, event contracts, schemas, migrations, command response projection, or the frontend. If implementation inspection disproves the read-port approach below, revise this plan before source edits.
2. **Introduce the Application query contract.** Add a read-only HUD query handler and an Application-owned read port that can distinguish an absent session from an existing session with no `GameStarted` event while providing the typed, ordered event stream for projection. The query handler maps absent session to the existing `GameSessionNotFoundException`, returns no projection before `GameStarted`, and otherwise uses the existing `HudProjector` and requested session id. The port must not expose aggregate mutation or a persistence implementation to Application.
3. **Implement the persistence adapter.** Provide the port from Persistence using the existing persisted event decoding/upcasting path and an existence check that preserves the null-versus-empty distinction. Avoid loading a `GameSession` aggregate for the HUD query and avoid duplicated serializers or bypasses around `PersistedPayloadLoader`. Register the adapter and handler through existing composition roots.
4. **Make the API a transport adapter.** Replace direct repository/projector orchestration with the Application handler. Map its absent-session exception to 404, absent pre-start projection to 204, and populated projection to 200. Keep route, response shape, and status behavior unchanged.
5. **Prove behavior at both boundaries.** Add Application behavior tests for missing session, existing stream without `GameStarted`, and a started stream projecting the expected HUD and session id. Prove the query does not store or commit and does not change authoritative session facts/history. Retain and run the PostgreSQL-backed HTTP tests for 404/204/200 and serialized response behavior; do not add source-shape or endpoint implementation tests. Prove any new test can fail for the behavior it claims to protect.
6. **Review durable truth and validate.** Compare the actual diff with ADR-0028 and event-sourcing doctrine; state in the PR whether the existing decisions remain accurate. No ADR change is expected if the query remains event-derived and read-only. Run focused Application and PostgreSQL-backed projection tests, then `py -3 tools/run.py ci --check` after ensuring PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`. Confirm no migration was generated. Obtain a fresh whole-branch review, resolve findings, rerun affected checks, and publish only after the PR gate is green.
7. **Close out.** Record exact source head, merge commit, hosted exact-head CI run, and develop-push CI run in row 08. Keep this plan until the next substantive successor slice semantically assesses and retires it.

## Acceptance

- HUD query orchestration belongs to Application; API only validates/maps transport behavior.
- Unknown, setup-phase, and started-session HTTP results remain 404, 204 without a body, and 200 with the same event-derived HUD respectively.
- Application uses a read-only port; the HUD route does not load or mutate a command aggregate or persist changes.
- Event decoding uses the existing version-check/upcasting funnel. No new event, DTO, schema, migration, or gameplay behavior is introduced.
- Behavioral tests prove the query branches and read-only behavior; PostgreSQL-backed route tests and the full repository gate pass.
- ADR-0028 remains truthful, hosted exact-head CI and the develop-push gate pass, and the roadmap contains verified delivery evidence.

## Explicit exclusions

Do not redesign HUD fields or projection semantics; merge the HUD into the journal or game-session response; replace the event-derived projector with aggregate/snapshot state; change command-side HUD projection; add SignalR, caching, or writeback; or broaden this into a general query-port migration.
