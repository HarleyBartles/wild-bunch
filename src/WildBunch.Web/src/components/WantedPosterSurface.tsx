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
  Tag,
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
          <Eyebrow style={{ fontSize: "0.72rem", color: "var(--accent-strong)" }}>
            Wanted notice
          </Eyebrow>
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
              <p style={{ margin: 0, color: "var(--accent-strong)" }}>
                {poster.quickView.headlineNameOrAlias} - {poster.quickView.headlineFeatureOrDescriptor}
              </p>
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
            <p style={{ marginTop: "4px" }}>{poster.details.summary}</p>
          </PosterMeta>

          <div>
            <div style={{ marginBottom: "10px" }}>
              <h5 style={{ margin: 0, fontSize: "0.98rem" }}>Feature notes</h5>
              <PanelSubtitle>
                Public-safe hints keep the poster legible without exposing hidden truth.
              </PanelSubtitle>
            </div>
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
              <p
                style={{
                  marginTop: "10px",
                  fontSize: "0.84rem",
                  fontStyle: "italic",
                  color: "var(--muted)",
                }}
              >
                Text-only cues: {textOnlyFeatures.map((feature) => feature.text).join(", ")}
              </p>
            ) : null}
          </div>
        </PosterContent>
      </PosterFrame>
    </PosterCard>
  );
}

export function WantedPosterSurface({ wantedPosters }: WantedPosterSurfaceProps) {
  return (
    <StatusCard as="article" style={{ gridColumn: "1 / -1" }}>
      <div style={{ marginBottom: "12px" }}>
        <h3>Wanted posters</h3>
        <PanelSubtitle>
          Public-safe sheriff notices, quick views, and feature notes from the current board.
        </PanelSubtitle>
      </div>
      {wantedPosters.length > 0 ? (
        <div style={{ display: "grid", gap: "20px" }}>
          {wantedPosters.map((poster) => (
            <WantedPosterCard key={poster.posterId} poster={poster} />
          ))}
        </div>
      ) : (
        <Muted>No wanted posters are known yet.</Muted>
      )}
    </StatusCard>
  );
}
