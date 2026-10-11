import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { JournalSurface } from "../components/JournalSurface";
import { createJournal } from "./test-utils/factories";

describe("JournalSurface", () => {
  it("renders a grouped player journal without leaking hidden internals", () => {
    const journal = createJournal({
      clock: { day: 5, turn: 2, timeOfDay: "Morning" },
      currentTown: { id: "t-town", name: "Tumbleweed" },
      logEntries: [
        { kind: 0, message: "The hunt begins in Tumbleweed.", day: 1, turn: 0 },
        { kind: 1, message: "Travelled to Red Mesa", day: 5, turn: 1 },
        { kind: 2, message: "Found a public lead", day: 5, turn: 2 },
        { kind: 2, message: "Bought food", day: 6, turn: 0 },
      ],
    });

    render(<JournalSurface journal={journal} loading={false} error="" />);

    expect(
      screen.queryByText("Find the culprit before the law closes in."),
    ).not.toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "Day 5, Morning in Tumbleweed" }),
    ).toBeInTheDocument();
    expect(screen.getByText("Day 1")).toBeInTheDocument();
    expect(screen.getByText("Day 5")).toBeInTheDocument();
    expect(screen.getByText("Day 6")).toBeInTheDocument();
    expect(screen.getByText("Started out in Tumbleweed.")).toBeInTheDocument();
    expect(screen.queryByText("The hunt begins in Tumbleweed.")).not.toBeInTheDocument();
    expect(screen.getByText("Travelled to Red Mesa")).toBeInTheDocument();
    expect(screen.getByText("Found a public lead", { selector: "p" })).toBeInTheDocument();
    expect(screen.getByText("Bought food")).toBeInTheDocument();
    expect(screen.queryByText("trueCulpritId")).not.toBeInTheDocument();
    expect(screen.queryByText("isTrueCulprit")).not.toBeInTheDocument();
    expect(screen.queryByText("linkedSuspectIds")).not.toBeInTheDocument();
    expect(screen.queryByText("killerReleaseState")).not.toBeInTheDocument();
    expect(screen.queryByText("t-town")).not.toBeInTheDocument();
  });

  it("keeps the last journal visible when a refresh fails and offers retry", async () => {
    const journal = createJournal({
      currentTown: { id: "t-town", name: "Tumbleweed" },
      logEntries: [{ kind: 2, message: "Found a public lead", day: 5, turn: 2 }],
    });
    const onRetry = vi.fn();

    render(
      <JournalSurface
        journal={journal}
        loading={false}
        error=""
        readError="Could not read the journal."
        onRetry={onRetry}
      />,
    );

    expect(screen.getAllByText("Found a public lead").length).toBeGreaterThan(0);
    expect(screen.getByRole("alert")).toHaveTextContent("Could not read the journal.");
    await userEvent.setup().click(screen.getByRole("button", { name: /try again/i }));
    expect(onRetry).toHaveBeenCalledOnce();
  });
});
