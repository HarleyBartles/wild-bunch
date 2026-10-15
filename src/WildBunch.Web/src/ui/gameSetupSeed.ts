const uuidPattern = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function normalizeGameSetupSeed(seedCode: string): string {
  const normalized = seedCode.trim();
  if (!uuidPattern.test(normalized)) {
    throw new Error("Seed code must be a UUID-shaped string.");
  }

  return normalized.toLowerCase();
}
