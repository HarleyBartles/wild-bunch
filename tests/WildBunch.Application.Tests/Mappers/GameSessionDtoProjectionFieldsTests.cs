using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Projections;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests.Mappers;

public sealed class GameSessionDtoProjectionFieldsTests
{
    [Fact]
    public void GameSessionDto_DefaultsProjectionFieldsToNull()
    {
        var session = new StubNewGameFactory().CreatedSession;
        var dto = GameSessionMapper.ToDto(session);

        Assert.Null(dto.HudProjection);
        Assert.Null(dto.DiaryProjection);
    }

    [Fact]
    public void GameSessionDto_AcceptsProjectionsViaWithExpression()
    {
        var session = new StubNewGameFactory().CreatedSession;
        var dto = GameSessionMapper.ToDto(session);

        var hud = new HudProjection(
            session.Id.Value,
            GameStatus.Active,
            session.Player.Name,
            session.Player.Health,
            session.Player.Wallet.Cash,
            session.Player.CurrentTownId!.Value,
            "Dustvale",
            Array.Empty<HudInventoryItem>());

        var diary = new DiaryProjection(
            session.Id.Value,
            session.Clock.Day,
            session.Clock.Turn,
            session.Player.CurrentTownId!.Value,
            "Dustvale",
            Array.Empty<DiaryEntry>());

        var withProjections = dto with { HudProjection = hud, DiaryProjection = diary };

        Assert.Same(hud, withProjections.HudProjection);
        Assert.Same(diary, withProjections.DiaryProjection);
        // Unrelated fields are preserved by the with expression.
        Assert.Equal(dto.Id, withProjections.Id);
        Assert.Equal(dto.Player, withProjections.Player);
    }
}
