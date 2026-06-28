import { afterEach, describe, expect, it, vi } from "vitest";
import { useState } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { GameEntropy, GameDifficulty } from "../api/types";
import { SetupHuntStep } from "../components/start-flow/SetupHuntStep";

afterEach(() => {
  cleanup();
});

interface StepHandlers {
  onPlayerNameChange: (value: string) => void;
  onGameDifficultyChange: (difficulty: GameDifficulty) => void;
  onEntropyChange: (entropy: GameEntropy) => void;
  onSeedDraftChange: (value: string) => void;
  onApplySeed: () => Promise<void>;
  onRandomizeSeed: () => void;
  onContinue: () => void;
}

function StatefulSetupHuntStep({
  initialName = "",
  initialDifficulty = 0 as GameDifficulty,
  initialEntropy = 1 as GameEntropy,
  initialSeedDraft = "00000000-0000-0000-0000-000000000000",
  onPlayerNameChange,
  onGameDifficultyChange,
  onEntropyChange,
  onSeedDraftChange,
  onApplySeed,
  onRandomizeSeed,
  onContinue,
}: {
  initialName?: string;
  initialDifficulty?: GameDifficulty;
  initialEntropy?: GameEntropy;
  initialSeedDraft?: string;
  onPlayerNameChange: (value: string) => void;
  onGameDifficultyChange: (difficulty: GameDifficulty) => void;
  onEntropyChange: (entropy: GameEntropy) => void;
  onSeedDraftChange: (value: string) => void;
  onApplySeed: () => Promise<void>;
  onRandomizeSeed: () => void;
  onContinue: () => void;
}) {
  const [playerName, setPlayerName] = useState(initialName);
  const [difficulty, setDifficulty] = useState<GameDifficulty>(initialDifficulty);
  const [entropy, setEntropy] = useState<GameEntropy>(initialEntropy);
  const [seedDraft, setSeedDraft] = useState(initialSeedDraft);
  const [seedDirty, setSeedDirty] = useState(false);
  return (
    <SetupHuntStep
      playerName={playerName}
      gameDifficulty={difficulty}
      entropy={entropy}
      seedDraft={seedDraft}
      seedDirty={seedDirty}
      decodeError={null}
      onPlayerNameChange={(value) => {
        setPlayerName(value);
        onPlayerNameChange(value);
      }}
      onGameDifficultyChange={(value) => {
        setDifficulty(value);
        onGameDifficultyChange(value);
      }}
      onEntropyChange={(value) => {
        setEntropy(value);
        onEntropyChange(value);
      }}
      onSeedDraftChange={(value) => {
        setSeedDraft(value);
        setSeedDirty(true);
        onSeedDraftChange(value);
      }}
      onApplySeed={onApplySeed}
      onRandomizeSeed={onRandomizeSeed}
      onContinue={onContinue}
    />
  );
}

