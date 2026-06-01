using WildBunch.Domain.Cases;

namespace WildBunch.Application.Games.Models;

public sealed record WantedPostersResultDto(
    bool Success,
    string Message,
    JournalDto CurrentJournal,
    IReadOnlyList<WantedPosterDto> WantedPosters);

public sealed record WantedPosterDto(
    string PosterId,
    string TargetDisplayName,
    IReadOnlyList<string> Aliases,
    WantedPosterLegalTermsDto LegalTerms,
    WantedPosterQuickViewDto QuickView,
    WantedPosterDetailsDto Details,
    string? PublicSafeClassification);

public sealed record WantedPosterLegalTermsDto(
    WarrantDisposition Disposition,
    decimal BountyAmount,
    string IssuingAuthority);

public sealed record WantedPosterQuickViewDto(
    string HeadlineNameOrAlias,
    string HeadlineFeatureOrDescriptor,
    string PocketCheckDescriptor);

public sealed record WantedPosterDetailsDto(
    string Summary,
    string PublicOrigin,
    IReadOnlyList<WantedPosterFeatureDto> Features);

public sealed record WantedPosterFeatureDto(
    string Text,
    WantedPosterFeatureSalience Salience,
    WantedPosterFeatureRenderMode RenderMode);

public enum WantedPosterFeatureSalience
{
    Headline = 0,
    Supporting = 1,
    Buried = 2
}

public enum WantedPosterFeatureRenderMode
{
    PortraitRenderable = 0,
    TextOnly = 1
}
