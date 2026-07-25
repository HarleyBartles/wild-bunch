# Repository Shape Standard

This file describes the surfaces `repo-standards` checks and can apply. It is the human-readable companion to `repository-shape-manifest.json`.

## Required surfaces

- `.agents/plugins/marketplace-source` as a git submodule pointing at the marketplace source.
- `.agents/plugins/marketplace.json` with `repo.local_skill_prefixes` configured.
- `scripts/ci-preflight.ps1` and `scripts/ci-preflight.sh` for the default preflight bundle.
- `.git/hooks/pre-commit` wired to `scripts/ci-preflight.sh --check`.
- `.agents/docs/repo-guide-policy.md` mapping the repo to `repo-standards`.
- `REVIEW.md` at the repo root pointing to the review guide and required skill invocations.
- `CONTRIBUTING.md` at the repo root as the contributor entry point.
- `.gitignore` containing the `.agents/superpowers/sdd/**` and `!.agents/superpowers/sdd/.gitignore` rule.
- `.agents/guides/<standard-guide>.md` for the core and declared guide set.
- Root `AGENTS.md` as a router with five core sections and a routing table.
- `.agents/guides/AGENTS.md` as an optional router for the guide set (may be scaffolded by `scaffold-guides`).

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

- `scaffold-repo-guide-policy` generates `.agents/docs/repo-guide-policy.md` from the standard template.
- `scaffold-guides` generates missing `.agents/guides/*.md` files from `repo-guide-policy.md` and the optional `.agents/guides/AGENTS.md` router.
- `scaffold-review` generates `REVIEW.md`.
- `scaffold-contributing` generates `CONTRIBUTING.md`.
- `scaffold-ci-preflight` generates `scripts/ci-preflight.sh` and `scripts/ci-preflight.ps1` from the skill templates.
- `scaffold-gitignore` ensures the sdd rule is in root `.gitignore`.
- `scaffold-agents-md` scaffolds or validates root `AGENTS.md` as a router.
- `scaffold-marketplace-json` scaffolds or validates `.agents/plugins/marketplace.json` with `repo.local_skill_prefixes`.
- `scaffold-all` runs the above in sequence.

`repo-standards --apply` also invokes the appropriate scaffold when a surface has `scaffold` set in the manifest.

## Exceptions

Repos may record surface exceptions in the `## Exceptions` section of `.agents/docs/repo-guide-policy.md` using the surface `id` (one per line). `repo-standards --check` and `--apply` skip those surfaces.

## Local overrides

Each repo supplies its own `repo.local_skill_prefixes` in `.agents/plugins/marketplace.json` so local skills are not pruned by `refreshing-installed-skills`.

## Local-only agent surfaces

The `.agents/superpowers/` directory has mixed residency:

- `superpowers/specs/**` is repo resident.
- `superpowers/plans/**` is repo resident.
- `superpowers/sdd/` and all subdirectories are intentionally ignored; SDD outputs are not repo resident.

A repo that follows this standard must have a root `.gitignore` rule equivalent to:

```gitignore
.agents/superpowers/sdd/**
!.agents/superpowers/sdd/.gitignore
```

This keeps the `sdd/` scaffold (only its `.gitignore`) while ignoring all session content at any depth.
