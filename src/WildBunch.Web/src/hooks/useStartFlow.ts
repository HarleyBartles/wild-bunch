import { useCallback, useState } from "react";
import type { GameEntropy, GameSessionDto, StartGameRequest, GameDifficulty } from "../api/types";
import { encodeGameSetupSeed } from "../ui/gameSetupSeedCodec";
import { useStartGameSeed } from "./useStartGameSeed";

export type StartFlowStep = "name" | "story" | "town" | "creating";

const stepOrder: readonly StartFlowStep[] = ["name", "story", "town", "creating"];

export interface UseStartFlowArgs {
  session: GameSessionDto | null;
  resetToken: number;
}

export interface StartFlowRequest {
  playerName: string;
  gameDifficulty: GameDifficulty;
  gameEntropy: GameEntropy;
  seedCode: string;
  startingTownId: string;
}

export interface UseStartFlowResult {
  step: StartFlowStep;
  playerName: string;
  selectedTownId: string | null;
  gameDifficulty: GameDifficulty;
  gameEntropy: GameEntropy;
  seedState: ReturnType<typeof useStartGameSeed>["seedState"];
  seedDraft: string;
  seedDirty: boolean;
  decodeError: string | null;
  setPlayerName: (value: string) => void;
  setSelectedTownId: (value: string | null) => void;
  setGameDifficulty: (difficulty: GameDifficulty) => void;
  setGameEntropy: (gameEntropy: GameEntropy) => void;
  setSeedDraft: (value: string) => void;
  randomizeSeed: () => void;
  goToStep: (step: StartFlowStep) => void;
  advance: () => void;
  goBack: () => void;
  buildStartGameRequest: (townId: string) => Promise<StartFlowRequest>;
}

export function useStartFlow({ session, resetToken }: UseStartFlowArgs): UseStartFlowResult {
  const seed = useStartGameSeed({ session, resetToken });
  const [step, setStep] = useState<StartFlowStep>("name");
  const [selectedTownId, setSelectedTownId] = useState<string | null>(null);

  const advance = useCallback(() => {
    setStep((current) => {
      const index = stepOrder.indexOf(current);
      if (index < 0 || index >= stepOrder.length - 1) {
        return current;
      }
      return stepOrder[index + 1];
    });
  }, []);

  const goBack = useCallback(() => {
    setStep((current) => {
      const index = stepOrder.indexOf(current);
      if (index <= 0) {
        return current;
      }
      return stepOrder[index - 1];
    });
  }, []);

  const goToStep = useCallback((next: StartFlowStep) => {
    setStep(next);
  }, []);

  const buildStartGameRequest = useCallback(
    async (townId: string): Promise<StartFlowRequest> => {
      const seedCode = await encodeGameSetupSeed(seed.seedState);
      const trimmedName = seed.playerName.trim();
      return {
        playerName: trimmedName,
        gameDifficulty: seed.gameDifficulty,
        gameEntropy: seed.gameEntropy,
        seedCode,
        startingTownId: townId,
      };
    },
    [seed.playerName, seed.seedState, seed.gameDifficulty, seed.gameEntropy],
  );

  return {
    step,
    playerName: seed.playerName,
    selectedTownId,
    gameDifficulty: seed.gameDifficulty,
    gameEntropy: seed.gameEntropy,
    seedState: seed.seedState,
    seedDraft: seed.seedDraft,
    seedDirty: seed.seedDirty,
    decodeError: seed.decodeError,
    setPlayerName: seed.setPlayerName,
    setSelectedTownId,
    setGameDifficulty: seed.setGameDifficulty,
    setGameEntropy: seed.setGameEntropy,
    setSeedDraft: seed.setSeedDraft,
    randomizeSeed: seed.randomizeSeed,
    goToStep,
    advance,
    goBack,
    buildStartGameRequest,
  };
}

export type StartGameHandler = (request: StartGameRequest) => Promise<void>;
