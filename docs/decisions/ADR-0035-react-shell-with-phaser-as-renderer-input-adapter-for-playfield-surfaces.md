# ADR-0035 React shell with Phaser as renderer/input adapter for playfield surfaces

## Status

live

## Dated Status History

- 2026-06-28 - live: the first React-hosted Phaser playfield (starting-town map POC) is implemented and validated.

## Decision Type

ui, architecture

## Related ADRs

- `extends`: ADR-0016
- `informs`: ADR-0011, ADR-0027

## Context

Wild Bunch is a server-authoritative C#/.NET game with a React/Vite/TanStack Query/styled-components web client (ADR-0016). The web client renders routes, HUD, text-heavy surfaces, forms, detail panels, case file, wanted posters, settings, and dev/debug affordances in DOM/React.

The starting-town selection step (BUNCH-102) presented towns as a list. A spatial map materially improves spatial awareness, trail-distance intuition, and the sense of riding out into a real frontier. Phaser is a mature 2D HTML5 game framework that can render town markers, trail edges, distance labels, and handle pointer input inside a React component lifecycle.

The question is where Phaser fits in the architecture: does it own game truth, or is it a renderer/input adapter inside the existing React shell?

## Decision Drivers

- The backend/domain/application layer must remain authoritative for towns, routes, distances, start eligibility, and game creation.
- `GameSession` is the live-play aggregate root (ADR-0002, ADR-0020). Gameplay truth must not move into the frontend.
- The React shell already owns routes, HUD, server-state coordination (React Query), and styling (styled-components). A full frontend rewrite in Phaser is not warranted.
- Spatial map presentation and pointer input are where Phaser materially improves game feel over a plain DOM list.
- The adapter seam must be testable in jsdom without a real WebGL canvas.

## Decision Summary

React remains the shell. Phaser is a renderer/input adapter only, mounted inside React components for playfield surfaces. Phaser receives authoritative map/read-model data as props, renders it, and emits player intent (e.g. `townSelected`) back to React via callbacks. Phaser must not call backend APIs, decide eligibility, store selected-town truth, or bypass the React-owned confirmation path.

## Detailed Decision Breakdown

The React shell owns:
- Routes and component lifecycle.
- Server-state coordination via React Query.
- Selected-town state and the final game-creation mutation (`POST /api/games`).
- HUD, explanatory copy, buttons, forms, detail panels, validation messages, and dev/debug surfaces.
- styled-components styling and design tokens.

The Phaser layer owns:
- Rendering town markers, trail edges, distance labels, and selection highlights on a canvas.
- Pointer input (hover, click) on interactive markers.
- Emitting `townSelected` intent via a callback prop.

The `PhaserMapHost` React component creates a Phaser `Game` in `useEffect`, destroys it in the cleanup function, and passes data/intent via props. The Phaser scene constructor receives only `mapData`, `selectedTownId`, and `onTownSelected` — no API client, no fetch, no mutation access. The `selectedTownId` field is `public readonly`; the scene cannot mutate it.

Backend read models (`StartingTownMapDto` from `GET /api/games/starting-town-map`) carry town coordinates, trail edges with ride-day distances, and a `Selectable` flag derived from the same `StartingTownCatalog.GetStartingTownCandidates()` eligibility source as the existing `GET /api/games/starting-towns` endpoint. Phaser does not recompute eligibility.

A DOM/React fallback (selectable-town buttons) remains alongside the Phaser canvas for keyboard/screen-reader accessibility.

## Options Considered and Rejected

- Rewrite the whole frontend in Phaser. Rejected: the React shell already handles routes, HUD, server state, forms, and dev surfaces well; a full rewrite is not warranted and would lose TanStack Query coordination.
- Render the map in pure DOM/SVG instead of Phaser. Rejected for this surface: Phaser materially improves pointer input, marker interaction, and future animation/pan/zoom headroom over a hand-rolled SVG map. The POC proves the Phaser seam for future playfield surfaces.
- Let Phaser own selected-town state and call `POST /api/games` directly. Rejected: would move gameplay truth into the frontend and bypass the React-owned confirmation path, violating ADR-0002 and ADR-0020.
- Add a second eligibility algorithm in the frontend. Rejected: the `Selectable` flag comes from the backend read model, which reuses `StartingTownCatalog.GetStartingTownCandidates()`.

