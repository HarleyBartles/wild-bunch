# Documentation Index

Lightweight table of contents for repo docs.

- [Testing Lanes](testing-lanes.md) - Defines the unit, acceptance, integration, and exceptional provider/storage test lanes.
- [Cleanup Doctrine](cleanup-doctrine.md) - Durable guidance for travel mapping ownership, aggregate authority, persistence codecs, and local SQLite artifact hygiene.
- [PostgreSQL JSONB Persistence Plan](postgresql-jsonb-persistence-plan.md) - Source-backed staged plan for adding PostgreSQL as an additional persistence provider while keeping SQLite as the local/dev default.

Agent-facing doctrine lives under [../.agents/INDEX.md](../.agents/INDEX.md).

When a validation command depends on a repo-local .NET tool, run `dotnet tool restore` first.