function renderStep(overrides: Partial<{
  playerName: string;
  gameDifficulty: GameDifficulty;
  entropy: GameEntropy;
  seedDraft: string;
  seedDirty: boolean;
  decodeError: string | null;
  stateful: boolean;
  onPlayerNameChange: (value: string) => void;
  onGameDifficultyChange: (difficulty: GameDifficulty) => void;
  onEntropyChange: (entropy: GameEntropy) => void;
  onSeedDraftChange: (value: string) => void;
  onApplySeed: () => Promise<void>;
  onRandomizeSeed: () => void;
  onContinue: () => void;
}> = {}) {
  const handlers: StepHandlers = {
    onPlayerNameChange: overrides.onPlayerNameChange ?? vi.fn(),
    onGameDifficultyChange: overrides.onGameDifficultyChange ?? vi.fn(),
    onEntropyChange: overrides.onEntropyChange ?? vi.fn(),
    onSeedDraftChange: overrides.onSeedDraftChange ?? vi.fn(),
    onApplySeed: overrides.onApplySeed ?? vi.fn().mockResolvedValue(undefined),
    onRandomizeSeed: overrides.onRandomizeSeed ?? vi.fn(),
    onContinue: overrides.onContinue ?? vi.fn(),
  };

  if (overrides.stateful) {
    render(
      <StatefulSetupHuntStep
        initialName={overrides.playerName ?? ""}
        initialDifficulty={overrides.gameDifficulty ?? 0}
        initialEntropy={overrides.entropy ?? 1}
        initialSeedDraft={overrides.seedDraft ?? "00000000-0000-0000-0000-000000000000"}
        onPlayerNameChange={handlers.onPlayerNameChange}
        onGameDifficultyChange={handlers.onGameDifficultyChange}
        onEntropyChange={handlers.onEntropyChange}
        onSeedDraftChange={handlers.onSeedDraftChange}
        onApplySeed={handlers.onApplySeed}
        onRandomizeSeed={handlers.onRandomizeSeed}
        onContinue={handlers.onContinue}
      />,
    );
  } else {
    render(
      <SetupHuntStep
        playerName={overrides.playerName ?? ""}
        gameDifficulty={overrides.gameDifficulty ?? 0}
        entropy={overrides.entropy ?? 1}
        seedDraft={overrides.seedDraft ?? "00000000-0000-0000-0000-000000000000"}
        seedDirty={overrides.seedDirty ?? false}
        decodeError={overrides.decodeError ?? null}
        onPlayerNameChange={handlers.onPlayerNameChange}
        onGameDifficultyChange={handlers.onGameDifficultyChange}
        onEntropyChange={handlers.onEntropyChange}
        onSeedDraftChange={handlers.onSeedDraftChange}
        onApplySeed={handlers.onApplySeed}
        onRandomizeSeed={handlers.onRandomizeSeed}
        onContinue={handlers.onContinue}
      />,
    );
  }

  return handlers;
}

