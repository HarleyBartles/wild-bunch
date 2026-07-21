# Authority record for dotnet

## Scholarly citation

- Microsoft. ".NET documentation." https://github.com/dotnet/docs (accessed 2026-07-20).
- Microsoft. "ASP.NET Core documentation." https://github.com/dotnet/AspNetCore.Docs (accessed 2026-07-20).
- Both repositories' documentation content is licensed under CC-BY-4.0.

## Derivation boundary

- No local vendored snapshot is retained; authority is derived from the cited sources.
- Derived: C# language fundamentals, .NET runtime primitives, ASP.NET Core minimal APIs and
  middleware, common data access and testing strategies, Clean Architecture, DDD, and vertical
  slice patterns as synthesized into the operational guidance.
- Outside scope: .NET runtime source code, Visual Studio tooling, Azure services, deep EF Core
  query tuning, cloud deployment, and infrastructure provisioning.

## Attribution

- .NET documentation and ASP.NET Core documentation used under CC-BY-4.0.
- Additional .NET ecosystem patterns were synthesized from the `dotnet-claude-kit` upstream
  skills (modern-csharp, ef-core, testing, clean-architecture, ddd, vertical-slice) and are
  represented as original operational guidance rather than verbatim copies.

## Human review

- Reviewer: Harley Bartles
- Date: 2026-07-20
- Decision: Approved. Operational SKILL.md text contains no inline citations.
## Authority record integrity

- The `content_sha256` value in `authority.yaml` and the `reconciled_against`
  values in `authority.yaml` and `source-map.yaml` are the SHA-256 of this
  `CITATIONS.md` file.
