---
name: wild-bunch-browser-game
description: Use when Wild Bunch work changes browser delivery, React or Phaser integration, player HUDs, DOM overlays, playtesting, or browser QA evidence.
metadata:
  status: active
  scope: Wild Bunch browser delivery and verification decisions.
  use_when:
    - Use when a task changes the web game, player-facing browser UI, or browser validation route.
  do_not_use_when:
    - Do not use for backend-only gameplay or persistence work.
---

# Wild Bunch Browser Game

## Owned decision

Keep the browser as a presentation and input adapter over backend-authoritative game state, then select the smallest browser implementation or QA route that fits the task.

## Current posture

- The web app uses React 18, Phaser 3, TypeScript, and Vite.
- Render player-known backend state. Do not make React, Phaser, or client stores authoritative for gameplay facts or hidden investigation truth.
- Send player intent through the established command/API boundary.
- Keep the player HUD and play surfaces useful in-world; keep developer controls in the dev overlay.
- Preserve domain rules for travel, inventory, wallet, horse state, clues, and culprit truth when adapting them for the browser.

## Route by need

- Use `react` for React component and hook structure.
- Use `phaser-2d-game` for Phaser scene, lifecycle, camera, or input work.
- Use `game-ui-frontend` and `interaction-design` for HUD and player-flow decisions.
- Use `game-playtest` for browser playtests and evidence.
- Use `webapp-testing` for local browser automation and route checks.
- Use `web-styling` plus `.agents/docs/frontend-standards.md` for styling decisions.

Load only the adjacent skill that owns the unresolved decision.

## Reference

Read [Browser game stack](references/browser-game-stack.md) for the verified stack, authority boundary, and focused QA route.

## Stop conditions

- Inspect `src/WildBunch.Web/package.json` and live source before changing stack claims.
- Do not accept a screenshot alone as behavior proof.
- Route gameplay-rule changes through `wild-bunch-domain-modeling` and backend structure changes through `wild-bunch-dotnet-architecture`.
