# Rooms, Mostly session pattern

For Rooms, Mostly, include repo stack availability and verification state for:

- `HarleyBartles/will-workspace`
- `HarleyBartles/rooms-mostly`
- `HarleyBartles/rooms-pit`
- `HarleyBartles/rooms-world`
- `HarleyBartles/rooms-manuscript`

Partition actor/domain state:

- Will: workspace orchestration and publication context
- Chris: Rooms project supervision and dispatch routing
- Albert: Pit archive/provenance/evidence work
- Brian: World canon management
- Derek: Manuscript/prose drafting

Preserve current running worker lanes, issue numbers, last verified heads, publication caveats, and exactly what the
next session must verify before accepting the buster as live truth.

## Rooms continuity extras

When a Rooms buster continues or supersedes an earlier one, include the chain fields but keep them subordinate to
current GitHub, repo, issue, and connector evidence. A prior buster can explain why the queue exists; it cannot prove
that an issue is still open, a worker is still running, or a repo head is still current.

For `recommended_next_action`, prefer a concrete first action such as:

- fetch a named issue and comments;
- compare a reported head to `main`;
- inspect a named skill or repo surface;
- continue with a named queue item after source-route verification.

If broad repo discovery is needed and `file_search` is not bound, say so in `next_session_first_checks` rather than
pretending exact GitHub API spot checks are equivalent.
