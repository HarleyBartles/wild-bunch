# Clean Architecture Operational Guidance

## Dependency direction

Source code dependencies must always point inward, toward the domain. The domain has no knowledge of frameworks, databases, or UI. Outer layers depend on inner layers, never the reverse.

## Layers

- **Entities**: Enterprise business rules and objects that would exist even without this application.
- **Use cases**: Application-specific operations and workflows that coordinate entities to produce outcomes.
- **Interface adapters**: Controllers, presenters, view models, and gateways that translate between use-case models and external formats.
- **Frameworks and drivers**: Databases, web frameworks, external services, and device drivers. Keep this layer thin and replaceable.

## Testability

Because the domain does not depend on frameworks, unit tests can exercise use cases and entities with test doubles. Adapters can be tested independently by verifying that they translate data correctly and that they honor the interfaces the domain expects.

## Boundary enforcement

- Define interfaces in inner layers; implement them in outer layers.
- Do not import framework types, ORM annotations, or UI classes into entities or use cases.
- Pass simple data structures across layer boundaries to keep the domain decoupled from delivery details.
