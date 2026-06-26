import { useMemo, useState } from "react";
import styled from "styled-components";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useGameSession } from "../../state/useGameSession";
import { clearSaloonOverride, forceSaloonOverride, getSaloonDevContext } from "../devApi";
import type { SaloonSuspectDevDto } from "../types";

const POI_KINDS = ["Suspect", "Citizen"] as const;
type PoiKind = (typeof POI_KINDS)[number];

export function SaloonDevPanel() {
  const { gameId } = useGameSession();
  const queryClient = useQueryClient();

  const [forcedKind, setForcedKind] = useState<PoiKind>("Suspect");
  const [selectedSuspectId, setSelectedSuspectId] = useState<string>("");
  const [error, setError] = useState<string | null>(null);
  const [actionPending, setActionPending] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ["dev-saloon-context", gameId],
    queryFn: () => getSaloonDevContext(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  const eligibleSuspects = useMemo(
    () => (data?.suspects ?? []).filter((s) => s.isEligibleSaloonPoi),
    [data?.suspects],
  );

  if (!gameId) {
    return <MutedText>No active session.</MutedText>;
  }

  if (isLoading) {
    return <MutedText>Loading saloon context...</MutedText>;
  }

  const refresh = () => queryClient.invalidateQueries({ queryKey: ["dev-saloon-context", gameId] });

  const handleForce = async () => {
    setError(null);
    setActionPending(true);
    try {
      await forceSaloonOverride(gameId, {
        forcedKind,
        forcedSuspectId: forcedKind === "Suspect" && selectedSuspectId !== ""
          ? selectedSuspectId
          : null,
      });
      refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to force override.");
    } finally {
      setActionPending(false);
    }
  };

  const handleClear = async () => {
    setError(null);
    setActionPending(true);
    try {
      await clearSaloonOverride(gameId);
      refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to clear override.");
    } finally {
      setActionPending(false);
    }
  };

  const contextMismatch = detectContextMismatch(data?.currentActionContext);

  return (
    <Container>
      <ContextSection>
        <SectionTitle>Saloon context</SectionTitle>
        <Row>
          <Label>Town:</Label>
          <Value>{data?.currentTownName ?? "-"}</Value>
        </Row>
        <Row>
          <Label>Aggregate context:</Label>
          <Value>{data?.currentActionContext ?? "None"}</Value>
        </Row>
        <Row>
          <Label>Source spent:</Label>
          <Value>{data?.sourceSpent ? "Yes" : "No"}</Value>
        </Row>
        {contextMismatch && (
          <MismatchWarning>
            Warning: UI surface is saloon but aggregate context is {data?.currentActionContext}.
            This may indicate a stale context transition.
          </MismatchWarning>
        )}
      </ContextSection>

      <Section>
        <SectionTitle>Active saloon POI</SectionTitle>
        {data?.activeSaloonPoi ? (
          <PoiCard>
            <Row>
              <Label>Kind:</Label>
              <Value>{formatPoiKind(data.activeSaloonPoi.personOfInterestKind)}</Value>
            </Row>
            {data.activeSaloonPoi.suspectName && (
              <Row>
                <Label>Person:</Label>
                <Value>{data.activeSaloonPoi.suspectName}</Value>
              </Row>
            )}
            {data.activeSaloonPoi.suspectId && !data.activeSaloonPoi.suspectName && (
              <Row>
                <Label>Suspect ID:</Label>
                <Value>{data.activeSaloonPoi.suspectId}</Value>
              </Row>
            )}
            {data.activeSaloonPoi.descriptor && (
              <Row>
                <Label>Descriptor:</Label>
                <Value>{data.activeSaloonPoi.descriptor}</Value>
              </Row>
            )}
            {!data.activeSaloonPoi.suspectId && !data.activeSaloonPoi.suspectName && (
              <MutedText>
                Generic citizen POI — no named entity. {data.citizenInfo?.descriptor ?? ""}
              </MutedText>
            )}
          </PoiCard>
        ) : (
          <MutedText>
            {data?.sourceSpent
              ? "No active POI (source spent — repeat visit or confrontation cleared)."
              : "No active POI (LookAroundSaloon not yet called)."}
          </MutedText>
        )}
      </Section>

      <Section>
        <SectionTitle>Hidden truth (dev only)</SectionTitle>
        {data?.hiddenTruth ? (
          <HiddenTruthCard>
            <Row>
              <Label>True culprit:</Label>
              <Value>{data.hiddenTruth.trueCulpritName}</Value>
            </Row>
            <Row>
              <Label>Killer release:</Label>
              <Value>{data.hiddenTruth.killerReleaseStatus}</Value>
            </Row>
            <Row>
              <Label>Culprit saloon eligibility:</Label>
              <Value>
                {data.hiddenTruth.killerIsReleased
                  ? "Eligible — killer trail is released"
                  : "Gated out — killer trail is locked"}
              </Value>
            </Row>
            <ExplanationText>{data.hiddenTruth.saloonLoopExplanation}</ExplanationText>
          </HiddenTruthCard>
        ) : (
          <MutedText>Not available.</MutedText>
        )}
      </Section>

      <Section>
        <SectionTitle>Suspects</SectionTitle>
        {data?.suspects && data.suspects.length > 0 ? (
          data.suspects.map((s) => <SuspectCard key={s.suspectId} suspect={s} />)
        ) : (
          <MutedText>No suspects.</MutedText>
        )}
      </Section>

      <Section>
        <SectionTitle>Pending dev override</SectionTitle>
        {data?.pendingDevOverride ? (
          <Row>
            <Label>Override:</Label>
            <Value>
              {data.pendingDevOverride.forcedKind}
              {data.pendingDevOverride.forcedSuspectName
                ? ` — ${data.pendingDevOverride.forcedSuspectName}`
                : data.pendingDevOverride.forcedSuspectId
                  ? ` (${data.pendingDevOverride.forcedSuspectId})`
                  : ""}
            </Value>
          </Row>
        ) : (
          <MutedText>None pending.</MutedText>
        )}
      </Section>

      <ForceSection>
        <SectionTitle>Force next saloon look-around POI</SectionTitle>
        <ScopeNote>
          Sets the POI shape for the next LookAroundSaloon call. Does not grant casefile
          knowledge, does not resolve confrontation, does not force sheriff/take-in success.
          Consumed by normal saloon gameplay.
        </ScopeNote>
        <Field>
          <Label>POI kind:</Label>
          <Select
            value={forcedKind}
            onChange={(e) => {
              setForcedKind(e.target.value as PoiKind);
              setSelectedSuspectId("");
            }}
            data-testid="force-kind-select"
          >
            {POI_KINDS.map((k) => (
              <option key={k} value={k}>
                {k}
              </option>
            ))}
          </Select>
        </Field>
        {forcedKind === "Suspect" && (
          <Field>
            <Label>Candidate:</Label>
            <Select
              value={selectedSuspectId}
              onChange={(e) => setSelectedSuspectId(e.target.value)}
              data-testid="force-suspect-select"
            >
              <option value="">(first eligible)</option>
              {eligibleSuspects.map((s) => (
                <option key={s.suspectId} value={s.suspectId}>
                  {formatSuspectLabel(s)}
                </option>
              ))}
            </Select>
          </Field>
        )}
        {forcedKind === "Citizen" && (
          <CitizenNote>
            {data?.citizenInfo
              ? data.citizenInfo.hasNamedArchetypes
                ? `Available archetypes: ${data.citizenInfo.availableArchetypes.join(", ")}`
                : `Generic citizen POI — ${data.citizenInfo.descriptor}. No named archetypes exist.`
              : "Generic citizen POI — no named archetypes."}
          </CitizenNote>
        )}
        <ButtonRow>
          <Button type="button" onClick={handleForce} disabled={actionPending}>
            Force next POI
          </Button>
          <Button type="button" onClick={handleClear} disabled={actionPending}>
            Clear override
          </Button>
        </ButtonRow>
        {error && <ErrorText>{error}</ErrorText>}
      </ForceSection>
    </Container>
  );
}

function detectContextMismatch(actionContext: string | null | undefined): boolean {
  if (!actionContext) return false;
  // The dev overlay is shown on the saloon surface. If the aggregate action context
  // is not Saloon, that's a mismatch.
  return actionContext !== "Saloon";
}

function formatPoiKind(kind: string | null | undefined): string {
  if (!kind) return "unknown";
  return kind === "WantedSuspect" ? "Wanted suspect" : kind;
}

function formatSuspectLabel(s: SaloonSuspectDevDto): string {
  const parts = [s.name];
  if (s.bountyAmount !== null && s.bountyAmount !== undefined) {
    parts.push(`$${s.bountyAmount}`);
  }
  if (s.warrantDisposition) {
    parts.push(s.warrantDisposition);
  }
  if (s.traitTags.length > 0) {
    parts.push(`[${s.traitTags.join(", ")}]`);
  }
  return parts.join(" — ");
}

function SuspectCard({ suspect }: { suspect: SaloonSuspectDevDto }) {
  return (
    <SuspectRow>
      <SuspectName>
        {suspect.name}
        {suspect.isTrueCulprit && <CulpritBadge>culprit</CulpritBadge>}
      </SuspectName>
      <SuspectDetail>
        {suspect.isEligibleSaloonPoi ? (
          <EligibleTag>eligible</EligibleTag>
        ) : (
          <IneligibleTag>ineligible</IneligibleTag>
        )}
        {suspect.hasKnownWarrant && <span> | warrant</span>}
        {suspect.presenceState && <span> | {suspect.presenceState}</span>}
        {suspect.bountyAmount !== null && suspect.bountyAmount !== undefined && (
          <span> | bounty ${suspect.bountyAmount}</span>
        )}
        {suspect.warrantDisposition && <span> | {suspect.warrantDisposition}</span>}
      </SuspectDetail>
      {suspect.aliases.length > 0 && (
        <SuspectFact>Aliases: {suspect.aliases.join(", ")}</SuspectFact>
      )}
      {suspect.identifyingFacts.length > 0 && (
        <SuspectFact>Identifying facts: {suspect.identifyingFacts.join("; ")}</SuspectFact>
      )}
      {suspect.traitTags.length > 0 && (
        <SuspectFact>Traits: {suspect.traitTags.join(", ")}</SuspectFact>
      )}
      {suspect.warrantKnownFeatures.length > 0 && (
        <SuspectFact>Known features: {suspect.warrantKnownFeatures.join("; ")}</SuspectFact>
      )}
      {suspect.warrantSummary && <SuspectFact>Warrant: {suspect.warrantSummary}</SuspectFact>}
      {suspect.ineligibilityReason && (
        <SuspectReason>{suspect.ineligibilityReason}</SuspectReason>
      )}
    </SuspectRow>
  );
}

const Container = styled.div`
  display: grid;
  gap: 16px;
`;

const Section = styled.section`
  display: grid;
  gap: 6px;
`;

const ContextSection = styled.section`
  display: grid;
  gap: 6px;
`;

const SectionTitle = styled.h3`
  margin: 0 0 4px;
  font-size: 0.88rem;
  color: var(--accent);
`;

const Row = styled.div`
  display: flex;
  gap: 8px;
  font-size: 0.82rem;
`;

const Label = styled.span`
  color: var(--muted);
  flex-shrink: 0;
  min-width: 140px;
`;

const Value = styled.span`
  color: var(--text);
`;

const Field = styled.div`
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.82rem;
`;

const Select = styled.select`
  flex: 1;
  padding: 4px 8px;
  border-radius: 6px;
  border: 1px solid var(--border-strong);
  background: var(--bg);
  color: var(--text);
  font-size: 0.82rem;
`;

const ButtonRow = styled.div`
  display: flex;
  gap: 8px;
  margin-top: 6px;
`;

const Button = styled.button`
  padding: 6px 14px;
  border-radius: 999px;
  border: 1px solid var(--border-strong);
  background: transparent;
  color: var(--text);
  cursor: pointer;
  font-size: 0.8rem;
  font-weight: 600;
  min-height: 32px;
  transition-property: background-color, border-color;
  transition-duration: 120ms;
  transition-timing-function: ease-out;

  &:hover:not(:disabled) {
    background: var(--bg-hover);
  }

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
`;

const MutedText = styled.p`
  color: var(--muted);
  font-size: 0.82rem;
  margin: 0;
`;

const ErrorText = styled.p`
  color: var(--danger);
  font-size: 0.8rem;
  margin: 4px 0 0;
`;

const SuspectRow = styled.div`
  display: grid;
  gap: 2px;
  font-size: 0.82rem;
  padding: 6px 0;
  border-bottom: 1px solid var(--border);
`;

const SuspectName = styled.span`
  color: var(--text);
  font-weight: 600;
`;

const SuspectDetail = styled.span`
  color: var(--muted);
  font-size: 0.78rem;
`;

const SuspectFact = styled.span`
  color: var(--muted);
  font-size: 0.76rem;
`;

const SuspectReason = styled.span`
  color: var(--danger);
  font-size: 0.76rem;
`;

const CulpritBadge = styled.span`
  display: inline-block;
  margin-left: 6px;
  padding: 1px 6px;
  border-radius: 4px;
  background: var(--accent);
  color: var(--bg);
  font-size: 0.7rem;
  font-weight: 700;
`;

const EligibleTag = styled.span`
  color: var(--success, #4caf50);
  font-weight: 600;
`;

const IneligibleTag = styled.span`
  color: var(--danger);
  font-weight: 600;
`;

const PoiCard = styled.div`
  display: grid;
  gap: 4px;
  padding: 8px;
  border-radius: 6px;
  border: 1px solid var(--border);
  background: rgba(255, 255, 255, 0.02);
`;

const HiddenTruthCard = styled.div`
  display: grid;
  gap: 4px;
  padding: 8px;
  border-radius: 6px;
  border: 1px solid var(--border);
  background: rgba(223, 159, 79, 0.06);
`;

const ExplanationText = styled.p`
  color: var(--muted);
  font-size: 0.78rem;
  margin: 4px 0 0;
  line-height: 1.4;
`;

const ForceSection = styled.section`
  display: grid;
  gap: 6px;
  padding: 8px;
  border-radius: 6px;
  border: 1px solid var(--border);
  background: rgba(255, 255, 255, 0.02);
`;

const ScopeNote = styled.p`
  color: var(--muted);
  font-size: 0.76rem;
  margin: 0;
  line-height: 1.4;
`;

const CitizenNote = styled.p`
  color: var(--muted);
  font-size: 0.78rem;
  margin: 0;
  padding: 4px 0;
`;

const MismatchWarning = styled.div`
  color: var(--danger);
  font-size: 0.78rem;
  padding: 6px 8px;
  border-radius: 6px;
  border: 1px solid var(--danger);
  background: rgba(220, 50, 50, 0.08);
`;
