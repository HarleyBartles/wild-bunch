# Partition Patterns

Use these buckets when splitting a large Linear issue into attached documents.

| Bucket | Keep in issue body | Move into doc |
| --- | --- | --- |
| Parent or tracker issue | Goal, parent coverage, readiness, blockers, child-track pointers | Source evidence, guardrails, coverage map, child details |
| Implementation issue | Goal, current state, linked docs, short status | Source seams, implementation plan, validation, return evidence |
| Research or planning issue | Goal, open questions, current decision, links | Source evidence, decision record, alternatives, follow-ups |

Rules:

- Keep the issue body as a readable TOC.
- Prefer one document per coherent topic instead of one giant appendix.
- Give each document a stable, descriptive title.
- Include a short summary line in the issue body for each linked document.
