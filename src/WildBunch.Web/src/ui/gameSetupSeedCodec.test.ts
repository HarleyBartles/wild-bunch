import { describe, expect, it } from "vitest";
import {
  createCanonicalSeedState,
  decodeGameSetupSeed,
  encodeGameSetupSeed,
  withDifficulty,
  withJourneyRandomnessMode,
  withLoadoutProfile,
  withRandomEntropy,
  withStartWithHorse,
} from "./gameSetupSeedCodec";

describe("gameSetupSeedCodec", () => {
  it("round-trips the encoded seed and decodes the v1 options", async () => {
    const seedState = withLoadoutProfile(
      withJourneyRandomnessMode(withStartWithHorse(withDifficulty(createCanonicalSeedState(), 2), false), 1),
      2,
    );

    const seedCode = await encodeGameSetupSeed(seedState);
    const decoded = await decodeGameSetupSeed(seedCode);

    expect(decoded.seedCode).toBe(seedCode);
    expect(decoded.difficulty).toBe(2);
    expect(decoded.startWithHorse).toBe(false);
    expect(decoded.loadoutProfile).toBe(2);
    expect(decoded.journeyRandomnessMode).toBe(1);
    expect(decoded.entropy).toBe(seedState.entropy);
    expect(decoded.canonical).toBe(false);
  });

  it("rewrites the seed when the difficulty or option flags change", async () => {
    const canonical = createCanonicalSeedState();
    const hardSeed = withDifficulty(canonical, 2);
    const noHorseSeed = withStartWithHorse(canonical, false);
    const stockedSeed = withLoadoutProfile(canonical, 2);
    const deterministicSeed = withJourneyRandomnessMode(canonical, 1);

    const canonicalCode = await encodeGameSetupSeed(canonical);
    const hardCode = await encodeGameSetupSeed(hardSeed);
    const noHorseCode = await encodeGameSetupSeed(noHorseSeed);
    const stockedCode = await encodeGameSetupSeed(stockedSeed);
    const deterministicCode = await encodeGameSetupSeed(deterministicSeed);

    expect(canonicalCode).not.toBe(hardCode);
    expect(canonicalCode).not.toBe(noHorseCode);
    expect(canonicalCode).not.toBe(stockedCode);
    expect(canonicalCode).not.toBe(deterministicCode);
  });

  it("round-trips canonical entropy boundaries and a representative middle value", async () => {
    for (const entropy of [0n, 1n, 0x800000000000n, 0xFFFFFFFFFFFFn]) {
      const seed = { ...createCanonicalSeedState(), entropy };
      const seedCode = await encodeGameSetupSeed(seed);
      const decoded = await decodeGameSetupSeed(seedCode);

      expect(decoded.entropy).toBe(entropy);
      expect(decoded.seedCode).toBe(seedCode);
      expect(decoded.journeyRandomnessMode).toBe(0);
    }
  });

  it("randomizes entropy only within the canonical range", async () => {
    for (let index = 0; index < 100; index++) {
      const randomSeed = withRandomEntropy(createCanonicalSeedState());

      expect(randomSeed.entropy).toBeGreaterThanOrEqual(0n);
      expect(randomSeed.entropy).toBeLessThanOrEqual(0xFFFFFFFFFFFFn);

      const seedCode = await encodeGameSetupSeed(randomSeed);
      const decoded = await decodeGameSetupSeed(seedCode);

      expect(decoded.entropy).toBe(randomSeed.entropy);
    }
  });

  it("rejects malformed or out-of-range entropy values", async () => {
    await expect(encodeGameSetupSeed({ ...createCanonicalSeedState(), entropy: 0x1000000000000n })).rejects.toThrow(
      /entropy must be between/i,
    );

    await expect(decodeGameSetupSeed("WB1-N-03-1000000000000-0000")).rejects.toThrow(/seed entropy is invalid/i);
    await expect(decodeGameSetupSeed("WB1-N-03-12345Z789ABC-0000")).rejects.toThrow(/seed entropy is invalid/i);
    await expect(decodeGameSetupSeed("WB1-N-03-123456789AB-0000")).rejects.toThrow(/seed entropy is invalid/i);
  });
});
