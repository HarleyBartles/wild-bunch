import type {
  WantedPosterDto,
  WantedPosterFeatureDto,
  WantedPosterFeatureRenderMode,
  WantedPosterFeatureSalience,
} from "../api/types";
import { formatWarrantDisposition } from "../ui/formatters";

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
    <li className="wanted-poster__feature">
      <div className="wanted-poster__feature-copy">
        <strong>{formatFeatureSalience(feature.salience)}</strong>
        <p>{feature.text}</p>
      </div>
      <div className="wanted-poster__feature-tags">
        <span className="tag">{formatFeatureRenderMode(feature.renderMode)}</span>
      </div>
    </li>
  );
}

function WantedPosterCard({ poster }: { poster: WantedPosterDto }) {
  const portraitFeatures = poster.details.features.filter((feature) => feature.renderMode === 0);
  const textOnlyFeatures = poster.details.features.filter((feature) => feature.renderMode === 1);

  return (
    <article className="wanted-poster-card">
      <div className="wanted-poster-card__frame">
        <div className="wanted-poster-card__portrait" aria-hidden="true">
          <span className="wanted-poster-card__portrait-label">Wanted notice</span>
          <strong>{poster.targetDisplayName}</strong>
          <p>
            {portraitFeatures.length > 0 ? portraitFeatures[0].text : "Simple head-and-shoulders portrait cue"}
          </p>
        </div>

        <div className="wanted-poster-card__content">
          <header className="wanted-poster-card__header">
            <div>
              <p className="eyebrow">Public notice</p>
              <h4>{poster.targetDisplayName}</h4>
              <p className="wanted-poster-card__quick-view">
                {poster.quickView.headlineNameOrAlias} - {poster.quickView.headlineFeatureOrDescriptor}
              </p>
            </div>
            {poster.publicSafeClassification ? <span className="tag">{poster.publicSafeClassification}</span> : null}
          </header>

          <div className="wanted-poster-card__meta">
            <p>
              <strong>Aliases:</strong> {poster.aliases.length > 0 ? poster.aliases.join(", ") : "None recorded"}
            </p>
            <p>
              <strong>Disposition:</strong> {formatWarrantDisposition(poster.legalTerms.disposition)}
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
            <p>{poster.details.summary}</p>
          </div>

          <div className="wanted-poster-card__feature-block">
            <div className="wanted-poster-card__section-head">
              <h5>Feature notes</h5>
              <p className="panel-subtitle">Public-safe hints keep the poster legible without exposing hidden truth.</p>
            </div>
            {poster.details.features.length > 0 ? (
              <ul className="wanted-poster__feature-list">
                {poster.details.features.map((feature) => (
                  <WantedPosterFeatureRow key={`${poster.posterId}-${feature.text}-${feature.salience}`} feature={feature} />
                ))}
              </ul>
            ) : (
              <p className="muted">No public feature notes were returned.</p>
            )}
            {textOnlyFeatures.length > 0 ? (
              <p className="wanted-poster-card__text-only-note">
                Text-only cues: {textOnlyFeatures.map((feature) => feature.text).join(", ")}
              </p>
            ) : null}
          </div>
        </div>
      </div>
    </article>
  );
}

export function WantedPosterSurface({ wantedPosters }: WantedPosterSurfaceProps) {
  return (
    <article className="case-modal__section case-modal__section--wide">
      <div className="case-modal__section-head">
        <div>
          <h3>Wanted posters</h3>
          <p className="panel-subtitle">Public-safe sheriff notices, quick views, and feature notes from the current board.</p>
        </div>
      </div>
      {wantedPosters.length > 0 ? (
        <div className="wanted-poster__list">
          {wantedPosters.map((poster) => (
            <WantedPosterCard key={poster.posterId} poster={poster} />
          ))}
        </div>
      ) : (
        <p className="muted">No wanted posters are known yet.</p>
      )}
    </article>
  );
}
