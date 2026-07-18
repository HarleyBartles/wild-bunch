# Architecture Profile

Portable anti-slop profile for pattern worship and abstract design language.

## Purpose

Catch pattern worship and abstract design language. Push agents to name ownership, runtime boundaries, data flow, tradeoffs, constraints, and decision evidence.

## Where to Use

Before writing architecture proposals, ADRs, system designs, decomposition plans, integration strategies, refactor designs, or architecture reviews.

## Slop Patterns to Avoid

- Pattern-first decisions: CQRS, event sourcing, microservices, clean architecture, hexagonal architecture, or abstraction layers used because they sound mature
- Scalability/maintainability/future-proof claims without load, ownership, lifecycle, or failure-mode evidence
- Boxes-and-arrows without authority boundaries, runtime behavior, persistence, or operational cost
- Abstractions created to preserve optionality without actual variation
- Designs that do not name tradeoffs or rejected alternatives

## Required Avoid Rules

Prohibit pattern names as justification. Catch `scalable`, `maintainable`, `future-proof`, `clean`, `reusable`, and `separation of concerns` when they are not tied to concrete constraints.

Reject architecture plans that omit ownership, runtime boundaries, persistence model, data flow, failure modes, and tradeoffs when those are material.

## Required Prefer-Instead Rules

- Start from the current pressure or requirement, not a named pattern
- Name components, owners, data flow, state boundaries, sync/async behavior, and failure behavior
- Explain why simpler designs are insufficient before adding architecture
- List tradeoffs and rejected alternatives
- Keep source code authority and runtime truth clear

## False Positives / Do Not Overapply

Do not ban architecture patterns. Ban patterns used as decoration or shortcut reasoning. A pattern is valid when the profile states the specific pressure it solves.

## Examples

### Before (Avoid)
> Use CQRS and event sourcing to make the system scalable.

### After (Prefer)
> Current pressure: read queries are slow due to complex joins. Proposed solution: separate read and write models. Read model: denormalized materialized view updated via events. Tradeoffs: increased complexity, eventual consistency. Rejected alternative: database caching (insufficient for query complexity).

### Before (Avoid)
> Split this into microservices for better separation of concerns.

### After (Prefer)
> Current pressure: team needs independent deployment cycles. Proposed solution: split into User Service (auth, profiles) and Order Service (orders, payments). Communication: synchronous HTTP for user data, async events for order updates. Tradeoffs: network latency, distributed transactions. Rejected alternative: monolith with feature flags (deployment coupling remains).

## Acceptance Checks

This profile is acceptable only if it would force a design from pattern-name justification into constraint-backed architecture reasoning.
