# Cleanup Doctrine

Use these rules when touching architecture hotspots in Wild Bunch:

- `GameSession` remains the live-play aggregate root and the only authority for gameplay mutation.
- Travel, journey, and encounter DTOs have one public mapping owner: `TravelMapper`.
- `GameSessionMapper` should delegate travel mapping instead of carrying parallel private travel DTO logic.
- Persistence snapshot conversion belongs in `WildBunch.Persistence` and should be split by coherent domain area when the serializer grows large.
- Snapshot codecs should preserve current JSON behavior first, then improve readability and ownership.
- Temporary cockpit or debug-shell UI should stay lightweight; do not over-refactor it for architecture polish alone.
- Repo-local SQLite files are disposable dev artifacts and should live outside `src/`, under repo-root `.local/`.
- Architecture cleanup should leave a clear before/after ownership boundary that tests can prove.
