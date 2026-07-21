# Hexagonal Architecture Operational Guidance

## Ports and adapters

A **port** is an interface the domain uses to receive input from or send output to the outside world. An **adapter** is a concrete implementation of a port for a specific technology. The domain sits at the center and depends only on ports, not on adapter implementations.

## Primary and secondary adapters

- **Primary (driving) adapters** initiate interaction with the domain. Examples: HTTP controllers, CLI commands, message consumers, scheduled jobs, and test drivers.
- **Secondary (driven) adapters** are invoked by the domain to perform side effects. Examples: repositories, email senders, payment gateways, file systems, and third-party service clients.

## Isolation from frameworks, databases, and UI

The domain code contains no references to web frameworks, database SDKs, ORM mappings, or UI components. Configuration and framework-specific types live exclusively inside adapter implementations. This lets you substitute in-memory fakes for tests and replace production technologies without rewriting domain logic.

## Similarities and differences with clean and onion architecture

- **Similarity**: All three place the domain at the center and use dependency inversion to keep frameworks at the edges.
- **Difference**: Hexagonal architecture expresses the boundary as a set of explicit ports and adapters; the shape of the boundary is intentionally agnostic about internal layering. Clean architecture adds a strict layered dependency rule (entities → use cases → interface adapters → frameworks). Onion architecture organizes the system into concentric layers and also depends on dependency inversion, with domain services and application services surrounding the core domain.
