# Repo-Local Skills Index

This is the endpoint of the repo-local skills mesh.

Canonical skill folders under `.agents/skills/<skill-name>/` do not contain their own `INDEX.md`. When a skill is relevant, read the linked `.agents/skills/<skill-name>/SKILL.md` directly, then follow references named by that skill.

Agents working in this repo should inspect this catalogue early, identify relevant skills for the task, and apply only the skills that match the work. Do not read every skill by default.

## Wild Bunch project skills

- [wild-bunch-project-doctrine](wild-bunch-project-doctrine/SKILL.md) - Project doctrine for seeded setup, difficulty, entropy, and world-start identity.
- [wild-bunch-domain-modeling](wild-bunch-domain-modeling/SKILL.md) - Wild Bunch gameplay-state and domain-language rules.
- [wild-bunch-dotnet-architecture](wild-bunch-dotnet-architecture/SKILL.md) - Wild Bunch .NET architecture, aggregate, persistence, CQRS, and event posture.
- [wild-bunch-browser-game](wild-bunch-browser-game/SKILL.md) - Browser-game delivery posture for Wild Bunch.
- [wild-bunch-worker-verification](wild-bunch-worker-verification/SKILL.md) - Worker-return verification for Wild Bunch work.

## Repo and workflow control

- [github-operations](github-operations/SKILL.md) - GitHub, PR, branch, commit, and repo verification.
- [connector-safety](connector-safety/SKILL.md) - Connector discovery, read-before-write, and mutation safety.
- [linear](linear/SKILL.md) - Linear issue and project workflow.
- [boring-loop](boring-loop/SKILL.md) - Boring implementation, verification, and reporting loop discipline.
- [inspecting-the-environment](inspecting-the-environment/SKILL.md) - Local and repo environment inspection.
- [crew](crew/SKILL.md) - Multi-lens planning and analysis.
- [crew-buster](crew-buster/SKILL.md) - Stress-test lens for crew outputs.

## Session, buster, and ambiguity skills

- [session-buster](session-buster/SKILL.md) - Session-buster generation.
- [session-buster-ingress](session-buster-ingress/SKILL.md) - Session-buster intake and routing.
- [buster-framework](buster-framework/SKILL.md) - General buster framework.
- [ambiguity-buster](ambiguity-buster/SKILL.md) - Ambiguity and clarification checks.
- [invariant-buster](invariant-buster/SKILL.md) - Invariant and guarantee checks.

## Architecture and backend implementation

- [ddd](ddd/SKILL.md) - Domain-Driven Design patterns.
- [clean-architecture](clean-architecture/SKILL.md) - Clean Architecture boundaries.
- [vertical-slice](vertical-slice/SKILL.md) - Vertical-slice feature work.
- [modern-csharp](modern-csharp/SKILL.md) - Modern C# guidance.
- [ef-core](ef-core/SKILL.md) - EF Core persistence guidance.
- [api-design-patterns](api-design-patterns/SKILL.md) - API contract design.
- [openapi-specification](openapi-specification/SKILL.md) - OpenAPI specification guidance.
- [testing](testing/SKILL.md) - General testing guidance.

## Frontend and browser-game implementation

- [react-patterns](react-patterns/SKILL.md) - React patterns.
- [react-testing](react-testing/SKILL.md) - React testing patterns.
- [game-studio](game-studio/SKILL.md) - Browser-game reference pack.
- [game-ui-frontend](game-ui-frontend/SKILL.md) - Game UI frontend guidance.
- [game-playtest](game-playtest/SKILL.md) - Browser playtest evidence.
- [phaser-2d-game](phaser-2d-game/SKILL.md) - Phaser 2D guidance.
- [three-webgl-game](three-webgl-game/SKILL.md) - Three.js/WebGL guidance.
- [react-three-fiber-game](react-three-fiber-game/SKILL.md) - React Three Fiber guidance.
- [sprite-pipeline](sprite-pipeline/SKILL.md) - Sprite pipeline guidance.

## Security skills

- [security-review](security-review/SKILL.md) - Security review.
- [security-scan](security-scan/SKILL.md) - Security scanning.
- [security-testing-patterns](security-testing-patterns/SKILL.md) - Security testing patterns.
- [secure-coding-practices](secure-coding-practices/SKILL.md) - Secure coding practices.
- [owasp-top-10](owasp-top-10/SKILL.md) - OWASP Top 10 lens.
- [threat-modeling-techniques](threat-modeling-techniques/SKILL.md) - Threat modeling techniques.

## Maintenance rule

When adding, removing, or materially changing a repo-local skill under `.agents/skills`, update this index in the same PR or commit. Keep descriptions short and task-trigger oriented.