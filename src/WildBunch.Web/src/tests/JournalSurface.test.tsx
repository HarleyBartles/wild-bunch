import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { JournalSurface } from "../components/JournalSurface";
import { createJournal } from "./test-utils/factories";

describe("JournalSurface", () => {
  it("renders a grouped player journal without leaking hidden internals", () => {
    const journal = createJournal({
      clock: { day: 5, turn: 2, timeOfDay: "Morning" },
      currentTown: { id: "t-town", name: "Tumbleweed" },
      logEntries: [
        { kind: 0, message: "Booted", day: 1, turn: 0 },
        { kind: 1, message: "Travelled to Red Mesa", day: 5, turn: 1 },
        { kind: 2, message: "Found a public lead", day: 5, turn: 2 },
        { kind: 2, message: "Bought food", day: 6, turn: 0 },
      ],
    });

    render(<JournalSurface journal={journal} loading={false} error="" />);

    expect(screen.getByRole("heading", { name: /journal/i })).toBeInTheDocument();
    expect(screen.getByText("Tumbleweed")).toBeInTheDocument();
    expect(screen.getByText("Find the culprit before the law closes in.")).toBeInTheDocument();
    expect(screen.getByText("Day 5, Morning in Tumbleweed")).toBeInTheDocument();
    expect(screen.getByText("Day 5, Morning")).toBeInTheDocument();
    expect(screen.getByText("Day 1")).toBeInTheDocument();
    expect(screen.getByText("Day 5")).toBeInTheDocument();
    expect(screen.getByText("Day 6")).toBeInTheDocument();
    expect(screen.getByText("Booted")).toBeInTheDocument();
    expect(screen.getByText("Travelled to Red Mesa")).toBeInTheDocument();
    expect(screen.getByText("Found a public lead")).toBeInTheDocument();
    expect(screen.getByText("Bought food")).toBeInTheDocument();
    expect(screen.queryByText("trueCulpritId")).not.toBeInTheDocument();
    expect(screen.queryByText("isTrueCulprit")).not.toBeInTheDocument();
    expect(screen.queryByText("linkedSuspectIds")).not.toBeInTheDocument();
    expect(screen.queryByText("killerReleaseState")).not.toBeInTheDocument();
    expect(screen.queryByText("t-town")).not.toBeInTheDocument();
  });
});
