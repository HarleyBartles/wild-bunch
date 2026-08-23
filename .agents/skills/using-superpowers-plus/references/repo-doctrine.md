# Repo doctrine and user instructions

User instructions (explicit requests), repo-local doctrine, and the active skill
all shape routing. The canonical repo-local doctrine surfaces are:

- Root `AGENTS.md` for global repo doctrine and publication rules.
- `.agents/doctrine/mesh-policy.md` for the canonical mesh statement.
- `.agents/doctrine/*.md` for repository-local operative doctrine.
- `.devin/rules/*.md` for conditional rule triggers; they do not contain the doctrine.

If they explicitly conflict, follow this priority:

1. Explicit human instruction.
2. Root `AGENTS.md` and `.agents/doctrine/mesh-policy.md`.
3. Repo-local doctrine in `.agents/doctrine/`.
4. Conditional rule triggers (`.devin/rules/*.md`) and the active skill.
5. Default behavior.

Only skip a skill workflow when your human partner has explicitly told you to.
