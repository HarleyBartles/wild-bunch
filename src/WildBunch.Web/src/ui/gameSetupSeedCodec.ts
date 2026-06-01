export interface GameSetupSeedState {
  seedCode: string;
}

export interface DecodedGameSetupSeed extends GameSetupSeedState {
  canonical: boolean;
}

const canonicalSeedCode = "00000000-0000-0000-0000-000000000000";
const uuidPattern = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function createCanonicalSeedState(): GameSetupSeedState {
  return { seedCode: canonicalSeedCode };
}

export function withRandomSeed(seed: GameSetupSeedState): GameSetupSeedState {
  return { ...seed, seedCode: crypto.randomUUID() };
}

export async function encodeGameSetupSeed(seed: GameSetupSeedState): Promise<string> {
  return normalizeSeedCode(seed.seedCode);
}

export async function decodeGameSetupSeed(seedCode: string): Promise<DecodedGameSetupSeed> {
  const normalized = normalizeSeedCode(seedCode);
  return {
    seedCode: normalized,
    canonical: normalized === canonicalSeedCode,
  };
}

function normalizeSeedCode(seedCode: string) {
  const normalized = seedCode.trim();
  if (!uuidPattern.test(normalized)) {
    throw new Error("Seed code must be a UUID-shaped string.");
  }

  return normalized.toLowerCase();
}
