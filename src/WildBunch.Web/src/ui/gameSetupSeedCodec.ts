import type { TravelDifficulty } from "../api/types";

export type GameSetupLoadoutProfile = 0 | 1 | 2;
export type GameSetupJourneyRandomnessMode = 0 | 1;

export interface GameSetupSeedState {
  difficulty: TravelDifficulty;
  startWithHorse: boolean;
  loadoutProfile: GameSetupLoadoutProfile;
  journeyRandomnessMode: GameSetupJourneyRandomnessMode;
  entropy: bigint;
}

export interface DecodedGameSetupSeed extends GameSetupSeedState {
  seedCode: string;
  canonical: boolean;
}

const seedPrefix = "WB1";
const generatorVersion = 1;
const canonicalEntropyHexDigits = 12;
const canonicalEntropyMaximum = 0xFFFFFFFFFFFFn;

export function createCanonicalSeedState(): GameSetupSeedState {
  return {
    difficulty: 0,
    startWithHorse: true,
    loadoutProfile: 0,
    journeyRandomnessMode: 0,
    entropy: 0n,
  };
}

export function withDifficulty(seed: GameSetupSeedState, difficulty: TravelDifficulty): GameSetupSeedState {
  return { ...seed, difficulty };
}

export function withStartWithHorse(seed: GameSetupSeedState, startWithHorse: boolean): GameSetupSeedState {
  return { ...seed, startWithHorse };
}

export function withLoadoutProfile(seed: GameSetupSeedState, loadoutProfile: GameSetupLoadoutProfile): GameSetupSeedState {
  return { ...seed, loadoutProfile };
}

export function withJourneyRandomnessMode(
  seed: GameSetupSeedState,
  journeyRandomnessMode: GameSetupJourneyRandomnessMode,
): GameSetupSeedState {
  return { ...seed, journeyRandomnessMode };
}

export function withRandomEntropy(seed: GameSetupSeedState): GameSetupSeedState {
  return { ...seed, entropy: generateEntropy() };
}

export async function encodeGameSetupSeed(seed: GameSetupSeedState): Promise<string> {
  const optionsCode = packOptions(seed).toString(16).toUpperCase().padStart(2, "0");
  if (seed.entropy < 0n || seed.entropy > canonicalEntropyMaximum) {
    throw new Error(`Seed entropy must be between 0 and ${canonicalEntropyMaximum.toString(16).toUpperCase().padStart(canonicalEntropyHexDigits, "0")}.`);
  }

  const entropyCode = seed.entropy.toString(16).toUpperCase().padStart(canonicalEntropyHexDigits, "0");
  const checksum = await computeChecksum(seed.difficulty, optionsCode, entropyCode);
  return `${seedPrefix}-${encodeDifficulty(seed.difficulty)}-${optionsCode}-${entropyCode}-${checksum}`;
}

