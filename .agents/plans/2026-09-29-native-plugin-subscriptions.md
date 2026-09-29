# Native Codex Plugin Subscriptions Implementation Plan

**Goal:** Load the four deliberately selected Wild Bunch plugins through native repository configuration and reserve `.agents/skills` for authored local skills.

**Architecture:** Adopt AOM's optional `repo-plugin-subscriptions` standard alongside all seven existing standards. Consumer-owned Git-subdirectory declarations track `main`; Codex owns installed payloads. Retain the pinned source submodule only for independently required tooling resources.

**Execution Strategy:** `executing-plans`, because scaffold deployment, projection removal, and canonical runner changes share one compatibility boundary.

**Requirements:** The human handover in this chat and Marketplace PR #343. Devin activation is deferred. No gameplay or web product changes.

## Tasks

- [ ] Adopt and deploy the new standard, using the refreshed AOM scaffold. Preserve existing standards and local-skill registrations. Declare and enable Game Studio, Architecture Pack, .NET Pack, and Frontend Pack under marketplace `wild-bunch`.
- [ ] Remove the checked-in Game Studio bundle, projected skills/provenance, refresh adapter and obsolete projection tests. Update runner, generated-path custody, README and operative guidance. Retain the pinned submodule's Markdown wheel and skill-script validator with explicit limited custody.
- [ ] Field-test Codex discovery and invocation in the worktree and repository context, unrelated-repository isolation, and native Marketplace Upgrade against actual selected source payloads. Record concrete commands, hashes and limitations in `.agents/docs/native-plugin-subscriptions-evidence.md`.
- [ ] Run focused script suites, adopted standard validation, canonical repository gate and normal hooked commit. Review the committed diff, publish a Draft PR, and verify its remote head and Draft state.

## Validation and review

Use existing behavior suites rather than change-detector tests. Run `py -3 .agents/standards/_runtime/repo_standards.py --check --standard repo-plugin-subscriptions`, `py -3 -m pytest scripts/tests`, and `py -3 tools/run.py ci --check`. PostgreSQL must be healthy for the canonical gate. Review exact local-skill byte preservation, independent resource ownership, all four activation identities, worktree scope, refresh targets, and absence of Devin activation.
