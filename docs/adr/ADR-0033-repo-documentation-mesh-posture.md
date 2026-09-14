# ADR-0033 Repo Documentation Mesh — Agents, Index, and README Posture

## Status

`live`

## Dated Status History

- 2026-06-27 - live: Mesh policy installed in root `AGENTS.md`. Index mesh installed across the full folder tree (88 new `INDEX.md` files, all-or-nothing with documented exclusions). Dev-overlay doctrine installed at `.agents/docs/dev-overlay-doctrine.md` as the first agent-facing doctrine file living outside the root/scoped `AGENTS.md` nodes. Scoped `AGENTS.md` nodes updated to link the doctrine as required reading.
- 2026-09-14 - clarified: binding agent rules live in `.agents/doctrine/`,
  executable agreements live in `.agents/contracts/`, and repo-local procedures
  live in `.agents/runbooks/`. `.agents/docs/` is not an authority catch-all.

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
- Agent-facing doctrine that is too long for an `AGENTS.md` node lives under
  `.agents/doctrine/` and is linked from the relevant routing surface.
- The generated `.agents/doctrine/INDEX.md` lists doctrine files; root
  `.agents/INDEX.md` routes to that directory.

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

### Authority placement

Agent-facing authority uses explicit homes:

- `.agents/doctrine/` — binding repository rules and invariants.
- `.agents/contracts/` — executable or independently consumed agreements.
- `.agents/runbooks/` — repo-local procedures and portable-workflow deltas.

Ordinary `docs/` and README surfaces remain human-facing explanation. Generated
indexes provide navigation only; they do not confer authority on their entries.

## Options Considered and Rejected

- **Put doctrine in `docs/doctrine/`.** Rejected: `docs/` is human-facing. Binding agent law should live where agents are expected to find law (the agents mesh). A `docs/` pointer is fine, but the primary home should be `.agents/`.
- **Put doctrine directly in `AGENTS.md`.** Rejected: the dev-overlay doctrine is ~120 lines. Inlining it would bloat the root `AGENTS.md` and make it harder to scan for the high-level rules. A dedicated file linked from `AGENTS.md` is cleaner.
- **Make the index mesh optional/per-folder.** Rejected: partial index coverage is worse than none. A reader who finds an `INDEX.md` in one folder expects sibling folders to have one too. All-or-nothing with documented exclusions is the only consistent posture.
- **Merge agents mesh and index mesh.** Rejected: `AGENTS.md` answers "what is lawful here?" and `INDEX.md` answers "what is here and where can I go?" These are different questions with different content. Merging them would produce files that are both too long and too shallow for either job.
- **Put agent law in README.** Rejected: README is human-facing. Workers who need agent law should not have to read README files to find it. The agents mesh is the authoritative source.

## Consequences

- The repo has 89 `INDEX.md` files covering the full folder tree (excluding build output, dependencies, git internals, local output, and skill folders).
- Agent-facing authority has explicit homes under `.agents/doctrine/`,
  `.agents/contracts/`, and `.agents/runbooks/`; routing surfaces point to them.
- `docs/` and README files remain human-facing. They may point at agent doctrine but do not own it.
- The self-healing rule means the mesh stays current: any worker who reads stale mesh content is responsible for repairing it in the same PR.
- Future authority-bearing files use the matching explicit home without needing
  a new ADR.
- The mesh policy adds a small ongoing cost: new folders need `INDEX.md` files, and new doctrine needs to be linked from the agents mesh. This cost is bounded by the self-healing rule and is far smaller than the cost of navigating a repo with inconsistent or missing documentation.
