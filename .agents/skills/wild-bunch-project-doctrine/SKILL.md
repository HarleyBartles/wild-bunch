---
name: wild-bunch-project-doctrine
description: Use when bootstrapping the Wild Bunch repo posture before any repo-sensitive
  change. Establishes source-truth posture, worker dispatch and return verification,
  issue-goal conformance, world setup, seeded identity, difficulty, entropy, working
  knowledge (worktree and scratch locations, script discovery), skill routing (session
  bootstrap, Linear/GitHub, anti-slop, planning/execution, repo-specific, architecture),
  and policy references (workflow, validation, artifact, architecture guardrails,
  coding discipline, worker environment, mesh, guides, ADR freshness). Use when chat
  summaries, session busters, worker reports, or issue comments might be mistaken
  for live repo truth. This skill establishes posture first, then routes to specialist
  skills (wild-bunch-dotnet-architecture, wild-bunch-domain-modeling, wild-bunch-browser-game)
  for domain-specific work.
metadata:
  source-id: wild-bunch-project-doctrine
  source-path: sources/first_party/skills/wild-bunch-project-doctrine/SKILL.md
  provenance-name: Wild Bunch Project Doctrine first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when bootstrapping the Wild Bunch repo posture before any repo-sensitive
    change. Establishes source-truth posture, worker dispatch and return verification,
    issue-goal conformance, world setup, seeded identity, difficulty, entropy, working
    knowledge (worktree and scratch locations, script discovery), skill routing (session
    bootstrap, Linear/GitHub, anti-slop, planning/execution, repo-specific, architecture),
    and policy references (workflow, validation, artifact, architecture guardrails,
    coding discipline, worker environment, mesh, guides, ADR freshness). Use when
    chat summaries, session busters, worker reports, or issue comments might be mistaken
    for live repo truth. This skill establishes posture first, then routes to specialist
    skills (wild-bunch-dotnet-architecture, wild-bunch-domain-modeling, wild-bunch-browser-game)
    for domain-specific work.
  use_when:
  - Use when bootstrapping the Wild Bunch repo posture before any repo-sensitive change.
  - Use when work touches HarleyBartles/wild-bunch, worker dispatch, worker return
    verification, source-truth claims, or issue-goal conformance.
  - Use when work touches world setup, seeded identity, difficulty, entropy, or starting
    inventory.
  - Use when an agent needs Wild Bunch working knowledge — worktree/scratch locations,
    script discovery, or environmental issue resolution.
  - Use when an agent needs skill routing for the Wild Bunch repo — session bootstrap,
    Linear/GitHub work, anti-slop/quality, planning/execution, repo-specific, or architecture
    skills.
  - Use when an agent needs policy reference routing — workflow policy, validation
    policy, artifact policy, architecture guardrails, coding discipline, worker environment,
    repo-skills policy, mesh policy, or guides.
  - Use when chat summaries, session busters, worker reports, or issue comments might
    be mistaken for live repo truth.
  do_not_use_when:
  - Do not use when Do not use for ordinary chat or questions that do not touch repo-sensitive
    work.
  - Do not use when Do not use as a substitute for the specialist skill that owns
    the actual task (wild-bunch-dotnet-architecture, wild-bunch-domain-modeling, wild-bunch-browser-game,
    ddd, cqrs-event-sourcing, ef-core, clean-architecture, etc.). Establish posture
    with this skill, then invoke the specialist for the domain-specific work.
license: MIT
---

# Wild Bunch Project Doctrine

Use this skill first when working on `HarleyBartles/wild-bunch`, or when a task needs the Wild Bunch setup doctrine. The live repo state on current `main` is the source of truth. Chat summaries, issue comments, session busters, and worker reports are support material only.

This skill establishes posture and then routes to specialist skills for domain-specific work. It does not replace `wild-bunch-dotnet-architecture`, `wild-bunch-domain-modeling`, `wild-bunch-browser-game`, or other specialist skills — it precedes them.

## Rules

- Treat `HarleyBartles/wild-bunch` as a mainline-only C#/.NET game project.
- Inspect live source before claiming current state.
- GPT prepares worker packets; Harley sends them; workers execute.
- When a task touches world setup, seed identity, difficulty, entropy, random selection, or starting inventory, read `references/difficulty-entropy-seeded-world-setup.md` first and keep it as the canonical anchor.
- Returns must include branch, commit, PR, validation, and issue-goal conformance notes.
- Scripts in `scripts/` are first-class surfaces. Before reporting environmental issues or running ad-hoc commands, read `references/policy-references.md` for the script discovery map.
- For specialist work (architecture, domain modeling, browser delivery, etc.), inspect current source and canonical repo decisions first, then invoke the smallest relevant specialist skill. See `references/skill-routing.md` for the full routing map.

## References

Read [Live repo posture](references/repo-posture.md) when a task needs source-truth posture, worker route boundaries, return/verification expectations, or script discovery guidance.

Read [Working Knowledge](references/working-knowledge.md) when a task needs Wild Bunch worktree/scratch locations, environmental issue resolution, or the required working knowledge checklist (architecture guardrails, coding discipline, frontend standards, validation policy, guides, dev overlay, web UI surfaces).

Read [Skill Routing](references/skill-routing.md) when a task needs the Wild Bunch skill routing map: session bootstrap, Linear/GitHub work, anti-slop/quality, planning/execution, repo-specific skills, or architecture skills.

Read [Policy References](references/policy-references.md) when a task needs the Wild Bunch policy reference map: workflow policy, validation policy, artifact policy, architecture guardrails, coding discipline, worker environment, repo-skills policy, mesh policy, guides, or ADR log freshness.

Read [Difficulty, Entropy, and Seeded Setup Doctrine](references/difficulty-entropy-seeded-world-setup.md) when a task needs world-start setup, seeded world identity, entropy, or difficulty posture.
