using WildBunch.Application.Games.Models;
using WildBunch.GameContent.Prologue;

namespace WildBunch.Application.Games.Queries;

/// <summary>
/// Resolves the prologue read model. Substitutes the player-visible true-culprit
/// descriptor (via <see cref="PrologueDescriptorResolver"/>) into the chosen variant's
/// body template. No hidden culprit internals are exposed in the result.
/// </summary>
public sealed class GetPrologueHandler
{
    public Task<PrologueDto> HandleAsync(GetPrologueQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var trueCulpritDescriptor = PrologueDescriptorResolver.ResolveTrueCulpritDescriptor(
            query.GameDifficulty, query.SeedCode, query.Entropy);

        var variant = query.VariantId is null
            ? PrologueContent.Variants[0]
            : PrologueContent.GetVariant(query.VariantId);

        var body = variant.BodyTemplate.Replace("{trueCulpritMainIdentifier}", trueCulpritDescriptor);

        var dto = new PrologueDto(
            PrologueContent.StorySoFarHeading,
            body,
            PrologueContent.StorySoFarPrimaryAction,
            variant.Id);

        return Task.FromResult(dto);
    }
}
