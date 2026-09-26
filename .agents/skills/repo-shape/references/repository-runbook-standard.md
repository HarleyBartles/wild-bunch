# Repository Runbook and Playbook Standard

This is the portable standard for repository lifecycle routing and available topical workflows.

## Artifact taxonomy

| Question                                               | Owning surface                  |
| ------------------------------------------------------ | ------------------------------- |
| What must remain true?                                 | doctrine (`.agents/doctrine/`)  |
| What exact shape must participants exchange?           | contract (`.agents/contracts/`) |
| How do I perform one focused judgment or action?       | capability skill                |
| How does this repository execute a lifecycle stage?    | runbook (`.agents/runbooks/`)   |
| How does this repository handle a conditional concern? | playbook (`.agents/playbooks/`) |
| Can a machine enforce it cheaply?                      | code or configuration           |

Runbooks and playbooks are different artifacts, not interchangeable names.

## Runbooks

A runbook owns one repository lifecycle stage. It is entered through its stage/workflow skill and is the local composition root for that stage. The standard stage set is `design.md`, `planning.md`, `implementing.md`, `code-review.md`, and `pr.md`.

Every runbook contains these exact second-level sections:

- `When`
- `Required skills`
- `Composition`
- `Doctrine and contracts`
- `Local commands and paths`
- `Evidence contract`
- `Prohibited combinations`
- `Playbook routing`

`Playbook routing` links each applicable `.agents/playbooks/*.md` file and states the condition that activates it. Use `none` only when the stage has no topical composition.

## Playbooks

A playbook owns a topical workflow available to an agent whenever that concern applies. A runbook may route to a playbook, but runbook selection is not a prerequisite for using one. Common playbooks include `code-style.md`, `testing.md`, `security.md`, `skill-authoring.md`, and `marketplace-generation.md`. Mandatory cross-repository capabilities belong in portable skills; repository runbooks bind those capabilities to local lifecycle stages, commands, and evidence.

Every playbook contains the same seven common composition sections as a runbook, followed by `Runbook routing`. That section optionally links stage runbooks that commonly route to it; `None.` is valid for a standalone playbook.

Playbooks name and sequence capability skills, doctrine, contracts, commands, and evidence. Durable architecture and policy belong in doctrine; reusable language or framework technique belongs in capability skills. A playbook binds those owners to repository-specific triggers and proof.

## Dependency direction

The lifecycle and topical composition relationships are:

```text
using-superpowers-plus -> stage skill -> runbook
runbook -> playbook
playbook -> playbook or doctrine/contracts/capability skills
```

- Agents may invoke playbooks directly, and runbooks may route to playbooks.
- Playbooks may record reciprocal runbook routes but must not take ownership of a lifecycle stage.
- Playbooks may compose, invoke, or hand off to other playbooks when the topical workflow requires it.
- Doctrine and contracts never orchestrate.
- Capability skills must not sequence a repository lifecycle.
- Cycles are invalid.

Declared runbook/playbook references must resolve, and reciprocal declarations must agree when a routing edge exists. A playbook does not need a runbook edge to be valid or available.

## Local policy

Each consumer keeps `.agents/doctrine/repo-runbook-policy.md` with separate `Standard runbooks` and `Standard playbooks` tables. The tables map standard names to local paths, state required/optional status, and record explicit exceptions.

## Workflow order

The canonical lifecycle is `design -> planning -> implementing -> review -> pull request`. `using-superpowers-plus` selects the hygiene and stage owner. The stage owner reads its baseline and matching runbook. The agent uses any applicable playbook, whether discovered directly or through runbook routing.

## Migration

Repositories adopting this version move lifecycle roots to `.agents/runbooks/` and topical workflow compositions to `.agents/playbooks/`. A topical file left in the runbook mapping is structural drift; a standalone playbook with no runbook edge is valid.
