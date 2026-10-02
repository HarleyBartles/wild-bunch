# Wild Bunch operating-standard certification

These assessments are repository-owned. The immutable definitions are recorded
in `.agents/contracts/operating-standards.json`; changing a plugin payload does
not change those requirements. Structural checks report record and link facts,
not semantic compliance. Every change to a listed surface must maintain its
assessment here.

## root-agent-router

**Assessment:** Self-certified. The root router is 24 lines, within Wild
Bunch's 40-line budget, and points to authoritative contribution, workflow,
review, publication, testing, security, and subscription guidance. Only the
root file is an `AGENTS.md`; Wild Bunch has no need for additional routers at
arbitrary directory boundaries. `scripts/check_agent_routers.py` checks
tracked router size, required root routes, local links, and the 15-line,
single scoped sentence policy if a natural domain router is added. The
repository check runs it locally and in hosted CI. Human review remains
responsible for usefulness, safe scope, and whether a new router is needed.

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

**Assessment:** Implementation and local candidate behavior are verified;
hosted Linux parity evidence is pending the non-draft GitHub run. The tracked
`githooks/pre-commit` materializes the staged tree, preserves unrelated
unstaged and untracked work, runs the complete declared apply/check gate, and
stages only declared generated output. GitHub Actions checks out two commits,
sets up PostgreSQL, .NET 10, Node 20, and Python 3.12, then runs that same hook
in detached committed-tree parity mode. The behavior fixture verified that a
staged failing configuration remains a failure despite an unstaged repair,
and that unrelated tracked and untracked changes survive both failed and
successful runs. A fixture verifies CRLF normalization stays within the
declared generated output while unrelated CRLF work survives. The canonical
gate runs the 41 Python behavior tests. All current scoped checks have Windows
execution evidence. The complete hook also passed a clean detached clone of
commit `dcb7c177211da5bba81a4c7c0bf9eb22924bc7b7`, with no `.gitmodules` file
or Marketplace source submodule. GitHub intentionally skips Draft PR checks;
an actual hosted Linux result cannot be claimed until the PR is marked ready.
The standard remains not fully certified until that evidence is available.

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
identities. Payload refs intentionally follow upstream `main`. Devin has
repository rules but no declared plugin dependency. The six
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
