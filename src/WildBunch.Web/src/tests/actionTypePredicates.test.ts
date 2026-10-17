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
  it.each([
    {
      label: "wanted posters",
      predicate: actionIsWantedPosters,
      matchingKind: AvailableActionKind.ReadWantedPosters,
      unrelatedKind: AvailableActionKind.BuySupplies,
    },
    {
      label: "local gossip",
      predicate: actionIsGatherLocalGossip,
      matchingKind: AvailableActionKind.GatherLocalGossip,
      unrelatedKind: AvailableActionKind.ReadWantedPosters,
    },
    {
      label: "saloon observation",
      predicate: actionIsLookAroundSaloon,
      matchingKind: AvailableActionKind.LookAroundSaloon,
      unrelatedKind: AvailableActionKind.GatherLocalGossip,
    },
  ])(
    "$label routes only its own available action",
    ({ predicate, matchingKind, unrelatedKind }) => {
      expect(predicate(makeAction(matchingKind))).toBe(true);
      expect(predicate(makeAction(unrelatedKind))).toBe(false);
    },
  );
});
