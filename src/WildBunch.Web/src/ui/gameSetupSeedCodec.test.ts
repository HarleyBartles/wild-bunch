import { describe, expect, it } from "vitest";
import {
  createCanonicalSeedState,
  decodeGameSetupSeed,
  encodeGameSetupSeed,
  withDifficulty,
  withLoadoutProfile,
  withStartWithHorse,
} from "./gameSetupSeedCodec";

describe("gameSetupSeedCodec", () => {
  it("round-trips the encoded seed and decodes the v1 options", async () => {
    const seedState = withLoadoutProfile(
      withStartWithHorse(withDifficulty(createCanonicalSeedState(), 2), false),
      2,
    );

    const seedCode = await encodeGameSetupSeed(seedState);
    const decoded = await decodeGameSetupSeed(seedCode);

    expect(decoded.seedCode).toBe(seedCode);
    expect(decoded.difficulty).toBe(2);
    expect(decoded.startWithHorse).toBe(false);
    expect(decoded.loadoutProfile).toBe(2);
    expect(decoded.entropy).toBe(seedState.entropy);
    expect(decoded.canonical).toBe(false);
  });

  it("rewrites the seed when the difficulty or option flags change", async () => {
    const canonical = createCanonicalSeedState();
    const hardSeed = withDifficulty(canonical, 2);
    const noHorseSeed = withStartWithHorse(canonical, false);
    const stockedSeed = withLoadoutProfile(canonical, 2);

    const canonicalCode = await encodeGameSetupSeed(canonical);
    const hardCode = await encodeGameSetupSeed(hardSeed);
    const noHorseCode = await encodeGameSetupSeed(noHorseSeed);
    const stockedCode = await encodeGameSetupSeed(stockedSeed);

    expect(canonicalCode).not.toBe(hardCode);
    expect(canonicalCode).not.toBe(noHorseCode);
    expect(canonicalCode).not.toBe(stockedCode);
  });
});
