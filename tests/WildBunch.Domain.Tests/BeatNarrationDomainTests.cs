using WildBunch.Domain.Game;
using Xunit;

namespace WildBunch.Domain.Tests;

public class BeatNarrationDomainTests
{
    [Fact]
    public void InspectNoticeBoard_PopulatesBeatNarration()
    {
        var session = TestSessionFactory.CreateDefault();
        var result = session.InspectNoticeBoard();
        Assert.NotNull(result.BeatNarration);
        Assert.Contains("town square", result.BeatNarration);
        Assert.DoesNotContain("turn", result.BeatNarration!.ToLowerInvariant());
    }

    [Fact]
    public void GatherLocalGossip_PopulatesBeatNarration()
    {
        var session = TestSessionFactory.CreateDefault();
        var result = session.GatherLocalGossip();
        Assert.NotNull(result.BeatNarration);
        Assert.Contains("saloon", result.BeatNarration);
    }

    [Fact]
    public void SameSceneAction_DoesNotAdvanceBeatButStillHasNarration()
    {
        var session = TestSessionFactory.CreateDefault();
        session.LookAroundSaloon();
        var turnBefore = session.Clock.Turn;
        var result = session.GatherLocalGossip();
        Assert.Equal(turnBefore, session.Clock.Turn);
        Assert.NotNull(result.BeatNarration);
    }

    [Fact]
    public void BeatNarration_DescribesBeatSpentNotResultingClockState()
    {
        // A morning action (Turn 0, TimeOfDay.Morning) advances the clock to Afternoon.
        // The narration must say "morning" (the beat spent), not "afternoon" (the resulting state).
        // This test prevents the drift where narration accidentally uses post-advance TimeOfDay.
        var session = TestSessionFactory.CreateDefault();
        Assert.Equal(TimeOfDay.Morning, session.Clock.TimeOfDay);

        var result = session.InspectNoticeBoard();

        // After the action, the clock has advanced to Afternoon
        Assert.Equal(TimeOfDay.Afternoon, session.Clock.TimeOfDay);

        // But the narration must describe the beat that was spent (Morning), not the result (Afternoon)
        Assert.NotNull(result.BeatNarration);
        Assert.Contains("morning", result.BeatNarration!.ToLowerInvariant());
        Assert.DoesNotContain("afternoon", result.BeatNarration!.ToLowerInvariant());
    }

    [Fact]
    public void BeatNarration_AfterEveningAction_DescribesEveningNotNight()
    {
        // An evening action (Turn 2) advances the clock to Night.
        // The narration must say "evening" (the beat spent), not "night" (the resulting state).
        var session = TestSessionFactory.CreateDefault();
        // Advance to Evening: take 2 cross-location actions
        session.InspectNoticeBoard();   // Morning -> Afternoon (TownSquare)
        session.GatherLocalGossip();    // Afternoon -> Evening (Saloon)
        Assert.Equal(TimeOfDay.Evening, session.Clock.TimeOfDay);

        var result = session.CheckSheriffRecords();

        // After the action, the clock has advanced to Night
        Assert.Equal(TimeOfDay.Night, session.Clock.TimeOfDay);

        // But the narration must describe the beat that was spent (Evening), not the result (Night)
        Assert.NotNull(result.BeatNarration);
        Assert.Contains("evening", result.BeatNarration!.ToLowerInvariant());
        Assert.DoesNotContain("night", result.BeatNarration!.ToLowerInvariant());
    }
}
