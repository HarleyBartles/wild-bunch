# Wild Bunch operating-standard certification

These assessments are repository-owned. The immutable definitions are recorded
in `.agents/contracts/operating-standards.json`; changing a plugin payload does
not change those requirements. Structural checks report record and link facts,
not semantic compliance. Every change to a listed surface must maintain its
assessment here.

## root-agent-router

**Assessment:** Self-certified. The root router remains within Wild Bunch's 40-line budget and points to authoritative contribution, workflow, review, publication, testing, security, subscription and unslop guidance. Thin scoped routers cover established scripts, web, game-content, asset and agent-guidance boundaries; [placement policy](../doctrine/repo-runbook-policy.md#scoped-agent-entrypoints) records their one-sentence scope, read condition and outside-scope disqualifier. They are the scoped pointers for both Codex and Devin; the separate Devin rules were replaced. `tools/check_agent_routers.py` checks tracked router size, required root routes, local links and the 15-line, single scoped sentence policy through the canonical hook and CI. Semantic review remains responsible for usefulness, safe scope, disqualifiers, harness-independent pointers and whether a new router is needed.

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

The [feature-matrix playbook](../playbooks/feature-matrix.md) owns maintenance of [docs/features.md](../../docs/features.md) and is routed from design, planning, implementation, code-review and PR stages. Authors update it when feature promises, dependencies, dispositions or evidence assessments change; reviewers compare the actual diff independently. Semantic review assesses promise truth and directed dependency meaning; no heading, file-existence or table-shape test substitutes for that work.

## command-bus

**Assessment:** Self-certified against the pinned definition at `a9d9f280316a87ac66cb2653384bc30a683603f0`. `tools/run.py` is the repository command bus; `tools/README.md` lists targets, modes, prerequisites, side effects, arguments, and ownership. Targets provide top-level discovery, target help, explicit meaningful modes, reject unsupported or conflicting requests before work, forward supported arguments, and preserve child output and exit status. `format --check` and `lint --check` do not mutate maintained files; `format --apply` requires explicit file paths or `--all`, rejects paths outside the repository and excluded content, and reports changed paths. Ruff owns Python style, Prettier and ESLint own web style, and SDK analyzers plus `dotnet format` own C#. `ci --check` is the check-only fail-fast gate for the staged candidate and hosted committed tree. It runs whitespace and repository contracts, then Python, web, and .NET format/lint, then .NET build and web typecheck, and only then behavioral tests and the web build. A failed early lint operation prevents later compilation and tests. `ci --check --diagnostics` is manual troubleshooting only and is not called by pre-commit or hosted CI. `setup-hooks --apply` owns the local Git configuration change instead of hiding it in CI. Focused build/test targets delegate to existing .NET, npm, and pytest tools. `tools/tests/` owns bus and checker behavior tests; `scripts/tests/` owns hook/standalone script behavior tests, including staged style rejection without candidate or unstaged-work mutation. The target table and behavior tests prove CLI dispatch, failure ordering, path boundaries, and check/apply behavior; review assesses target inventory, help truth, argument ownership, and whether new operations belong in the bus. This standard does not require adopting the optional starter or prescribing a module ABI.

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

Branch custody adds [ADR-0049](../../docs/decisions/ADR-0049-planning-custody-across-integration-and-promotion.md) without changing this subscription pin: agents perform semantic preflight on develop before a main-bound cut, asking about each unclear whole-scope classification. Durable product rules move to `docs/gameplay.md`, deferred intent to `docs/features.md`, and historical references use immutable source links. Main carries no planning, while reconciliation restores exactly the current pre-merge develop planning snapshot. The check-only main guard enforces indexed-path exclusion; review judges completion, knowledge promotion and disguised planning. The exclusion guard cannot certify semantic completion, and documented reconciliation is not proof that a promoted merge has occurred.

## repo-plugin-subscriptions

**Assessment:** Self-certified for repository declarations and authored-skill
custody. `.agents/plugins/marketplace.json` declares Game Studio, .NET Pack,
Architecture Pack, and Frontend Pack as Git-subdirectory dependencies;
`.codex/config.toml` registers the repository catalog and enables these four
identities. Payload refs intentionally follow upstream `main`. Codex and Devin use the same scoped AGENTS pointers; Devin has no separate rule layer or declared plugin dependency. The six
`.agents/skills/*/SKILL.md` files are repository-authored, and each frontmatter
name matches its directory. `tools/check_plugin_subscriptions.py` checks
local Codex syntax, paths, selectors, catalog registration, and activation
targets; behavior tests cover invalid paths, dangling activation, and inactive
catalog entries. The checker does not fetch or prove access, trust,
authentication, installation, or current runtime loading. Fresh-clone users still need Codex, repository
trust, and access to the declared Git sources; use `codex plugin marketplace
upgrade wild-bunch` to refresh the catalog and payloads.

`release-verify --check` is a focused local release operation that requires clean source, runs `npm ci` from the committed lockfile before bundle construction, builds the API and both web variants, binds their file-tree digests to source/version in an ignored immutable bundle, and verifies existing output without reinstalling or rebuilding. It does not publish or deploy.

## gitflow

**Assessment:** Partially certified against the immutable definition at
`7a20ef191bb6be3b67fbbb9e86bd7b0050f96fc5`. The repository declares `develop`
as its integration/default branch and `main` as its stable release line.
Ordinary branches start from current `develop` and return by PR; release and
hotfix promotion and reconciliation use merge commits. The [release lifecycle
runbook](../runbooks/release.md) defines those routes, affected release-line
reconciliation, and publication-time verification. GitHub state observed on
2026-10-15 reports `develop` as the default, allows merge, squash, and rebase
methods, and returns no branch-protection configuration for `develop`. These
are dated hosting facts, not guarantees; the runbook requires checking them
again before publication. The branch model and routing are documented, but
release and hotfix promotion/reconciliation have not yet been exercised as
completed release flows. Do not claim hosted enforcement or end-to-end
conformance from documentation alone.

## semver

**Assessment:** Partially certified against the immutable definition at
`12c86626f80cad1a2ac05b2f6690c882a3d5959a`. The version subject is the Wild
Bunch browser-game product. During `0.y.z`, breaking changes are allowed and
there is no public API or gameplay-compatibility promise; `0.1.0` names the
qualified initial baseline, not historical-playthrough preservation. The
future `1.0.0` compatibility contract is intentionally undecided. The
repository authors one application version in `Directory.Build.props`; the
Vite build reads effective MSBuild `Version` and emits `dist/version.json`,
while the command bus checks SemVer syntax, duplicate npm root versions, and
generated identity equality after the web build. Development checkpoints,
release candidates, and stable versions follow ADR-0048 and the joint
Gitflow/SemVer rules. Focused tests reject malformed and stale identities;
`release-verify --check` binds the exact API, public-web, and
playtest-web artifact trees produced from one clean full source commit and
authored version, and re-verifies an existing immutable bundle without
rebuilding. This records the bytes produced by that invocation; it does not
claim independent builds are reproducible. The browser lane checks built bootstrap and version identity alongside application-specific player contracts. Local source-bound bundles and browser checks do not establish independent
build reproducibility, hosted release or hotfix behavior, stable publication,
cloud deployment, or complete feature implementation.
