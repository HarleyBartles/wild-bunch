# Repo Posture

- Wild Bunch is a mainline-only C#/.NET game project in `HarleyBartles/wild-bunch`.
- Current `main` is the live truth.
- Do not rely on chat summaries, issue comments, or worker reports as final state.
- Inspect live source before asserting what the repo does today.
- GPT prepares the worker packet.
- Harley sends the packet.
- The worker executes the packet and reports the result.
- Return payloads should include branch, commit SHA, PR URL or number, validation commands, and issue-goal conformance notes.

## Script discovery

Scripts in `scripts/` are first-class surfaces. Before reporting environmental issues or running ad-hoc commands, read `scripts/AGENTS.md` and use the provided scripts. See [Policy References](policy-references.md) for the full script discovery map.

## Specialist skill discovery

For specialist work (architecture, domain modeling, browser delivery, testing, etc.), inspect current source and canonical repo decisions first, then invoke the smallest relevant specialist skill. See [Skill Routing](skill-routing.md) for the full routing map.

## Companion references

- [Working Knowledge](working-knowledge.md) — worktree/scratch locations, required reading before specific work types.
- [Skill Routing](skill-routing.md) — full skill routing map for the Wild Bunch repo.
- [Policy References](policy-references.md) — policy reference map, script discovery, ADR log freshness.
- [Difficulty, Entropy, and Seeded Setup Doctrine](difficulty-entropy-seeded-world-setup.md) — world-start setup, seeded identity, entropy, difficulty.
