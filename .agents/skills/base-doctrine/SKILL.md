---
name: base-doctrine
description: Use when cross-runtime doctrine store for cross-project operating invariants
  not owned by a more specific skill. Use when work involves system-prompt limits,
  tool/source evidence honesty, durable doctrine routing, bounded skill/reference
  read loops, correction/trust posture, canonical agent asset source truth, or output
  artifact-shape authority such as reserved YAML, dispatch/continuity confusion,
  worker-copy attention guards, and lower-skill format conflicts.
metadata:
  source-id: base-doctrine
  source-path: sources/first_party/skills/base-doctrine/SKILL.md
  provenance-name: Base Doctrine first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when cross-runtime doctrine store for cross-project operating invariants
    not owned by a more specific skill. Use when work involves system-prompt limits,
    tool/source evidence honesty, durable doctrine routing, bounded skill/reference
    read loops, correction/trust posture, canonical agent asset source truth, or output
    artifact-shape authority such as reserved YAML, dispatch/continuity confusion,
    worker-copy attention guards, and lower-skill format conflicts.
  use_when:
  - Use when cross-runtime doctrine store for cross-project operating invariants not
    owned by a more specific skill. Use when work involves system-prompt limits, tool/source
    evidence honesty, durable doctrine routing, bounded skill/reference read loops,
    correction/trust posture, canonical agent asset source truth, or output artifact-shape
    authority such as reserved YAML, dispatch/continuity confusion, worker-copy
    attention guards, and lower-skill format conflicts.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---
# Base Doctrine

Use this skill as the cross-runtime doctrine store for cross-project operating invariants that are not owned by a more specific skill.

`SKILL.md` is only the control plane. Load the relevant reference for the task; do not load every reference by default.

## Table of contents

- System prompt work: read `references/system-prompt-contract.md`.
- Tool, memory, connector, repository-route, source-availability, or evidence-route claims: read `references/tool-surface-and-evidence.md`.
- Deciding where durable doctrine belongs, canonical source truth, installed-skill versus repo-source boundaries, or agent asset marketplace routing: read `references/durable-doctrine-routing.md`.
- Bounded skill/reference reading, anti-loop stop rules, or "how much should I read before acting" questions: read `references/bounded-read-loop.md`.
- Failure, correction, and trust posture: read `references/failure-and-trust-posture.md`.
- Worker and subagent continuity, dispatch lifecycle, or evidence-based stall handling: read `references/worker-continuity.md`.
- Output shape, reserved artifact forms, YAML-vs-non-YAML conflicts, worker-copy attention guards, or artifact authority: read `references/output-artifact-shape.md`.
- Report partitioning, report laundering, worker returns, verification summaries, publication notes, or closure summaries: read `references/report-hygiene.md`.
- Quick map: read `references/doctrine-index.md`.

## Core control-plane rule

This entrypoint should classify the kind of base doctrine needed and point to the smallest relevant reference. Do not perform source-route selection, connector inspection, repository lookup, memory claims, or tool-surface diagnosis from `SKILL.md` alone.

For ordinary chat, acknowledgements, pings, or lightweight meta that does not require source or tool evidence, answer directly after any project bootstrap that was already required by the active project context. Connector presence, file presence, runtime tool availability, or plugin availability is not itself a reason to load a source-route reference.

Load `references/tool-surface-and-evidence.md` only when the current task actually involves a tool, connector, memory, repository, source availability, evidence route, artifact-save claim, or a user challenge about one of those surfaces.

Load `references/durable-doctrine-routing.md` when the current task asks where doctrine, skills, plugin marketplace entries, repo overlays, or agent asset source truth should live. This includes deciding whether a GPT-native skill update belongs in installed skill state, a canonical repo source, a Codex plugin marketplace, a repo overlay, Linear, GitHub, or a project repo.

Load `references/output-artifact-shape.md` only when the current task involves output format authority, reserved workspace forms, dispatch/continuity confusion, YAML-shaped content, reusable handoff shapes, or lower-skill output templates that may conflict with project/workspace conventions.

Load `references/report-hygiene.md` only when the current task involves drafting or reviewing report-like surfaces — worker returns, verification summaries, publication notes, closure summaries, or continuity notes — where reporting language could change the authority of information.

Use the most specific project skill, workflow skill, plugin, repo skill, or repo playbook when one owns the work. Use this skill only for base doctrine that crosses projects or prevents recurring GPT failure modes.

## Linear/Codex and agent-asset source boundary

For coding work, Linear/Codex is now the default workflow control plane when available; use `linear-issue-shaping` for dispatch, worker state, PR-gate handling, and golden-gate routing. This base skill does not manage coding workflow state.

For GPT-native skill work, installed skills are deployment targets, not durable source truth when a canonical repo source exists. The durable source should be the versioned agent asset repository or other named source artifact, then package/install through the native skill stack. Do not claim an installed skill is canonical merely because it is available in this session.

For Codex worker enablement, prefer native plugin marketplaces and curated plugin bundles for generic worker capabilities. Use repo overlays only for project-specific domain, validation, and local runtime guidance that generic plugins cannot know.

## No-Shit / entropy doctrine

Across projects, smaller final surface is valuable only when evidence, provenance, ambiguity, authority, validation, and publication proof remain intact. Do not treat deletion, consolidation, or fewer files as inherently better.

For cleanup or residue questions, distinguish live source, cold store, governed trash, delete-now residue, and block-and-route protected material. Governed trash is reversible deletion-staging custody with repo-visible sentinel expectations; it is not authority to delete protected material.

Use project-specific cleanup/custody skills when a project owns the surface. Keep this base doctrine as the cross-project guardrail against deletion-first behavior and skill/doctrine sprawl.

## Evidence-grounded doctrine capture

Durable doctrine proposals need an observed evidence basis. Use actual commits, diffs, Linear issue threads, GitHub issue threads, worker returns, verified closures, repeated package failures, plugin marketplace evidence, or other inspected source evidence before proposing new doctrine.

Do not turn conversation memory, vibes, or a single unverified anecdote into cross-project doctrine. If the evidence is partial, label it and create a bounded follow-up issue instead of claiming a permanent rule.

A good doctrine-capture note states what happened, the evidence route, the principle at work, why it matters, and the next lawful capture route. Refuse to force lessons from trivial housekeeping.

## Scratch folder architecture

The centralized scratch folder provides a cross-runtime workspace pattern for temporary agent workspaces that should not be committed to repositories.

### Architecture

- **Scratch root**: `../_agent-scratch/`
- **Per-repo structure**: `../_agent-scratch/<repo-name>/`
- **Per-branch structure**: `../_agent-scratch/<repo-name>/<branch-name>/`

### Scratch folder properties

- **Disposable**: Not persistent beyond the agent's session
- **Outside repo**: Prevents accidental commits
- **Per-branch**: Matches worktree/branch name for isolation
- **Auto-cleanup**: Agents must clean up scratch folder when cleaning up worktree
- **Not for durable work**: Use the repo for persistent changes

### Cleanup contract

Agents must clean up their scratch folder when cleaning up their worktree. This ensures the scratch space remains clean and does not accumulate orphaned temporary files across sessions.

### Implementation details

Reference `repo-worker-base` for implementation details and usage patterns. The scratch folder architecture is designed to complement the worktree system by providing a disposable workspace for temporary outputs that should not be committed to the repository.
