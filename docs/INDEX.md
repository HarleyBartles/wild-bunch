# Documentation Index

Lightweight table of contents for repo docs.

## Subdirectories

- [adr/](adr/INDEX.md) - Architecture decision record log with per-file freshness timestamps.
- [superpowers/](superpowers/INDEX.md) - Superpowers plan records and screenshots.
- [unslop/](unslop/INDEX.md) - Unslop drift-prevention profiles.

## Key files

- [Testing Lanes](testing-lanes.md) - Defines the unit, acceptance, integration, and exceptional provider/storage test lanes.
- [Local PostgreSQL](local-postgresql.md) - Defines the persistent local development PostgreSQL convention and the repo-local setup/reset command.
- [Testing Posture](testing-posture.md) - Defines the backend and frontend testing ladders, minimum acceptable coverage for new code, and how manual browser evidence fits into the evidence model.
- [Product Roadmap and Milestone Scheme](product-roadmap.md) - Defines the repo-level horizon vocabulary, labels-versus-milestones rules, and when issues should carry a milestone.
- [Unslop Style Guide](unslop-style-guide.md) - Defines the repo-specific language, copy, and naming rules for avoiding generic AI patterns.
- [Backend Architecture Unslop Profile](unslop/backend-architecture.md) - Defines the backend drift-prevention profile for Wild Bunch's selected Onion/DDD/CQRS/Event-Sourcing/projection posture.
- [Web Play-Surface UI Unslop Profile](../src/WildBunch.Web/docs/unslop/play-surface-ui.md) - Defines the web-specific review profile for player-facing game surfaces, HUD/shell placement, overlays, and React UI state ownership.
- [Dev Overlay Doctrine](../.agents/dev-overlay/DOCTRINE.md) - Binding agent-facing doctrine for dev overlay state/action boundary, panel ownership, and layout (lives in the agents mesh; this is the human-facing pointer).
- [ADR Log](adr/INDEX.md) - Architecture decision log index and per-file freshness timestamps.

When a validation command depends on a repo-local .NET tool, run `dotnet tool restore` first.

Back to [root](../INDEX.md)
