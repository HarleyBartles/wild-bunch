---
name: openapi-specification
description: OpenAPI 3.x contract expression, schema composition, and validation for REST API specs. Use with api-design-patterns when you need the format-specific OpenAPI slice rather than broad API doctrine.
tags:
  - openapi
  - swagger
  - api
  - rest
  - specification
triggers:
  - openapi spec
  - swagger definition
  - api specification
  - oas design
keywords:
  - OpenAPI
  - API spec
  - REST contract
  - specification
  - openapi specification
metadata:
  source_author: NickCrew
  source_license: MIT
  source_repo: https://github.com/NickCrew/Claude-Cortex
  source_path: sources/third_party/claude-cortex/upstream/skills/openapi-specification/SKILL.md
  content_mode: adapted
  adapted_author: Harley Bartles
  adaptation_note: Projected as the OpenAPI-specific companion slice for api-contracts-pack, composed with api-design-patterns.
---

# OpenAPI Specification

Use this skill to author, review, and validate OpenAPI 3.x documents. Keep the
broader contract and service-design rules in `api-design-patterns`; keep the
format-specific OpenAPI syntax, schema composition, and validation rules here.

## When to Use This Skill

- creating or updating an OpenAPI specification;
- adding paths, operations, components, or examples;
- reviewing an API document for schema correctness;
- checking operation IDs, response descriptions, and reusable components;
- validating generated docs or clients against the spec; or
- aligning an OpenAPI document with the broader contract rules from
  `api-design-patterns`.

## Quick Reference

| Resource | Purpose | Load when |
| --- | --- | --- |
| `references/spec-patterns.md` | OpenAPI structure, schema composition, examples, and validation | Designing or reviewing specs |
| `validation/rubric.yaml` | OpenAPI quality rubric and blocking issues | Scoring or reviewing spec quality |

## Workflow

```text
Phase 1: Design    -> define the API surface and document structure
Phase 2: Model     -> build schemas, parameters, and reusable components
Phase 3: Validate  -> lint and verify the document
Phase 4: Integrate -> generate docs, SDKs, and contract tests
```

## Core Principles

- treat the OpenAPI document as the executable contract for the service;
- keep shared definitions in `components` instead of inlining duplicates;
- make every response, parameter, and schema explicit enough for codegen;
- use examples to anchor request and response shape;
- keep validation rules stable and machine-checkable; and
- rely on `api-design-patterns` for the broader REST and service-contract
  doctrine.

## Spec Skeleton

```yaml
openapi: 3.1.0
info:
  title: Example API
  version: 1.0.0
paths:
  /resources:
    get:
      operationId: listResources
      responses:
        "200":
          description: Successful response
components:
  schemas: {}
  securitySchemes: {}
```

## Validation

Run the spec linter that matches the project, then confirm the OpenAPI document
still satisfies the common structural rules:

- every operation has a unique `operationId`;
- every response has a `description`;
- every path parameter is declared where the path uses it;
- reusable schemas live under `components/schemas`;
- request and response examples are present where they matter; and
- the document passes the repo's chosen OpenAPI validator.

Typical validators include:

```bash
spectral lint openapi.yaml
redocly lint openapi.yaml
swagger-cli validate openapi.yaml
```

## Integration

Use a validated spec to drive:

- documentation rendering;
- client SDK generation;
- server stub generation; and
- contract tests or mock servers.

## Quality Checklist

- [ ] operation IDs are unique and stable;
- [ ] reusable schemas are factored into `components`;
- [ ] response descriptions are present for every response code;
- [ ] examples exist for important request and response payloads;
- [ ] security requirements are explicit; and
- [ ] the spec passes validation with no errors.

## Anti-Patterns

- do not inline reusable schemas;
- do not rely on implicit schema behavior when `required` is clearer;
- do not leave undocumented responses or parameters in the document;
- do not use `200 OK` for create operations when `201 Created` fits better; and
- do not broaden this slice into the general API-design doctrine already owned
  by `api-design-patterns`.
