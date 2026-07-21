# API Design Profile

Portable anti-slop profile for explicit API semantics and contract design.

## Purpose

Catch generic endpoint and contract design. Push agents toward explicit API semantics, error behavior, versioning, idempotency, limits, and examples that match the contract.

## Where to Use

Before designing or reviewing REST APIs, RPC contracts, SDK methods, OpenAPI descriptions, schemas, webhooks, integration docs, or API migration plans.

## Slop Patterns to Avoid

- CRUD autopilot without resource semantics or lifecycle behavior
- `Standard pagination`, `standard auth`, and generic error handling without specifics
- Missing idempotency, retry, rate-limit, versioning, authorization, and compatibility rules
- Examples that do not match the schema or omit negative/error cases
- `RESTful` or `easy to consume` claims as substitutes for contract design
- Endpoint lists that ignore consumer workflows

## Required Avoid Rules

Prohibit `CRUD`, `generic error`, `standard pagination`, `standard authentication`, `RESTful`, `easy to consume`, and `document the fields` when those phrases are not backed by explicit contract details.

Catch API plans that do not name success response, error shape, auth boundary, idempotency/retry behavior, rate limits, versioning, or example payloads where relevant.

## Required Prefer-Instead Rules

- Name resources, operations, lifecycle transitions, and consumer workflows
- Define request/response schema, status/error semantics, and example payloads
- State auth/authorization boundaries and sensitive-field handling
- State idempotency, retries, ordering, pagination, rate limits, and compatibility/versioning when relevant
- Include at least one negative or failure example for nontrivial APIs

## False Positives / Do Not Overapply

Do not require every small internal endpoint to carry heavyweight public-API process. The profile should scale detail to risk, audience, and compatibility surface.

## Examples

### Before (Avoid)
> Add CRUD endpoints for managing resources.

### After (Prefer)
> Add `POST /api/users` to create users, `GET /api/users/{id}` to retrieve users, `PUT /api/users/{id}` to update users, and `DELETE /api/users/{id}` to delete users. All endpoints require Bearer token auth. Success responses return HTTP 200 with user JSON. Error responses return HTTP 400/401/404/409 with error code and message.

### Before (Avoid)
> Return a generic error message if something goes wrong.

### After (Prefer)
> Error responses follow this schema: `{"error": {"code": "VALIDATION_ERROR", "message": "Email is required", "details": {"field": "email"}}}`. Status codes: 400 for validation errors, 401 for auth errors, 404 for not found, 409 for conflicts, 500 for server errors.

## Acceptance Checks

This profile is acceptable only if it would force an API design from a list of endpoints into an explicit, testable contract.
