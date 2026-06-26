# Dev

Dev-only endpoints and the dev role guard that gates them.

## Key files

- [DevEndpoints.cs](DevEndpoints.cs) - Dev endpoint registrations (travel/saloon overrides, session audit).
- [DevRoleGuard.cs](DevRoleGuard.cs) - Guard that denies dev endpoints outside dev roles.
- [DevAccessDeniedException.cs](DevAccessDeniedException.cs) - Exception thrown when dev access is denied.

Back to [WildBunch.Api/](../INDEX.md)
