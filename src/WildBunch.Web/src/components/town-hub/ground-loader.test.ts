import { describe, expect, it } from "vitest";
import {
  getDirtTileUrl,
  getPathTileUrl,
  getPropSpriteUrl,
  getRoadTileUrl,
  getSpurTileUrl,
  pickDirtMirroring,
  pickDirtVariantIndex,
  pickPropKind,
  pickPropPlacement,
  shouldPlaceProp,
} from "./ground-loader";

describe("ground-loader", () => {
  it("maps dirt variants to the shipped tile urls", () => {
    expect(getDirtTileUrl(0)).toBe("/assets/town-hub-ground/dirt/dirt-1.png");
    expect(getDirtTileUrl(1)).toBe("/assets/town-hub-ground/dirt/dirt-2.png");
    expect(getDirtTileUrl(2)).toBe("/assets/town-hub-ground/dirt/dirt-3.png");
  });

  it("maps road variants to the shipped road tile urls", () => {
    expect(getRoadTileUrl("flat")).toBe("/assets/town-hub-roads/main-road/road-flat-edge.png");
    expect(getRoadTileUrl("path")).toBe("/assets/town-hub-roads/main-road/road-path-edge.png");
    expect(getRoadTileUrl("spur")).toBe("/assets/town-hub-roads/main-road/road-spur-edge.png");
  });

  it("maps spur variants to the shipped east-leading tile urls", () => {
    expect(getSpurTileUrl("straight")).toBe("/assets/town-hub-roads/spur-road/spur-straight.png");
    expect(getSpurTileUrl("path")).toBe("/assets/town-hub-roads/spur-road/spur-path-edge.png");
    expect(getSpurTileUrl("end-cap")).toBe("/assets/town-hub-roads/spur-road/spur-end-cap.png");
    expect(getSpurTileUrl("cross")).toBe("/assets/town-hub-roads/spur-road/spur-path-cross.png");
  });

  it("maps path tile variants to the shipped path tile urls", () => {
    expect(getPathTileUrl("horizontal", "straight")).toBe(
      "/assets/town-hub-roads/path/path-horizontal-straight.png",
    );
    expect(getPathTileUrl("horizontal", "diagonal")).toBe(
      "/assets/town-hub-roads/path/path-horizontal-diagonal.png",
    );
    expect(getPathTileUrl("vertical", "straight")).toBe(
      "/assets/town-hub-roads/path/path-vertical-straight.png",
    );
    expect(getPathTileUrl("vertical", "diagonal")).toBe(
      "/assets/town-hub-roads/path/path-vertical-diagonal.png",
    );
  });

  it("maps prop kinds to normalized sprite urls", () => {
    expect(getPropSpriteUrl("barrel")).toBe("/assets/town-hub-ground/props/barrel-normalized.png");
    expect(getPropSpriteUrl("cactus")).toBe("/assets/town-hub-ground/props/cactus-normalized.png");
    expect(getPropSpriteUrl("fence-piece")).toBe("/assets/town-hub-ground/props/fence-piece-normalized.png");
    expect(getPropSpriteUrl("tumbleweed")).toBe("/assets/town-hub-ground/props/tumbleweed-normalized.png");
    expect(getPropSpriteUrl("water-trough")).toBe("/assets/town-hub-ground/props/water-trough-normalized.png");
  });

  it("picks deterministic dirt variants from a salt", () => {
    const first = pickDirtVariantIndex("dirt-salt", 1, 2);
    const second = pickDirtVariantIndex("dirt-salt", 1, 2);
    expect(first).toBe(second);
    expect(first).toBeGreaterThanOrEqual(0);
    expect(first).toBeLessThan(3);
  });

  it("picks deterministic dirt mirroring from a salt", () => {
    const first = pickDirtMirroring("dirt-salt", 1, 2);
    const second = pickDirtMirroring("dirt-salt", 1, 2);
    expect(first).toEqual(second);
  });

  it("picks deterministic prop placement from a salt", () => {
    const first = pickPropPlacement("prop-salt", 4, 5, "cactus");
    const second = pickPropPlacement("prop-salt", 4, 5, "cactus");
    expect(first).toEqual(second);
    expect(first.scale).toBe(0.6);
  });

  it("picks deterministic prop kinds from a salt", () => {
    const first = pickPropKind("prop-salt", 4, 5);
    const second = pickPropKind("prop-salt", 4, 5);
    expect(first).toBe(second);
  });

  it("suppresses props when a building blocks the tile", () => {
    expect(shouldPlaceProp("prop-salt", 4, 5, true)).toBe(false);
  });
});
