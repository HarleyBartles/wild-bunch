import type { CanteenStateDto, HorseTravelStateDto, InventoryCapabilitiesDto } from "../api/types";

export function formatGameStatus(status: number) {
  switch (status) {
    case 0:
      return "Active";
    case 1:
      return "Completed";
    case 2:
      return "Failed";
    default:
      return `Status ${status}`;
  }
}

export function formatTravelDifficulty(difficulty: number) {
  switch (difficulty) {
    case 0:
      return "Normal";
    case 1:
      return "Easy";
    case 2:
      return "Hard";
    default:
      return `Difficulty ${difficulty}`;
  }
}

export function formatActionKind(kind: number) {
  switch (kind) {
    case 0:
      return "Travel";
    case 1:
      return "View map";
    case 2:
      return "View journal";
    case 3:
      return "Buy supplies";
    case 4:
      return "Stay at lodging";
    case 5:
      return "Visit doctor";
    case 6:
      return "Send telegram";
    case 7:
      return "Read wanted posters";
    case 8:
      return "Advance travel day";
    case 9:
      return "Resolve travel encounter";
    case 10:
      return "Inspect notice board";
    case 11:
      return "Check local records";
    case 12:
      return "Follow telegraph leads";
    case 13:
      return "Gather local gossip";
    default:
      return `Action ${kind}`;
  }
}

export function formatRisk(risk: number) {
  switch (risk) {
    case 1:
      return "Low";
    case 2:
      return "Moderate";
    case 3:
      return "High";
    default:
      return `Risk ${risk}`;
  }
}

export function formatTravelMode(mode: number) {
  switch (mode) {
    case 0:
      return "Mounted";
    case 1:
      return "Foot";
    default:
      return `Mode ${mode}`;
  }
}

export function formatJourneyStatus(status: number) {
  switch (status) {
    case 0:
      return "Active";
    case 1:
      return "Interrupted";
    case 2:
      return "Completed";
    case 3:
      return "Failed";
    default:
      return `Status ${status}`;
  }
}

export function formatTrailTerrain(terrain: number) {
  switch (terrain) {
    case 0:
      return "Open range";
    case 1:
      return "Hills";
    case 2:
      return "Badlands";
    case 3:
      return "Mountains";
    default:
      return `Terrain ${terrain}`;
  }
}

export function formatWaterFeature(feature: number) {
  switch (feature) {
    case 0:
      return "None";
    case 1:
      return "Creek";
    case 2:
      return "River";
    case 3:
      return "Spring";
    default:
      return `Water ${feature}`;
  }
}

export function formatServices(services: number) {
  const labels: string[] = [];
  if (services & 1) labels.push("Supplies");
  if (services & 2) labels.push("Lodging");
  if (services & 4) labels.push("Doctor");
  if (services & 8) labels.push("Telegraph");
  if (services & 16) labels.push("Notice board");
  return labels.length > 0 ? labels.join(", ") : "None";
}

export function formatClueKind(kind: number) {
  switch (kind) {
    case 0:
      return "Physical";
    case 1:
      return "Witness";
    case 2:
      return "Record";
    case 3:
      return "Rumor";
    case 4:
      return "Culprit trail";
    case 5:
      return "Identity fact";
    case 6:
      return "Alias";
    case 7:
      return "Whereabouts";
    case 8:
      return "Warrant";
    case 9:
      return "Contradiction";
    case 10:
      return "Context";
    default:
      return `Clue ${kind}`;
  }
}

export function formatCaseIdentityKind(kind: number) {
  switch (kind) {
    case 0:
      return "Known name";
    case 1:
      return "Known name";
    case 2:
      return "Feature lead";
    case 3:
      return "Route lead";
    case 4:
      return "Wanted target";
    default:
      return `Identity ${kind}`;
  }
}

export function formatCaseIdentityStatus(status: number) {
  switch (status) {
    case 0:
      return "Unresolved";
    case 1:
      return "Possible match";
    case 2:
      return "Resolved";
    default:
      return `Status ${status}`;
  }
}

export function formatSuspectStatus(status: number) {
  switch (status) {
    case 0:
      return "At large";
    case 1:
      return "Captured";
    case 2:
      return "Exonerated";
    default:
      return `Status ${status}`;
  }
}

export function formatWarrantDisposition(disposition: number) {
  switch (disposition) {
    case 0:
      return "Alive only";
    case 1:
      return "Dead or alive";
    default:
      return `Disposition ${disposition}`;
  }
}

export function formatItemKind(kind: number) {
  switch (kind) {
    case 0:
      return "Food";
    case 1:
      return "Horse feed";
    case 2:
      return "Canteen";
    case 3:
      return "Horse";
    case 4:
      return "Saddle";
    case 5:
      return "Knife";
    case 6:
      return "Revolver";
    case 7:
      return "Revolver ammo";
    case 8:
      return "Rifle";
    case 9:
      return "Rifle ammo";
    default:
      return `Item ${kind}`;
  }
}

export function formatLoadoutProfile(profile: number) {
  switch (profile) {
    case 0:
      return "Standard";
    case 1:
      return "Light";
    case 2:
      return "Stocked";
    default:
      return `Loadout ${profile}`;
  }
}

export function formatJourneyRandomnessMode(mode: number) {
  switch (mode) {
    case 0:
      return "Runtime-salted";
    case 1:
      return "Deterministic no-salt";
    default:
      return `Journey randomness ${mode}`;
  }
}

export function formatStoreVendorType(vendorType: number) {
  switch (vendorType) {
    case 0:
      return "General store";
    case 1:
      return "Stable";
    case 2:
      return "Gunsmith";
    default:
      return `Vendor ${vendorType}`;
  }
}

export function formatStoreOfferAvailability(availability: number) {
  switch (availability) {
    case 0:
      return "Available";
    case 1:
      return "Unavailable";
    default:
      return `Availability ${availability}`;
  }
}

export function formatHorseTravelState(state: HorseTravelStateDto | null) {
  if (state === null) {
    return "None";
  }

  const summary = [`Hunger ${state.hunger}`, `Thirst ${state.thirst}`, `Exhaustion ${state.exhaustion}`];

  if (state.isDead) {
    summary.push("Dead");
  } else if (state.isLame) {
    summary.push("Lame");
  } else if (state.canProvideMountedTravel) {
    summary.push("Mounted travel ready");
  }

  return summary.join(", ");
}

export function formatCanteenState(state: CanteenStateDto | null) {
  if (state === null) {
    return "None";
  }

  return `${state.charges}/${state.capacity} charges${state.hasWater ? "" : " (empty)"}`;
}

export function formatCapabilityLabel(label: keyof InventoryCapabilitiesDto) {
  switch (label) {
    case "mountedTravelAvailable":
      return "Mounted travel";
    case "horseUpkeepRequired":
      return "Horse upkeep";
    case "normalRouteWaterSecure":
      return "Water secure";
    case "trailUtility":
      return "Trail utility";
    case "closeThreatAvailable":
      return "Close threat";
    case "firearmThreatAvailable":
      return "Firearm threat";
    case "gunfightCapable":
      return "Gunfight capable";
    case "revolverUsable":
      return "Revolver usable";
    case "rifleUsable":
      return "Rifle usable";
    default:
      return label;
  }
}

export function formatLogKind(kind: number) {
  switch (kind) {
    case 0:
      return "Opening";
    case 1:
      return "Travel";
    case 2:
      return "Case update";
    case 3:
      return "Purchase";
    default:
      return `Log ${kind}`;
  }
}