describe("SetupHuntStep", () => {
  it("renders the setup heading and lead copy", () => {
    renderStep();

    expect(
      screen.getByRole("heading", { name: /set up your hunt/i }),
    ).toBeInTheDocument();
  });

  it("shows the validation message when the name is empty after being touched", async () => {
    const user = userEvent.setup();
    renderStep({ stateful: true });

    expect(screen.queryByText(/tell me what name you go by before we ride on\./i)).not.toBeInTheDocument();

    const nameInput = screen.getByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.clear(nameInput);

    expect(screen.getByText(/tell me what name you go by before we ride on\./i)).toBeInTheDocument();
  });

  it("does not show the validation message before the field has been touched", () => {
    renderStep();

    expect(screen.queryByText(/tell me what name you go by before we ride on\./i)).not.toBeInTheDocument();
  });

  it("disables the Ride on button until a valid name is entered", async () => {
    const user = userEvent.setup();
    renderStep({ stateful: true });

    const rideButton = screen.getByRole("button", { name: /ride on/i });
    expect(rideButton).toBeDisabled();

    const nameInput = screen.getByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");

    expect(rideButton).not.toBeDisabled();
  });

  it("calls onContinue when Ride on is clicked with a valid name", async () => {
    const user = userEvent.setup();
    const onContinue = vi.fn();
    renderStep({ stateful: true, onContinue });

    const nameInput = screen.getByLabelText(/your name/i);
    await user.type(nameInput, "Ranger Vale");

    await user.click(screen.getByRole("button", { name: /ride on/i }));

    expect(onContinue).toHaveBeenCalledTimes(1);
  });

  it("does not call onContinue when submitting with an empty name", () => {
    const onContinue = vi.fn();
    renderStep({ onContinue });

    const form = screen.getByLabelText(/your name/i).closest("form")!;
    fireEvent.submit(form);

    expect(onContinue).not.toHaveBeenCalled();
    expect(screen.getByText(/tell me what name you go by before we ride on\./i)).toBeInTheDocument();
  });

  it("calls onGameDifficultyChange when a difficulty option is selected", async () => {
    const user = userEvent.setup();
    const onGameDifficultyChange = vi.fn();
    renderStep({ stateful: true, onGameDifficultyChange });

    const challenging = screen.getByRole("button", { name: /^challenging$/i });
    await user.click(challenging);

    expect(onGameDifficultyChange).toHaveBeenCalledWith(2);
  });

  it("calls onEntropyChange when an entropy option is selected", async () => {
    const user = userEvent.setup();
    const onEntropyChange = vi.fn();
    renderStep({ stateful: true, onEntropyChange });

    const wild = screen.getByRole("button", { name: /^wild$/i });
    await user.click(wild);

    expect(onEntropyChange).toHaveBeenCalledWith(3);
  });

  it("renders difficulty options as Standard, Challenging, Brutal in that order", () => {
    renderStep();

    const groups = screen.getAllByRole("group");
    const difficultyGroup = groups[0];
    const buttons = Array.from(difficultyGroup.querySelectorAll("button"));
    const labels = buttons.map((b) => b.textContent?.trim() ?? "");

    expect(labels).toEqual(["Standard", "Challenging", "Brutal"]);
    expect(labels).not.toContain("Easy");
    expect(labels).not.toContain("Normal");
    expect(labels).not.toContain("Hard");
  });

  it("renders entropy options as Classic, Adventurous, Wild in that order", () => {
    renderStep();

    const groups = screen.getAllByRole("group");
    const entropyGroup = groups[1];
    const buttons = Array.from(entropyGroup.querySelectorAll("button"));
    const labels = buttons.map((b) => b.textContent?.trim() ?? "");

    expect(labels).toEqual(["Classic", "Adventurous", "Wild"]);
    expect(labels).not.toContain("Placid");
    expect(labels).not.toContain("Restless");
    expect(labels).not.toContain("Rowdy");
  });

  it("does not render Boring as a player-facing entropy option", () => {
    renderStep();

    expect(screen.queryByRole("button", { name: /^boring$/i })).not.toBeInTheDocument();
  });

  it("uses a single thumb element per segmented toggle (not per-option backgrounds)", () => {
    renderStep();

    const groups = document.querySelectorAll('[role="group"]');
    expect(groups).toHaveLength(2);

    for (const group of Array.from(groups)) {
      // One thumb div (aria-hidden) that slides, separate from the label buttons.
      const thumbs = group.querySelectorAll('div[aria-hidden="true"]');
      expect(thumbs).toHaveLength(1);

      // Label buttons must not have their own background styling for selection.
      // The thumb provides the selected visual; buttons are transparent.
      const buttons = Array.from(group.querySelectorAll("button"));
      for (const btn of buttons) {
        const bg = window.getComputedStyle(btn).backgroundColor;
        expect(bg).toBe("rgba(0, 0, 0, 0)"); // transparent
      }
    }
  });

  it("renders each segmented toggle as a single non-wrapping flex row", () => {
    renderStep();

    const groups = document.querySelectorAll('[role="group"]');
    for (const group of Array.from(groups)) {
      const style = window.getComputedStyle(group);
      expect(style.flexWrap).toBe("nowrap");
    }
  });

  it("calls onRandomizeSeed when Randomize is clicked", async () => {
    const user = userEvent.setup();
    const onRandomizeSeed = vi.fn();
    renderStep({ onRandomizeSeed });

    await user.click(screen.getByRole("button", { name: /randomize/i }));

    expect(onRandomizeSeed).toHaveBeenCalledTimes(1);
  });

  it("disables Apply until the seed draft is dirty", () => {
    renderStep({ seedDirty: false });

    expect(screen.getByRole("button", { name: /apply/i })).toBeDisabled();
  });

  it("enables Apply when the seed draft is dirty", () => {
    renderStep({ seedDirty: true });

    expect(screen.getByRole("button", { name: /apply/i })).not.toBeDisabled();
  });

  it("calls onApplySeed when Apply is clicked with a dirty seed", async () => {
    const user = userEvent.setup();
    const onApplySeed = vi.fn().mockResolvedValue(undefined);
    renderStep({ seedDirty: true, onApplySeed });

    await user.click(screen.getByRole("button", { name: /apply/i }));

    expect(onApplySeed).toHaveBeenCalledTimes(1);
  });

  it("shows the decode error when provided", () => {
    renderStep({ decodeError: "Seed code must be a UUID-shaped string." });

    expect(screen.getByText(/seed code must be a uuid-shaped string\./i)).toBeInTheDocument();
  });

  it("does not render a Back button", () => {
    renderStep();

    expect(screen.queryByRole("button", { name: /back/i })).not.toBeInTheDocument();
  });

  it("does not render step-progress chrome such as Step 1 of N", () => {
    renderStep();

    expect(document.body.textContent ?? "").not.toMatch(/step \d+ of \d+/i);
  });
});
