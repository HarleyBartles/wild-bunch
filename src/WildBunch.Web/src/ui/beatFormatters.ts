import { TrailBeatSlotType, type GameClockDto, type ClueTimeAnchorDto, type TrailBeatSlotDto } from "../api/types";

export function formatClockBeat(clock: GameClockDto): string {
  return clock.beatLabel || `Day ${clock.day}, ${clock.timeOfDay}`;
}

export function formatClueWhen(anchor: ClueTimeAnchorDto): string {
  if (anchor.timeOfDayLabel) {
    return anchor.timeOfDayLabel;
  }
  const parts: string[] = [];
  if (anchor.day !== null) {
    parts.push(`day ${anchor.day}`);
  }
  if (anchor.turn !== null) {
    parts.push(`turn ${anchor.turn}`);
  }
  return parts.join(", ");
}

export function formatRemainingRideDays(remainingDays: number): string {
  if (remainingDays === 0) {
    return "Arriving today";
  }
  if (remainingDays === 1) {
    return "1 ride day remaining";
  }
  return `${remainingDays} ride days remaining`;
}

export function formatInvestigationNotice(beatNarration: string | null, message: string): string {
  if (beatNarration) {
    return `${beatNarration} ${message}`;
  }
  return message;
}

export function formatBeatSlotLabel(slotType: TrailBeatSlotType): string {
  switch (slotType) {
    case TrailBeatSlotType.Quiet:
      return "Quiet stretch";
    case TrailBeatSlotType.Minor:
      return "Minor event";
    case TrailBeatSlotType.Eventful:
      return "Eventful stretch";
    case TrailBeatSlotType.Interrupting:
      return "Interrupted";
    default:
      return String(slotType);
  }
}

export function formatBeatSlots(slots: TrailBeatSlotDto[]): string[] {
  return slots.map((slot) => {
    const label = formatBeatSlotLabel(slot.slotType);
    if (slot.title) {
      return `${label}: ${slot.title}`;
    }
    return label;
  });
}
