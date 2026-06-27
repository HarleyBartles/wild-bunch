import styled from "styled-components";
import type {
  WantedPosterDto,
  WantedPosterFeatureDto,
  WantedPosterFeatureRenderMode,
  WantedPosterFeatureSalience,
} from "../api/types";
import { formatWarrantDisposition } from "../ui/formatters";
import {
  StatusCard,
  PanelSubtitle,
  Eyebrow,
  Muted,
} from "./ui/sharedStyled";

const PosterCard = styled.article`
  padding: 16px;
  border-radius: 18px;
  background:
    linear-gradient(180deg, rgba(62, 45, 20, 0.42), rgba(17, 15, 13, 0.2)),
    rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(223, 159, 79, 0.26);
`;

const PosterFrame = styled.div`
  display: grid;
  grid-template-columns: minmax(160px, 200px) minmax(0, 1fr);
  gap: 16px;

  @media (max-width: 640px) {
    grid-template-columns: 1fr;
  }
`;

const PosterPortrait = styled.div`
  display: grid;
  align-content: start;
  gap: 10px;
  padding: 16px;
  min-height: 100%;
  border-radius: 16px;
  border: 1px solid rgba(223, 159, 79, 0.26);
  background:
    radial-gradient(circle at top, rgba(223, 159, 79, 0.2), transparent 55%),
    rgba(10, 9, 8, 0.45);

  strong {
    font-size: 1.15rem;
    line-height: 1.1;
  }

  p {
    margin: 0;
    color: var(--muted);
    font-size: 0.88rem;
  }
`;

const PosterContent = styled.div`
  display: grid;
  gap: 14px;
`;

const PosterHeader = styled.header`
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;

  h4 {
    margin: 0 0 6px;
    font-size: 1.2rem;
  }
`;

const PosterMeta = styled.div`
  display: grid;
  gap: 6px;

  p {
    margin: 0;
    font-size: 0.94rem;
  }
`;

const FeatureRow = styled.li`
  display: flex;
  justify-content: space-between;
  gap: 12px;
  padding: 8px 10px;
  background: rgba(0, 0, 0, 0.2);
  border-radius: 10px;

  p {
    margin: 2px 0 0;
    font-size: 0.88rem;
    color: var(--muted);
  }
`;

const FeatureList = styled.ul`
  display: grid;
  gap: 10px;
  margin: 0;
  padding: 0;
  list-style: none;
`;

const Tag = styled.span`
  padding: 5px 9px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.06);
  border: 1px solid var(--border);
  font-size: 0.76rem;
  font-weight: 600;
  color: var(--muted);
`;

const WantedNoticeEyebrow = styled(Eyebrow)`
  font-size: 0.72rem;
  color: var(--accent-strong);
`;

const PosterHeadline = styled.p`
  margin: 0;
  color: var(--accent-strong);
`;

const SummaryLine = styled.p`
  margin-top: 4px;
`;

const FeatureNotesHeader = styled.div`
  margin-bottom: 10px;
`;

const FeatureNotesHeading = styled.h5`
  margin: 0;
  font-size: 0.98rem;
`;

const TextOnlyCues = styled.p`
  margin-top: 10px;
  font-size: 0.84rem;
  font-style: italic;
  color: var(--muted);
`;

const PosterSurfaceCard = styled(StatusCard)`
  grid-column: 1 / -1;
`;

const SurfaceHeader = styled.div`
  margin-bottom: 12px;
`;

const PosterList = styled.div`
  display: grid;
  gap: 20px;
`;

interface WantedPosterSurfaceProps {
  wantedPosters: WantedPosterDto[];
}

const currencyFormatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function formatBounty(amount: number) {
  return currencyFormatter.format(amount);
}

function formatFeatureSalience(salience: WantedPosterFeatureSalience) {
  switch (salience) {
    case 0:
      return "Headline";
    case 1:
      return "Supporting";
    case 2:
      return "Buried";
    default:
      return `Salience ${salience}`;
  }
}

function formatFeatureRenderMode(renderMode: WantedPosterFeatureRenderMode) {
  switch (renderMode) {
    case 0:
      return "Portrait-renderable";
    case 1:
      return "Text-only";
    default:
      return `Render mode ${renderMode}`;
  }
}

