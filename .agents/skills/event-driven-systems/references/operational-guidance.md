# Event-Driven Systems Operational Guidance

## When to apply

Use when the event-driven-systems skill is loaded and the question requires depth:
- selecting a broker,
- choosing choreography or orchestration,
- designing sagas and compensations,
- handling idempotency and schema versioning.

## Events vs messages

- An event is an immutable fact about something that already happened, named in past tense.
- A message is a transport envelope that may carry an event, command, or query.
- A command expresses intent and targets a specific handler; an event is broadcast to subscribers.

## Brokers

- Kafka: high throughput, durable ordered partitions, replayable consumers. Best for event streaming and telemetry.
- RabbitMQ: flexible exchanges and routing, request/reply, dead-letter queues. Best for complex routing and job queues.
- Choose managed services when operations, partitioning, and replication are not core competencies.

## Choreography and orchestration

- Choreography: services react to events independently. Use for simple workflows, high autonomy, and stable event contracts.
- Orchestration: a coordinator drives the workflow. Use for complex, sequential steps, strict ordering, and centralized observability.

## Sagas

- Break long-lived workflows into local transactions.
- Each service publishes an event when its step completes.
- Define compensating actions to undo earlier steps on failure.

## Idempotency and delivery

- Design consumers to handle at-least-once delivery safely.
- Track processed IDs or use natural idempotency keys.
- Retry transient errors; dead-letter persistent failures.

## Schema versioning

- Publish stable, versioned event schemas.
- Use a schema registry for discovery and compatibility checks.
- Avoid breaking changes; add optional fields before removing old ones.

## Related references

- Apache Kafka docs: https://kafka.apache.org/documentation/
- RabbitMQ docs: https://www.rabbitmq.com/docs
- Martin Fowler on event sourcing: https://martinfowler.com/eaaDev/EventSourcing.html
- Microservices.io EDA pattern: https://microservices.io/patterns/event-driven-architecture.html
