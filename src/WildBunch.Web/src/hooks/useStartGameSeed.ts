import { useEffect, useState } from "react";
import type { GameDifficulty, GameEntropy, GameSessionDto } from "../api/types";
import {
  createCanonicalSeedState,
  decodeGameSetupSeed,
  encodeGameSetupSeed,
  type GameSetupSeedState,
} from "../ui/gameSetupSeedCodec";
import { getRepresentativeSeed, decodeSeed } from "../api/wildBunchApi";

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

  async function applySeed() {
    if (!seedDirty) {
      return;
    }

    try {
      const decoded = await decodeGameSetupSeed(seedDraft);
      setDecodeError(null);
      setSeedDirty(false);
      setSeedState(decoded);

      // Decode the seed to get the encoded difficulty and entropy
      const seedDecoded = await decodeSeed(decoded.seedCode);
      setGameDifficulty(seedDecoded.gameDifficulty);
      setGameEntropy(seedDecoded.gameEntropy);
    } catch (error) {
      setDecodeError(getErrorMessage(error));
    }
  }

  function handleSeedDraftChange(value: string) {
    setDecodeError(null);
    setSeedDraft(value);
    setSeedDirty(true);
  }

  async function handleGameDifficultyChange(difficulty: GameDifficulty) {
    setDecodeError(null);
    setGameDifficulty(difficulty);
    try {
      const seed = await getRepresentativeSeed(difficulty, gameEntropy);
      setSeedState({ seedCode: seed });
      setSeedDirty(false);
    } catch (error) {
      setDecodeError(getErrorMessage(error));
    }
  }

  async function handleGameEntropyChange(value: GameEntropy) {
    setGameEntropy(value);
    try {
      const seed = await getRepresentativeSeed(gameDifficulty, value);
      setSeedState({ seedCode: seed });
      setSeedDirty(false);
    } catch (error) {
      setDecodeError(getErrorMessage(error));
    }
  }

  async function randomizeSeed() {
    setDecodeError(null);
    setSeedDirty(false);
    try {
      const seed = await getRepresentativeSeed(gameDifficulty, gameEntropy);
      setSeedState({ seedCode: seed });
    } catch (error) {
      setDecodeError(getErrorMessage(error));
    }
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
    applySeed,
    randomizeSeed,
  };
}
