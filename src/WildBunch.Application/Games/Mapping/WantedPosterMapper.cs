using System.Globalization;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;

namespace WildBunch.Application.Games.Mapping;

public static class WantedPosterMapper
{
    public static IReadOnlyList<WantedPosterDto> ToDto(IReadOnlyList<Warrant> warrants)
    {
        ArgumentNullException.ThrowIfNull(warrants);

        return warrants.Select(ToDto).ToArray();
    }

    private static WantedPosterDto ToDto(Warrant warrant)
    {
        ArgumentNullException.ThrowIfNull(warrant);

        var aliases = warrant.Terms.KnownAliases.ToArray();
        var features = warrant.Terms.KnownFeatures
            .Select((feature, index) => new WantedPosterFeatureDto(
                feature,
                index == 0 ? WantedPosterFeatureSalience.Headline : index == 1 ? WantedPosterFeatureSalience.Supporting : WantedPosterFeatureSalience.Buried,
                ToRenderMode(feature)))
            .ToArray();

        var headlineFeatureOrDescriptor = features.FirstOrDefault()?.Text
            ?? GetPosterDescriptor(warrant);

        return new WantedPosterDto(
            warrant.Id.Value,
            warrant.TargetName,
            aliases,
            new WantedPosterLegalTermsDto(
                warrant.Terms.Disposition,
                warrant.Terms.BountyAmount,
                warrant.Terms.IssuingSource),
            new WantedPosterQuickViewDto(
                warrant.TargetName,
                headlineFeatureOrDescriptor,
                GetPocketCheckDescriptor(warrant)),
            new WantedPosterDetailsDto(
                string.IsNullOrWhiteSpace(warrant.Summary) ? "No public summary supplied." : warrant.Summary,
                warrant.Terms.IssuingSource,
                features),
            ToPublicSafeClassification(warrant));
    }

    private static string GetPosterDescriptor(Warrant warrant)
    {
        if (!string.IsNullOrWhiteSpace(warrant.Summary))
        {
            return warrant.Summary.Trim();
        }

        if (!string.IsNullOrWhiteSpace(warrant.Terms.IssuingSource))
        {
            return warrant.Terms.IssuingSource.Trim();
        }

        return warrant.TargetName;
    }

    private static string GetPocketCheckDescriptor(Warrant warrant)
    {
        var disposition = warrant.Terms.Disposition == WarrantDisposition.DeadOrAlive
            ? "Dead or alive"
            : "Alive only";

        return string.Format(CultureInfo.InvariantCulture, "{0}, ${1:N2} bounty", disposition, warrant.Terms.BountyAmount);
    }

    private static string ToPublicSafeClassification(Warrant warrant)
        => warrant.Terms.GangAffiliations.Count > 0
            ? "gang-affiliated wanted criminal"
            : "wanted criminal";

    private static WantedPosterFeatureRenderMode ToRenderMode(string feature)
    {
        var cleaned = feature.Trim().ToLowerInvariant();
        if (cleaned.Length == 0)
        {
            return WantedPosterFeatureRenderMode.TextOnly;
        }

        var portraitRenderableKeywords =
            cleaned.Contains("hat", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("eyepatch", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("mustache", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("moustache", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("beard", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("sideburn", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("hair", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("earring", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("eyebrow", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("face", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("cheek", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("scar", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("glasses", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("bandana", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("neckerchief", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("stubble", StringComparison.OrdinalIgnoreCase);

        var textOnlyKeywords =
            cleaned.Contains("limp", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("boot", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("hand scar", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("finger", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("gait", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("leg", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("foot", StringComparison.OrdinalIgnoreCase)
            || cleaned.Contains("wrist", StringComparison.OrdinalIgnoreCase);

        if (textOnlyKeywords)
        {
            return WantedPosterFeatureRenderMode.TextOnly;
        }

        return portraitRenderableKeywords
            ? WantedPosterFeatureRenderMode.PortraitRenderable
            : WantedPosterFeatureRenderMode.TextOnly;
    }
}
