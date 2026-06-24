import type { GameLogEntryDto, JournalDto } from "../api/types";
import { JournalSurface } from "./JournalSurface";

interface LogPanelProps {
  journal: JournalDto | null;
  sessionLogEntries: GameLogEntryDto[];
}

export function LogPanel({ journal, sessionLogEntries }: LogPanelProps) {
  return <JournalSurface journal={journal} loading={false} error="" sessionLogEntries={sessionLogEntries} />;
}
