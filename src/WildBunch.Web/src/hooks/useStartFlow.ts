import { useCallback, useState } from "react";
import type { GameEntropy, GameSessionDto, GameDifficulty } from "../api/types";
import { useStartGameSeed } from "./useStartGameSeed";

export type StartFlowStep = "name" | "story" | "town" | "creating";

const stepOrder: readonly StartFlowStep[] = ["name", "story", "town", "creating"];

export interface UseStartFlowArgs {
  session: GameSessionDto | null;
  resetToken: number;
}

export interface UseStartFlowResult {
  step: StartFlowStep;
  playerName: string;
  selectedTownId: string | null;
  gameDifficulty: GameDifficulty;
  gameEntropy: GameEntropy;
  seedDraft: string;
  decodeError: string | null;
  setPlayerName: (value: string) => void;
  setSelectedTownId: (value: string | null) => void;
  setGameDifficulty: (difficulty: GameDifficulty) => void;
  setGameEntropy: (gameEntropy: GameEntropy) => void;
  setSeedDraft: (value: string) => void;
  randomizeSeed: () => void;
  validateSeedDraft: () => Promise<string | null>;
  goToStep: (step: StartFlowStep) => void;
  advance: () => void;
  goBack: () => void;
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

  return {
    step,
    playerName: seed.playerName,
    selectedTownId,
    gameDifficulty: seed.gameDifficulty,
    gameEntropy: seed.gameEntropy,
    seedDraft: seed.seedDraft,
    decodeError: seed.decodeError,
    setPlayerName: seed.setPlayerName,
    setSelectedTownId,
    setGameDifficulty: seed.setGameDifficulty,
    setGameEntropy: seed.setGameEntropy,
    setSeedDraft: seed.setSeedDraft,
    randomizeSeed: seed.randomizeSeed,
    validateSeedDraft: seed.validateSeedDraft,
    goToStep,
    advance,
    goBack,
  };
}
