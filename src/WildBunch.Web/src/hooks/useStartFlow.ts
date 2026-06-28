import { useCallback, useState } from "react";
import type { AdventureRandomnessPolicy, GameSessionDto, StartGameRequest, TravelDifficulty } from "../api/types";
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
  travelDifficulty: TravelDifficulty;
  entropy: AdventureRandomnessPolicy;
  seedCode: string;
  startingTownId: string;
}

export interface UseStartFlowResult {
  step: StartFlowStep;
  playerName: string;
  selectedTownId: string | null;
  travelDifficulty: TravelDifficulty;
  entropy: AdventureRandomnessPolicy;
  seedState: ReturnType<typeof useStartGameSeed>["seedState"];
  seedDraft: string;
  seedDirty: boolean;
  decodeError: string | null;
  setPlayerName: (value: string) => void;
  setSelectedTownId: (value: string | null) => void;
  setTravelDifficulty: (difficulty: TravelDifficulty) => void;
  setEntropy: (entropy: AdventureRandomnessPolicy) => void;
  setSeedDraft: (value: string) => void;
  applySeed: () => Promise<void>;
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
        travelDifficulty: seed.travelDifficulty,
        entropy: seed.entropy,
        seedCode,
        startingTownId: townId,
      };
    },
    [seed.playerName, seed.seedState, seed.travelDifficulty, seed.entropy],
  );

  return {
    step,
    playerName: seed.playerName,
    selectedTownId,
    travelDifficulty: seed.travelDifficulty,
    entropy: seed.entropy,
    seedState: seed.seedState,
    seedDraft: seed.seedDraft,
    seedDirty: seed.seedDirty,
    decodeError: seed.decodeError,
    setPlayerName: seed.setPlayerName,
    setSelectedTownId,
    setTravelDifficulty: seed.setTravelDifficulty,
    setEntropy: seed.setEntropy,
    setSeedDraft: seed.setSeedDraft,
    applySeed: seed.applySeed,
    randomizeSeed: seed.randomizeSeed,
    goToStep,
    advance,
    goBack,
    buildStartGameRequest,
  };
}

export type StartGameHandler = (request: StartGameRequest) => Promise<void>;
