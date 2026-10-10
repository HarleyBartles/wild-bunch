import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { CaseFileSurface } from "../components/CaseFileSurface";
import { createJournal } from "./test-utils/factories";

describe("CaseFileSurface", () => {
  it("keeps learned clues and same-name warrant records distinct after a turn-in", () => {
    const journal = createJournal({
      caseFile: {
        accusationId: null,
        openingLead: "The Wild Bunch robbed the bank.",
        caseState: { statusText: "Three outlaws remain." },
        caseSummary: "Bring the Wild Bunch to justice.",
        discoveredSuspects: [{ name: "Elzy Lay", status: 1 }],
        caseBoard: {
          warrants: [
            {
              id: "warrant-elzy-bank",
              targetName: "Elzy Lay",
              disposition: 0,
              bountyAmount: 50,
              knownAliases: ["Kid Curry"],
              knownFeatures: ["No left ear"],
              issuingSource: "Dodge City Sheriff",
              summary: "Wanted for the Dodge City bank robbery.",
              settlement: { isAlive: true, bountyAmount: 50, day: 4, turn: 2 },
            },
            {
              id: "warrant-elzy-other",
              targetName: "Elzy Lay",
              disposition: 0,
              bountyAmount: 25,
              knownAliases: [],
              knownFeatures: ["Distinctive red neckerchief"],
              issuingSource: "Bulletville Sheriff",
              summary: "Wanted for a separate train robbery.",
              settlement: null,
            },
          ],
          clues: [
            {
              id: "clue-red-neckerchief",
              kind: 3,
              description:
                "A man with a distinctive red neckerchief was seen leaving Bulletville headed east.",
              sourceKind: 3,
              source: "The Red Dog",
              context: "Saloon gossip, day 3",
              anchors: {
                subjects: [
                  {
                    label: "A man",
                    alias: null,
                    feature: "Distinctive red neckerchief",
                    fact: null,
                  },
                ],
                locations: [{ label: "Bulletville", place: "Bulletville", route: null }],
                times: [],
                directions: [],
              },
            },
          ],
        },
        knownClues: [],
        knownWarrants: [],
        wantedPosters: [],
      },
    });

    render(<CaseFileSurface journal={journal} loading={false} error="" />);

    expect(
      screen.getByText(
        "A man with a distinctive red neckerchief was seen leaving Bulletville headed east.",
      ),
    ).toBeInTheDocument();
    expect(screen.getByText("The Red Dog")).toBeInTheDocument();
    expect(screen.getByText("Saloon gossip, day 3")).toBeInTheDocument();
    expect(screen.getAllByText("Distinctive red neckerchief").length).toBeGreaterThan(0);
    expect(screen.getByText("Wanted for the Dodge City bank robbery.")).toBeInTheDocument();
    expect(screen.getByText("Wanted for a separate train robbery.")).toBeInTheDocument();
    expect(screen.getByText("Turned in alive on day 4; bounty paid $50.00.")).toBeInTheDocument();
    expect(screen.queryAllByText("Resolved to: Elzy Lay")).toHaveLength(0);
    expect(screen.queryByText("warrant-elzy-bank")).not.toBeInTheDocument();
  });
});
