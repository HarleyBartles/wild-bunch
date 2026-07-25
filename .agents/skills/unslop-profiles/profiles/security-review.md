# Security Review Profile

Portable anti-slop profile for concrete security analysis.

## Purpose

Catch checklist theatre and vague security reassurance. Push agents to name assets, actors, trust boundaries, attack paths, exploitability, impact, and concrete mitigations.

## Where to Use

Before writing or reviewing security notes, threat models, auth changes, input handling, secret management, dependency/security upgrade notes, infrastructure reviews, or security closeout summaries.

## Slop Patterns to Avoid

- `Validate and sanitize input` without identifying input, parser, boundary, or exploit path
- `Make auth secure` without actor, permission, session, token, or resource boundary
- `Best practices` as a substitute for named controls
- `No obvious security issues` without inspection scope
- Severity claims without exploitability, impact, or likelihood
- Security review that ignores secrets, logs, dependencies, transport, storage, and authorization where relevant

## Required Avoid Rules

Prohibit generic security verbs and reassurances such as `sanitize`, `secure`, `best practices`, `harden`, `internal`, `low risk`, and `no obvious issues` unless tied to an actual trust boundary and attack path.

Catch review notes that do not name the asset, actor, boundary, exploitability, impact, and mitigation.

## Required Prefer-Instead Rules

- Identify assets and actors
- Name trust boundaries and data/control flow
- Describe concrete attack paths or abuse cases
- Distinguish authn from authz and user input from trusted internal input
- Tie severity to exploitability and impact
- Name mitigation, validation, and residual risk

## False Positives / Do Not Overapply

Do not force a full threat model for every trivial change. Scale the profile to risk, but require explicit scope even for low-risk review.

## Examples

### Before (Avoid)
> Validate and sanitize all user input.

### After (Prefer)
> User input is validated at the API boundary using JSON Schema. SQL queries use parameterized statements to prevent injection. HTML output is escaped using the templating system's auto-escape. Attack path: SQL injection via user email field - mitigated by parameterized queries.

### Before (Avoid)
> Make sure authentication and authorization are secure.

### After (Prefer)
> Authentication: JWT tokens with 1-hour expiration, signed with HS256. Authorization: role-based access control (RBAC) with admin, editor, viewer roles. Attack path: stolen JWT tokens - mitigated by short expiration and secure cookie storage. Attack path: privilege escalation - mitigated by server-side role validation on every request.

## Acceptance Checks

This profile is acceptable only if it would prevent a security review from passing with checklist phrases and no named attack surface.
