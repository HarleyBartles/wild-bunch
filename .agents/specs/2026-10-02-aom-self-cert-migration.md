# AOM self-certification migration

## Goal and authority

Migrate Wild Bunch's explicit AOM v1 adoption to the current self-certification model and remove unused deployment infrastructure. This specification implements the user's request to assess current subscriptions, delete unnecessary AOM material, and adopt the new model. Implementation follows review of the accompanying plan.

Assessment base: Wild Bunch `ebe6741683fd5830e733eaf4ac220078898a4c61`. Historical AOM authority: `https://github.com/HarleyBartles/agent-asset-marketplace.git` at `5fcfb473948fd0f32998ffce31d8e528f2261572`. Proposed published v2 authority: the same repository at `b481f98ae90aa45e5271d10fe1f7aaeb6c7047aa`, fetched from remote main on 2026-10-02. Read definitions with `git show <commit>:<definition>` from a checkout whose origin matches that repository; plugin cache availability does not set authority.

## Current subscriptions and proposed selection

| Current v1 standard | Proposed disposition | Definition at proposed pin |
| --- | --- | --- |
| marketplace-skill-management | Retire. Its old scaffold/projection and exact registration contract is superseded by native plugin custody. | None in current catalog |
| root-agent-router | Migrate with repository-owned budget and structure checks. | skills/agents-routing/references/standard.md |
| runbook-composition | Migrate; preserve useful stage guides and assess their actual routing. | skills/runbook-composition/references/standard.md |
| playbook-composition | Migrate; preserve useful concern guides and assess capability availability. | skills/playbook-composition/references/standard.md |
| tracked-validation-hook | Migrate; preserve complete candidate-tree validation and Windows/Linux parity. | skills/tracked-repo-hooks/references/standard.md |
| markdown-formatting | Explicitly retire. Not in current catalog, no formatter contract or configuration is tracked, and no active runner invokes its formatter. | None in current catalog |
| completed-artifact-custody | Migrate; replace marker-dependent discovery with semantic next-slice classification. | skills/completed-artifact-custody/references/standard.md |
| repo-plugin-subscriptions | Migrate; retain native Git declarations and repository-authored skill custody. | skills/repo-agent-assets/references/standard.md |

Do not adopt adjacent catalog entries merely because their files already exist. The four native plugin dependencies are separate: game-studio, dotnet-pack, architecture-pack, frontend-pack. Preserve their sources, paths, activation keys, and deliberate main-branch refresh policy. Preserve all six authored `.agents/skills/*/SKILL.md` files.

## Desired implementation

The v2 record contains only version and standard entries with ID, source repository, immutable commit, definition path, and certification reference. A readable repository-owned certification records each pledge, implementation locations, required invariants, semantic assessment, mechanical evidence, drift controls, and limitations. Root AGENTS routes to both records. A successful structural check cannot certify semantics.

Keep the v1 authority until its implementation callers are replaced and the selected v2 compliance chain is established. Commit the new subscription and accurate certification together with the cutover. Do not assert certified compliance when required evidence is missing.

Remove `.agents/standards/` entirely, including copied scaffolders, templates, manifests, duplicate modules, deployment runtime, and provenance. Replace useful checks with narrowly selected repository-owned checks under `scripts/`, without a resource installer, apply dispatcher, replacement standards tree, or shared AOM framework. Only a small subscription structural checker and native declaration checker may be adapted from the pinned optional assets. Router structure/budget enforcement is owned here.

Remove `.agents/plugins/marketplace-source` and its gitmodule after eliminating every executable dependency. The deployed formatter's requirement file references a wheel in that submodule, but no active formatter contract exists. The wrapper `scripts/validate_repo_skill_scripts.py` returns early because all six local skills have no Python scripts. Remove that no-op wrapper and CI lane. Preserve the general script dependencies in `scripts/requirements.txt`. Document how a future repository-authored skill with executable scripts brings its own validation rather than implying an unavailable external validator.

Keep the existing tracked hook, candidate materialization, command declaration, runner diagnostics, shared-checkout protection, ADR freshness generation, product tests, and PostgreSQL infrastructure. The command declaration remains a repository-owned adapter, not an AOM-mandated format. Retain only actual generated paths, currently `docs/decisions/README.md`. Hook activation remains an explicit `ci --apply` responsibility after the old runtime is removed.

Router policy: root at most 40 lines; scoped routers, if introduced, at most 15 lines and an explicit scope/read condition. Only natural domain boundaries get routers. The checker examines tracked AGENTS files, verifies root subscription/certification routes and local route existence, and enforces this policy. Review still judges usefulness and safety. Do not fabricate scoped routers or a fixed book inventory.

## Guardrails and acceptance

- No game, API, persistence, asset, or web product behavior changes.
- No new standards, plugin payload copies, scaffolders, generated navigation mesh, or generic deployment framework.
- Preserve meaningful local policies; remove only boilerplate or obsolete obligations supported by the assessment.
- Do not create tautological, source-string, or change-detector tests. Add behavior tests only for new or changed gate behavior.
- Do not skip hooks. Normalize text without arbitrary Markdown wrapping.
- Certification distinguishes Windows proof, hosted Linux proof, native configuration validity, and installed runtime availability. Missing hosted or runtime evidence is explicit.
- Every retained standard has a valid immutable subscription and a discoverable certification section; required router checking runs in the complete gate.
- A fresh clone's gate needs no ambient AOM plugin or Marketplace submodule. Local and hosted checks use the same candidate/committed repository-owned validation chain.
- Retired paths have no live callers or guidance routes. Native plugin declarations and authored skills retain their behavior and custody.
- Focused behavior checks pass, then the ordinary hooked commit proves the complete staged product. Hosted parity is separately exercised against that committed counterpart.

## Implementation evidence and limits

The implementation is on `codex/aom-self-cert-migration`. The source and definition pin is `b481f98ae90aa45e5271d10fe1f7aaeb6c7047aa`. It removes the AOM source gitlink, `.gitmodules`, and the complete `.agents/standards/` deployment tree while preserving the four separate native plugin dependencies and six authored skills. Task and review evidence is recorded in the implementation plan and the repository-owned certification.

The focused suite passes with 41 tests on Windows and runs in the canonical gate. The ordinary hooked implementation commit and clean detached clone at implementation commit `df6a1190c23a59723380969646a3b8e11405d847` passed the complete product gate. These local runs do not establish hosted Linux results: GitHub Actions skips Draft PR checks. The tracked-hook certification remains pending the first non-draft hosted run. The historical 2026-09-29 plugin field report is retained as dated evidence, not a claim of fresh-process runtime or authentication validation.
