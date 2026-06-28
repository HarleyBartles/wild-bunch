# Durable doctrine routing

Choose the smallest durable home for each rule or asset.

## Source-truth hierarchy

- System prompt: tiny boot/routing invariants needed before skills load.
- cross-runtime doctrine skill: cross-project rules and contracts not owned elsewhere.
- Specific GPT-native skill: detailed workflow, output contract, checklist, or tool procedure.
- Canonical agent asset repo: versioned source truth for GPT-native skill sources, Codex plugin marketplaces, repo overlays, provenance, package evidence, and rollout metadata.
- Codex plugin marketplace: generic worker capabilities that Codex can install natively, especially GitHub, Linear, review, CI, debugging, planning, and other reusable workflow plugins.
- Repo overlay / repo-resident skills: project-specific domain anchors, validation lanes, local runtime expectations, and protected surfaces that generic plugins cannot know.
- Repo playbook: canonical staged project workflows and production gates.
- Repo docs/issues: auditable project truth, decisions, receipts, and planned work.
- Linear issue/project/comment: planning/control-plane truth, worker event log, side-discovery persistence, prioritization, and handoff state.
- Project sources/assets: inspectable evidence or output materials, not behavior law by default.
- Saved memory: short personal preferences only after verification.

## Installed versus source

Installed GPT skills and Codex plugins are deployment targets. Treat them as current runtime capability, not as durable source truth, when a canonical repo source exists or is being created.

A GPT-native skill update should eventually become:

1. edit the canonical repo source;
2. validate/package through the native skill stack;
3. install or overwrite the deployed skill;
4. record source commit, package hash, and rollout evidence.

Until the canonical repo exists for a skill, chat-packaged skill updates can be lawful deployment work, but do not confuse that temporary route with the desired source-of-truth model.

## Plugin and overlay routing

Prefer a native Codex plugin marketplace for generic worker enablement. Do not hand-roll repo skills for generic GitHub, Linear, PR, CI, debugging, planning, or review behavior if a trusted plugin provides it boringly.

Use repo overlays only for local project law: domain anchors, validation lanes, runtime setup expectations, architecture boundaries, and project-specific done evidence.

Do not let a plugin marketplace replace project-specific domain law or repository source proof. Plugins equip workers; they do not prove issue-goal conformance.

## Growth rule

If a rule grows, branch it into a reference, skill, plugin, repo overlay, or issue. Keep `SKILL.md` files as control planes and route maps.

Do not preserve obsolete thick doctrine as an equal fallback when a product-backed workflow now owns the normal route. Keep legacy Plan B guidance compact and explicit.

