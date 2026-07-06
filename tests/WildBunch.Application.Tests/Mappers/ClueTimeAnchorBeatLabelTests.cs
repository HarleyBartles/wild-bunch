using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Cases;
using Xunit;

namespace WildBunch.Application.Tests.Mappers;

public class ClueTimeAnchorBeatLabelTests
{
    [Fact]
    public void ToDto_PopulatesTimeOfDayLabelFromTurn()
    {
        var anchor = new ClueTimeAnchor(ClueRecency.Recent, Day: 2, Turn: 1);
        var dto = CaseReadMapper.ToDto(anchor);
        Assert.NotNull(dto.TimeOfDayLabel);
        Assert.Contains("Afternoon", dto.TimeOfDayLabel);
        Assert.Contains("Day 2", dto.TimeOfDayLabel);
    }

    [Fact]
    public void ToDto_TimeOfDayLabelIsNullWhenTurnIsNull()
    {
        var anchor = new ClueTimeAnchor(ClueRecency.Recent, Day: null, Turn: null);
        var dto = CaseReadMapper.ToDto(anchor);
        Assert.Null(dto.TimeOfDayLabel);
    }

    [Fact]
    public void ToDto_TimeOfDayLabelShowsTimeOnlyWhenDayIsNull()
    {
        var anchor = new ClueTimeAnchor(ClueRecency.Recent, Day: null, Turn: 2);
        var dto = CaseReadMapper.ToDto(anchor);
        Assert.NotNull(dto.TimeOfDayLabel);
        Assert.Contains("Evening", dto.TimeOfDayLabel);
        Assert.DoesNotContain("Day", dto.TimeOfDayLabel);
    }
}
