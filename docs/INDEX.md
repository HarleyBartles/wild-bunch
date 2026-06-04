# Documentation Index

Lightweight table of contents for repo docs.

- [Testing Lanes](testing-lanes.md) - Defines the unit, acceptance, integration, and exceptional provider/storage test lanes.
- [Local PostgreSQL](local-postgresql.md) - Defines the persistent local development PostgreSQL convention and the repo-local setup/reset command.
- [Testing Posture](testing-posture.md) - Defines the backend and frontend testing ladders, minimum acceptable coverage for new code, and how manual browser evidence fits into the evidence model.
- [BUNCH-24 Cloud Codex Publication Probe](bunch-24-cloud-codex-publication.md) - Records the docs-only probe used to test Cloud Codex GitHub publication evidence.

When a validation command depends on a repo-local .NET tool, run `dotnet tool restore` first.
