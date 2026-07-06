# Test Patterns

> Guidance for organizing and naming tests in the Wild Bunch repo.
> Source snapshot: `0b3b6b86` (origin/main, 2026-07-06).

## Test types

| Type | What it proves | Where it lives | Naming convention |
| --- | --- | --- | --- |
| Unit | A single domain/application class behaves correctly in isolation | `{Project}.Tests/` root or `{Concern}/` subdirectory | `{ClassUnderTest}Tests.cs` |
| Structural / Guardrail | Source inspection proves an invariant about the codebase shape (no deleted table, no leaked secret, read-only handler) | `{Project}.Tests/Guardrails/` | `{Invariant}GuardrailTests.cs` or `{Invariant}StructuralTests.cs` |
| Projection | A projector produces the expected read-model from events | `{Project}.Tests/Projections/` | `{Projector}Tests.cs` or `{Projection}Tests.cs` |
| Mapper | A mapper converts domain state to DTO shape correctly | `{Project}.Tests/Mappers/` | `{Mapper}Tests.cs` |
| Renderer | A renderer produces text/label output from domain state | `{Project}.Tests/Renderers/` | `{Renderer}Tests.cs` |
| Handler | A command/query handler orchestrates the aggregate and returns the right result | `{Project}.Tests/Handlers/` | `{Handler}Tests.cs` |
| Factory orchestration | A factory wires dependencies and produces a valid aggregate/session | `{Project}.Tests/` root or `Factories/` | `{Factory}Tests.cs` |
| Configuration | A configuration/policy registration produces the expected service collection | `{Project}.Tests/Configuration/` | `{Config}ConfigurationTests.cs` |
| Characterization | Pins exact current behavior before a migration; values are captured from deterministic scenarios | `{Project}.Tests/Characterization/` | `{Subject}CharacterizationTests.cs` |
| Event sourcing | Proves event apply/replay produces the same state as the command path | `{Project}.Tests/EventSourcing/` or `Events/` | `{Subject}EventSourcingTests.cs` |
| Brute-force | Iterates over thousands of seed/salt/parameter combinations to catch silent bias, rare anti-patterns, and verify distribution fairness | `GameContent.Tests/` (typically) | `{Subject}BruteForceAnalysisTests.cs` or `{Subject}BruteForceTests.cs` |
| Acceptance | End-to-end scenario through the API proving a user-facing flow works | `Integration.Tests/Acceptance/` | `{Flow}AcceptanceTests.cs` |
| Integration | Full HTTP pipeline via `WebApplicationFactory` | `Integration.Tests/` root or `{Concern}/` | `{Endpoint}Tests.cs` |

## When to add which test type

1. **New behavior in a domain class** → unit test in `Domain.Tests/` root or the matching concern subdirectory.
2. **New projector** → projection test in `Application.Tests/Projections/`.
3. **New mapper** → mapper test in `Application.Tests/Mappers/`.
4. **New renderer** → renderer test in `Application.Tests/Renderers/`.
5. **New command/query handler** → handler test in `Application.Tests/Handlers/` (or `Dev/` for dev handlers).
6. **New API endpoint** → integration test in `Integration.Tests/` root.
7. **New user-facing flow** → acceptance test in `Integration.Tests/Acceptance/`.
8. **New configuration/policy** → configuration test in `Api.Tests/Configuration/`.
9. **Migration that changes behavior** → characterization tests first, then update them after migration.
10. **Source-shape invariant** → structural/guardrail test in `{Project}.Tests/Guardrails/`.
11. **Generator with seed/salt parameters** → brute-force test in `GameContent.Tests/` to verify distribution fairness and catch anti-patterns across thousands of combinations.

## Test organization rules

- One test type per file. Do not mix unit, projection, and handler tests in the same file.
- Folder structure mirrors the source layer: `Application.Tests/Handlers/` mirrors `Application/Games/Commands/` and `Application/Games/Queries/`.
- Namespace matches folder path: `WildBunch.Application.Tests.Handlers`, `WildBunch.Application.Tests.Mappers`, etc.
- Test doubles (fakes, stubs, in-memory repos) live in `TestDoubles/` and are NOT test files.
- Test infrastructure (fixtures, harnesses, builders) lives in `TestInfrastructure/` and is NOT test files.
- Characterization tests are temporary by nature — they become regular unit tests after the migration they pin is complete. Keep them in `Characterization/` only while the migration is in progress; move them to the matching concern folder when the migration lands.
- Brute-force tests typically live in the root of their test project (e.g., `GameContent.Tests/`) since they test the generator's overall distribution rather than a specific class.

## Naming conventions

- Test class: `{ClassUnderTest}Tests.cs` (e.g., `GameSessionMapperTests`, `CorsConfigurationTests`).
- Test method: `MethodName_StateUnderTest_ExpectedBehavior` (e.g., `CreateOrder_WithValidItems_ReturnsSuccessResult`).
- Configuration tests: `{Config}ConfigurationTests` (not `{Config}PolicyTests`).
- Guardrail tests: `{Invariant}GuardrailTests` (not `{Invariant}Tests`).
- Characterization tests: `{Subject}CharacterizationTests` (not `{Subject}Tests`).
- Brute-force tests: `{Subject}BruteForceAnalysisTests.cs` or `{Subject}BruteForceTests.cs` (to distinguish from unit tests).
