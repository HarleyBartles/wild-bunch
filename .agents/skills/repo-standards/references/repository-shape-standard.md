# Repository Shape Standard

This file describes the surfaces `repo-standards` checks and can apply. It is the human-readable companion to `repository-shape-manifest.json`.

## Required surfaces

- `.agents/plugins/marketplace-source` as a git submodule pointing at the marketplace source.
- `.agents/plugins/marketplace.json` with `repo.local_skills` configured.
- `tools/run.py` - the repo's canonical `ci` (and other) task runner. See [ci-validation-pipeline.md](ci-validation-pipeline.md) for the contract.
- `.git/hooks/pre-commit` wired to `tools/run.py ci --apply`. The hook is validated by contract (it must be executable on POSIX; it must carry a `#!` shebang on Windows/NT where the executable bit is not reliably represented; it must run `tools/run.py ci --apply`; and it must enable `errexit`, `nounset`, and `pipefail`), not by byte-for-byte comparison to a template.
- `.agents/doctrine/repo-runbook-policy.md` mapping the repo to `repo-standards`.
- `REVIEW.md` at the repo root pointing to the review runbook and required skill invocations.
- `CONTRIBUTING.md` at the repo root as the contributor entry point.
- `.gitignore` at the repo root, free of stale `.agents/superpowers/sdd/**` or `!.agents/superpowers/sdd/.gitignore` rules.
- `.agents/runbooks/<standard-runbook>.md` for the core and declared runbook set.
- Root `AGENTS.md` as a router with five core sections and a routing table.
- `.agents/runbooks/AGENTS.md` as an optional router for the runbook set (may be scaffolded by `scaffold-runbooks`).
- `.agents/plans/completed/` and `.agents/specs/completed/` as the historical archive for in-flight plans and specs that have been completed. `repo-standards --apply` creates these directories and places a `.gitkeep` placeholder in each so they survive a clean checkout.
- `.agents/doctrine/completed-plans.md` stating that completed plans/specs are historical context, not live pattern sources.
- `.devin/rules/completed-plans.md` as a conditional trigger on completed plan/spec files, routing to `.agents/doctrine/completed-plans.md`.

## Router AGENTS.md model

Root `AGENTS.md` is a router, not an encyclopedia. It must contain exactly five core sections:

1. `## Repository purpose`
2. `## Source-of-truth split`
3. `## Build and test commands`
4. `## Routing pointers`
5. `## Maintenance responsibility`

The `## Routing pointers` section must list resolvable links to the scoped surfaces that own each canonical topic. Canonical topics include: Repository purpose, Source-of-truth split, Publication proof, Build and test commands, Testing instructions, Code style guidelines, Review guidelines, PR instructions, Contributing, Security considerations, Routing pointers, and Maintenance responsibility.

`repo-standards --check` validates that the five core sections exist, that every routing pointer resolves to a tracked file, and that the 12 canonical topics are covered by the union of root sections and routed targets.

## Scaffold helpers

Use these idempotent scripts to create missing user-content surfaces. The agent remains responsible for repo-specific content.

- `scaffold-repo-runbook-policy` generates `.agents/doctrine/repo-runbook-policy.md` from the standard template.
- `scaffold-runbooks` generates missing `.agents/runbooks/*.md` files from `repo-runbook-policy.md` and the optional `.agents/runbooks/AGENTS.md` router.
- `scaffold-review` generates `REVIEW.md`.
- `scaffold-contributing` generates `CONTRIBUTING.md`.
- `scaffold-gitignore` removes any stale `.agents/superpowers/sdd/**` root `.gitignore` rule and any obsolete `.agents/superpowers/sdd/.gitignore` directory.
- `scaffold-agents-md` scaffolds or validates root `AGENTS.md` as a router.
- `scaffold-marketplace-json` scaffolds or validates `.agents/plugins/marketplace.json` with `repo.local_skills`.
- `scaffold-all` runs the above in sequence.

`repo-standards --apply` also invokes the appropriate scaffold when a surface has `scaffold` set in the manifest.

## Migration notes

- **Guides → runbooks:** Repos implementing this standard must use `.agents/runbooks/` and the `repo-runbook-policy.md` mapping. The `.agents/guides/` directory and the `repo-guide-policy.md` name are retired. `repo-standards --check` treats a missing `.agents/runbooks/` or a stale `repo-guide-policy.md` as drift.
- **Playbooks → runbooks:** If a repo still maintains playbooks under any path, it should migrate them to `.agents/runbooks/`. `.agents/runbooks/` is the one supported home for runbooks. `repo-standards` may emit a warning for a legacy `playbooks/` surface but does not fail the check.

## Exceptions

Repos may record surface exceptions in the `## Exceptions` section of `.agents/doctrine/repo-runbook-policy.md` using the surface `id` (one per line). `repo-standards --check` and `--apply` skip those surfaces.

## Local overrides

Each repo supplies its own `repo.local_skills` in `.agents/plugins/marketplace.json` so local skills are not pruned by `refreshing-installed-skills`.

## SDD scratch

The Superpowers+ SDD workspace lives outside the repo at:

```
<repo-root>/../_agent-scratch/<branch>/<plan-basename>/
```

SDD outputs (task briefs, implementer reports, review packages, and progress
ledgers) are not repo resident and are not governed by `.gitignore`.

The root `.gitignore` must not contain a stale in-repo rule such as:

```gitignore
.agents/superpowers/sdd/**
!.agents/superpowers/sdd/.gitignore
```

`scaffold-gitignore` removes the stale rule and any leftover `.agents/superpowers/sdd/.gitignore` directory from older repo layouts.
