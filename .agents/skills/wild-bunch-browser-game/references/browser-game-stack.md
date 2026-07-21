# Browser Game Stack

## Verified stack

`src/WildBunch.Web/package.json` currently declares React 18, Phaser 3, TypeScript, and Vite. Check that file before retaining version or dependency claims.

## Authority boundary

- The backend owns gameplay state, rules, command legality, and hidden truth.
- React and Phaser render player-known state and emit player intent.
- Player-facing surfaces belong in the game shell or relevant route. Developer controls belong in the dev overlay.
- Follow `.agents/docs/frontend-standards.md` for styling, routing, source-truth, play-surface, and dev-overlay rules.

## Focused routes

| Need | Route |
| --- | --- |
| React structure | `react` |
| Phaser runtime | `phaser-2d-game` |
| HUD or interaction flow | `game-ui-frontend`, `interaction-design` |
| Styling | `web-styling` |
| Playtest | `game-playtest` |
| Browser automation | `webapp-testing` |

Pair visual evidence with behavior checks through the real client-to-server path. A screenshot proves appearance at one moment, not command handling or backend state changes.
