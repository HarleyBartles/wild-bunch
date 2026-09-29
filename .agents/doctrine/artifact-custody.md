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
- Binding repo-specific review profiles live under `.agents/contracts/unslop/`;
  scoped profiles use `<scope>/.agents/contracts/unslop/`.

## Agent document placement

- `AGENTS.md` files route work to the guidance that owns it.
- Doctrine records repository rules; contracts define executable or independently consumed agreements.
- Runbooks bind lifecycle stages to repository paths, commands, and evidence.
- Playbooks bind topical workflows to repository-specific decisions and proof.
- Human-facing explanations belong in `README.md` files or ordinary documents under `docs/`.
- Directory inventories are not maintained as generated files; keep useful discovery in the owning README or router.
