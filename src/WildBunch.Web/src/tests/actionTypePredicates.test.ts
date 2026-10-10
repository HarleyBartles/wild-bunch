import { describe, expect, it } from "vitest";
import {
  actionIsWantedPosters,
  actionIsGatherLocalGossip,
  actionIsLookAroundSaloon,
} from "../utils/actionTypePredicates";
import { AvailableActionKind } from "../api/types";
import type { AvailableActionDto } from "../api/types";

function makeAction(kind: AvailableActionKind): AvailableActionDto {
  return { kind, label: "Test action", destinationTownId: null } as AvailableActionDto;
}

describe("actionTypePredicates", () => {
  it("identifies wanted posters", () => {
    expect(actionIsWantedPosters(makeAction(AvailableActionKind.ReadWantedPosters))).toBe(true);
  });

  it("identifies local gossip", () => {
    expect(actionIsGatherLocalGossip(makeAction(AvailableActionKind.GatherLocalGossip))).toBe(true);
  });

  it("identifies saloon observation", () => {
    expect(actionIsLookAroundSaloon(makeAction(AvailableActionKind.LookAroundSaloon))).toBe(true);
  });
});
