# Repo Standards

This file is the portable cross-repo standard for repo-local runbooks and agent-facing routing surfaces.

## Required root surfaces

Every repo using this standard must have:

- `AGENTS.md` with these canonical headings in order:
  1. `## Repository purpose`
  2. `## Source-of-truth split`
  3. `## Publication proof for repo work`
  4. `## Build and test commands`
  5. `## Testing instructions`
  6. `## Code style guidelines`
  7. `## Review guidelines`
  8. `## PR instructions`
  9. `## Contributing`
  10. `## Security considerations`
  11. `## Routing pointers` (repo-specific router table)
  12. `## Maintenance responsibility`

- `REVIEW.md` — review entry point. It contains first-class review concerns and routes through `using-superpowers-plus` to the review owner, with `.agents/runbooks/code-review.md` supplying the local review delta.
- `CONTRIBUTING.md` — contributor entry point. It routes through `using-superpowers-plus`; the selected owner reads the matching local runbook. It may be a thin pointer to `.agents/runbooks/contributing.md` when the repo keeps detailed guidance there.

## Core runbook set

`.agents/runbooks/` must contain:

- `design.md`
- `planning.md`
- `implementing.md`
- `code-review.md`
- `pr.md`

## Surface taxonomy

Route content by this test:

| Question | Owning surface |
| --- | --- |
| What must remain true? | doctrine (`.agents/doctrine/`) |
| What exact shape must participants exchange? | contract (`.agents/contracts/`) |
| How do I perform one focused judgment or work? | capability skill |
| How does this repo combine capabilities for this class of change? | runbook (`.agents/runbooks/`) |
| Can a machine enforce it cheaply? | code or configuration |

## Skill shapes

- **Router** - bootstrap composition and stage routing (for example
  `using-superpowers-plus`).
- **Stage/workflow skill** - portable orchestration of a generic stage or a
  bounded artifact workflow. It owns a portable stage verb and delegates repo
  specifics to the local runbook.
- **Capability skill** - one focused verb or judgment. It may delegate to a
  narrower prerequisite skill.
- **Doctrine/policy carrier** - portable doctrine shipped in skill packaging;
  it routes to policy references and does not own a workflow verb.

## Composition rule

- Runbooks may compose peer skills.
- Capability skills must not sequence a repository delivery lifecycle.
- Stage/workflow skills orchestrate a portable stage and read the local
  runbook for binding; they never restate repo specifics.
- Doctrine and contracts never orchestrate.

## Pull request runbook policy

Every repo using this standard must define a thin `.agents/runbooks/pr.md`
overlay containing only its local base branch, validation and remote-check
commands, draft-aware CI configuration, exceptions, and publication-proof
surface. Generic Draft lifecycle, commit discipline, review sequencing, and
publication handoff belong to `publishing-source` and `repo-worker-base`.

## Allowed additional runbooks

Additional `<topic>.md` files may live in `.agents/runbooks/`. They are composition manifests for additional change classes, not repeats of portable doctrine. Common additional runbooks include:

- `security.md`
- `testing.md`
- `contributing.md`
- `code-style.md`
- `marketplace-generation.md`
- `skill-authoring.md`
- `completing-plans.md`

A runbook is the repository's composition manifest for a class of change.
Each runbook carries these sections:

- `When` - the change class or trigger it covers.
- `Required skills` - the skills the composition invokes (the owning stage
  skill for stage runbooks).
- `Composition` - the order or conditions under which the skills apply.
- `Doctrine and contracts` - local truths and shapes that constrain it.
- `Local commands and paths` - repository commands, paths, and exceptions.
- `Evidence contract` - what the combined workflow must prove.
- `Prohibited combinations` - combinations not legitimate here, or `none`.

Runbooks name and sequence owners; they must not repeat portable doctrine or
skill internals.

New runbooks carry all seven sections. Existing runbooks must declare
`Required skills` and adopt the remaining sections as they are touched; the
validator warns on a missing `Required skills` heading as the enforced
minimum, so warning-free output does not certify the full contract.

## Local overlay policy

Each repo keeps `.agents/doctrine/repo-runbook-policy.md`. It must:

- State that the repo follows `repo-standards`.
- Map standard runbook names to local paths.
- List existing and missing runbooks.
- Note any repo-specific exceptions.

## Workflow order

The canonical stage order is:

```text
design -> planning -> implementing -> review
```

At every stage, `using-superpowers-plus` classifies the request and hands off
to the required hygiene and workflow owners. The selected workflow owner reads
this standard, the repo's `.agents/doctrine/repo-runbook-policy.md`, and the
matching local runbook. Entry points and runbooks must not reproduce the skill
selection table.

## Relationship to repo-worker-base

`repo-standards` owns runbook layout and stage order, not session composition.
`repo-worker-base` owns worktree, branch, scratch, validation, and publication
boundaries. Each stage skill owns its baseline and reads the matching local
runbook. `using-superpowers-plus` composes those owners.
