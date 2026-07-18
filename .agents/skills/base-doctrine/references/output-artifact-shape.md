# Output artifact shape and authority

Output form is part of the message's authority. A shape can imply execution, continuity, handoff, publication, or machine-readable intent even when the prose says otherwise.

## Core rule

Do not borrow a reserved artifact shape for a different kind of content. If a lower-level skill suggests a format that conflicts with the active workspace's reserved forms, the workspace/base artifact-shape rule wins.

This is an attention guard as much as a machine guard: a user may copy a block into a worker or future session without rereading every surrounding sentence. The block's form must not suggest the wrong action.

## YAML reservation

When a workspace reserves YAML for execution or continuity artifacts, do not use YAML blocks for ordinary assessments, gate summaries, planning notes, status updates, analysis, or route explanations.

In Wild Bunch and similar worker-control contexts, YAML-shaped blocks are reserved for:

- lawful send-ready dispatches;
- continuity artifacts;
- other user-explicit YAML artifacts.

If the user did not ask for YAML and the content is not one of those artifacts, use prose, a markdown table, a JSON code block, or another clearly non-dispatch shape.

## Lower-skill output templates

A skill's local output template is not authority to override workspace artifact-shape law. If a skill says to use compact YAML but the active workspace treats YAML as dispatch/continuity shaped, use a non-reserved shape and preserve the same semantics.

Examples:

- A readiness assessment in ordinary chat should be prose or non-reserved structured text, not YAML, when YAML may be pasted to a worker as a packet.
- A dispatch that has cleared the current dispatch gate may use dispatch-shaped YAML because that is the artifact requested.
- A continuity export may use the project's continuity shape because continuity export is the requested artifact.
- A reusable draft should use the requested writing/document surface, not a dispatch-shaped block.

## Repair posture

When output shape is wrong but content is useful, repair the shape before emission. Do not treat shape repair as a blocker if the safer shape is clear and lawful.

If the user explicitly asks for a reserved shape for non-reserved content, confirm or clearly label the artifact so it cannot be mistaken for execution or continuity authority.
