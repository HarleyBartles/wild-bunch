# Design Guide

Use this reference when turning an idea into a repo-ready design spec for the Wild Bunch repo. This guide only adds repo-specific design and handoff rules. The general brainstorming workflow comes from `/brainstorming`.

## Before You Begin: Read the Standards

A design that ignores the repo's standards will produce specs that do not hand off cleanly. Read these before you start:

- **[`.agents/docs/coding-discipline.md`](../coding-discipline.md)** - scope discipline, architecture stack discipline, and refactoring boundaries.
- **[`.agents/docs/mesh-policy.md`](../mesh-policy.md)** - how AGENTS.md, README, and INDEX.md surfaces are supposed to work.
- **[`.agents/docs/workflow-policy.md`](../workflow-policy.md)** - fresh-main discipline, PR hygiene, and publication honesty.
- **[`.agents/docs/validation-policy.md`](../validation-policy.md)** - what kinds of validation the final implementation will need, so the design can anticipate them.

## Design Spec Expectations

The design spec is the durable record of the decision, not the implementation plan.

- Write the spec to `.agents/superpowers/specs/`.
- Keep it concrete enough that a planning agent can turn it into a task plan without inventing missing decisions.
- Include the real names, counts, file targets, and contract rules that define the work.
- Separate goals, scope, non-goals, contract, and validation.
- Call out any tradeoffs or intentionally deferred decisions explicitly.
- Keep the spec focused on the requested slice. If the idea is too large for one spec, split it before writing.
- Use the repo's existing vocabulary and file locations. The spec should not invent a new terminology layer when the repo already has one.
- Include only the additional repo-specific facts the planner will need, not the full text of the Superpowers brainstorming workflow.

## Spec Self-Review

After writing the spec, review it against these checks before handing it off:

1. **Placeholder scan** - remove any `TBD`, `TODO`, or vague shorthand.
2. **Internal consistency** - make sure the goals, scope, contracts, and validation all agree.
3. **Scope check** - confirm the spec is narrow enough for one implementation plan.
4. **Ambiguity check** - if a requirement could be interpreted two ways, make it explicit now.
5. **Source sanity** - verify the file paths, family names, and contract details against the live repo.
6. **Repo-only content check** - remove any generic brainstorming instructions that are already covered by `/brainstorming`.

If the spec fails any of those checks, fix it before proceeding.

## Handoff to Planning

Before handing the spec to a planning agent, assess whether it is already strong enough to avoid avoidable in-flight invention.

- Rate the spec's handoff confidence honestly on a 0-10 scale.
- If the confidence is below `8/10`, do not hand it off yet.
- Tighten the design, verify source facts, or close obvious gaps until the score reaches the floor or the remaining gap is clearly user-owned.
- If a gap materially changes scope, sequence, or file targets, surface it in the design instead of burying it for the planner.
- The planner should receive a spec that is both honest and as de-risked as the current source allows.
- If the spec is missing repository-specific contract details, keep editing it rather than hoping the planner will infer them.

When the spec is ready, hand it off with the key contract points the planner will need:

- exact file or family names
- counts or cardinalities
- path and naming rules
- validation expectations
- any explicit non-goals or out-of-scope items
- any repo-specific handoff gate or confidence floor that differs from the generic brainstorming workflow

## What a Design Spec Is Not

- A design spec is not an implementation plan.
- A design spec is not a commit log.
- A design spec is not permission to broaden the work beyond the asked slice.
- A design spec is not ready until it can hand off cleanly to planning without forcing the planner to invent the contract.


