import { AvailableActionKind } from "../api/types";
import type { AvailableActionDto } from "../api/types";

export function actionIsWantedPosters(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.ReadWantedPosters;
}

export function actionIsInspectNoticeBoard(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.InspectNoticeBoard;
}

export function actionIsCheckLocalRecords(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.CheckSheriffRecords;
}

export function actionIsFollowTelegraphLeads(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.FollowTelegraphLeads;
}

export function actionIsGatherLocalGossip(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.GatherLocalGossip;
}

export function actionIsLookAroundSaloon(action: AvailableActionDto) {
  return action.kind === AvailableActionKind.LookAroundSaloon;
}
