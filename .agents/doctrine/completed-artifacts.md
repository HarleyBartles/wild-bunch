# Completed planning artifacts

Plans and specifications guide only the work they describe. At the next
substantive slice, inspect prior plans, specifications, roadmaps, and
checkpoints with their related implementation and delivery evidence. Decide
whether each artifact's entire scope shipped, remains live, or was explicitly
abandoned. A checkbox, status marker, merged PR, or age can prompt review but
cannot decide the result by itself.

Retire an artifact only after its whole scope is complete or abandonment is
explicit, durable decisions and operating guidance are at their current
owners, and the successor branch contains that promoted knowledge. Keep
future or ambiguous work. Do not restore authority to an artifact because it
remains in Git. Preserve its history through the completing pull request, then
remove eligible artifacts and stale links in the next substantive slice.

Durable architecture decisions belong in `docs/decisions/`. Operating rules
belong in `.agents/doctrine/`, `.agents/runbooks/`, or `.agents/playbooks/`.
When scope or promotion is unclear, retain the artifact and state the evidence
needed for a later classification. Any marker is optional evidence; it is not
a discovery path or deletion prerequisite.
