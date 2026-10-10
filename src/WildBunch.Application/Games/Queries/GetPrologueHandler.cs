using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.GameContent.Prologue;

namespace WildBunch.Application.Games.Queries;

/// <summary>
/// Resolves the prologue read model. Substitutes the player-visible true-culprit
/// descriptor (via <see cref="PrologueDescriptorResolver"/>) into the chosen variant's
/// body template. No hidden culprit internals are exposed in the result.
/// </summary>
public sealed class GetPrologueHandler
{
    private readonly IGameSessionReadRepository _repository;

    public GetPrologueHandler(IGameSessionReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<PrologueDto> HandleAsync(GetPrologueQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessionId = new GameSessionId(query.GameSessionId);
        var session = await _repository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new GameSessionNotFoundException(sessionId);
        var trueCulpritDescriptor = PrologueDescriptorResolver.ResolveTrueCulpritDescriptor(session.CaseFile);

        var variant = query.VariantId is null
            ? PrologueContent.Variants[0]
            : PrologueContent.GetVariant(query.VariantId);

        var body = variant.BodyTemplate.Replace("{trueCulpritMainIdentifier}", trueCulpritDescriptor);

        var dto = new PrologueDto(
            PrologueContent.StorySoFarHeading,
            body,
            PrologueContent.StorySoFarPrimaryAction,
            variant.Id);

        return dto;
    }
}
