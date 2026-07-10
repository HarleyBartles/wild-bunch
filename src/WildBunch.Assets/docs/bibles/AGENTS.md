# Asset Bible Routing

This directory holds the canonical asset bibles for the Assets project.

## Taxonomy

- Use `*-bible-master.md` for the umbrella document that owns the routing table
  and the shared contract for a family set.
- Use `*-bible.md` for family-specific or rule-specific guidance beneath that
  master.
- Keep one master doc per family set.
- Keep the family bibles and the master bibles together under the matching
  subfolder.

## Responsibility

- Before creating or updating a bible, check whether the rule belongs in the
  family master, a family-specific bible, or a project-level doc.
- If a bible looks stale, misleading, incomplete, or wrong while you are
  working, fix it as part of the same task instead of deferring the correction.
- Do not create a new naming branch when an existing bible family can absorb
  the rule; extend the current routing table or family master instead.
- If a new bible name would create ambiguity in the taxonomy, prefer the
  clearest existing naming pattern and update the routing table to match.
- Keep the routing tables prominent and current; they are the index into the
  family rules, not an afterthought.
