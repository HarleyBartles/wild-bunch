import { act, renderHook } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { useStartGameSeed } from "../hooks/useStartGameSeed";

describe("useStartGameSeed", () => {
  afterEach(() => vi.restoreAllMocks());

  it("creates one seed per setup visit and replaces it only when the visit resets", () => {
    const uuid = vi.spyOn(crypto, "randomUUID");
    uuid.mockReturnValueOnce("11111111-1111-4111-8111-111111111111");
    uuid.mockReturnValueOnce("22222222-2222-4222-8222-222222222222");
    const { result, rerender } = renderHook(
      ({ resetToken }) => useStartGameSeed({ session: null, resetToken }),
      { initialProps: { resetToken: 0 } },
    );

    expect(result.current.seedDraft).toBe("11111111-1111-4111-8111-111111111111");
    act(() => result.current.setGameDifficulty(2));
    expect(result.current.seedDraft).toBe("11111111-1111-4111-8111-111111111111");

    rerender({ resetToken: 1 });

    expect(result.current.seedDraft).toBe("22222222-2222-4222-8222-222222222222");
    expect(uuid).toHaveBeenCalledTimes(2);
  });
});
