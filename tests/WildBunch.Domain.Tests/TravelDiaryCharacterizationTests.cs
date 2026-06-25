using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Characterization tests pinning exact travel diary and Travel-kind log entry
/// accumulation behavior. These tests MUST pass before and after the Phase 2
/// event-sourcing migration. All values are captured from deterministic scenarios
/// using TravelRandomnessState.CreateDeterministic(string.Empty) and ForcedRoll.
/// </summary>
public sealed class TravelDiaryCharacterizationTests
{
    // ----- EasyShortJourney: Travel-kind log entry accumulation -----

    [Fact]
    public void StartJourney_EasyShortJourney_AppendsSingleTravelLogEntry()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();

        session.StartJourney(preview);

        var travelLogs = session.LogEntries
            .Where(e => e.Kind == GameLogEntryKind.Travel)
            .ToList();
        Assert.Equal(1, travelLogs.Count);
        Assert.Equal(1, travelLogs[0].Day);
        Assert.Equal(0, travelLogs[0].Turn);
        Assert.Equal(
            "You set out from Current Town toward Connected Town by mounted travel. The route is 1 ride-day unit(s) and should take 1 day(s). Route water is secure, so no canteen reserve is required.",
            travelLogs[0].Message);
        Assert.Equal(0, session.TravelDiaryDays.Count);
    }

    // Heat no longer affects trail events or encounters, so the deterministic
    // rolls now produce a different outcome for the same route profile: the
    // EasyShortJourney is interrupted by an NPC encounter on day 1 instead of
    // completing quietly with a LuckyCoinCache. See ADR-0029.
    [Fact]
    public void AdvanceJourneyDay_EasyShortJourney_InterruptedAccumulatesTwoTravelLogEntries()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);

        session.AdvanceJourneyDay();

        var travelLogs = session.LogEntries
            .Where(e => e.Kind == GameLogEntryKind.Travel)
            .ToList();
        Assert.Equal(2, travelLogs.Count);
        Assert.Equal(
            "You set out from Current Town toward Connected Town by mounted travel. The route is 1 ride-day unit(s) and should take 1 day(s). Route water is secure, so no canteen reserve is required.",
            travelLogs[0].Message);
        Assert.Equal(2, travelLogs[1].Day);
        Assert.Equal(0, travelLogs[1].Turn);
        Assert.Equal("A weathered stranger shared the water side of the trail and swapped a few words.", travelLogs[1].Message);
    }

    // ----- EasyShortJourney: TravelDiaryDays accumulation -----

    [Fact]
    public void AdvanceJourneyDay_EasyShortJourney_RecordsSingleInterruptedDiaryDay()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);

        session.AdvanceJourneyDay();

        Assert.Equal(1, session.TravelDiaryDays.Count);
        var day = session.TravelDiaryDays[0];
        Assert.Equal(1, day.DayNumber);
        Assert.Equal("Current Town", day.OriginTownName);
        Assert.Equal("Connected Town", day.DestinationTownName);
        Assert.Equal(TravelMode.Mounted, day.StartingTravelMode);
        Assert.Equal(TravelMode.Mounted, day.EndingTravelMode);
        Assert.Equal(JourneyStatus.Interrupted, day.Status);
        Assert.Equal(1m, day.StartingRideDayDistance);
        Assert.Equal(0m, day.RemainingRideDayDistance);
        Assert.Equal(1, day.StartingDaysRemaining);
        Assert.Equal(0, day.RemainingDays);
        Assert.Equal(
            "I set out for Connected Town on a 1-day open-range ride by mounted travel. The route looks steady enough for now. I had enough water for the base trail, though the canteen still needed watching on a 1-day run. My food should have held if the trail behaved itself. My horse was fit enough to carry me for now.",
            day.OpeningNarration);
        Assert.Null(day.JourneyBeat);
        Assert.Null(day.ResourceBeat);
        Assert.Equal(2, day.Entries.Count);
        Assert.Equal("A weathered stranger shared the water side of the trail and swapped a few words.", day.Entries[0]);
        Assert.Equal("I could run, fight, or bribe my way through.", day.Entries[1]);
        Assert.Equal(0, day.HealthDelta);
        Assert.Equal(0m, day.WalletDelta);
        Assert.Equal(-1, day.FoodDelta);
        Assert.Equal(0, day.HorseFeedDelta);
        Assert.Equal(0, day.CanteenChargeDelta);
        Assert.Equal(0, day.AmmoSpent);
        Assert.Equal(0, day.HorseHungerDelta);
        Assert.Equal(0, day.HorseThirstDelta);
        Assert.Equal(0, day.HorseExhaustionDelta);
        Assert.Equal(0, day.DelayDays);
        Assert.Equal(0, day.HeatIncrease);
        Assert.Equal(1250, day.CurrentHealth);
        Assert.Equal(25m, day.CurrentWallet);
        Assert.Equal(3, day.CurrentFood);
        Assert.Equal(0, day.CurrentHorseFeed);
        Assert.Equal(10, day.CurrentCanteenCharges);
        Assert.Equal(0, day.CurrentAmmo);
        Assert.Equal(0, day.CurrentHeat);
        Assert.Empty(day.Warnings);
        Assert.Equal(HorseTravelState.Healthy, day.HorseStateBefore);
        Assert.Equal(HorseTravelState.Healthy, day.HorseStateAfter);
        Assert.Null(day.TrailEvent);
        Assert.NotNull(day.PendingEncounter);
        Assert.Equal("npc", day.PendingEncounter!.Kind);
        Assert.Null(day.EncounterResolution);
    }

    // ----- SixDayQuietJourney: Travel-kind log entry accumulation -----

    [Fact]
    public void StartJourney_SixDayQuietJourney_AppendsSingleTravelLogEntry()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();

        session.StartJourney(preview);

        var travelLogs = session.LogEntries
            .Where(e => e.Kind == GameLogEntryKind.Travel)
            .ToList();
        Assert.Equal(1, travelLogs.Count);
        Assert.Equal(1, travelLogs[0].Day);
        Assert.Equal(0, travelLogs[0].Turn);
        Assert.Equal(
            "You set out from Pinecross toward Six Mile on foot. The route is 3 ride-day unit(s) and should take 4 day(s). The canteen has 2 spare charge(s) and can absorb 2 delay day(s).",
            travelLogs[0].Message);
        Assert.Equal(0, session.TravelDiaryDays.Count);
    }

    [Fact]
    public void AdvanceJourneyDay_SixDayQuietJourney_AfterOneAdvance_AccumulatesThreeTravelLogEntries()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);

        session.AdvanceJourneyDay();

        var travelLogs = session.LogEntries
            .Where(e => e.Kind == GameLogEntryKind.Travel)
            .ToList();
        Assert.Equal(3, travelLogs.Count);
        Assert.Equal(
            "You set out from Pinecross toward Six Mile on foot. The route is 3 ride-day unit(s) and should take 4 day(s). The canteen has 2 spare charge(s) and can absorb 2 delay day(s).",
            travelLogs[0].Message);
        Assert.Equal(2, travelLogs[1].Day);
        Assert.Equal(0, travelLogs[1].Turn);
        Assert.Equal("I found a seep under the rocks and topped off my canteen by 2 charge(s).", travelLogs[1].Message);
        Assert.Equal(2, travelLogs[2].Day);
        Assert.Equal(0, travelLogs[2].Turn);
        Assert.Equal(
            "One trail day passes. 2.25 ride-day unit(s) remain and 3 day(s) remain on the route. The canteen has 4 spare charge(s) and can absorb 4 delay day(s).",
            travelLogs[2].Message);
    }

    [Fact]
    public void AdvanceJourneyDay_SixDayQuietJourney_AfterOneAdvance_RecordsSingleActiveDiaryDay()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);

        session.AdvanceJourneyDay();

        Assert.Equal(1, session.TravelDiaryDays.Count);
        var day = session.TravelDiaryDays[0];
        Assert.Equal(1, day.DayNumber);
        Assert.Equal("Pinecross", day.OriginTownName);
        Assert.Equal("Six Mile", day.DestinationTownName);
        Assert.Equal(TravelMode.Foot, day.StartingTravelMode);
        Assert.Equal(TravelMode.Foot, day.EndingTravelMode);
        Assert.Equal(JourneyStatus.Active, day.Status);
        Assert.Equal(3m, day.StartingRideDayDistance);
        Assert.Equal(2.25m, day.RemainingRideDayDistance);
        Assert.Equal(4, day.StartingDaysRemaining);
        Assert.Equal(3, day.RemainingDays);
        Assert.Equal(
            "I set out for Six Mile on a 2-day badlands ride, but without a horse it would take 4 days on foot. The route looks steady enough for now. I had enough water for the base trail, though the canteen still needed watching on a 4-day run. My food should have held if the trail behaved itself. I was traveling without a horse, so the road had to be enough.",
            day.OpeningNarration);
        Assert.Null(day.JourneyBeat);
        Assert.Null(day.ResourceBeat);
        Assert.Single(day.Entries);
        Assert.Equal("I found a seep under the rocks and topped off my canteen by 2 charge(s).", day.Entries[0]);
        Assert.Equal(0, day.HealthDelta);
        Assert.Equal(0m, day.WalletDelta);
        Assert.Equal(-1, day.FoodDelta);
        Assert.Equal(0, day.HorseFeedDelta);
        Assert.Equal(1, day.CanteenChargeDelta);
        Assert.Equal(0, day.AmmoSpent);
        Assert.Equal(0, day.HorseHungerDelta);
        Assert.Equal(0, day.HorseThirstDelta);
        Assert.Equal(0, day.HorseExhaustionDelta);
        Assert.Equal(0, day.DelayDays);
        Assert.Equal(0, day.HeatIncrease);
        Assert.Equal(1250, day.CurrentHealth);
        Assert.Equal(25m, day.CurrentWallet);
        Assert.Equal(7, day.CurrentFood);
        Assert.Equal(0, day.CurrentHorseFeed);
        Assert.Equal(7, day.CurrentCanteenCharges);
        Assert.Equal(0, day.CurrentAmmo);
        Assert.Equal(0, day.CurrentHeat);
        Assert.Equal(4, day.Warnings.Count);
        Assert.Null(day.HorseStateBefore);
        Assert.Null(day.HorseStateAfter);
        Assert.NotNull(day.TrailEvent);
        Assert.Equal(JourneyTrailEventId.LuckyWaterSeep, day.TrailEvent!.Id);
        Assert.Null(day.PendingEncounter);
        Assert.Null(day.EncounterResolution);
    }

    // ----- SixDayQuietJourney: full completion accumulation -----

    [Fact]
    public void SixDayQuietJourney_FullCompletion_AccumulatesNineTravelLogEntries()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);

        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);

        Assert.Equal(JourneyStatus.Completed, result.Status);
        var travelLogs = session.LogEntries
            .Where(e => e.Kind == GameLogEntryKind.Travel)
            .ToList();
        Assert.Equal(9, travelLogs.Count);
        Assert.Equal(
            "You set out from Pinecross toward Six Mile on foot. The route is 3 ride-day unit(s) and should take 4 day(s). The canteen has 2 spare charge(s) and can absorb 2 delay day(s).",
            travelLogs[0].Message);
        Assert.Equal(2, travelLogs[1].Day);
        Assert.Equal(0, travelLogs[1].Turn);
        Assert.Equal("I found a seep under the rocks and topped off my canteen by 2 charge(s).", travelLogs[1].Message);
        Assert.Equal(2, travelLogs[2].Day);
        Assert.Equal(0, travelLogs[2].Turn);
        Assert.Equal(
            "One trail day passes. 2.25 ride-day unit(s) remain and 3 day(s) remain on the route. The canteen has 4 spare charge(s) and can absorb 4 delay day(s).",
            travelLogs[2].Message);
        Assert.Equal(3, travelLogs[3].Day);
        Assert.Equal(0, travelLogs[3].Turn);
        Assert.Equal("The trail went quiet and the dust hung still.", travelLogs[3].Message);
        Assert.Equal(3, travelLogs[4].Day);
        Assert.Equal(0, travelLogs[4].Turn);
        Assert.Equal(
            "One trail day passes. 1.5 ride-day unit(s) remain and 2 day(s) remain on the route. The canteen has 4 spare charge(s) and can absorb 4 delay day(s).",
            travelLogs[4].Message);
        Assert.Equal(4, travelLogs[5].Day);
        Assert.Equal(0, travelLogs[5].Turn);
        Assert.Equal("The weather keeps the trail honest and the dust keeps my eyes narrowed.", travelLogs[5].Message);
        Assert.Equal(4, travelLogs[6].Day);
        Assert.Equal(0, travelLogs[6].Turn);
        Assert.Equal(
            "One trail day passes. 0.75 ride-day unit(s) remain and 1 day(s) remain on the route. The canteen has 4 spare charge(s) and can absorb 4 delay day(s).",
            travelLogs[6].Message);
        Assert.Equal(5, travelLogs[7].Day);
        Assert.Equal(0, travelLogs[7].Turn);
        Assert.Equal("The trail goes mean and I have to earn every mile the hard way.", travelLogs[7].Message);
        Assert.Equal(5, travelLogs[8].Day);
        Assert.Equal(0, travelLogs[8].Turn);
        Assert.Equal("You reach Six Mile after 4 trail day(s).", travelLogs[8].Message);
    }

    [Fact]
    public void SixDayQuietJourney_FullCompletion_AccumulatesFourDiaryDays()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);
        // Heat stays 0 throughout — travel no longer raises heat from route risk.
        // The day-plan seed changed from Wary/Hot progression to Calm throughout,
        // so the deterministic day plan changed. See ADR-0029.
        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);

        Assert.Equal(JourneyStatus.Completed, result.Status);
        Assert.Equal(4, session.TravelDiaryDays.Count);

        // Day 1 — LuckyWaterSeep, Active
        var day1 = session.TravelDiaryDays[0];
        Assert.Equal(1, day1.DayNumber);
        Assert.Equal(TravelMode.Foot, day1.StartingTravelMode);
        Assert.Equal(TravelMode.Foot, day1.EndingTravelMode);
        Assert.Equal(JourneyStatus.Active, day1.Status);
        Assert.Equal(3m, day1.StartingRideDayDistance);
        Assert.Equal(2.25m, day1.RemainingRideDayDistance);
        Assert.Equal(4, day1.StartingDaysRemaining);
        Assert.Equal(3, day1.RemainingDays);
        Assert.Single(day1.Entries);
        Assert.Equal("I found a seep under the rocks and topped off my canteen by 2 charge(s).", day1.Entries[0]);
        Assert.Equal(0, day1.HealthDelta);
        Assert.Equal(0m, day1.WalletDelta);
        Assert.Equal(-1, day1.FoodDelta);
        Assert.Equal(1, day1.CanteenChargeDelta);
        Assert.Equal(0, day1.HeatIncrease);
        Assert.Equal(1250, day1.CurrentHealth);
        Assert.Equal(25m, day1.CurrentWallet);
        Assert.Equal(7, day1.CurrentFood);
        Assert.Equal(7, day1.CurrentCanteenCharges);
        Assert.Equal(0, day1.CurrentHeat);
        Assert.NotNull(day1.TrailEvent);
        Assert.Equal(JourneyTrailEventId.LuckyWaterSeep, day1.TrailEvent!.Id);

        // Day 2 — quiet (no trail event), Active
        var day2 = session.TravelDiaryDays[1];
        Assert.Equal(2, day2.DayNumber);
        Assert.Equal(TravelMode.Foot, day2.StartingTravelMode);
        Assert.Equal(TravelMode.Foot, day2.EndingTravelMode);
        Assert.Equal(JourneyStatus.Active, day2.Status);
        Assert.Equal(2.25m, day2.StartingRideDayDistance);
        Assert.Equal(1.50m, day2.RemainingRideDayDistance);
        Assert.Equal(3, day2.StartingDaysRemaining);
        Assert.Equal(2, day2.RemainingDays);
        Assert.Single(day2.Entries);
        Assert.Equal("The trail went quiet and the dust hung still.", day2.Entries[0]);
        Assert.Equal(0, day2.HealthDelta);
        Assert.Equal(0m, day2.WalletDelta);
        Assert.Equal(-1, day2.FoodDelta);
        Assert.Equal(-1, day2.CanteenChargeDelta);
        Assert.Equal(0, day2.HeatIncrease);
        Assert.Equal(1250, day2.CurrentHealth);
        Assert.Equal(25m, day2.CurrentWallet);
        Assert.Equal(6, day2.CurrentFood);
        Assert.Equal(6, day2.CurrentCanteenCharges);
        Assert.Equal(0, day2.CurrentHeat);
        Assert.Null(day2.TrailEvent);

        // Day 3 — quiet (no trail event), Active
        var day3 = session.TravelDiaryDays[2];
        Assert.Equal(3, day3.DayNumber);
        Assert.Equal(TravelMode.Foot, day3.StartingTravelMode);
        Assert.Equal(TravelMode.Foot, day3.EndingTravelMode);
        Assert.Equal(JourneyStatus.Active, day3.Status);
        Assert.Equal(1.50m, day3.StartingRideDayDistance);
        Assert.Equal(0.75m, day3.RemainingRideDayDistance);
        Assert.Equal(2, day3.StartingDaysRemaining);
        Assert.Equal(1, day3.RemainingDays);
        Assert.Single(day3.Entries);
        Assert.Equal("The weather keeps the trail honest and the dust keeps my eyes narrowed.", day3.Entries[0]);
        Assert.Equal(0, day3.HealthDelta);
        Assert.Equal(0m, day3.WalletDelta);
        Assert.Equal(-1, day3.FoodDelta);
        Assert.Equal(-1, day3.CanteenChargeDelta);
        Assert.Equal(0, day3.HeatIncrease);
        Assert.Equal(1250, day3.CurrentHealth);
        Assert.Equal(25m, day3.CurrentWallet);
        Assert.Equal(5, day3.CurrentFood);
        Assert.Equal(5, day3.CurrentCanteenCharges);
        Assert.Equal(0, day3.CurrentHeat);
        Assert.Null(day3.TrailEvent);

        // Day 4 — BadLuckDustStorm, Completed
        var day4 = session.TravelDiaryDays[3];
        Assert.Equal(4, day4.DayNumber);
        Assert.Equal(TravelMode.Foot, day4.StartingTravelMode);
        Assert.Equal(TravelMode.Foot, day4.EndingTravelMode);
        Assert.Equal(JourneyStatus.Completed, day4.Status);
        Assert.Equal(0.75m, day4.StartingRideDayDistance);
        Assert.Equal(0m, day4.RemainingRideDayDistance);
        Assert.Equal(1, day4.StartingDaysRemaining);
        Assert.Equal(0, day4.RemainingDays);
        Assert.Single(day4.Entries);
        Assert.Equal("The trail goes mean and I have to earn every mile the hard way.", day4.Entries[0]);
        Assert.Equal(0, day4.HealthDelta);
        Assert.Equal(0m, day4.WalletDelta);
        Assert.Equal(-1, day4.FoodDelta);
        Assert.Equal(5, day4.CanteenChargeDelta);
        Assert.Equal(0, day4.HeatIncrease);
        Assert.Equal(1250, day4.CurrentHealth);
        Assert.Equal(25m, day4.CurrentWallet);
        Assert.Equal(4, day4.CurrentFood);
        Assert.Equal(10, day4.CurrentCanteenCharges);
        Assert.Equal(0, day4.CurrentHeat);
        Assert.NotNull(day4.TrailEvent);
        Assert.Equal(JourneyTrailEventId.BadLuckDustStorm, day4.TrailEvent!.Id);
    }

    // ----- SixDayQuietJourney: acknowledge preserves diary/log accumulation -----

    [Fact]
    public void AcknowledgeJourneyArrival_SixDayQuietJourney_PreservesDiaryAndLogAccumulation()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);

        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);

        Assert.Equal(JourneyStatus.Completed, result.Status);
        var travelLogCountBeforeAck = session.LogEntries.Count(e => e.Kind == GameLogEntryKind.Travel);
        var diaryCountBeforeAck = session.TravelDiaryDays.Count;

        var ackResult = session.AcknowledgeJourneyArrival();

        Assert.True(ackResult.Success);
        Assert.Null(session.Journey);
        Assert.Equal(travelLogCountBeforeAck,
            session.LogEntries.Count(e => e.Kind == GameLogEntryKind.Travel));
        Assert.Equal(diaryCountBeforeAck, session.TravelDiaryDays.Count);
        Assert.Equal(9, session.LogEntries.Count(e => e.Kind == GameLogEntryKind.Travel));
        Assert.Equal(4, session.TravelDiaryDays.Count);
    }

    // ----- Diary day opening narration only on first day -----

    [Fact]
    public void SixDayQuietJourney_OnlyFirstDiaryDayHasOpeningNarration()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);

        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);

        Assert.Equal(4, session.TravelDiaryDays.Count);
        Assert.NotNull(session.TravelDiaryDays[0].OpeningNarration);
        Assert.Null(session.TravelDiaryDays[1].OpeningNarration);
        Assert.Null(session.TravelDiaryDays[2].OpeningNarration);
        Assert.Null(session.TravelDiaryDays[3].OpeningNarration);
    }
}
