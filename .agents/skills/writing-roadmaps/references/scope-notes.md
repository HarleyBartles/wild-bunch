# Scope Notes

Use when the main `writing-roadmaps` guidance does not cleanly cover the case in front of you.

## When a roadmap item should become a new epic

A single plan may grow until it contains multiple independent subsystems. If the current plan is already too large for `writing-plans`, do not force it. Instead:

- Extract the oversized or independent subsystem into a new epic.
- Add it to the roadmap as a pending future plan.
- Leave the current epic focused on the original goal.
- Hand off the new epic to `brainstorming` or `writing-plans` when it becomes the active plan.

## How to handle scope changes that invalidate multiple pending plans

When a decision changes the path for several roadmap items:

- Do not edit every pending plan at once.
- Update the roadmap table with the new path and mark the affected plans as `blocked` or `replan`.
- Re-write the next plan only; leave the others as placeholders.
- Document the change in `Handoff Notes` so the next agent understands why the roadmap shifted.

## When to ask the human a focused question versus escalating through risk-gates

If the next step depends on an unresolved assumption or a value judgement:

- Ask the human one focused question when the answer is a preference, business call, or missing fact.
- Use `risk-gates` when the proposed action could violate scope, authority, canon, or safety.
- Do not ask the human a question that is really a safety/authority decision in disguise. Route those to `risk-gates`.
