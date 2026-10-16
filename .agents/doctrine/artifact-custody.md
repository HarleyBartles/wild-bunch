# Artifact custody doctrine

## Scratch and evidence

- Branch-scoped temporary work lives under
  `Z:\_agent-scratch\wild-bunch\<branch-name>`.
- Task briefs, worker reports, review packages, screenshots, and temporary notes
  are scratch artifacts, not tracked source.
- Clean the branch scratch directory when its worktree is removed.
- Do not create loose review reports, screenshots, or session files at repo root
  or under product source.

## Durable outputs

- Architecture decisions belong in `docs/decisions/`.
- Current agent rules belong in `.agents/doctrine/`, `.agents/contracts/`, or
  `.agents/runbooks/` according to their authority role.
- Canonical unslop profiles and distinct observations live under `.agents/unslop/`. The [unslop playbook](../playbooks/unslop.md) binds their selection, work-point routing and feedback loop; do not retain competing profiles at old contract or scoped locations.

## Agent document placement

- `AGENTS.md` files route work to the guidance that owns it.
- Doctrine records repository rules; contracts define executable or independently consumed agreements.
- Runbooks bind lifecycle stages to repository paths, commands, and evidence.
- Playbooks bind topical workflows to repository-specific decisions and proof.
- Human-facing explanations belong in `README.md` files or ordinary documents under `docs/`.
- Directory inventories are not maintained as generated files; keep useful discovery in the owning README or router.

## Planning across branches

Live plans, specifications, roadmaps and investigations belong on `develop` in `.agents/plans/`, `.agents/specs/`, `.agents/roadmaps/` and `.agents/investigations/`. Main's current tree contains none, including completed artifacts already on main. Git history retains retired material; do not move it into a tracked archive or another planning home disguised as product documentation.

Before a main-bound cut, read every planning artifact against its entire scope and current delivery evidence. Ask the maintainer about each unclear classification. Promote enduring knowledge to current owners and useful deferred feature intent to the feature matrix, repair references, and commit completed-artifact retirement to develop first. Keep live and ambiguous work on develop. The pinned next-slice retirement policy still applies.

Before reconciling main into develop, capture the current develop commit and restore exactly its four planning homes into the merge result, including files added, edited or deleted since the cut. Verify the indexed planning tree equals that pre-merge snapshot; refresh both if develop advances. Do not restore an older cut snapshot. See [ADR-0049](../../docs/decisions/ADR-0049-planning-custody-across-integration-and-promotion.md) and the [release procedure](../runbooks/release.md).
