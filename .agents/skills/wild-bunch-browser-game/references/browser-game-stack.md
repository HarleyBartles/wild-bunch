# Browser Game Stack Notes

Use these notes when Wild Bunch work needs browser implementation or verification routing.

- Use the `game-studio` plugin as the primary browser-game reference pack.
- Default to Phaser, TypeScript, and Vite with a DOM HUD unless the scoped issue chooses another stack.
- Keep the renderer authoritative only for presentation.
- Keep simulation and game-state truth in the domain/application route defined by current Wild Bunch source.
- Browser code should adapt authoritative game state and emit player commands, not own game invariants.
- Use `web-game-foundations`, `phaser-2d-game`, `game-ui-frontend`, and `game-playtest` as supporting local patterns when their specific surface is needed.
- Use `agent-browser` from the Vercel plugin for dev-server verification and screenshot-based QA when it is installed.
- Pair screenshot evidence with behavior checks; screenshots alone do not prove issue-goal conformance.
