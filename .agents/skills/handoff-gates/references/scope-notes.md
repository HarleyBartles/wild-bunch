# Scope Notes

Use when the main `handoff-gates` lanes do not cleanly fit the artifact or the boundary is unclear.

## Plans with external blockers

A plan may depend on a third-party API, a pending user decision, an upstream release, or another external event.

- Separate the plan into **contained** tasks the agent can do now and **blocked** tasks that require the external thing.
- If the contained tasks form a meaningful, testable slice, rate it and hand it off. Leave the blocked tasks in the roadmap, not in this plan.
- If there is no meaningful contained slice, the plan is not at a stage boundary. Classify as `blocked`, do not hand off, and return to `writing-plans` or `writing-roadmaps`.

## Overlap with verification and review

`handoff-gates` is a stage-boundary readiness check, not a final verification or a review request.

- Use `handoff-gates completion-readiness` before `verification-before-completion` or `requesting-code-review`.
- Use `verification-before-completion` when you are about to claim the work is green and a fresh command can prove it.
- Use `requesting-code-review` to dispatch a subagent reviewer.
- Use `receiving-code-review` when the subagent reviewer returns feedback.
- If `completion-readiness` finds a likely defect, do not skip straight to `requesting-code-review`. Return to `finishing-a-development-branch` or `executing-plans` first.

## Return posture

When a boundary case applies, return:

- the final rating if it is at least 8, or `blocked`;
- the lane;
- the specific boundary exception that led to this reference;
- the next skill or stage the artifact should move to.
