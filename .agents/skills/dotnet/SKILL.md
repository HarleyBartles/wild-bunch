---
name: dotnet
description: Use when building or reviewing .NET ecosystem applications, C# language
  patterns, ASP.NET Core APIs, and common library choices. Do not use when the work is
  SQL/EF deep tuning, cloud deployment, or a language other than C#/.NET.
metadata:
  source-id: dotnet
  source-path: sources/first_party/skills/dotnet/SKILL.md
  provenance-name: Dotnet first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when building or reviewing .NET ecosystem applications, C# language patterns,
    ASP.NET Core APIs, and common library choices.
  use_when:
  - Use when building or reviewing .NET ecosystem applications in C#.
  - Use when choosing C# language patterns such as records, pattern matching, async/await,
    and nullable reference types.
  - Use when configuring ASP.NET Core APIs with minimal APIs, middleware, routing, or
    validation.
  - Use when deciding on common .NET runtime concerns such as dependency injection, logging,
    and configuration.
  - Use when selecting data access or testing approaches for .NET projects.
  do_not_use_when:
  - Do not use when the work is SQL/EF deep tuning, cloud deployment, or a language other
    than C#/.NET.
  - Do not use when another more specific skill owns the task.
license: MIT
---

# Dotnet

## Overview
Guidance for building and reviewing .NET ecosystem applications in C#. Covers modern C#
language patterns, runtime primitives, ASP.NET Core APIs, and common data access and testing
choices.

## When to Use
- Building or reviewing C#/.NET applications, libraries, or services.
- Choosing C# idioms: `record`, pattern matching, nullable reference types, generics, and
  `async`/`await`.
- Configuring the .NET runtime: dependency injection, logging, and the options/configuration
  patterns.
- Designing ASP.NET Core minimal APIs, middleware, routing, and validation.
- Deciding between common data-access or test strategies.

## When Not to Use
- Do not use for SQL query tuning, deep EF Core optimization, or database schema design.
- Do not use for cloud deployment, infrastructure provisioning, or container orchestration.
- Do not use for languages outside the C#/.NET ecosystem.

## Core Patterns
- Prefer modern C# idioms. Use `record` for immutable data, pattern matching for expressive
  branching, `async`/`await` for I/O-bound work, and nullable reference types for clarity.
- Rely on built-in .NET primitives. Register services with `IServiceCollection`, read settings
  through `IConfiguration` and the options pattern, and log with `ILogger<T>`.
- Favor ASP.NET Core minimal APIs with explicit routes, endpoint filters for validation, and
  middleware only for cross-cutting concerns.
- Keep data access high-level. Use EF Core for typical CRUD, Dapper or raw SQL only when
  performance demands it, and test with `WebApplicationFactory` or focused unit tests.
- Structure code around use cases or vertical slices; keep domain logic free of framework and
  UI concerns.

## Common Mistakes
- Mixing synchronous and asynchronous code or blocking on async calls.
- Using `null` without nullable annotations; ignoring `IResult`/`Result<T>` boundaries.
- Putting business logic in endpoints instead of handlers or domain services.
- Over-engineering with repository abstractions over `DbContext` for simple cases.
- Hard-coding configuration or dependencies instead of injecting them.
