# OpenAPI Specification Patterns Reference

This reference focuses on the OpenAPI-specific companion slice. For broader API
contract doctrine, use `api-design-patterns`.

## OpenAPI Structure

```yaml
openapi: 3.1.0
info:
  title: Example API
  version: 1.0.0
servers:
  - url: https://api.example.com/v1
paths:
  /resources:
    get:
      summary: List resources
      operationId: listResources
      responses:
        "200":
          description: Successful response
components:
  schemas: {}
  parameters: {}
  responses: {}
  securitySchemes: {}
```

## Path and Operation Rules

- keep every operation under a path item;
- give every operation a unique `operationId`;
- include a `description` for every response code;
- define every path parameter where the path uses it; and
- prefer tags that mirror actual API domains rather than documentation pages.

## Schema Design Patterns

### Composition with `allOf`

```yaml
components:
  schemas:
    BaseResource:
      type: object
      properties:
        id:
          type: string
          format: uuid
        createdAt:
          type: string
          format: date-time

    User:
      allOf:
        - $ref: "#/components/schemas/BaseResource"
        - type: object
          required: [email, name]
          properties:
            email:
              type: string
              format: email
            name:
              type: string
```

### Polymorphism with `oneOf`

```yaml
components:
  schemas:
    Notification:
      oneOf:
        - $ref: "#/components/schemas/EmailNotification"
        - $ref: "#/components/schemas/SmsNotification"
      discriminator:
        propertyName: type
```

### Read and Write Shapes

Use separate request and response models when the server owns fields like `id`,
`createdAt`, or `updatedAt`.

### Pagination Shape

Keep pagination metadata explicit and reusable. Prefer cursor or keyset
pagination when scale and consistency matter.

## Examples

- add examples to important request bodies and responses;
- prefer named examples when one operation supports multiple valid payloads;
- keep example values consistent with the schema; and
- use examples to show deprecation or migration behavior when relevant.

## Security Schemes

- bearer auth for JWT or opaque tokens;
- API key when a simpler trust boundary is acceptable; and
- OAuth 2.0 only when the flow is actually required by the client and server.

## Validation Rules

| Rule | Why it matters |
| --- | --- |
| Unique `operationId` | Generates stable method names and prevents collisions |
| Response `description` | Required by the spec and useful to clients |
| Path parameter coverage | Prevents malformed templates |
| Explicit `type` | Avoids ambiguous schema generation |
| Shared `components` | Keeps clients and docs deduplicated |

## Validation Commands

```bash
spectral lint openapi.yaml
redocly lint openapi.yaml
swagger-cli validate openapi.yaml
```

## Integration

Use the validated document to generate docs, SDKs, mocks, and contract tests.

## Anti-Patterns

- do not inline reusable schemas;
- do not omit error responses for client and server failures;
- do not mix multiple API versions in one document; and
- do not broaden this slice into the non-OpenAPI doctrine already owned by
  `api-design-patterns`.