## When a Rejected Option Would Have Been Better

A pure DOM/SVG map would be better if the product never needs another playfield surface (travel animation, encounter scenes, world exploration) and the only goal were a static clickable map. In that narrow case, Phaser's dependency weight (~1.9MB bundle) would not be justified. The decision accepts the bundle cost because the POC establishes the seam for future spatial playfield surfaces.

## Benefits

- Spatial map presentation materially improves trail-distance awareness over a plain list.
- Phaser handles canvas rendering and pointer input without React re-render overhead.
- The adapter seam is clean: React owns truth, Phaser renders and emits intent.
- Future playfield surfaces (travel, encounters) can reuse the `PhaserMapHost` mount/unmount pattern.
- The backend read model stays the single source of map truth.

## Accepted Tradeoffs

- Phaser adds ~1.9MB to the frontend bundle (code-splitting is a future optimization).
- jsdom tests must mock the `phaser` module; real canvas rendering is verified via browser proof, not unit tests.
- The `PhaserMapHost` `useEffect` recreates the game if `mapData` object identity changes (mitigated by `staleTime: Infinity` on the query).

## Risks

- Phaser bundle size could grow if not code-split.
- The adapter seam could drift if a future change passes an API client or mutation into the scene constructor. The truth-boundary tests in `PhaserMapHost.test.tsx` are the falsifiable guard against this.
- Real browser rendering correctness depends on manual/browser-evidence lanes (ADR-0022), not jsdom unit tests.

## Consequences for Future Work

New playfield surfaces (travel map, encounter scenes, world exploration) should follow the same pattern: a React host component that mounts/unmounts Phaser, receives read-model data as props, and emits player intent via callbacks. Phaser must never own game truth or call backend mutation endpoints.

Frontend map/read-model changes should extend the backend read model (`StartingTownMapDto` and its successors) rather than inventing frontend-only map truth.

## Implementation Status or Plan

Live. The starting-town map POC is implemented and validated:
- `PhaserMapHost.tsx` mounts/unmounts Phaser and emits `townSelected` intent.
- `StartingTownStep.tsx` renders the map host with a DOM fallback.
- `GET /api/games/starting-town-map` returns `StartingTownMapDto` with coordinates, trail edges, and `Selectable` flags.
- Truth-boundary tests prove Phaser does not call APIs, does not decide eligibility, and does not store selected-town truth.
- React-owned confirmation tests prove `POST /api/games` is called through the React path, not Phaser.

## Related Stable Source Surfaces

- `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx`
- `src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx`
- `src/WildBunch.Web/src/api/types.ts` (`StartingTownMapDto` and companion DTOs)
- `src/WildBunch.Web/src/api/wildBunchApi.ts` (`getStartingTownMap`)
- `src/WildBunch.Application/Games/Models/StartingTownMapDto.cs`
- `src/WildBunch.Application/Games/Queries/GetStartingTownMapHandler.cs`
- `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs`
- `src/WildBunch.Api/Games/GameSessionEndpoints.cs` (`starting-town-map` route)
- `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx`
- `src/WildBunch.Web/src/tests/StartFlow.test.tsx`
- `tests/WildBunch.Application.Tests/GetStartingTownMapHandlerTests.cs`
- `tests/WildBunch.Integration.Tests/StartingTownMapEndpointTests.cs`

## Proof of Implementation or Explicit Non-Implementation

`PhaserMapHost.tsx` creates and destroys a Phaser `Game` in `useEffect`/cleanup, receives `mapData`/`selectedTownId`/`onTownSelected` props, and the scene's `selectTown` method only calls `onTownSelected` (no API, no fetch, no mutation). `PhaserMapHost.test.tsx` asserts the scene has no API-surface properties, `selectTown` does not call `globalThis.fetch`, and `selectedTownId` is not mutated. `StartFlow.test.tsx` proves the map selection → React confirmation → `createGame` chain and that `createGame` is not called until a town is selected.

## Review Triggers

- When a future playfield surface needs Phaser to own game truth or call mutations directly — revisit the adapter boundary.
- When Phaser is removed or replaced with another renderer — update or supersede this ADR.
- When the frontend moves away from the React shell — supersede this ADR and ADR-0016 together.
- When the map read model gains frontend-computed eligibility or distance logic — revisit the backend-authoritative constraint.
