import { describe, expect, it } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useStartGameSeed } from "../hooks/useStartGameSeed";

describe("useStartGameSeed", () => {
  it("updates seedState when seedDraft is changed to a valid UUID", () => {
    const { result } = renderHook(() => useStartGameSeed({ session: null, resetToken: 0 }));

    expect(result.current.seedState.seedCode).toBe("00000000-0000-0000-0000-000000000000");

    act(() => {
      result.current.setSeedDraft("0320c0c4-0000-0000-0000-000000000000");
    });

    expect(result.current.seedState.seedCode).toBe("0320c0c4-0000-0000-0000-000000000000");
  });

  it("does not update seedState when seedDraft is changed to an invalid UUID", () => {
    const { result } = renderHook(() => useStartGameSeed({ session: null, resetToken: 0 }));

    const originalSeedCode = result.current.seedState.seedCode;

    act(() => {
      result.current.setSeedDraft("invalid-uuid");
    });

    expect(result.current.seedState.seedCode).toBe(originalSeedCode);
  });
});
