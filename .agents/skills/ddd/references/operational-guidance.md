# DDD Operational Guidance

## Before modeling

Start with a bounded context and a ubiquitous language. A bounded context is
the explicit boundary inside which a particular domain model applies and terms
have unambiguous meaning. The ubiquitous language is the shared, precise
vocabulary used by everyone involved, expressed in the model and the code.
Define these before choosing tactical patterns.

## Strategic design

Strategic patterns shape large-scale structure:

- **Bounded context**: delimit where a model is consistent and complete.
- **Context map**: describe relationships between contexts, including
  partnership, customer-supplier, conformist, anti-corruption layer, separate
  ways, shared kernel, and published language.
- **Subdomain types**: classify areas as core (competitive differentiator),
  supporting (necessary but not differentiating), or generic (available
  off-the-shelf).

## Tactical design

Tactical patterns model inside a bounded context:

- **Aggregates**: clusters of entities and value objects governed by a single
  root; enforce invariants within a transaction boundary.
- **Entities**: objects defined by identity and continuity, not attributes.
- **Value objects**: immutable descriptors defined entirely by their
  attributes.
- **Domain events**: capture occurrences the business cares about, named in
  the ubiquitous language.
- **Repositories**: mediate between the domain model and persistence for
  aggregate roots.
- **Services**: encapsulate domain operations that do not fit naturally into
  an entity or value object.
- **Modules**: organize related model concepts by language, not by technical
  layer.

## When to use DDD

Use DDD when the domain has genuine complexity: changing business rules,
multiple stakeholders with distinct models, or workflows that justify careful
modeling. Use it when the cost of misunderstanding the domain exceeds the cost
of building a precise model.

## When not to use DDD

Do not use DDD for simple CRUD, read-heavy reporting with no behavior, or
situations where a more specific skill already owns the abstraction. If the
problem reduces to data entry and retrieval, prefer a simpler approach.

Authority source: `assets/authority/reference-source/ddd.html` and
`assets/authority/CITATIONS.md`.
