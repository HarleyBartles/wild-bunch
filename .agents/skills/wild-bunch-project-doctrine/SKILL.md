---
name: wild-bunch-project-doctrine
description: bootstrap the wild bunch repo posture before any repo-sensitive change. use when work touches harleybartles/wild-bunch, worker dispatch, worker return verification, source-truth claims, issue-goal conformance, world setup, seeded setup, difficulty, entropy, or when chat summaries, session busters, worker reports, or issue comments might be mistaken for live repo truth.
metadata:
  origin: first_party
  source_author: Harley Bartles
  source_license: MIT
  source_repo: https://github.com/HarleyBartles/agent-asset-marketplace
  source_path: sources/first_party/skills/wild-bunch-project-doctrine/SKILL.md
  content_mode: verbatim
---

# Wild Bunch Project Doctrine

Use this skill first when working on `HarleyBartles/wild-bunch`, or when a task needs the Wild Bunch setup doctrine. The live repo state on current `main` is the source of truth. Chat summaries, issue comments, session busters, and worker reports are support material only.

## Rules

- Treat `HarleyBartles/wild-bunch` as a mainline-only C#/.NET game project.
- Inspect live source before claiming current state.
- GPT prepares worker packets; Harley sends them; workers execute.
- When a task touches world setup, seed identity, difficulty, entropy, random selection, or starting inventory, read `references/difficulty-entropy-seeded-world-setup.md` first and keep it as the canonical anchor.
- Returns must include branch, commit, PR, validation, and issue-goal conformance notes.

## References

Read [Live repo posture](references/repo-posture.md) when a task needs source-truth posture, worker route boundaries, or return/verification expectations.
Read [Difficulty, Entropy, and Seeded Setup Doctrine](references/difficulty-entropy-seeded-world-setup.md) when a task needs world-start setup, seeded world identity, entropy, or difficulty posture.
