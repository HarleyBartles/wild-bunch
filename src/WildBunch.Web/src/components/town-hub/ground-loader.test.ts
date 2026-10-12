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
    expect(getPropSpriteUrl("fence-piece")).toBe(
      "/assets/town-hub-ground/props/fence-piece-normalized.png",
    );
    expect(getPropSpriteUrl("tumbleweed")).toBe(
      "/assets/town-hub-ground/props/tumbleweed-normalized.png",
    );
    expect(getPropSpriteUrl("water-trough")).toBe(
      "/assets/town-hub-ground/props/water-trough-normalized.png",
    );
  });

  it("preserves the dirt variant selected by the recorded salt and cell", () => {
    expect(pickDirtVariantIndex("dirt-salt", 1, 2)).toBe(1);
  });

  it("preserves dirt mirroring selected by the recorded salt and cell", () => {
    expect(pickDirtMirroring("dirt-salt", 1, 2)).toEqual({ flipX: true, flipY: true });
  });

  it("preserves prop placement selected by the recorded salt and cell", () => {
    expect(pickPropPlacement("prop-salt", 4, 5, "cactus")).toEqual({
      offsetX: 2,
      offsetY: 7,
      scale: 0.6,
    });
  });

  it("preserves the prop kind selected by the recorded salt and cell", () => {
    expect(pickPropKind("prop-salt", 4, 5)).toBe("cactus");
  });

  it("places an eligible prop unless a building occupies the cell", () => {
    expect(shouldPlaceProp("prop-salt", 5, 4)).toBe(true);
    expect(shouldPlaceProp("prop-salt", 5, 4, true)).toBe(false);
  });
});
