# Source and evidence posture

Load this reference only when a classified task actually requires source evidence, connector or tool-surface diagnosis, repository claims, unavailable-route claims, or audit output about what was inspected.

## Capability-based route selection

Skills describe the evidence capability required; the runtime chooses the concrete tool from the active session surface.

Use an available exact source-state capability for known targets such as named issues, comments, repository paths, commits, branches, pull requests, compares, durable artifacts, or explicitly authorized mutations.

Use an available indexed search or corpus-reading capability only for broad discovery tasks such as stale-reference sweeps, duplicate checks, unknown-file discovery, multi-file inventories, and corpus-style reads, and only when the active runtime explicitly exposes the relevant source as searchable content.

Use uploaded-file or package-inspection capabilities only for uploaded artifacts, file-library items, or source/package mirrors. Do not treat uploaded files or package mirrors as live repo truth unless a project rule or source evidence makes that relationship explicit.

Do not assume the model-facing name of any connector or tool. Do not let the mere presence of a bound connector, file library, uploaded file, indexed source, or tool namespace trigger source work for ordinary chat.

## Evidence claims

Do not claim a repo, connector, issue, file, prompt, image, or artifact was inspected unless an actual route inspected it in the current session or an explicitly trusted current source provides it.

Separate broad search, exact source-state reads, uploaded files, conversation-derived material, reports, and inference. If a route fails, state the layer that failed rather than treating one failure as source absence.
