import { describe, it, expect } from "vitest";
import {
  formatClockBeat,
  formatClueWhen,
  formatRemainingRideDays,
  formatInvestigationNotice,
  formatBeatSlotLabel,
  formatBeatSlots,
} from "../ui/beatFormatters";
import type { GameClockDto, ClueTimeAnchorDto, TrailBeatSlotDto } from "../api/types";

describe("formatClockBeat", () => {
  it("uses beatLabel when available", () => {
    const clock: GameClockDto = { day: 1, turn: 0, timeOfDay: "Morning", beatLabel: "Morning of Day 1" };
    expect(formatClockBeat(clock)).toBe("Morning of Day 1");
  });

  it("falls back to day/timeOfDay when beatLabel is empty", () => {
    const clock: GameClockDto = { day: 2, turn: 1, timeOfDay: "Afternoon", beatLabel: "" };
    expect(formatClockBeat(clock)).toBe("Day 2, Afternoon");
  });
});

describe("formatClueWhen", () => {
  it("uses timeOfDayLabel when available", () => {
    const anchor: ClueTimeAnchorDto = { recency: 1, day: 2, turn: 1, timeOfDayLabel: "Afternoon of Day 2" };
    expect(formatClueWhen(anchor)).toBe("Afternoon of Day 2");
  });

  it("falls back to day/turn when timeOfDayLabel is null", () => {
    const anchor: ClueTimeAnchorDto = { recency: 1, day: 2, turn: 1, timeOfDayLabel: null };
    expect(formatClueWhen(anchor)).toBe("day 2, turn 1");
  });

  it("returns empty string when all fields are null", () => {
    const anchor: ClueTimeAnchorDto = { recency: 0, day: null, turn: null, timeOfDayLabel: null };
    expect(formatClueWhen(anchor)).toBe("");
  });
});

describe("formatRemainingRideDays", () => {
  it("says Arriving today when 0", () => {
    expect(formatRemainingRideDays(0)).toBe("Arriving today");
  });

  it("says 1 ride day remaining when 1", () => {
    expect(formatRemainingRideDays(1)).toBe("1 ride day remaining");
  });

  it("uses plural for >1", () => {
    expect(formatRemainingRideDays(3)).toBe("3 ride days remaining");
  });
});

describe("formatInvestigationNotice", () => {
  it("prepends beat narration to message", () => {
    expect(formatInvestigationNotice("You spent the morning at the saloon", "No new gossip."))
      .toBe("You spent the morning at the saloon No new gossip.");
  });

  it("falls back to message-only when beatNarration is null", () => {
    expect(formatInvestigationNotice(null, "No new gossip.")).toBe("No new gossip.");
  });
});

describe("formatBeatSlotLabel", () => {
  it("formats each slot type", () => {
    expect(formatBeatSlotLabel("Quiet")).toBe("Quiet stretch");
    expect(formatBeatSlotLabel("Minor")).toBe("Minor event");
    expect(formatBeatSlotLabel("Eventful")).toBe("Eventful stretch");
    expect(formatBeatSlotLabel("Interrupting")).toBe("Interrupted");
  });
});

describe("formatBeatSlots", () => {
  it("formats slots with titles", () => {
    const slots: TrailBeatSlotDto[] = [
      { slotIndex: 0, slotType: "Minor", label: "Minor", title: "A lucky find", message: "You find supplies." },
      { slotIndex: 1, slotType: "Quiet", label: "Quiet", title: null, message: null },
    ];
    const result = formatBeatSlots(slots);
    expect(result).toEqual(["Minor event: A lucky find", "Quiet stretch"]);
  });
});
