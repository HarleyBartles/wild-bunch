import { describe, expect, it } from "vitest";
import { getPathTileUrl } from "../components/town-hub/path-loader";

describe("path-loader", () => {
  it("maps path tile names to the town-hub road asset tree", () => {
    expect(getPathTileUrl("path-horizontal-diagonal")).toBe(
      "/assets/town-hub-roads/path/path-horizontal-diagonal.png",
    );
    expect(getPathTileUrl("path-horizontal-straight")).toBe(
      "/assets/town-hub-roads/path/path-horizontal-straight.png",
    );
    expect(getPathTileUrl("path-vertical-diagonal")).toBe(
      "/assets/town-hub-roads/path/path-vertical-diagonal.png",
    );
    expect(getPathTileUrl("path-vertical-straight")).toBe(
      "/assets/town-hub-roads/path/path-vertical-straight.png",
    );
  });
});
