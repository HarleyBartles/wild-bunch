import { afterEach, describe, expect, it, vi } from "vitest";
import { useState } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { AdventureRandomnessPolicy, TravelDifficulty } from "../api/types";
import { SetupHuntStep } from "../components/start-flow/SetupHuntStep";

afterEach(() => {
  cleanup();
});

interface StepHandlers {
  onPlayerNameChange: (value: string) => void;
  onTravelDifficultyChange: (difficulty: TravelDifficulty) => void;
  onEntropyChange: (entropy: AdventureRandomnessPolicy) => void;
  onSeedDraftChange: (value: string) => void;
  onApplySeed: () => Promise<void>;
  onRandomizeSeed: () => void;
  onContinue: () => void;
}

function StatefulSetupHuntStep({
  initialName = "",
  initialDifficulty = 0 as TravelDifficulty,
  initialEntropy = 1 as AdventureRandomnessPolicy,
  initialSeedDraft = "00000000-0000-0000-0000-000000000000",
  onPlayerNameChange,
  onTravelDifficultyChange,
  onEntropyChange,
  onSeedDraftChange,
  onApplySeed,
  onRandomizeSeed,
  onContinue,
}: {
  initialName?: string;
  initialDifficulty?: TravelDifficulty;
  initialEntropy?: AdventureRandomnessPolicy;
  initialSeedDraft?: string;
  onPlayerNameChange: (value: string) => void;
  onTravelDifficultyChange: (difficulty: TravelDifficulty) => void;
  onEntropyChange: (entropy: AdventureRandomnessPolicy) => void;
  onSeedDraftChange: (value: string) => void;
  onApplySeed: () => Promise<void>;
  onRandomizeSeed: () => void;
  onContinue: () => void;
}) {
  const [playerName, setPlayerName] = useState(initialName);
  const [difficulty, setDifficulty] = useState<TravelDifficulty>(initialDifficulty);
  const [entropy, setEntropy] = useState<AdventureRandomnessPolicy>(initialEntropy);
  const [seedDraft, setSeedDraft] = useState(initialSeedDraft);
  const [seedDirty, setSeedDirty] = useState(false);
  return (
    <SetupHuntStep
      playerName={playerName}
      travelDifficulty={difficulty}
      entropy={entropy}
      seedDraft={seedDraft}
      seedDirty={seedDirty}
      decodeError={null}
      onPlayerNameChange={(value) => {
        setPlayerName(value);
        onPlayerNameChange(value);
      }}
      onTravelDifficultyChange={(value) => {
        setDifficulty(value);
        onTravelDifficultyChange(value);
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
  travelDifficulty: TravelDifficulty;
  entropy: AdventureRandomnessPolicy;
  seedDraft: string;
  seedDirty: boolean;
  decodeError: string | null;
  stateful: boolean;
  onPlayerNameChange: (value: string) => void;
  onTravelDifficultyChange: (difficulty: TravelDifficulty) => void;
  onEntropyChange: (entropy: AdventureRandomnessPolicy) => void;
  onSeedDraftChange: (value: string) => void;
  onApplySeed: () => Promise<void>;
  onRandomizeSeed: () => void;
  onContinue: () => void;
}> = {}) {
  const handlers: StepHandlers = {
    onPlayerNameChange: overrides.onPlayerNameChange ?? vi.fn(),
    onTravelDifficultyChange: overrides.onTravelDifficultyChange ?? vi.fn(),
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
        initialDifficulty={overrides.travelDifficulty ?? 0}
        initialEntropy={overrides.entropy ?? 1}
        initialSeedDraft={overrides.seedDraft ?? "00000000-0000-0000-0000-000000000000"}
        onPlayerNameChange={handlers.onPlayerNameChange}
        onTravelDifficultyChange={handlers.onTravelDifficultyChange}
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
        travelDifficulty={overrides.travelDifficulty ?? 0}
        entropy={overrides.entropy ?? 1}
        seedDraft={overrides.seedDraft ?? "00000000-0000-0000-0000-000000000000"}
        seedDirty={overrides.seedDirty ?? false}
        decodeError={overrides.decodeError ?? null}
        onPlayerNameChange={handlers.onPlayerNameChange}
        onTravelDifficultyChange={handlers.onTravelDifficultyChange}
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

  it("calls onTravelDifficultyChange when a difficulty option is selected", async () => {
    const user = userEvent.setup();
    const onTravelDifficultyChange = vi.fn();
    renderStep({ stateful: true, onTravelDifficultyChange });

    const hard = screen.getByRole("button", { name: /^hard$/i });
    await user.click(hard);

    expect(onTravelDifficultyChange).toHaveBeenCalledWith(2);
  });

  it("calls onEntropyChange when an entropy option is selected", async () => {
    const user = userEvent.setup();
    const onEntropyChange = vi.fn();
    renderStep({ stateful: true, onEntropyChange });

    const wild = screen.getByRole("button", { name: /^wild$/i });
    await user.click(wild);

    expect(onEntropyChange).toHaveBeenCalledWith(3);
  });

  it("renders difficulty options as Easy, Normal, Hard in that order", () => {
    renderStep();

    const difficultyLabel = screen.getByText("Difficulty");
    const toggle = difficultyLabel.parentElement!.querySelector("div")!;
    const buttons = Array.from(toggle.querySelectorAll("button"));
    const labels = buttons.map((b) => b.textContent?.trim() ?? "");

    expect(labels).toEqual(["Easy", "Normal", "Hard"]);
    // Guard against invented Western-flavoured labels.
    expect(labels).not.toContain("Greenhorn");
    expect(labels).not.toContain("Trail hand");
    expect(labels).not.toContain("Iron rider");
  });

  it("renders entropy options as Classic, Adventurous, Wild in that order", () => {
    renderStep();

    const entropyLabel = screen.getByText("Entropy");
    const toggle = entropyLabel.parentElement!.querySelector("div")!;
    const buttons = Array.from(toggle.querySelectorAll("button"));
    const labels = buttons.map((b) => b.textContent?.trim() ?? "");

    expect(labels).toEqual(["Classic", "Adventurous", "Wild"]);
    // Guard against invented Western-flavoured labels.
    expect(labels).not.toContain("Placid");
    expect(labels).not.toContain("Restless");
    expect(labels).not.toContain("Rowdy");
  });

  it("does not render Boring as a player-facing entropy option", () => {
    renderStep();

    expect(screen.queryByRole("button", { name: /^boring$/i })).not.toBeInTheDocument();
  });

  it("renders each segmented toggle as a single non-wrapping flex row", () => {
    renderStep();

    const toggles = document.querySelectorAll("div[style*='border-radius: 999px'], div");
    const difficultyLabel = screen.getByText("Difficulty");
    const entropyLabel = screen.getByText("Entropy");

    for (const label of [difficultyLabel, entropyLabel]) {
      const toggle = label.parentElement!.querySelector("div")!;
      const style = window.getComputedStyle(toggle);
      // The segmented toggle must not wrap — it stays one horizontal row.
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
