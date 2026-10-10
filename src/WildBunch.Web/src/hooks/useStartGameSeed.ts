import { useEffect, useState } from "react";
import type { GameDifficulty, GameEntropy, GameSessionDto } from "../api/types";
import { encodeGameSetupSeed } from "../ui/gameSetupSeedCodec";

interface UseStartGameSeedArgs {
  session: GameSessionDto | null;
  resetToken: number;
}

export interface UseStartGameSeedResult {
  playerName: string;
  gameDifficulty: GameDifficulty;
  gameEntropy: GameEntropy;
  seedDraft: string;
  decodeError: string | null;
  setPlayerName: (value: string) => void;
  setSeedDraft: (value: string) => void;
  setGameDifficulty: (difficulty: GameDifficulty) => void;
  setGameEntropy: (gameEntropy: GameEntropy) => void;
  randomizeSeed: () => void;
  validateSeedDraft: () => Promise<string | null>;
}

export function useStartGameSeed({
  session,
  resetToken,
}: UseStartGameSeedArgs): UseStartGameSeedResult {
  const [playerName, setPlayerName] = useState("");
  const [gameDifficulty, setGameDifficulty] = useState<GameDifficulty>(0);
  const [gameEntropy, setGameEntropy] = useState<GameEntropy>(1);
  const [seedDraft, setSeedDraft] = useState<string>(() => crypto.randomUUID());
  const [decodeError, setDecodeError] = useState<string | null>(null);

  useEffect(() => {
    setPlayerName(session?.player.name ?? "");
  }, [session?.id, session?.player.name, resetToken]);

  useEffect(() => {
    if (resetToken === 0) {
      return;
    }

    setGameDifficulty(0);
    setGameEntropy(1);
    setSeedDraft(crypto.randomUUID());
    setDecodeError(null);
  }, [resetToken]);

  function handleSeedDraftChange(value: string) {
    setDecodeError(null);
    setSeedDraft(value);
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
    setSeedDraft(crypto.randomUUID());
  }

  async function validateSeedDraft() {
    try {
      const seedCode = await encodeGameSetupSeed({ seedCode: seedDraft });
      setDecodeError(null);
      return seedCode;
    } catch (error) {
      setDecodeError(error instanceof Error ? error.message : "Seed code is invalid.");
      return null;
    }
  }

  return {
    playerName,
    gameDifficulty,
    gameEntropy,
    seedDraft,
    decodeError,
    setPlayerName,
    setSeedDraft: handleSeedDraftChange,
    setGameDifficulty: handleGameDifficultyChange,
    setGameEntropy: handleGameEntropyChange,
    randomizeSeed,
    validateSeedDraft,
  };
}
