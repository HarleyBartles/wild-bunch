---
name: writing
description: Use when drafting, revising, or reviewing prose for human readers and clarity, an authorised voice, or reader-fatigue concerns may interact.
metadata:
  source-id: writing
  source-path: codex-marketplace/plugins/writing-pack/skills/writing/SKILL.md
  provenance-name: Writing composition first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Composed human-facing writing with clear authority boundaries.
  use_when:
  - Use when prose needs a coordinated draft, revision, or review.
  - Use when a declared voice card or evidence-backed fatigue review may apply.
  do_not_use_when:
  - Use writing-with-clarity directly for clarity-only or final-edit work.
  related_skills:
  - writing-with-clarity
  - writing-style
license: MIT
---

# Writing

Use this as the normal entrypoint for human-facing prose. It composes specialist
skills; it does not invent facts, infer authorship, or optimise text for
detector evasion.

1. Establish the artifact, audience, purpose, verified facts, hard constraints,
   supplied draft, and any explicit project or editorial rules. Read a declared
   voice card only when it is authorised for this task.
2. Invoke `$writing-with-clarity` to draft or revise. Preserve facts,
   qualifications, accessibility, and intended meaning.
3. If an authorised voice card is present, invoke `$writing-style` to apply it.
   Do not infer a private voice profile or retain a supplied corpus.
4. Invoke `$writing-style` for writing-specific fatigue review only when the
   available evidence supports a material contextual finding. A phrase match or
   a request to “sound human” is not enough.
5. Invoke `$writing-with-clarity` for its final edit. Restore anything removed
   by a style repair if the removal damaged meaning, necessary qualification,
   readability, or authorised voice.

Follow [the workflow](references/workflow.md) and
[authority order](references/authority-order.md). Load only the bounded
specialist material needed for the task. If no material fatigue pattern is
present, return the clear draft unchanged. Disclose material voice or style
choices briefly when that helps the reader assess the revision.
