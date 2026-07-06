import { BuildingKind, TownProsperity } from "../../api/types";

/**
 * Maps BuildingKind enum values to sprite directory names.
 * Note: Trailhead (3) has no sprite assets and returns null.
 */
function getBuildingDirectoryName(kind: BuildingKind): string | null {
  switch (kind) {
    case BuildingKind.Store:
      return "general-store";
    case BuildingKind.Sheriff:
      return "sheriff-office";
    case BuildingKind.Saloon:
      return "saloon";
    case BuildingKind.Telegraph:
      return "telegraph-office";
    case BuildingKind.Trailhead:
      // Trailhead has no sprite assets
      return null;
    default:
      return null;
  }
}

/**
 * Maps TownProsperity enum values to sprite directory names.
 * Note: Poor (2) has no dedicated sprite tier; maps to prosperous.
 */
function getProsperityDirectoryName(prosperity: TownProsperity): string {
  switch (prosperity) {
    case TownProsperity.Boomtown:
      return "boomtown";
    case TownProsperity.Prosperous:
      return "prosperous";
    case TownProsperity.Poor:
      // Poor tier has no dedicated sprites; use prosperous
      return "prosperous";
    case TownProsperity.Destitute:
      return "destitute";
    default:
      return "prosperous";
  }
}

/**
 * Gets the sprite URL for a building kind, view, and prosperity tier.
 * Returns null if the building has no sprite assets (e.g., Trailhead).
 *
 * @param kind - The building kind (Store, Sheriff, Saloon, etc.)
 * @param view - The view angle (front, profile, rear, front-oblique, rear-oblique)
 * @param prosperity - The town prosperity tier (Boomtown, Prosperous, Poor, Destitute)
 * @returns The sprite URL or null if no sprite exists
 */
export function getSpriteUrl(
  kind: BuildingKind,
  view: string,
  prosperity: TownProsperity,
): string | null {
  const buildingDir = getBuildingDirectoryName(kind);
  if (!buildingDir) {
    return null;
  }

  const prosperityDir = getProsperityDirectoryName(prosperity);
  return `/assets/town-buildings/${prosperityDir}/${buildingDir}/${view}.png`;
}
