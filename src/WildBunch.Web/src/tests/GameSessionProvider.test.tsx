import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { useGameSession } from "../state/useGameSession";
import type { WantedPosterDto } from "../api/types";

const currentSession: { value: Record<string, unknown> } = vi.hoisted(() => ({ value: {} }));

vi.mock("../hooks/useCurrentGameSession", () => ({
  useCurrentGameSession: () => currentSession.value,
}));

afterEach(() => {
  cleanup();
  currentSession.value = {};
});

const poster: WantedPosterDto = {
  posterId: "poster-mira",
  targetDisplayName: "Mira Cline",
  aliases: [],
  legalTerms: { disposition: 0, bountyAmount: 100, issuingAuthority: "Sheriff" },
  quickView: {
    headlineNameOrAlias: "Mira Cline",
    headlineFeatureOrDescriptor: "Known outlaw",
    pocketCheckDescriptor: "",
  },
  details: { summary: "Wanted for questioning.", publicOrigin: "Tumbleweed", features: [] },
  publicSafeClassification: null,
};

function ContextReader() {
  const { selectedWantedPoster } = useGameSession();
  return <output>{selectedWantedPoster?.targetDisplayName ?? "No identity selected"}</output>;
}

function renderProvider(identityHandle = "") {
  currentSession.value = {
    wantedPosters: [poster],
    declaredWantedIdentityHandle: identityHandle,
  };

  function Wrapper({ children }: { children: ReactNode }) {
    return <GameSessionProvider>{children}</GameSessionProvider>;
  }

  render(
    <Wrapper>
      <ContextReader />
    </Wrapper>,
  );
}

describe("GameSessionProvider wanted identity selection", () => {
  it("does not imply the first poster is selected when the player has declared nobody", () => {
    renderProvider();
    expect(screen.getByText("No identity selected")).toBeInTheDocument();
  });

  it("resolves the poster only after the player's declaration selects it", () => {
    renderProvider("poster-mira");
    expect(screen.getByText("Mira Cline")).toBeInTheDocument();
  });
});
