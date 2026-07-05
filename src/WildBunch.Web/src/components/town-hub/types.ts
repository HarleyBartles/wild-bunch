// Town hub layout DTO types are defined in the canonical API types file and
// re-exported here so town-hub consumers can import them from a single local
// surface. The backend owns the shape (see TownLayoutDto / BuildingPlacementDto
// / BuildingKind in WildBunch.Application.Games.Models); the frontend mirrors it.
export { BuildingKind } from "../../api/types";
export type { BuildingPlacementDto, TownLayoutDto } from "../../api/types";
