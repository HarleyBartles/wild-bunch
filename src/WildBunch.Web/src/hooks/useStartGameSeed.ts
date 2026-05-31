import { useEffect, useState } from "react";
import type { GameSessionDto, TravelDifficulty } from "../api/types";
import {
  createCanonicalSeedState,
  decodeGameSetupSeed,
  encodeGameSetupSeed,
  withDifficulty,
  withJourneyRandomnessMode,
  withLoadoutProfile,
  withRandomEntropy,
  withStartWithHorse,
  type GameSetupLoadoutProfile,
  type GameSetupSeedState,
} from "../ui/gameSetupSeedCodec";

interface UseStartGameSeedArgs {
  session: GameSessionDto | null;
  resetToken: number;
}

export interface UseStartGameSeedResult {
  playerName: string;
  seedState: GameSetupSeedState;
  seedDraft: string;
  seedDirty: boolean;
  decodeError: string | null;
  setPlayerName: (value: string) => void;
  setSeedDraft: (value: string) => void;
  setDifficulty: (difficulty: TravelDifficulty) => void;
  setStartWithHorse: (value: boolean) => void;
  setLoadoutProfile: (profile: GameSetupLoadoutProfile) => void;
  setJourneyRandomnessMode: (mode: 0 | 1) => void;
  applySeed: () => Promise<void>;
  randomizeSeed: () => void;
}

function getErrorMessage(error: unknown) {
  if (error instanceof Error) {
    return error.message;
  }

  if (typeof error === "string" && error.trim()) {
    return error;
  }

  return "Unable to update the setup seed.";
}

export function useStartGameSeed({ session, resetToken }: UseStartGameSeedArgs): UseStartGameSeedResult {
  const [playerName, setPlayerName] = useState("");
  const [seedState, setSeedState] = useState(createCanonicalSeedState());
  const [seedDraft, setSeedDraft] = useState("");
  const [seedDirty, setSeedDirty] = useState(false);
  const [decodeError, setDecodeError] = useState<string | null>(null);

  useEffect(() => {
    setPlayerName(session?.player.name ?? "");
  }, [session?.id, session?.player.name, resetToken]);

  useEffect(() => {
    if (resetToken === 0) {
      return;
    }

    const resetSeed = createCanonicalSeedState();
    setSeedState(resetSeed);
    setSeedDirty(false);
    setDecodeError(null);
  }, [resetToken]);

  useEffect(() => {
    let cancelled = false;

    void encodeGameSetupSeed(seedState)
      .then((seedCode) => {
        if (!cancelled) {
          setSeedDraft(seedCode);
        }
      })
      .catch((error) => {
        if (!cancelled) {
          setDecodeError(getErrorMessage(error));
        }
      });

    return () => {
      cancelled = true;
    };
  }, [seedState]);

  async function applySeed() {
    if (!seedDirty) {
      return;
    }

    try {
      const decoded = await decodeGameSetupSeed(seedDraft);
      setDecodeError(null);
      setSeedDirty(false);
      setSeedState(decoded);
    } catch (error) {
      setDecodeError(getErrorMessage(error));
    }
  }

  function handleSeedDraftChange(value: string) {
    setDecodeError(null);
    setSeedDraft(value);
    setSeedDirty(true);
  }

  function handleDifficultyChange(difficulty: TravelDifficulty) {
    setDecodeError(null);
    setSeedDirty(false);
    setSeedState((current) => withDifficulty(current, difficulty));
  }

  function handleStartWithHorseChange(value: boolean) {
    setDecodeError(null);
    setSeedDirty(false);
    setSeedState((current) => withStartWithHorse(current, value));
  }

  function handleLoadoutProfileChange(profile: GameSetupLoadoutProfile) {
    setDecodeError(null);
    setSeedDirty(false);
    setSeedState((current) => withLoadoutProfile(current, profile));
  }

  function handleJourneyRandomnessModeChange(mode: 0 | 1) {
    setDecodeError(null);
    setSeedDirty(false);
    setSeedState((current) => withJourneyRandomnessMode(current, mode));
  }

  function randomizeSeed() {
    setDecodeError(null);
    setSeedDirty(false);
    setSeedState((current) => withRandomEntropy(current));
  }

  return {
    playerName,
    seedState,
    seedDraft,
    seedDirty,
    decodeError,
    setPlayerName,
    setSeedDraft: handleSeedDraftChange,
    setDifficulty: handleDifficultyChange,
    setStartWithHorse: handleStartWithHorseChange,
    setLoadoutProfile: handleLoadoutProfileChange,
    setJourneyRandomnessMode: handleJourneyRandomnessModeChange,
    applySeed,
    randomizeSeed,
  };
}
