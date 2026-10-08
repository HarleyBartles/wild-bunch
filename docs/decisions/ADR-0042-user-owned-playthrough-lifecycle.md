# ADR-0042 User-Owned Playthrough Lifecycle

## Status

`planned`

## Dated History

- `2026-10-08` - Planned immutable user ownership and a one-active-playthrough-per-user rule; confirmed start-over archives the current playthrough immediately and durably.

## Decision Type

`architecture`, `gameplay`, `persistence`, `security`

## Related ADRs

- `partially supersedes`: ADR-0034
- `related to`: ADR-0002, ADR-0028, ADR-0041

## Context

The current local playtest data has no retained historical ownership requirement. Public play requires a durable user marker to associate a person with a playthrough, while the existing player character remains part of the game rather than the account identity.

## Decision

Each playthrough has one immutable internal user owner established by event history. A user may have multiple playthroughs over time but at most one active playthrough at once. Confirming Start over immediately archives the current playthrough through its own event. That archival remains true if the user leaves setup or never creates a replacement; replacement creation is a separate action and must preserve the per-user active-playthrough rule. A user may resume the active playthrough after signing in again. The first identity provider is Google, associated by its stable provider subject.

## Rationale and Alternatives

An immutable internal user marker separates account identity from the cowboy character name and allows the server to resolve the user's playthrough after browser state or authentication expires. Immediate archival gives the confirmation a durable game effect instead of recording an ignored request.

## Consequences

Ownership must be reconstructed from event history and enforced on reads and writes. A browser-supplied game ID or user ID does not establish ownership. Existing unowned local playtests may be discarded under the separately approved data policy; migrations must not fabricate their historical owner. Concurrent creation and resumption must not permit more than one active playthrough for a user.

## Successors and Surviving Scope

This record partially supersedes ADR-0034's globally scoped one-active-playthrough description with a per-user rule and clarifies that confirmed archival is independent of replacement creation. ADR-0034's event-backed archival, retention of archived history, and terminal archived state remain authoritative. This is a planned cloud identity and lifecycle decision, not current account enforcement in the 0.1.0 application.
