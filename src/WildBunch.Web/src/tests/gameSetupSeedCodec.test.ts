import { afterEach, describe, expect, it, vi } from "vitest";
import { createCanonicalSeedState, decodeGameSetupSeed, encodeGameSetupSeed, withRandomSeed } from "../ui/gameSetupSeedCodec";

afterEach(() => {
  vi.restoreAllMocks();
});

describe("gameSetupSeedCodec", () => {
  it("round-trips UUID-shaped seed codes", async () => {
    const seedState = createCanonicalSeedState();

    const seedCode = await encodeGameSetupSeed(seedState);
    const decoded = await decodeGameSetupSeed(seedCode);

    expect(seedCode).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/);
    expect(decoded.seedCode).toBe(seedCode);
    expect(decoded.canonical).toBe(true);
  });

  it("normalizes valid UUID input to lowercase canonical form", async () => {
    const decoded = await decodeGameSetupSeed("7D455293-F269-A642-72AF-0193FDBDFB51");

    expect(decoded.seedCode).toBe("7d455293-f269-a642-72af-0193fdbdfb51");
    expect(decoded.canonical).toBe(false);
  });

  it("randomizes seed codes without leaving the UUID shape", () => {
    const randomUUID = vi.spyOn(globalThis.crypto, "randomUUID").mockReturnValue("11111111-2222-3333-4444-555555555555");

    const randomSeed = withRandomSeed(createCanonicalSeedState());

    expect(randomSeed.seedCode).toBe("11111111-2222-3333-4444-555555555555");
    expect(randomSeed.seedCode).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/);
    expect(randomUUID).toHaveBeenCalledTimes(1);
  });

  it("rejects malformed and WB1 seed codes", async () => {
    await expect(encodeGameSetupSeed({ seedCode: "WB1-N-03-1000000000000-0000" })).rejects.toThrow(/uuid-shaped/i);
    await expect(decodeGameSetupSeed("WB1-N-03-1000000000000-0000")).rejects.toThrow(/uuid-shaped/i);
    await expect(decodeGameSetupSeed("not-a-uuid")).rejects.toThrow(/uuid-shaped/i);
  });
});
