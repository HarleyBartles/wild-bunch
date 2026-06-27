import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { StorySoFarStep } from "../components/start-flow/StorySoFarStep";
import { getPrologue } from "../api/wildBunchApi";
import type { PrologueDto } from "../api/types";

vi.mock("../api/wildBunchApi", () => ({
  getPrologue: vi.fn(),
}));

const mockedGetPrologue = vi.mocked(getPrologue);

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

function createPrologue(overrides: Partial<PrologueDto> = {}): PrologueDto {
  return {
    heading: "The story so far",
    body: "A culprit known as Black Bart is on the run. The trail is fresh, but it won't stay that way for long.",
    primaryAction: "I understand. Keep riding.",
    variantId: "variant-1",
    ...overrides,
  };
}

interface StepHarnessProps {
  seedCode?: string | null;
  initialAcknowledged?: boolean;
  onContinue?: () => void;
  onStoryAcknowledgedChange?: (value: boolean) => void;
}

function StepHarness({
  seedCode = "SEED-CODE-1",
  initialAcknowledged = false,
  onContinue,
  onStoryAcknowledgedChange,
}: StepHarnessProps) {
  const [acknowledged, setAcknowledged] = useState(initialAcknowledged);
  const [continueHandler] = useState(() => onContinue ?? vi.fn());
  const [ackChangeHandler] = useState(
    () =>
      onStoryAcknowledgedChange ??
      ((value: boolean) => {
        setAcknowledged(value);
      }),
  );

  return (
    <StorySoFarStep
      storyAcknowledged={acknowledged}
      onStoryAcknowledgedChange={ackChangeHandler}
      onContinue={continueHandler}
      onBack={vi.fn()}
      seedCode={seedCode}
    />
  );
}

function renderStep(overrides: {
  seedCode?: string | null;
  initialAcknowledged?: boolean;
  onContinue?: () => void;
  onStoryAcknowledgedChange?: (value: boolean) => void;
} = {}) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  const onContinue = overrides.onContinue ?? vi.fn();

  render(
    <QueryClientProvider client={queryClient}>
      <StepHarness
        seedCode={overrides.seedCode ?? "SEED-CODE-1"}
        initialAcknowledged={overrides.initialAcknowledged ?? false}
        onContinue={onContinue}
        onStoryAcknowledgedChange={overrides.onStoryAcknowledgedChange}
      />
    </QueryClientProvider>,
  );

  return { onContinue, queryClient };
}

describe("StorySoFarStep", () => {
  it("renders copy fetched from the prologue API", async () => {
    mockedGetPrologue.mockResolvedValue(
      createPrologue({
        heading: "The story so far",
        body: "The outlaw known as Black Bart robbed the Dust Fork bank and fled east.",
        primaryAction: "I understand. Keep riding.",
      }),
    );

    renderStep();

    expect(await screen.findByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    expect(
      await screen.findByText(/the outlaw known as black bart robbed the dust fork bank/i),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /i understand\. keep riding\./i }),
    ).toBeInTheDocument();
  });

  it("does not render a literal {trueCulpritMainIdentifier} placeholder in the body", async () => {
    mockedGetPrologue.mockResolvedValue(
      createPrologue({
        body: "The outlaw known as Black Bart robbed the Dust Fork bank and fled east.",
      }),
    );

    renderStep();

    await screen.findByText(/the outlaw known as black bart/i);
    expect(screen.queryByText(/{trueCulpritMainIdentifier}/i)).not.toBeInTheDocument();
    expect(document.body.textContent ?? "").not.toContain("{trueCulpritMainIdentifier}");
  });

  it("does not expose hidden internal fields (TrueCulpritId, isTrueCulprit, suspect- ids)", async () => {
    mockedGetPrologue.mockResolvedValue(
      createPrologue({
        body: "The outlaw known as Black Bart robbed the Dust Fork bank and fled east.",
      }),
    );

    renderStep();

    await screen.findByText(/the outlaw known as black bart/i);

    const text = document.body.textContent ?? "";
    expect(text).not.toContain("TrueCulpritId");
    expect(text).not.toContain("isTrueCulprit");
    expect(text).not.toMatch(/suspect-/i);
    expect(text).not.toContain("variantId");
    expect(text).not.toContain("variant-1");
  });

  it("shows a loading state while the prologue is fetching", () => {
    mockedGetPrologue.mockReturnValue(new Promise(() => {}));

    renderStep();

    expect(screen.getByText(/loading the story so far/i)).toBeInTheDocument();
  });

  it("disables the primary action until the story is acknowledged", async () => {
    mockedGetPrologue.mockResolvedValue(createPrologue());

    renderStep({ initialAcknowledged: false });

    await screen.findByText(/black bart/i);

    const primaryButton = screen.getByRole("button", { name: /i understand\. keep riding\./i });
    expect(primaryButton).toBeDisabled();
  });

  it("advances when acknowledged and the primary action is clicked", async () => {
    mockedGetPrologue.mockResolvedValue(createPrologue());

    const onContinue = vi.fn();
    const user = userEvent.setup();

    renderStep({ initialAcknowledged: false, onContinue });

    await screen.findByText(/black bart/i);

    const checkbox = screen.getByRole("checkbox", { name: /i've read the story so far/i });
    await user.click(checkbox);

    const primaryButton = screen.getByRole("button", { name: /i understand\. keep riding\./i });
    await user.click(primaryButton);

    await waitFor(() => {
      expect(onContinue).toHaveBeenCalledTimes(1);
    });
  });

  it("does not advance when the primary action is clicked without acknowledgement", async () => {
    mockedGetPrologue.mockResolvedValue(createPrologue());

    const onContinue = vi.fn();
    const user = userEvent.setup();

    renderStep({ initialAcknowledged: false, onContinue });

    await screen.findByText(/black bart/i);

    // The button is disabled, so clicking it should not fire onContinue.
    const primaryButton = screen.getByRole("button", { name: /i understand\. keep riding\./i });
    expect(primaryButton).toBeDisabled();
    await user.click(primaryButton);

    expect(onContinue).not.toHaveBeenCalled();
  });

  it("passes the seedCode to getPrologue", async () => {
    mockedGetPrologue.mockResolvedValue(createPrologue());

    renderStep({ seedCode: "MY-SEED-42" });

    await screen.findByText(/black bart/i);

    expect(mockedGetPrologue).toHaveBeenCalledWith("MY-SEED-42");
  });

  it("shows an error state when the prologue fetch fails", async () => {
    mockedGetPrologue.mockRejectedValue(new Error("Network down"));

    renderStep();

    await waitFor(() => {
      expect(screen.getByText(/couldn't load the prologue/i)).toBeInTheDocument();
    });

    const primaryButton = screen.getByRole("button", { name: /i understand\. keep riding\./i });
    expect(primaryButton).toBeDisabled();
  });

  it("renders the Back button", async () => {
    mockedGetPrologue.mockResolvedValue(createPrologue());

    renderStep();

    await screen.findByText(/black bart/i);
    expect(screen.getByRole("button", { name: /back/i })).toBeInTheDocument();
  });
});
