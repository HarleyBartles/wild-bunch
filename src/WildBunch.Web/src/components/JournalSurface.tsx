import styled from "styled-components";
import type { GameLogEntryDto, JournalDto } from "../api/types";
import { StatusCard } from "./ui/sharedStyled";
import { formatClockBeat } from "../ui/beatFormatters";
import { JournalEntryTimeline } from "./journal/JournalEntryTimeline";

const ModalState = styled.div`
  padding: 18px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid var(--border);
  color: var(--text);
`;

const JournalClock = styled.h3`
  margin: 0;
  padding: 10px 14px;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid var(--border);
  color: var(--text);
  font-size: 1rem;
  line-height: 1.3;
  font-variant-numeric: tabular-nums;
  text-wrap: pretty;
  white-space: nowrap;
`;

const JournalSurfaceSection = styled(StatusCard).attrs({ as: "section" })`
  grid-column: 1 / -1;
  display: grid;
  gap: 18px;
`;

const ReadError = styled.div`
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
  padding: 12px 14px;
  border: 1px solid color-mix(in srgb, var(--danger) 28%, transparent);
  border-radius: 12px;
  color: var(--text);
`;

const RetryButton = styled.button`
  min-height: 36px;
  padding: 6px 14px;
  border: 1px solid var(--border-strong);
  border-radius: 999px;
  background: transparent;
  color: var(--text);
  font: inherit;
  cursor: pointer;
`;

interface JournalSurfaceProps {
  journal: JournalDto | null;
  loading: boolean;
  refreshing?: boolean;
  error: string;
  readError?: string | null;
  onRetry?: () => void;
  sessionLogEntries?: GameLogEntryDto[];
}

function getEntries(journal: JournalDto | null, sessionLogEntries: GameLogEntryDto[] | undefined) {
  return journal?.logEntries ?? sessionLogEntries ?? [];
}

function formatJournalClock(journal: JournalDto) {
  return `${formatClockBeat(journal.clock)} in ${journal.currentTown.name}`;
}

export function JournalSurface({
  journal,
  loading,
  refreshing = false,
  error,
  readError = null,
  onRetry,
  sessionLogEntries,
}: JournalSurfaceProps) {
  const entries = getEntries(journal, sessionLogEntries);

  if (loading && !journal) {
    return <ModalState>Opening the trail journal...</ModalState>;
  }

  if (!journal && readError) {
    return (
      <ModalState role="alert">
        <p>{readError}</p>
        {onRetry ? (
          <RetryButton type="button" onClick={onRetry}>
            Try again
          </RetryButton>
        ) : null}
      </ModalState>
    );
  }

  if (!journal && error) {
    return <ModalState>{error || "Load a game to read the trail journal."}</ModalState>;
  }

  if (!journal) {
    return <ModalState>Load a game to read the trail journal.</ModalState>;
  }

  return (
    <>
      {readError ? (
        <ReadError role="alert">
          <span>{readError}</span>
          {onRetry ? (
            <RetryButton type="button" onClick={onRetry}>
              Try again
            </RetryButton>
          ) : null}
        </ReadError>
      ) : null}
      {refreshing ? <ModalState role="status">Checking the journal...</ModalState> : null}
      {error ? <ModalState role="alert">{error}</ModalState> : null}
      <JournalSurfaceSection>
        <header>
          <JournalClock>{formatJournalClock(journal)}</JournalClock>
        </header>

        <JournalEntryTimeline
          entries={entries}
          emptyMessage="No notes yet. The trail is still being written."
        />
      </JournalSurfaceSection>
    </>
  );
}
