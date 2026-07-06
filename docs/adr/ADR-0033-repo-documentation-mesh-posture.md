# ADR-0033 Repo Documentation Mesh — Agents, Index, and README Posture

## Status

`live`

## Dated Status History

- 2026-06-27 - live: Mesh policy installed in root `AGENTS.md`. Index mesh installed across the full folder tree (88 new `INDEX.md` files, all-or-nothing with documented exclusions). Dev-overlay doctrine installed at `.agents/docs/dev-overlay-doctrine.md` as the first agent-facing doctrine file living outside the root/scoped `AGENTS.md` nodes. Scoped `AGENTS.md` nodes updated to link the doctrine as required reading.

## Decision Type

architecture, process, documentation

## Related ADRs

- `complements`: ADR-0030 (dev overlay and dev endpoint namespace — the dev-overlay doctrine lives in the agents mesh and is linked from ADR-0030)
- `complements`: ADR-0032 (event-sourced dev saloon controls — the doctrine governs the saloon dev panel's state/action boundary)
- `depends on`: ADR-0028 (onion/DDD/CQRS/event-sourcing posture — the mesh does not change architecture, it organizes how agents and humans navigate it)

## Context

The repo had accumulated several documentation surfaces without a clear separation of concerns:

- `AGENTS.md` files at root and in scoped project folders contained agent-facing law, but there was no policy stating what belongs there vs elsewhere.
- `INDEX.md` files existed in a few folders (`docs/`, `docs/adr/`, `.agents/`) but coverage was incomplete and ad-hoc. There was no all-or-nothing rule, so a reader encountering an `INDEX.md` in one folder could not trust that sibling folders also had one.
- `README` files existed in some folders but their role relative to agent law was unclear. Some contained agent-facing content that should have been in `AGENTS.md`.
- Doctrine-length content (like the dev-overlay state/action boundary) had no home. Putting it in `AGENTS.md` would bloat the node; putting it in `docs/` would make it human-facing documentation rather than binding agent law.

The BUNCH-90 dev-overlay work required a durable doctrine file that future workers would encounter through the agents mesh, not through happenstance reading of `docs/`. This forced the question: where does operative agent doctrine live when it's too long for an `AGENTS.md` node?

## Decision

The repo uses three separate documentation/navigation surfaces with different jobs.

### 1. Agents mesh (`AGENTS.md` files)

**Job:** What is lawful here, what differs from upstream law, and what upstream law still applies.

- Scoped node mesh — not every folder needs an `AGENTS.md`. Add or update only at meaningful law-boundary nodes (root, project folders, sub-areas with distinct rules).
- No `AGENTS.md` should be siloed; scoped nodes must be understandable from root agent law and the upstream nodes between here and root.
- Agent-facing doctrine that is too long for an `AGENTS.md` node lives in a dedicated file under `.agents/docs/` (e.g., `.agents/docs/dev-overlay-doctrine.md`) and is linked from the relevant `AGENTS.md` nodes as required reading.
- The `.agents/INDEX.md` catalogue lists all doctrine files so they are discoverable from one place.

### 2. Index mesh (`INDEX.md` files)

**Job:** What is here, where can I go, and how do I get back to root?

- All-or-nothing if installed. Must cover the whole folder/file tree except explicit documented exclusions.
- Exclusions: `bin/`, `obj/` (build output), `node_modules/` (dependencies), `.git/` (git internals), `.local/` (local output), and `.agents/skills/` subdirectories (canonical skill-shaped folders where `SKILL.md` is the entrypoint; the skills index lives at `.agents/skills/INDEX.md`).
- Navigation surfaces, not doctrine. Orient traversal without duplicating source architecture.
- Each `INDEX.md` has: a heading, a one-line folder description, subdirectory links, key file links, and a back-to-parent link.

### 3. README files

**Job:** Human-facing explanation. Not a mesh. Not agent law.

- Do not put operative agent law only in README.
- If a README is stale or contains agent law, repair it or move the law into the agents mesh.
- READMEs may point humans at agent doctrine files, but the binding law lives in the agents mesh.

### Self-healing rule

If a worker reads stale or misleading `AGENTS.md`, `INDEX.md`, or README content, the worker repairs the relevant mesh in the same PR or returns AMBER with the exact deferred repair. This prevents the mesh from rotting silently.

### Doctrine file placement

Doctrine-length agent-facing content lives under `.agents/` in a dedicated subfolder:

- `.agents/docs/dev-overlay-doctrine.md` — dev overlay state/action boundary, panel ownership, layout, hidden truth, backend authority, closeout proof.

Future doctrine files follow the same pattern: `.agents/docs/<topic>.md`, linked from the relevant `AGENTS.md` nodes and catalogued in `.agents/INDEX.md`.

`docs/` and README surfaces point humans at the doctrine but do not own the binding law. The key distinction: AGENTS mesh owns law; INDEX mesh owns navigation; README/docs are human-facing explanation unless deliberately linked from AGENTS as agent doctrine.

## Options Considered and Rejected

- **Put doctrine in `docs/doctrine/`.** Rejected: `docs/` is human-facing. Binding agent law should live where agents are expected to find law (the agents mesh). A `docs/` pointer is fine, but the primary home should be `.agents/`.
- **Put doctrine directly in `AGENTS.md`.** Rejected: the dev-overlay doctrine is ~120 lines. Inlining it would bloat the root `AGENTS.md` and make it harder to scan for the high-level rules. A dedicated file linked from `AGENTS.md` is cleaner.
- **Make the index mesh optional/per-folder.** Rejected: partial index coverage is worse than none. A reader who finds an `INDEX.md` in one folder expects sibling folders to have one too. All-or-nothing with documented exclusions is the only consistent posture.
- **Merge agents mesh and index mesh.** Rejected: `AGENTS.md` answers "what is lawful here?" and `INDEX.md` answers "what is here and where can I go?" These are different questions with different content. Merging them would produce files that are both too long and too shallow for either job.
- **Put agent law in README.** Rejected: README is human-facing. Workers who need agent law should not have to read README files to find it. The agents mesh is the authoritative source.

## Consequences

- The repo has 89 `INDEX.md` files covering the full folder tree (excluding build output, dependencies, git internals, local output, and skill folders).
- Agent-facing doctrine has a clear home: `AGENTS.md` nodes for scoped law, `.agents/docs/<topic>.md` for doctrine-length content, `.agents/INDEX.md` for discovery.
- `docs/` and README files remain human-facing. They may point at agent doctrine but do not own it.
- The self-healing rule means the mesh stays current: any worker who reads stale mesh content is responsible for repairing it in the same PR.
- Future doctrine files (e.g., for travel dev, casefile dev, suspect dev) follow the same pattern without needing a new ADR.
- The mesh policy adds a small ongoing cost: new folders need `INDEX.md` files, and new doctrine needs to be linked from the agents mesh. This cost is bounded by the self-healing rule and is far smaller than the cost of navigating a repo with inconsistent or missing documentation.


