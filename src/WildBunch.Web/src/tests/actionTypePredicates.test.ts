import { describe, expect, it } from "vitest";
import {
  actionIsWantedPosters,
  actionIsInspectNoticeBoard,
  actionIsCheckLocalRecords,
  actionIsFollowTelegraphLeads,
  actionIsGatherLocalGossip,
  actionIsLookAroundSaloon,
} from "../utils/actionTypePredicates";
import { AvailableActionKind } from "../api/types";
import type { AvailableActionDto } from "../api/types";

function makeAction(kind: AvailableActionKind): AvailableActionDto {
  return {
    kind,
    label: "Test action",
    destinationTownId: null,
  } as AvailableActionDto;
}

describe("actionTypePredicates", () => {
  it("returns true only for the matching kind", () => {
    expect(actionIsWantedPosters(makeAction(AvailableActionKind.ReadWantedPosters))).toBe(true);
    expect(actionIsWantedPosters(makeAction(AvailableActionKind.InspectNoticeBoard))).toBe(false);
  });

  it("identifies InspectNoticeBoard", () => {
    expect(actionIsInspectNoticeBoard(makeAction(AvailableActionKind.InspectNoticeBoard))).toBe(true);
    expect(actionIsInspectNoticeBoard(makeAction(AvailableActionKind.ReadWantedPosters))).toBe(false);
  });

  it("identifies CheckSheriffRecords as checkLocalRecords", () => {
    expect(actionIsCheckLocalRecords(makeAction(AvailableActionKind.CheckSheriffRecords))).toBe(true);
  });

  it("identifies FollowTelegraphLeads", () => {
    expect(actionIsFollowTelegraphLeads(makeAction(AvailableActionKind.FollowTelegraphLeads))).toBe(true);
  });

  it("identifies GatherLocalGossip", () => {
    expect(actionIsGatherLocalGossip(makeAction(AvailableActionKind.GatherLocalGossip))).toBe(true);
  });

  it("identifies LookAroundSaloon", () => {
    expect(actionIsLookAroundSaloon(makeAction(AvailableActionKind.LookAroundSaloon))).toBe(true);
  });
});
