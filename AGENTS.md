# AGENTS.md

## Project
- Wild Bunch is a C#/.NET Western adventure game in `HarleyBartles/wild-bunch`.
- Work goes in a worktree, a branch, and a PR to `main`.
- Root index: `INDEX.md` | top-level navigation node.
- Docs index: `docs/INDEX.md`.
- Authored docs live in `docs/` and `.agents/docs/`; keep `.agents/` itself for routing, workflow, and other agent surfaces rather than scattering topic docs there.

## MUST INVOKE

- `/wild-bunch-project-doctrine` — MUST invoke before any repo-sensitive work.
  This skill carries all required reading, skill routing rules, script discovery,
  policy references, and working-knowledge directives for this repo.

## Bootstrap Skills

- `/using-superpowers` — primary workflow entrypoint; routes to specialist skills.
- `/work-mode-router` — session bootstrap and mode classification.
