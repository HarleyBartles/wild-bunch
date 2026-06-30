---
name: using-linear
description: Use when working with the Linear connector surface, choosing the right
  tool call, or finding create/update tools exposed under `save_*` rather than `create_*`
  or `update_*`.
metadata:
  source-id: using-linear
  source-path: sources/first_party/skills/using-linear/SKILL.md
  provenance-name: Using Linear first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when working with the Linear connector surface, choosing the right tool
    call, or finding create/update tools exposed under save_* rather than create_*
    or update_*.
  use_when:
  - Use when working with the Linear connector surface, choosing the right tool call,
    or finding create/update tools exposed under save_* rather than create_* or update_*.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---
# Using Linear

Use this skill to pick the right Linear connector surface from the task intent, then open the matching reference.

## Router

| Intent | Read first |
| --- | --- |
| Find or inspect issues, projects, or documents | [`references/read-discover.md`](references/read-discover.md) |
| Read or write comments and discussion threads | [`references/comments.md`](references/comments.md) |
| Work with diff reviews or diff threads | [`references/diffs.md`](references/diffs.md) |
| Create or update Linear objects | [`references/mutate-save.md`](references/mutate-save.md) |
| Work with teams, labels, statuses, cycles, milestones, or status updates | [`references/admin-metadata.md`](references/admin-metadata.md) |
| Work with customers or customer needs | [`references/customers.md`](references/customers.md) |
| Upload or inspect attachments, images, or docs help | [`references/attachments.md`](references/attachments.md) |
| Archive or delete a status update, comment, attachment, customer, or customer need | [`references/destructive.md`](references/destructive.md) |
| Need the complete callable surface | [`references/surface-map.md`](references/surface-map.md) |

## Fast rule

If you are looking for a create or update tool and do not see `create_*` or `update_*`, search `save_*` first. The surfaced create-only exception here is `create_issue_label`.

If the intent is still unclear after the first pass, open `references/surface-map.md` and then return to the use-case file that matches the object you are touching.
