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

## Preflight document

- Include one Preflight document for non-trivial worker implementation issues.
- Keep Preflight limited to investigation seams and questions that prove understanding.
- Do not put the full implementation plan, validation matrix, or dense evidence dump into Preflight.
- Do not treat Preflight as a readiness state.

## Anti-patterns

- Do not keep a separate compactor trigger for normal worker issue shaping.
- Do not hide the real goal inside document text while leaving the issue body vague.
- Do not let the Preflight doc become the second plan.
- Do not use comments as the durable source of truth for shaping decisions.
