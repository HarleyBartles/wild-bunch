# CQRS Operational Guidance

## Separate commands from queries
Commands express intent to change state and should be task-oriented, not CRUD-shaped. Queries are read-only and return DTOs optimized for the consumer.

## Event sourcing as a persistence option, not a requirement
Event sourcing can feed read models, but CQRS works with relational, document, or key-value stores. Choose it only when audit and temporal benefits outweigh operational cost.

## Consistency trade-offs and when CQRS is overkill
Reads may lag writes. Avoid CQRS when the domain is small, team boundaries are unclear, or the operational overhead of multiple models is not justified.

## Typical project shapes and team boundaries
CQRS fits systems with high read/write asymmetry, multiple query surfaces, or clear team ownership of read and write paths.