function WantedPosterFeatureRow({ feature }: { feature: WantedPosterFeatureDto }) {
  return (
    <FeatureRow>
      <div>
        <strong>{formatFeatureSalience(feature.salience)}</strong>
        <p>{feature.text}</p>
      </div>
      <div>
        <Tag>{formatFeatureRenderMode(feature.renderMode)}</Tag>
      </div>
    </FeatureRow>
  );
}

function WantedPosterCard({ poster }: { poster: WantedPosterDto }) {
  const portraitFeatures = poster.details.features.filter((feature) => feature.renderMode === 0);
  const textOnlyFeatures = poster.details.features.filter((feature) => feature.renderMode === 1);

  return (
    <PosterCard>
      <PosterFrame>
        <PosterPortrait aria-hidden="true">
          <WantedNoticeEyebrow>
            Wanted notice
          </WantedNoticeEyebrow>
          <strong>{poster.targetDisplayName}</strong>
          <p>
            {portraitFeatures.length > 0
              ? portraitFeatures[0].text
              : "Simple head-and-shoulders portrait cue"}
          </p>
        </PosterPortrait>

        <PosterContent>
          <PosterHeader>
            <div>
              <Eyebrow>Public notice</Eyebrow>
              <h4>{poster.targetDisplayName}</h4>
              <PosterHeadline>
                {poster.quickView.headlineNameOrAlias} - {poster.quickView.headlineFeatureOrDescriptor}
              </PosterHeadline>
            </div>
            {poster.publicSafeClassification ? <Tag>{poster.publicSafeClassification}</Tag> : null}
          </PosterHeader>

          <PosterMeta>
            <p>
              <strong>Aliases:</strong>{" "}
              {poster.aliases.length > 0 ? poster.aliases.join(", ") : "None recorded"}
            </p>
            <p>
              <strong>Disposition:</strong>{" "}
              {formatWarrantDisposition(poster.legalTerms.disposition)}
            </p>
            <p>
              <strong>Bounty:</strong> {formatBounty(poster.legalTerms.bountyAmount)}
            </p>
            <p>
              <strong>Issuing authority:</strong> {poster.legalTerms.issuingAuthority}
            </p>
            <p>
              <strong>Quick view:</strong> {poster.quickView.pocketCheckDescriptor}
            </p>
            <p>
              <strong>Public origin:</strong> {poster.details.publicOrigin}
            </p>
            <SummaryLine>{poster.details.summary}</SummaryLine>
          </PosterMeta>

          <div>
            <FeatureNotesHeader>
              <FeatureNotesHeading>Feature notes</FeatureNotesHeading>
              <PanelSubtitle>
                Public-safe hints keep the poster legible without exposing hidden truth.
              </PanelSubtitle>
            </FeatureNotesHeader>
            {poster.details.features.length > 0 ? (
              <FeatureList>
                {poster.details.features.map((feature) => (
                  <WantedPosterFeatureRow
                    key={`${poster.posterId}-${feature.text}-${feature.salience}`}
                    feature={feature}
                  />
                ))}
              </FeatureList>
            ) : (
              <Muted>No public feature notes were returned.</Muted>
            )}
            {textOnlyFeatures.length > 0 ? (
              <TextOnlyCues>
                Text-only cues: {textOnlyFeatures.map((feature) => feature.text).join(", ")}
              </TextOnlyCues>
            ) : null}
          </div>
        </PosterContent>
      </PosterFrame>
    </PosterCard>
  );
}

export function WantedPosterSurface({ wantedPosters }: WantedPosterSurfaceProps) {
  return (
    <PosterSurfaceCard as="article">
      <SurfaceHeader>
        <h3>Wanted posters</h3>
        <PanelSubtitle>
          Public-safe sheriff notices, quick views, and feature notes from the current board.
        </PanelSubtitle>
      </SurfaceHeader>
      {wantedPosters.length > 0 ? (
        <PosterList>
          {wantedPosters.map((poster) => (
            <WantedPosterCard key={poster.posterId} poster={poster} />
          ))}
        </PosterList>
      ) : (
        <Muted>No wanted posters are known yet.</Muted>
      )}
    </PosterSurfaceCard>
  );
}
