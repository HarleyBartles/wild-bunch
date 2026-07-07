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
 */
function getProsperityDirectoryName(prosperity: TownProsperity): string {
  switch (prosperity) {
    case TownProsperity.Boomtown:
      return "boomtown";
    case TownProsperity.Prosperous:
      return "prosperous";
    case TownProsperity.Poor:
      return "poor";
    case TownProsperity.Destitute:
      return "destitute";
    default:
      return "prosperous";
  }
}

/**
 * Maps BuildingView numeric values to sprite file names.
 * BuildingView is defined as 0 | 1 | 2 | 3 | 4 in types.ts.
 */
function getViewFileName(view: number): string {
  switch (view) {
    case 0:
      return "front";
    case 1:
      return "profile";
    case 2:
      return "rear";
    case 3:
      return "front-oblique";
    case 4:
      return "rear-oblique";
    default:
      return "front";
  }
}

/**
 * Gets the sprite URL for a building kind, view, and prosperity tier.
 * Returns null if the building has no sprite assets (e.g., Trailhead).
 *
 * @param kind - The building kind (Store, Sheriff, Saloon, etc.)
 * @param view - The view angle (0=Front, 1=Profile, 2=Rear, 3=FrontOblique, 4=RearOblique)
 * @param prosperity - The town prosperity tier (Boomtown, Prosperous, Poor, Destitute)
 * @returns The sprite URL or null if no sprite exists
 */
export function getSpriteUrl(
  kind: BuildingKind,
  view: number,
  prosperity: TownProsperity,
): string | null {
  const buildingDir = getBuildingDirectoryName(kind);
  if (!buildingDir) {
    return null;
  }

  const prosperityDir = getProsperityDirectoryName(prosperity);
  const viewFileName = getViewFileName(view);
  return `/assets/town-hub-buildings/${prosperityDir}/${buildingDir}/${viewFileName}.png`;
}
