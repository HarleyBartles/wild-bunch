# Dotnet operational guidance

## C# language fundamentals

Prefer modern C# idioms that make illegal states unrepresentable and keep code readable.

- Use `record` for immutable data transfer objects and value objects; use `record class` for
  reference semantics and `readonly record struct` for small stack-allocated values.
- Use pattern matching with `is`, `switch` expressions, and list/property patterns for clear,
  expressive branching. Avoid deeply nested patterns that obscure intent.
- Enable nullable reference types and treat `null` as an explicit part of the type system;
  use `required` and `init` members to enforce initialization.
- Use generics for reusable algorithms and type-safe collections; avoid boxing where possible.
- Use `async`/`await` for I/O-bound work and `CancellationToken` for cooperative cancellation.
  Never block on async calls with `.Result` or `.Wait()`.
- Use collection expressions `[]` and spread `..` for collection creation; use raw string
  literals `"""` for multi-line JSON, SQL, or XML.
- Use `Span<T>` and `Memory<T>` for low-allocation slicing of strings, arrays, and buffers.

## .NET runtime

Rely on the built-in host and framework primitives rather than custom abstractions.

- Register application services with `IServiceCollection` and resolve them through constructor
  injection. Avoid service locator patterns.
- Read configuration with `IConfiguration`; bind related settings to strongly-typed options
  classes with `IOptions<T>` and `IOptionsMonitor<T>`.
- Log with `ILogger<T>` and structured logging templates. Keep log messages free of sensitive
  data and avoid string interpolation in log message templates.
- Use `TimeProvider` for time-dependent code so tests can inject `FakeTimeProvider` instead of
  relying on `DateTime.Now`.
- Prefer `IHostApplicationBuilder`/`WebApplicationBuilder` for standard setup and teardown.

## ASP.NET Core

Favor minimal APIs and explicit routing over heavy controller plumbing.

- Define endpoints with `IEndpointRouteBuilder` and `MapGet`/`MapPost`/`MapPut`/`MapDelete`.
  Group routes with `MapGroup` and attach metadata such as route names and OpenAPI tags.
- Use endpoint filters for validation, authorization, and mapping `Result<T>` to HTTP responses.
  Keep endpoint bodies thin; delegate to handlers or domain services.
- Use middleware only for cross-cutting concerns such as correlation IDs, request logging, and
  global exception handling. Order middleware carefully.
- Use route constraints and explicit parameter binding. Prefer typed request objects and
  `AsParameters` for complex route/query/body binding.
- Return `Results<T1, T2>` or `TypedResults` for strongly-typed responses. Map domain failures
  to `ProblemDetails` with consistent status codes.

## Data access and testing

Keep data access high-level and test with realistic boundaries.

- Use EF Core for typical CRUD and LINQ projections. Use `IEntityTypeConfiguration<T>` to keep
  entity configuration separate and discoverable.
- Project queries to DTOs with `.Select()` to avoid over-fetching and N+1 problems.
- Use `ExecuteUpdateAsync` and `ExecuteDeleteAsync` for bulk operations that bypass change
  tracking.
- Use Dapper or raw SQL only when query performance or complexity demands it; keep SQL in
  infrastructure and parameterize inputs.
- Write integration tests with `WebApplicationFactory` against real databases via Testcontainers
  when persistence behavior matters. Use focused unit tests for pure domain logic.
- Structure tests with Arrange/Act/Assert and `TimeProvider`/`FakeTimeProvider` for deterministic
  time assertions.

## Common architecture choices

Choose an architecture that matches team size and application complexity.

- **Clean Architecture**: Domain at the center, Application orchestrates use cases, Infrastructure
  implements interfaces, and the API layer is thin. Dependencies point inward.
- **Domain-Driven Design**: Use aggregates, value objects, and domain events where the domain has
  rich behavior and invariants. One repository per aggregate root; load and save the whole
  aggregate as a unit.
- **Vertical Slice Architecture**: Organize by feature, not by layer. Each feature contains its
  endpoint, handler, request/response, and validation. Shared concerns live in `Common/`.
- Keep domain logic free of ASP.NET Core, EF Core, and UI concerns. Place framework-specific code
  in the outer layers or feature endpoints.
