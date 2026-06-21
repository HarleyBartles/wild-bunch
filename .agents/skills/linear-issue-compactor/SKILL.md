---
name: linear-issue-compactor
description: Compact oversized or connector-hostile Linear issue bodies into attached Linear documents plus a short Table-of-Contents issue body without losing fidelity. Use when asked to make a Linear issue easier to read through the connector, split bulky sections into documents, preserve the original goal and readiness state, or process multiple issues one by one.
metadata:
  source-id: linear-issue-compactor-v1
  source-path: sources/first_party/skills/linear-issue-compactor/SKILL.md
  provenance-name: MARK-135 Linear issue compactor skill
license: "MIT"
---
# Linear Issue Compactor

Use this skill when a Linear issue is too long, too dense, or too awkward for the connector to read cleanly.

## Owned decision

Turn one issue body into a readable control-plane summary and move dense sections into attached Linear documents without losing issue fidelity.

## Workflow

1. Read the issue first through the Linear connector, including the current body, attachments, relations, comments, and any existing documents.
2. Preserve the issue's goal, parent/child coverage, blocking state, readiness state, and explicit scope.
3. Move bulky sections into attached Linear documents when they add bulk without adding immediate readability, especially:
   - source seams;
   - implementation plans;
   - guardrails;
   - validation;
   - return evidence;
   - coverage tables;
   - child tracks;
   - research notes or decision records.
4. Rewrite the issue body as a compact table of contents:
   - goal;
   - current state;
   - linked documents with short summaries;
   - blockers or readiness;
   - worker-facing summary;
   - return evidence;
   - next actions if needed.
5. Read the issue back through the connector and confirm the body is readable and the documents are attached.
6. If multiple issues are in scope, finish one issue fully before starting the next.

## Guardrails

- Do not use comments as live planning truth; comments are audit/history only.
- Do not change status, assignee, labels, project, priority, parent/child links, or scope unless explicitly asked.
- Do not drop readiness or blocking state.
- Do not claim success without a final connector readback.
- Do not treat document creation responses alone as proof that the issue body is readable.
- Keep the body compact enough that the connector can render it without hiding the important parts.

## When to load the reference

Read `references/partition-patterns.md` when choosing how to bucket content into documents or when the issue needs a repeatable partitioning pattern.
