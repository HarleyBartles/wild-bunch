# Documentation Index

Lightweight table of contents for repo docs.

- [Testing Lanes](testing-lanes.md) - Defines the unit, acceptance, integration, and exceptional provider/storage test lanes.
- [Cleanup Doctrine](cleanup-doctrine.md) - Durable guidance for travel mapping ownership, aggregate authority, persistence codecs, and local SQLite artifact hygiene.
- [Architecture Hygiene](architecture-hygiene.md) - Recurring worker responsibilities for CQRS, aggregate authority, repository boundaries, hidden-state checks, and verification safety.

When a validation command depends on a repo-local .NET tool, run `dotnet tool restore` first.
