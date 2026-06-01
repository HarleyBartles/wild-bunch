# Documentation Index

Lightweight table of contents for repo docs.

- [Testing Lanes](testing-lanes.md) - Defines the unit, acceptance, integration, and exceptional provider/storage test lanes.
- [Local PostgreSQL](local-postgresql.md) - Defines the persistent local development PostgreSQL convention and the repo-local setup/reset command.
- [UI Browser Check Playbook](ui-browser-check-playbook.md) - Defines when manual browser checks are appropriate, how to run them locally, and how to report evidence separately from automated validation.

When a validation command depends on a repo-local .NET tool, run `dotnet tool restore` first.
