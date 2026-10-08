# Wild Bunch operating-standard certification

These assessments are repository-owned. The immutable definitions are recorded
in `.agents/contracts/operating-standards.json`; changing a plugin payload does
not change those requirements. Structural checks report record and link facts,
not semantic compliance. Every change to a listed surface must maintain its
assessment here.

## root-agent-router

**Assessment:** Self-certified. The root router remains within Wild Bunch's 40-line budget and points to authoritative contribution, workflow, review, publication, testing, security, subscription and unslop guidance. Thin scoped routers cover established scripts, web, game-content, asset and agent-guidance boundaries; [placement policy](../doctrine/repo-runbook-policy.md#scoped-agent-entrypoints) records their one-sentence scope, read condition and outside-scope disqualifier. They are the scoped pointers for both Codex and Devin; the separate Devin rules were replaced. `scripts/check_agent_routers.py` checks tracked router size, required root routes, local links and the 15-line, single scoped sentence policy through the canonical hook and CI. Semantic review remains responsible for usefulness, safe scope, disqualifiers, harness-independent pointers and whether a new router is needed.

## unslop

**Assessment:** Self-certified for the repository-owned maintenance and routing mechanism. Canonical profiles and observations live in `.agents/unslop/`; the five existing profiles were moved there without retaining competing contract/scoped copies. The central [unslop playbook](../playbooks/unslop.md) selects applicable profiles, routes their observations and defines evidence-based maintenance; `README.md` is the profile catalogue. `routing.md` addresses the concrete discovery gap recorded in `observations.md`. Historical recurrence and prior agents' reading or compliance are unknown, not inferred from existing profile content.

Root `AGENTS.md`, `CONTRIBUTING.md`, `REVIEW.md`, lifecycle runbooks, topical playbooks, owning doctrine, all six repository skills and thin scoped `AGENTS.md` pointers route through the central unslop playbook. Decision-record obligations are routed to the decision-record playbook; PR authors and reviewers independently compare the actual diff with relevant ADRs. Profile changes must preserve routes, applicability, corrective behavior and false-positive boundaries. Occurrence records distinguish separate incidents from duplicate reports and distinguish missing discovery, ineffective correction and ignored useful guidance. Agents revise, narrow, consolidate or retire guards based on those observations while preserving useful evidence.

Mechanical checks establish subscription structure, root routes and local link facts. Semantic review confirms that work-point guidance routes through the selector and that U-001 maps to a corrective guard and feedback loop; U-002 records removal of an obsolete location-only detector rather than freezing the new layout in a replacement test. Neither link existence nor a passing CI gate proves that a later agent read a guard or that it prevented a defect. Continue to assess observed reach and effect, leave unknown outcomes unknown, and update guards and this assessment when evidence changes the mechanism. This adoption adds no required plugin dependency or telemetry service.

## runbook-composition

**Assessment:** Self-certified. `.agents/doctrine/repo-runbook-policy.md`
routes the five lifecycle stages to the maintained design, planning,
implementation, code-review, and pull-request guides. Those documents explain
when their stage applies and how work is performed and handed off. The table is
Wild Bunch's current route map, not an AOM-mandated inventory. Local links are
reviewed with their owning edits; stage and reference usefulness receive
semantic review, not a heading-count test.

## playbook-composition

**Assessment:** Self-certified. The same repository policy routes cross-stage
concerns to maintained guidance under `.agents/playbooks/`. Each playbook
declares when it applies and composes only available repository doctrine,
commands, and skills. Authored skills live in `.agents/skills/`; declared
plugin skills are named with their plugin where relevant; ambient tools are
described by capability and are not promised to every clone. Stage runbooks
route contributors to relevant testing and implementation concerns. Useful
scope, reachability, category, and upkeep are assessed by maintainers; the
router checker checks links and does not score content.

## tracked-validation-hook

**Assessment:** Self-certified against the pinned definition at
`a9d9f280316a87ac66cb2653384bc30a683603f0`. Maintained-file normalization,
formatting, generation, and repairs belong in explicit apply targets; the hook
rejects a candidate that needs repair without changing or staging it. The
tracked hook materializes the staged candidate locally, runs only the declared
check command, and restores unrelated unstaged and untracked work. Hosted mode
requires a clean detached checkout at the declared `HEAD`, runs the same check
command in place, and verifies that `HEAD`, the index, and candidate worktree
remain unchanged. Behavior tests observe stale generated content rejection,
partial-staging isolation, success and failure restoration, and hosted state
preservation. The canonical gate is run locally on Windows and by the required
hosted Linux status check on each proposed commit; parity claims rely on the
successful check for the exact PR head, never a prior revision.

## completed-artifact-custody

**Assessment:** Self-certified. `.agents/doctrine/completed-artifacts.md` and
`.agents/playbooks/completing-plans.md` direct each successor slice to inspect
prior artifacts with their complete implementation and delivery evidence.
Classification covers the full scope: shipped, live, or explicitly
abandoned. Durable decisions move to `docs/decisions/`; operating knowledge
moves to its doctrine, runbook, or playbook owner. Future and ambiguous work
is retained. Checkboxes, markers, age, and merged PRs prompt investigation but
do not determine completion or serve as a required deletion marker. The
maintainer records evidence and promotes durable content before retiring an
artifact and stale links. The change that updates this policy keeps its own
plan and spec through its completing PR.

## repo-plugin-subscriptions

**Assessment:** Self-certified for repository declarations and authored-skill
custody. `.agents/plugins/marketplace.json` declares Game Studio, .NET Pack,
Architecture Pack, and Frontend Pack as Git-subdirectory dependencies;
`.codex/config.toml` registers the repository catalog and enables these four
identities. Payload refs intentionally follow upstream `main`. Codex and Devin use the same scoped AGENTS pointers; Devin has no separate rule layer or declared plugin dependency. The six
`.agents/skills/*/SKILL.md` files are repository-authored, and each frontmatter
name matches its directory. `scripts/check_plugin_subscriptions.py` checks
local Codex syntax, paths, selectors, catalog registration, and activation
targets; behavior tests cover invalid paths, dangling activation, and inactive
catalog entries. The checker does not fetch or prove access, trust,
authentication, installation, or current runtime loading. The 2026-09-29
field-test report under `.agents/docs/` records installed and invocable plugin
evidence on Codex CLI 0.159.0. Fresh-clone users still need Codex, repository
trust, and access to the declared Git sources; use `codex plugin marketplace
upgrade wild-bunch` to refresh the catalog and payloads.
