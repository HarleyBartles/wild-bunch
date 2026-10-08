# ADR-0036 Developer Overrides Must Stay Outside Player Command Contracts

## Status

`partially superseded`

## Dated History

- `2026-07-10` - Chose to keep developer override parameters out of player commands and let the backend own override validation and consumption.
- `2026-10-08` - Partially superseded the prepped-snapshot player start path through ADR-0039. The player starts with Go, then reads the prologue and makes one free first arrival; a snapshot-only preparation path does not establish that game.
- `2026-10-08` - The manual town-layout salt override, its exclusive prepped-start path and the session RNG lock/clear mutations were retired from the 0.1.0 candidate. The separation of developer controls from player command contracts remains.

## Decision Type

`architecture`, `security`

## Related ADRs

- `depends on`: ADR-0028, ADR-0030
- `partially superseded by`: ADR-0039
- `related to`: ADR-0031, ADR-0032, ADR-0041

## Context

The developer workflow sought to apply test controls such as generation overrides without adding developer parameters to ordinary player commands. Its original three-phase preparation flow used a prepared snapshot before the developer override and subsequent action.

## Decision

Developer overrides do not become player-supplied command parameters. Their interpretation and consumption belong to a developer-enabled backend path, guarded and separated as described by ADR-0030 and ADR-0041. A developer preparation mechanism does not define or replace the player game-start lifecycle.

The original prepped-snapshot flow was not an alternate player start. ADR-0039 requires player Go to settle world and case truth before the prologue, followed by one free arrival choice. The separate developer prepped-state implementation was retired from the 0.1.0 candidate on 2026-10-08; no current prepped-state path establishes ordinary event replay. Any future developer setup remains distinct from the player setup lifecycle.

## Rationale and Alternatives

Putting test overrides in normal command contracts would expose development controls to every client and couple gameplay commands to test intent. Server-owned developer controls preserve the player contract while allowing a controlled environment to supply overrides.

## Consequences

Developer control work must preserve ordinary player command semantics and must not claim replay compliance unless the event stream can reconstruct the complete resulting state. The historical preparation details and component recipes are not a reusable player-flow contract.

## Successors and Surviving Scope

ADR-0039 supersedes the use of snapshot-only preparation as the player setup/start flow. The separation of developer overrides from player commands and backend ownership of their use remain.
