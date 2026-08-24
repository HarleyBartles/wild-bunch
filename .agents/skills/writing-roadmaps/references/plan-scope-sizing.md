# Plan Scope Sizing

Use when a `writing-plans` or `writing-roadmaps` session feels large, overwhelming, or "huge".

## The three escape hatches

A plan is too large when it no longer fits in your context as a single execution unit. If you find yourself thinking "this is a lot", "this is huge", or "this plan is too big", use one of these escape hatches in this order.

### 1. Well-sliced but long plan

The plan has many tasks, but each task is self-contained, has its own test cycle, and fits in a single review.

- This is correct. Do not shrink the plan to make it shorter.
- The length is not the problem; the slice quality is what matters.
- Hand the plan to `subagent-driven-development` and execute one task at a time.
- If the tasks are tightly coupled and not independent, see hatch 2.

### 2. Cross-concern plan

The plan covers multiple independent subsystems, concerns, or code boundaries that could be reviewed and delivered separately.

- Stop writing the current plan.
- Invoke `writing-roadmaps` and build a sequenced roadmap.
- Write Plan 1 from the first concern, and leave the others as pending plans.
- This is for legitimately cross-concern work, not for making tiny plans.

### 3. Oversized plan inside an epic

The plan is already part of an epic, but while writing it you discover it is too big for one deliverable.

- Stop writing the current plan.
- Split the remaining scope into a new plan.
- Update the epic roadmap with the new plan, its place in the sequence, and a note explaining the split.
- This is a fallback for legitimately over-scoped plans, not a license to create endless epics of tiny plans.

## What "huge" does not mean

- A large number of well-scoped tasks is not huge. It is the expected shape for `subagent-driven-development`.
- A long plan with every task testable on its own is not a bug.
- The right response to a well-sliced large plan is execution, not decomposition.
