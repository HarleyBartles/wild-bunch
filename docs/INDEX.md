# Documentation Index

Lightweight table of contents for repo docs.

- [Testing Lanes](testing-lanes.md) - Defines the unit, acceptance, integration, and exceptional provider/storage test lanes.
- [Local PostgreSQL](local-postgresql.md) - Defines the persistent local development PostgreSQL convention and the repo-local setup/reset command.
- [Testing Posture](testing-posture.md) - Defines the backend and frontend testing ladders, minimum acceptable coverage for new code, and how manual browser evidence fits into the evidence model.
- [Product Roadmap and Milestone Scheme](product-roadmap.md) - Defines the repo-level horizon vocabulary, labels-versus-milestones rules, and when issues should carry a milestone.
- [ADR Log](adr/README.md) - Defines the durable architecture decision log and its current decision index.

When a validation command depends on a repo-local .NET tool, run `dotnet tool restore` first.
