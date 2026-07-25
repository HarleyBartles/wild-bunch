# Risk Gates operational guidance

## When to apply

Use when the risk-gates skill loaded and the proposed action needs more detail than the routing table provides.

## Risk gate usage

1. Name the exact next action and the durable surface it would affect.
2. Use the routing table in `SKILL.md` to select only the gates that match.
3. Read the matching gate references; do not read all gate references by default.
4. Classify the result as `green`, `amber`, `red`, or `blocked`.
5. Resolve forced decisions internally; surface only real, unresolved choices.

## Safety gate

Use before destructive or irreversible actions such as delete, truncate, drop, rewrite history, bulk update, or permission changes.

- The target must be explicitly identified.
- The scope must be confirmed by source or user authority.
- A backup, dry-run, or rollback path should exist when feasible.
- Use `internal_mode` for clearly scoped, recoverable actions.
- Use `interactive_mode` when recovery is uncertain.
- Use `blocked_mode` when authority or target evidence is missing.

## Related references

- Leveson, Nancy G. "Engineering a Safer World: Systems Thinking Applied to Safety." https://direct.mit.edu/books/oa-monograph/2908/Engineering-a-Safer-WorldSystems-Thinking-Applied
- US Department of Energy. "Two-Person Control: A Brief History and Modern Industry Practices." https://www.osti.gov/servlets/purl/1374246
- nibzard. "Hook-based safety guard rails." https://github.com/nibzard/awesome-agentic-patterns/blob/main/patterns/hook-based-safety-guard-rails.md
