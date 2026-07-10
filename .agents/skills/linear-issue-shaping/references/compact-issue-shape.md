# Compact Issue Shape

Use this reference when shaping a worker-ready Linear packet for repo or code execution.

## Issue body

- Keep the issue body short and readable through the connector.
- Use the issue body as the TOC or control surface.
- Put the goal, repo target, current state, and return contract in the issue body.

## Dense docs

- Move dense scope into attached Linear documents.
- Put implementation detail, validation appendices, source seams, and evidence into separate docs when they would make the issue body awkward.
- Use one document per coherent topic instead of one oversized appendix.

## Route-state block

- Include a compact route-state block in the issue body or an attached document for non-trivial worker implementation issues.
- The route-state block contains workflow phase markers (design_needed, planning_needed, etc.) and is used by work-mode-router to classify the current phase.
- Do not put the full implementation plan, validation matrix, or dense evidence dump into the route-state block.
- Do not use the route-state block as a readiness state or second plan.

## Anti-patterns

- Do not keep a separate compactor trigger for normal worker issue shaping.
- Do not hide the real goal inside document text while leaving the issue body vague.
- Do not let the route-state block become the second plan.
- Do not use comments as the durable source of truth for shaping decisions.
