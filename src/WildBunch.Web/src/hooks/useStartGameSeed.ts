import { useEffect, useState } from "react";
import type { AdventureRandomnessPolicy, GameSessionDto, TravelDifficulty } from "../api/types";
import {
  createCanonicalSeedState,
  decodeGameSetupSeed,
  encodeGameSetupSeed,
  withRandomSeed,
  type GameSetupSeedState,
} from "../ui/gameSetupSeedCodec";

interface UseStartGameSeedArgs {
  session: GameSessionDto | null;
  resetToken: number;
}

export interface UseStartGameSeedResult {
  playerName: string;
  travelDifficulty: TravelDifficulty;
  entropy: AdventureRandomnessPolicy;
  seedState: GameSetupSeedState;
  seedDraft: string;
  seedDirty: boolean;
  decodeError: string | null;
  setPlayerName: (value: string) => void;
  setSeedDraft: (value: string) => void;
  setTravelDifficulty: (difficulty: TravelDifficulty) => void;
  setEntropy: (entropy: AdventureRandomnessPolicy) => void;
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
  const [travelDifficulty, setTravelDifficulty] = useState<TravelDifficulty>(0);
  const [entropy, setEntropy] = useState<AdventureRandomnessPolicy>(1);
  const [seedState, setSeedState] = useState(createCanonicalSeedState());
  const [seedDraft, setSeedDraft] = useState(createCanonicalSeedState().seedCode);
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
    setTravelDifficulty(0);
    setEntropy(1);
    setSeedState(resetSeed);
    setSeedDraft(resetSeed.seedCode);
    setSeedDirty(false);
    setDecodeError(null);
  }, [resetToken]);

  useEffect(() => {
    setSeedDraft(seedState.seedCode);
  }, [seedState.seedCode]);

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

  function handleTravelDifficultyChange(difficulty: TravelDifficulty) {
    setDecodeError(null);
    setTravelDifficulty(difficulty);
  }

  function handleEntropyChange(value: AdventureRandomnessPolicy) {
    setEntropy(value);
  }

  function randomizeSeed() {
    setDecodeError(null);
    setSeedDirty(false);
    setSeedState((current) => withRandomSeed(current));
  }

  return {
    playerName,
    travelDifficulty,
    entropy,
    seedState,
    seedDraft,
    seedDirty,
    decodeError,
    setPlayerName,
    setSeedDraft: handleSeedDraftChange,
    setTravelDifficulty: handleTravelDifficultyChange,
    setEntropy: handleEntropyChange,
    applySeed,
    randomizeSeed,
  };
}