export async function decodeGameSetupSeed(seedCode: string): Promise<DecodedGameSetupSeed> {
  const normalized = seedCode.trim();
  if (!normalized) {
    throw new Error("Seed code is required.");
  }

  const parts = normalized.split("-").filter(Boolean);
  if (parts.length !== 5 || parts[0].toUpperCase() !== seedPrefix) {
    throw new Error("Seed code format is invalid.");
  }

  const difficulty = decodeDifficulty(parts[1]);
  if (difficulty === null) {
    throw new Error("Seed difficulty is invalid.");
  }

  const optionsBits = Number.parseInt(parts[2], 16);
  if (!Number.isFinite(optionsBits) || optionsBits < 0 || optionsBits > 0xff) {
    throw new Error("Seed options are invalid.");
  }

  const entropy = parseEntropy(parts[3]);
  if (entropy === null) {
    throw new Error("Seed entropy is invalid.");
  }

  const checksum = parts[4].toUpperCase();
  if (checksum.length < 4 || checksum.length > 8 || !/^[0-9A-F]+$/.test(checksum)) {
    throw new Error("Seed checksum is invalid.");
  }

  const loadoutProfile = ((optionsBits >> 1) & 0x03) as GameSetupLoadoutProfile;
  if (loadoutProfile > 2) {
    throw new Error("Seed options are invalid.");
  }

  const startWithHorse = (optionsBits & 0x01) !== 0;
  const journeyRandomnessMode = ((optionsBits >> 3) & 0x01) as GameSetupJourneyRandomnessMode;
  const expectedChecksum = await computeChecksum(difficulty, parts[2].toUpperCase(), parts[3].toUpperCase());
  if (checksum !== expectedChecksum) {
    throw new Error("Seed checksum does not match.");
  }

  const seedState: GameSetupSeedState = {
    difficulty,
    startWithHorse,
    loadoutProfile,
    journeyRandomnessMode,
    entropy,
  };

  return {
    ...seedState,
    seedCode: await encodeGameSetupSeed(seedState),
    canonical:
      seedState.entropy === 0n &&
      seedState.startWithHorse === true &&
      seedState.loadoutProfile === 0 &&
      seedState.journeyRandomnessMode === 0,
  };
}

function encodeDifficulty(difficulty: TravelDifficulty) {
  switch (difficulty) {
    case 0:
      return "N";
    case 1:
      return "E";
    case 2:
      return "H";
    default:
      throw new Error("Unsupported travel difficulty.");
  }
}

function decodeDifficulty(code: string): TravelDifficulty | null {
  if (code.length !== 1) {
    return null;
  }

  switch (code.toUpperCase()) {
    case "N":
      return 0;
    case "E":
      return 1;
    case "H":
      return 2;
    default:
      return null;
  }
}

function packOptions(seed: GameSetupSeedState) {
  if (seed.loadoutProfile < 0 || seed.loadoutProfile > 2) {
    throw new Error("Unsupported loadout profile.");
  }

  if (seed.journeyRandomnessMode < 0 || seed.journeyRandomnessMode > 1) {
    throw new Error("Unsupported journey randomness mode.");
  }

  let bits = 0;
  if (seed.startWithHorse) {
    bits |= 1;
  }

  bits |= (seed.loadoutProfile & 0x03) << 1;
  if (seed.journeyRandomnessMode === 1) {
    bits |= 1 << 3;
  }
  return bits;
}

function parseEntropy(part: string): bigint | null {
  if (part.length !== canonicalEntropyHexDigits || !/^[0-9A-Fa-f]+$/.test(part)) {
    return null;
  }

  try {
    const entropy = BigInt(`0x${part}`);
    return entropy <= canonicalEntropyMaximum ? entropy : null;
  } catch {
    return null;
  }
}

async function computeChecksum(difficulty: TravelDifficulty, optionsCode: string, entropyCode: string) {
  const payload = `${seedPrefix}|${generatorVersion}|${formatDifficultyName(difficulty)}|${Number.parseInt(optionsCode, 16)}|${entropyCode}`;
  const encoder = new TextEncoder();
  const bytes = encoder.encode(payload);
  const hash = await crypto.subtle.digest("SHA-256", bytes);
  const view = new Uint8Array(hash);
  const checksum = view[0] | (view[1] << 8);
  return checksum.toString(16).toUpperCase().padStart(4, "0");
}

function formatDifficultyName(difficulty: TravelDifficulty) {
  switch (difficulty) {
    case 0:
      return "Normal";
    case 1:
      return "Easy";
    case 2:
      return "Hard";
    default:
      throw new Error("Unsupported travel difficulty.");
  }
}

function generateEntropy() {
  const buffer = new Uint8Array(6);
  crypto.getRandomValues(buffer);
  let entropy = 0n;
  for (let index = 0; index < buffer.length; index++) {
    entropy |= BigInt(buffer[index]) << BigInt(index * 8);
  }

  return entropy;
}
