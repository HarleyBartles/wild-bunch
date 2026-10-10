import type { SetupGameRequest } from "../api/types";

const pendingSetupKey = "wild-bunch.pending-setup-operation";
const pendingArrivalKeyPrefix = "wild-bunch.pending-first-arrival:";

export interface PendingSetupOperation {
  operationId: string;
  request: SetupGameRequest;
}

export interface PendingFirstArrivalOperation {
  operationId: string;
  sessionId: string;
  townId: string;
}

export function readPendingSetupOperation(): PendingSetupOperation | null {
  const value = window.localStorage.getItem(pendingSetupKey);
  if (!value) return null;
  try {
    const parsed = JSON.parse(value) as Partial<PendingSetupOperation>;
    if (
      typeof parsed.operationId === "string" &&
      typeof parsed.request?.playerName === "string" &&
      typeof parsed.request.seedCode === "string" &&
      typeof parsed.request.gameDifficulty === "number" &&
      typeof parsed.request.gameEntropy === "number"
    ) {
      return { operationId: parsed.operationId, request: parsed.request };
    }
  } catch {
    // An incomplete local write is not an operation the server can recognize.
  }
  window.localStorage.removeItem(pendingSetupKey);
  return null;
}

export function writePendingSetupOperation(operation: PendingSetupOperation) {
  window.localStorage.setItem(pendingSetupKey, JSON.stringify(operation));
}

export function clearPendingSetupOperation() {
  window.localStorage.removeItem(pendingSetupKey);
}

export function readPendingFirstArrivalOperation(
  sessionId: string,
): PendingFirstArrivalOperation | null {
  const value = window.localStorage.getItem(`${pendingArrivalKeyPrefix}${sessionId}`);
  if (!value) return null;
  try {
    const parsed = JSON.parse(value) as Partial<PendingFirstArrivalOperation>;
    if (
      typeof parsed.operationId === "string" &&
      parsed.sessionId === sessionId &&
      typeof parsed.townId === "string"
    ) {
      return { operationId: parsed.operationId, sessionId, townId: parsed.townId };
    }
  } catch {
    // An incomplete local write is not an operation the server can recognize.
  }
  clearPendingFirstArrivalOperation(sessionId);
  return null;
}

export function writePendingFirstArrivalOperation(operation: PendingFirstArrivalOperation) {
  window.localStorage.setItem(
    `${pendingArrivalKeyPrefix}${operation.sessionId}`,
    JSON.stringify(operation),
  );
}

export function clearPendingFirstArrivalOperation(sessionId: string) {
  window.localStorage.removeItem(`${pendingArrivalKeyPrefix}${sessionId}`);
}
