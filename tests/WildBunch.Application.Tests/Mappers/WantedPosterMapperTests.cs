using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;

namespace WildBunch.Application.Tests.Mappers;

public sealed class WantedPosterMapperTests
{
    [Fact]
    public void MapsWarrantsIntoPublicSafeWantedPosterReadModels()
    {
        var posters = WantedPosterMapper.ToDto(new[]
        {
            new Warrant(
                new WarrantId("warrant-gang"),
                "Mira Cline",
                new WarrantTerms(
                    WarrantDisposition.DeadOrAlive,
                    2500m,
                    new[] { "Red Wren", "Aunt Tess" },
                    new[] { "Raven-feather pin", "Black felt hat" },
                    "Dodge City Marshal",
                    InvestigationTargetKind.TrueCulprit,
                    [OutlawGangIds.WildBunch],
                    OutlawGangIds.WildBunch,
                    InvestigationSourceKind.SheriffWarrants),
                "Wanted for a Wild Bunch robbery and related killings.")
        });

        Assert.Single(posters);
        var poster = posters[0];
        Assert.Equal("warrant-gang", poster.PosterId);
        Assert.Equal("Mira Cline", poster.TargetDisplayName);
        Assert.Equal(new[] { "Red Wren", "Aunt Tess" }, poster.Aliases);
        Assert.Equal(WarrantDisposition.DeadOrAlive, poster.LegalTerms.Disposition);
        Assert.Equal(2500m, poster.LegalTerms.BountyAmount);
        Assert.Equal("Dodge City Marshal", poster.LegalTerms.IssuingAuthority);
        Assert.Equal("Mira Cline", poster.QuickView.HeadlineNameOrAlias);
        Assert.Equal("Raven-feather pin", poster.QuickView.HeadlineFeatureOrDescriptor);
        Assert.Equal("Dead or alive, $2,500.00 bounty", poster.QuickView.PocketCheckDescriptor);
        Assert.Equal("Wanted for a Wild Bunch robbery and related killings.", poster.Details.Summary);
        Assert.Equal("Dodge City Marshal", poster.Details.PublicOrigin);
        Assert.Equal(2, poster.Details.Features.Count);
        Assert.Equal(WantedPosterFeatureSalience.Headline, poster.Details.Features[0].Salience);
        Assert.Equal(WantedPosterFeatureRenderMode.TextOnly, poster.Details.Features[0].RenderMode);
        Assert.Equal(WantedPosterFeatureSalience.Supporting, poster.Details.Features[1].Salience);
        Assert.Equal(WantedPosterFeatureRenderMode.PortraitRenderable, poster.Details.Features[1].RenderMode);
        Assert.Equal("gang-affiliated wanted criminal", poster.PublicSafeClassification);
    }
}
