import { useEffect, useState } from "react";
import type { GameDifficulty, GameEntropy, GameSessionDto } from "../api/types";
import {
  createCanonicalSeedState,
  decodeGameSetupSeed,
  encodeGameSetupSeed,
  type GameSetupSeedState,
} from "../ui/gameSetupSeedCodec";

const uuidPattern = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

interface UseStartGameSeedArgs {
  session: GameSessionDto | null;
  resetToken: number;
}

export interface UseStartGameSeedResult {
  playerName: string;
  gameDifficulty: GameDifficulty;
  gameEntropy: GameEntropy;
  seedState: GameSetupSeedState;
  seedDraft: string;
  seedDirty: boolean;
  decodeError: string | null;
  setPlayerName: (value: string) => void;
  setSeedDraft: (value: string) => void;
  setGameDifficulty: (difficulty: GameDifficulty) => void;
  setGameEntropy: (gameEntropy: GameEntropy) => void;
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
  const [gameDifficulty, setGameDifficulty] = useState<GameDifficulty>(0);
  const [gameEntropy, setGameEntropy] = useState<GameEntropy>(1);
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
    setGameDifficulty(0);
    setGameEntropy(1);
    setSeedState(resetSeed);
    setSeedDraft(resetSeed.seedCode);
    setSeedDirty(false);
    setDecodeError(null);
  }, [resetToken]);

  useEffect(() => {
    setSeedDraft(seedState.seedCode);
  }, [seedState.seedCode]);

  function handleSeedDraftChange(value: string) {
    setDecodeError(null);
    setSeedDraft(value);
    setSeedDirty(true);

    // Update seedState if the draft is a valid UUID
    const normalized = value.trim().toLowerCase();
    if (uuidPattern.test(normalized)) {
      setSeedState({ seedCode: normalized });
    }
  }

  function handleGameDifficultyChange(difficulty: GameDifficulty) {
    setDecodeError(null);
    setGameDifficulty(difficulty);
  }

  function handleGameEntropyChange(value: GameEntropy) {
    setGameEntropy(value);
  }

  function randomizeSeed() {
    setDecodeError(null);
    setSeedDirty(false);
    const randomSeed = crypto.randomUUID();
    setSeedState({ seedCode: randomSeed });
    setSeedDraft(randomSeed);
  }

  return {
    playerName,
    gameDifficulty,
    gameEntropy,
    seedState,
    seedDraft,
    seedDirty,
    decodeError,
    setPlayerName,
    setSeedDraft: handleSeedDraftChange,
    setGameDifficulty: handleGameDifficultyChange,
    setGameEntropy: handleGameEntropyChange,
    randomizeSeed,
  };
}
