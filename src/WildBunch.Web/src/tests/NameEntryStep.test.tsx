import { afterEach, describe, expect, it, vi } from "vitest";
import { useState } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { NameEntryStep } from "../components/start-flow/NameEntryStep";

afterEach(() => {
  cleanup();
});

interface StepHandlers {
  onPlayerNameChange: ReturnType<typeof vi.fn>;
  onContinue: ReturnType<typeof vi.fn>;
  onBack: ReturnType<typeof vi.fn>;
}

// Stateful wrapper so controlled-input typing updates playerName and re-renders.
function StatefulNameEntryStep({
  initialName = "",
  onPlayerNameChange,
  onContinue,
  onBack,
}: {
  initialName?: string;
  onPlayerNameChange: (value: string) => void;
  onContinue: () => void;
  onBack: () => void;
}) {
  const [playerName, setPlayerName] = useState(initialName);
  return (
    <NameEntryStep
      playerName={playerName}
      onPlayerNameChange={(value) => {
        setPlayerName(value);
        onPlayerNameChange(value);
      }}
      onContinue={onContinue}
      onBack={onBack}
    />
  );
}

function renderStep(overrides: Partial<{
  playerName: string;
  stateful: boolean;
  onPlayerNameChange: (value: string) => void;
  onContinue: () => void;
  onBack: () => void;
}> = {}) {
  const handlers: StepHandlers = {
    onPlayerNameChange: overrides.onPlayerNameChange ?? vi.fn(),
    onContinue: overrides.onContinue ?? vi.fn(),
    onBack: overrides.onBack ?? vi.fn(),
  };

  if (overrides.stateful) {
    render(
      <StatefulNameEntryStep
        initialName={overrides.playerName ?? ""}
        onPlayerNameChange={handlers.onPlayerNameChange}
        onContinue={handlers.onContinue}
        onBack={handlers.onBack}
      />,
    );
  } else {
    render(
      <NameEntryStep
        playerName={overrides.playerName ?? ""}
        onPlayerNameChange={handlers.onPlayerNameChange}
        onContinue={handlers.onContinue}
        onBack={handlers.onBack}
      />,
    );
  }

  return handlers;
}

describe("NameEntryStep", () => {
  it("renders the copy-doc heading and helper text", () => {
    renderStep();

    expect(
      screen.getByRole("heading", { name: /howdy, pard'ner\. what name d'you go by\?/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/a name's a useful thing to have when folks start shouting after you\./i),
    ).toBeInTheDocument();
  });

  it("shows the validation message when the name is empty after being touched", async () => {
    const user = userEvent.setup();
    renderStep({ stateful: true });

    expect(screen.queryByText(/tell me what name you go by before we ride on\./i)).not.toBeInTheDocument();

    const nameInput = screen.getByLabelText(/player name/i);
    await user.type(nameInput, "Ranger Vale");
    await user.clear(nameInput);

    expect(screen.getByText(/tell me what name you go by before we ride on\./i)).toBeInTheDocument();
  });

  it("does not show the validation message before the field has been touched", () => {
    renderStep();

    expect(screen.queryByText(/tell me what name you go by before we ride on\./i)).not.toBeInTheDocument();
  });

  it("enables the Continue button once a valid name is entered", async () => {
    const user = userEvent.setup();
    const { onPlayerNameChange } = renderStep({ stateful: true });

    const continueButton = screen.getByRole("button", { name: /continue/i });
    expect(continueButton).toBeDisabled();

    const nameInput = screen.getByLabelText(/player name/i);
    await user.type(nameInput, "Ranger Vale");

    expect(onPlayerNameChange).toHaveBeenCalled();
    expect(continueButton).not.toBeDisabled();
  });

  it("keeps the Continue button disabled while only whitespace is present", async () => {
    const user = userEvent.setup();
    renderStep({ stateful: true, playerName: "   " });

    const continueButton = screen.getByRole("button", { name: /continue/i });
    expect(continueButton).toBeDisabled();

    const nameInput = screen.getByLabelText(/player name/i);
    await user.type(nameInput, " ");
    expect(continueButton).toBeDisabled();
  });

  it("calls onContinue when Continue is clicked with a valid name", async () => {
    const user = userEvent.setup();
    const onContinue = vi.fn();
    renderStep({ stateful: true, onContinue });

    const nameInput = screen.getByLabelText(/player name/i);
    await user.type(nameInput, "Ranger Vale");

    await user.click(screen.getByRole("button", { name: /continue/i }));

    expect(onContinue).toHaveBeenCalledTimes(1);
  });

  it("does not call onContinue when submitting with an empty name", () => {
    const onContinue = vi.fn();
    renderStep({ onContinue });

    const form = screen.getByLabelText(/player name/i).closest("form")!;
    fireEvent.submit(form);

    expect(onContinue).not.toHaveBeenCalled();
    expect(screen.getByText(/tell me what name you go by before we ride on\./i)).toBeInTheDocument();
  });

  it("wires onBack to the Back button onClick handler", () => {
    const onBack = vi.fn();
    renderStep({ onBack });

    const backButton = screen.getByRole("button", { name: /back/i }) as HTMLButtonElement;
    // The Back button is disabled on the first step by design, so a real click
    // is blocked by the DOM. We verify onBack is wired to the button's onClick
    // by reading the React props and invoking the handler directly.
    const reactPropsKey = Object.keys(backButton).find((k) => k.startsWith("__reactProps$"));
    const onClick = (backButton as unknown as Record<string, { onClick?: () => void }>)[
      reactPropsKey ?? ""
    ]?.onClick;
    expect(typeof onClick).toBe("function");
    onClick?.();

    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it("disables the Back button on the first step", () => {
    renderStep();

    expect(screen.getByRole("button", { name: /back/i })).toBeDisabled();
  });
});
