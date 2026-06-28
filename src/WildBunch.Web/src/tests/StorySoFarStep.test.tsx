import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { StorySoFarStep } from "../components/start-flow/StorySoFarStep";
import { getPrologue } from "../api/wildBunchApi";
import type { GameEntropy, PrologueDto, GameDifficulty } from "../api/types";

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
    primaryAction: "Ride on",
    variantId: "variant-1",
    ...overrides,
  };
}

function renderStep(overrides: { seedCode?: string | null; gameDifficulty?: GameDifficulty; gameEntropy?: GameEntropy; onContinue?: () => void } = {}) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  const onContinue = overrides.onContinue ?? vi.fn();

  render(
    <QueryClientProvider client={queryClient}>
      <StorySoFarStep
        onContinue={onContinue}
        seedCode={overrides.seedCode ?? "SEED-CODE-1"}
        gameDifficulty={overrides.gameDifficulty}
        gameEntropy={overrides.gameEntropy}
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
        primaryAction: "Ride on",
      }),
    );

    renderStep();

    expect(await screen.findByRole("heading", { name: /the story so far/i })).toBeInTheDocument();
    expect(
      await screen.findByText(/the outlaw known as black bart robbed the dust fork bank/i),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /ride on/i }),
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

  it("shows an in-world loading state while the prologue is fetching", () => {
    mockedGetPrologue.mockReturnValue(new Promise(() => {}));

    renderStep();

    expect(screen.getByText(/the trail ahead is still coming into focus/i)).toBeInTheDocument();
  });

  it("disables the primary action while the prologue is loading", () => {
    mockedGetPrologue.mockReturnValue(new Promise(() => {}));

    renderStep();

    const primaryButton = screen.getByRole("button", { name: /ride on/i });
    expect(primaryButton).toBeDisabled();
  });

  it("enables the primary action once the prologue has loaded", async () => {
    mockedGetPrologue.mockResolvedValue(createPrologue());

    renderStep();

    await screen.findByText(/black bart/i);

    const primaryButton = screen.getByRole("button", { name: /ride on/i });
    expect(primaryButton).toBeEnabled();
  });

  it("advances when the primary action is clicked after the prologue loads", async () => {
    mockedGetPrologue.mockResolvedValue(createPrologue());

    const onContinue = vi.fn();
    const user = userEvent.setup();

    renderStep({ onContinue });

    await screen.findByText(/black bart/i);

    const primaryButton = screen.getByRole("button", { name: /ride on/i });
    await user.click(primaryButton);

    await waitFor(() => {
      expect(onContinue).toHaveBeenCalledTimes(1);
    });
  });

  it("does not advance while the prologue is still loading", async () => {
    mockedGetPrologue.mockReturnValue(new Promise(() => {}));

    const onContinue = vi.fn();
    const user = userEvent.setup();

    renderStep({ onContinue });

    const primaryButton = screen.getByRole("button", { name: /ride on/i });
    expect(primaryButton).toBeDisabled();
    await user.click(primaryButton);

    expect(onContinue).not.toHaveBeenCalled();
  });

  it("passes the seedCode, gameDifficulty, and gameEntropy to getPrologue", async () => {
    mockedGetPrologue.mockResolvedValue(createPrologue());

    renderStep({ seedCode: "MY-SEED-42", gameDifficulty: 2 as GameDifficulty, gameEntropy: 3 as GameEntropy });

    await screen.findByText(/black bart/i);

    expect(mockedGetPrologue).toHaveBeenCalledWith("MY-SEED-42", 2, 3);
  });

  it("shows an in-world error state with a retry button when the prologue fetch fails", async () => {
    mockedGetPrologue.mockRejectedValue(new Error("Network down"));

    renderStep();

    await waitFor(() => {
      expect(screen.getByText(/the trail fades into dust/i)).toBeInTheDocument();
    });

    expect(screen.getByRole("button", { name: /try again/i })).toBeInTheDocument();

    const primaryButton = screen.getByRole("button", { name: /ride on/i });
    expect(primaryButton).toBeDisabled();
  });

  it("retries the prologue fetch when the retry button is clicked", async () => {
    mockedGetPrologue.mockRejectedValueOnce(new Error("Network down"));
    mockedGetPrologue.mockResolvedValueOnce(createPrologue());

    const user = userEvent.setup();

    renderStep();

    const retryButton = await screen.findByRole("button", { name: /try again/i });
    await user.click(retryButton);

    await screen.findByText(/black bart/i);
    expect(
      screen.getByRole("button", { name: /ride on/i }),
    ).toBeEnabled();
  });

  it("does not render a checkbox acknowledgement gate", async () => {
    mockedGetPrologue.mockResolvedValue(createPrologue());

    renderStep();

    await screen.findByText(/black bart/i);
    expect(screen.queryByRole("checkbox")).not.toBeInTheDocument();
  });

  it("does not render a Back button", async () => {
    mockedGetPrologue.mockResolvedValue(createPrologue());

    renderStep();

    await screen.findByText(/black bart/i);
    expect(screen.queryByRole("button", { name: /back/i })).not.toBeInTheDocument();
  });
});
