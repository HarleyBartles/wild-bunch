---
name: wild-bunch-browser-game
description: bridge Wild Bunch to browser-game implementation and QA when work touches browser delivery, HUD design, Phaser/TypeScript/Vite, DOM overlays, playtest evidence, dev-server checks, screenshot QA, or agent-browser verification. Use to keep browser rendering as a presentation adapter over authoritative Wild Bunch game state, compose with web-game-foundations, phaser-2d-game, game-ui-frontend, game-playtest, and agent-browser where installed, and avoid turning UI or renderer code into domain truth.
metadata:
  origin: first_party
  source_author: Harley Bartles
  source_license: MIT
  source_repo: https://github.com/HarleyBartles/agent-asset-marketplace
  source_path: sources/first_party/skills/wild-bunch-browser-game/SKILL.md
  content_mode: verbatim
---

# Wild Bunch Browser Game

Use this skill when Wild Bunch work touches browser delivery, HUD design, browser playtesting, dev-server verification, screenshot QA, or the browser-game marketplace stack.

## Core posture

- Default the browser route to Phaser, TypeScript, and Vite with a DOM HUD unless the issue explicitly chooses another stack.
- Treat browser rendering as a presentation adapter over authoritative game state and command intent.
- Do not let the renderer, HUD, or frontend state become Wild Bunch domain truth.
- Keep simulation and game-state authority in the domain/application route defined by the current repo source.
- Preserve Wild Bunch domain constraints from project skills when browser work touches wallet, inventory, clues, wanted posters, horse state, travel, journey state, or culprit truth.

## Composition

Use these supporting skills or marketplace-derived capabilities when the current task specifically needs them:

- `game-studio` for the primary browser-game reference pack.
- `web-game-foundations` for simulation, render, UI, and save boundaries.
- `phaser-2d-game` for the 2D Phaser implementation shape.
- `game-ui-frontend` for HUD, menu, overlay, and frontend interaction direction.
- `game-playtest` for browser playtest expectations and evidence.
- `agent-browser` for dev-server verification and screenshot-based QA where installed.

Do not read supporting skills just because this skill is active. Load only the specific adjacent surface that owns the unresolved browser-game decision.

## Reference trigger

Read `references/browser-game-stack.md` when a task needs the stack map, supporting-skill composition cues, or browser verification route. After reading it once for the current task, do not reread it unless the task changes or the browser route is contradicted by live repo evidence.

## Stop rules

- Do not mutate source, dispatch workers, post comments, or claim browser QA completion from this skill alone.
- Do not treat screenshot existence as issue-goal conformance; compare browser evidence against the requested behavior.
- Do not override Wild Bunch domain-modeling or .NET architecture guidance when browser code adapts domain state.
