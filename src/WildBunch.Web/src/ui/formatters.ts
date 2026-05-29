import type { InventoryCapabilitiesDto } from "../api/types";

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

export function formatServices(services: number) {
  const labels: string[] = [];
  if (services & 1) labels.push("Supplies");
  if (services & 2) labels.push("Lodging");
  if (services & 4) labels.push("Doctor");
  if (services & 8) labels.push("Telegraph");
  if (services & 16) labels.push("Notice board");
  return labels.length > 0 ? labels.join(", ") : "None";
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
    default:
      return `Clue ${kind}`;
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

export function formatHorseCondition(condition: number) {
  switch (condition) {
    case 0:
      return "Healthy";
    case 1:
      return "Lame";
    case 2:
      return "Dead";
    default:
      return `Condition ${condition}`;
  }
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
    default:
      return `Log ${kind}`;
  }
}

export function formatAliasKind(kind: number) {
  switch (kind) {
    case 0:
      return "Nickname";
    case 1:
      return "Former name";
    case 2:
      return "Street name";
    case 3:
      return "Known as";
    case 4:
      return "Cover identity";
    default:
      return `Alias ${kind}`;
  }
}
