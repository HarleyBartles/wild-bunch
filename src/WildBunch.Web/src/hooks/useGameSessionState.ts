import { useState } from "react";
import { readStoredGameId } from "../utils/formatting";
import {
  readPendingSetupOperation,
  type PendingSetupOperation,
} from "../utils/startupOperationStorage";

export function useGameSessionState() {
  const [storedGameId, setStoredGameId] = useState<string | null>(readStoredGameId);
  const [pendingSetup, setPendingSetup] = useState<PendingSetupOperation | null>(
    readPendingSetupOperation,
  );
  const [declaredWantedIdentityHandle, setDeclaredWantedIdentityHandle] = useState("");
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");
  const [resetToken, setResetToken] = useState(0);

  return {
    storedGameId,
    setStoredGameId,
    pendingSetup,
    setPendingSetup,
    declaredWantedIdentityHandle,
    setDeclaredWantedIdentityHandle,
    notice,
    setNotice,
    error,
    setError,
    resetToken,
    setResetToken,
  };
}
