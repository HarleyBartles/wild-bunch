# Technical Writing Profile

Portable anti-slop profile for documentation, README, design-note, migration-note, and API-explanation content.

## Purpose

Catch documentation, README, design-note, migration-note, and API-explanation slop. Push agents toward precise, example-backed, constraint-aware technical prose.

## Where to Use

When writing docs, setup instructions, architecture notes, migration guides, API references, changelog explanations, operational runbooks, or technical summaries.

## Slop Patterns to Avoid

- Vague verbs: `handle`, `manage`, `support`, `allow`, `ensure` without concrete behavior
- Claims of simplicity, flexibility, scalability, or robustness without constraints
- Missing examples, inputs, outputs, failure cases, versions, or environment assumptions
- Docs that praise the system instead of specifying how it behaves
- Passive `as needed` guidance that leaves implementation or operation undefined

## Required Avoid Rules

Do not use unexplained praise words and requirement-shaped filler:
- `easy`, `simple`, `intuitive`, `robust`, `scalable`, `flexible`
- `properly`, `as needed`, `future requirements`

Do not tell readers to `ensure` or `configure appropriately` without naming the exact check, file, command, flag, value, or observable result.

## Required Prefer-Instead Rules

- Name the exact behavior, contract, command, file, option, or failure mode
- Include at least one concrete example when explaining usage or behavior
- State assumptions and version/environment boundaries when they matter
- Replace praise with observable properties: latency, ordering, retries, schema fields, error codes, compatibility ranges, or limits

## False Positives / Do Not Overapply

Do not ban terms like `scalable` or `flexible` when the doc defines the dimension being scaled or the extension point being used. The issue is unsupported abstraction, not the vocabulary itself.

## Examples

### Before (Avoid)
> This feature allows users to easily manage their data in a robust and scalable way.

### After (Prefer)
> This feature supports CRUD operations on user records with a maximum of 10,000 records per account. Data is replicated across 3 availability zones with a 99.9% uptime SLA.

### Before (Avoid)
> Ensure the service handles errors properly and validates inputs as needed.

### After (Prefer)
> The service validates all inputs against JSON Schema before processing. Invalid requests return HTTP 400 with error details. Internal errors are logged and return HTTP 500 with a correlation ID.

## Acceptance Checks

This profile is acceptable only if it would force the negative examples to become specific enough for a reader to execute, test, or falsify.
