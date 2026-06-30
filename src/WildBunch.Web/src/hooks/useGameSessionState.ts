import { useEffect, useState } from "react";
import type { WantedPosterDto } from "../api/types";
import { readStoredGameId } from "../utils/formatting";

export function useGameSessionState() {
  const [storedGameId, setStoredGameId] = useState<string | null>(readStoredGameId);
  const [wantedPosters, setWantedPosters] = useState<WantedPosterDto[]>([]);
  const [declaredWantedIdentityHandle, setDeclaredWantedIdentityHandle] = useState("");
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");
  const [resetToken, setResetToken] = useState(0);

  useEffect(() => {
    if (wantedPosters.length === 0) {
      setDeclaredWantedIdentityHandle("");
      return;
    }
    setDeclaredWantedIdentityHandle((current) =>
      wantedPosters.some((poster) => poster.posterId === current) ? current : wantedPosters[0].posterId,
    );
  }, [wantedPosters]);

  return {
    storedGameId,
    setStoredGameId,
    wantedPosters,
    setWantedPosters,
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
