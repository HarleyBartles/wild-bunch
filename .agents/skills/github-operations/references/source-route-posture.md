# Source route posture

Load this reference only when GitHub or repository verification depends on source-route coverage, connector availability, indexed discovery, exact repository reads, local/disk evidence, or route failure analysis.

## Principle

Choose by capability and evidence need, not by fixed runtime tool names.

A skill should describe what kind of evidence is required. GPT should select the concrete available route from the current runtime. Do not assume the model-facing name of any connector. Do not let the mere presence of a bound connector, uploaded file, file library, or tool namespace trigger repository lookup or source-route work for ordinary chat.

## Capability classes

Use an available live repository-state capability for exact current-state checks: known issues, comments, pull requests, commits, files, refs, branches, labels, closure state, remote heads, comparisons, and authorized repository mutations.

Use an available indexed repository-search capability for broad discovery: stale-reference sweeps, multi-file inventories, duplicate hunts, codebase-wide checks, unknown-file discovery, and cited corpus-style reads. Use it only when the active runtime exposes the target repository as searchable content and the task needs that breadth.

Use local/disk evidence only when a lawful local route is actually available. In chat-only verification, treat clean worktree and local path claims as worker-reported unless independently observable.

## Failure and coverage

Do not treat one route failure as repository absence. State which layer failed or remains unverified: exact repository route, indexed search, connector scope, authorization, local disk, or runtime.

If broad indexed discovery would materially reduce false-green risk but is unavailable, say that broad discovery was not used. Proceed from exact routes only when exact coverage is sufficient for the judgment; otherwise mark the judgment AMBER or BLOCKED and name the missing capability.

Search discovery is not grounding by itself. Exact reads, current issue/PR state, commits, file contents, compares, or other observable repository evidence must support source claims.
