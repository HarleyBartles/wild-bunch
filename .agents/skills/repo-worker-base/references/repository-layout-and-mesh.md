# Repository layout and mesh

## Read when

Read before initializing or restructuring README, AGENTS.md, INDEX.md,
doctrine, docs, plans, specifications, scripts, or self-healing mesh surfaces.

## Boundary map

One authority owns each rule. Repository doctrine owns durable policy;
repository-local guides own stage-specific paths, commands, exclusions, CI,
and exceptions; playbooks or runbooks own repeatable operations; skills own
portable decision and technique guidance. Do not duplicate an authority's
rules in a router.

Keep AGENTS.md files as thin scoped law routers, README files as human-facing
explanation, and generated INDEX.md files as navigation only. Doctrine carries
stable metadata identifying its owner, scope, status, and canonical home.
Canonical homes are declared by the consuming repository; its explicitly
retired legacy homes are forbidden for new authored content.

## Custody and proof

Local authored skills remain in their canonical repository source home.
Installers, projections, and caches are runtime surfaces and must not prune or
replace authored custody. Cleanup removes stale disposable or generated
surfaces only after proving their source and publication requirements survive.

When authored mesh law changes, repair it in scope. When generated navigation
changes, regenerate the whole mesh through its generator and run its check
mode; never hand-edit generated indexes. Publication proof remains a
GitHub-visible PR or explicitly authorized direct-main commit, not local files
or generator output.
