using WildBunch.Domain.Events;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

/// <summary>
/// Child domain component inside the session boundary that owns travel/journey
/// state and behavior. Receives narrow context records, returns results plus
/// events-to-produce. Does NOT reference the parent aggregate, produce events
/// directly, enter action context, adjust cash, or mutate CaseFile/TownVisitState/Player.
/// See BUNCH-119 and ADR-0002/ADR-0020.
/// </summary>
internal sealed class JourneyLoop
{
    private readonly List<TravelDiaryDayState> _travelDiaryDays = [];
    private readonly List<TravelJourneySnapshot> _completedJourneyHistory = [];
    private int _nextJourneySequence = 1;
    private DevTravelOverride? _pendingDevTravelOverride;
    private TravelJourney? _journey;

    internal JourneyLoop(
        TravelJourney? journey,
        IReadOnlyList<TravelJourneySnapshot>? completedJourneyHistory)
    {
        _journey = journey;
        if (completedJourneyHistory is not null)
        {
            _completedJourneyHistory.AddRange(completedJourneyHistory);
        }
        _nextJourneySequence = CalculateNextJourneySequence(journey, _completedJourneyHistory);
    }

    internal TravelJourney? Journey => _journey;
    internal IReadOnlyList<TravelDiaryDayState> TravelDiaryDays => _travelDiaryDays;
    internal IReadOnlyList<TravelJourneySnapshot> CompletedJourneyHistory => _completedJourneyHistory;
    internal int NextJourneySequence => _nextJourneySequence;
    internal DevTravelOverride? PendingDevTravelOverride => _pendingDevTravelOverride;

    // Command methods — filled in by Tasks 3–7
    // Apply methods — filled in by Task 8

    internal void Apply(JourneyStarted e)
    {
        _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
        _nextJourneySequence = e.JourneySnapshot.JourneySequence + 1;
        _travelDiaryDays.Clear();
    }

    internal void Apply(TravelDayAdvanced e)
    {
        _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
    }

    internal void Apply(TrailEventApplied e)
    {
        _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
    }

    internal void Apply(JourneyEncounterResolved e)
    {
        _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
    }

    internal void Apply(JourneyCompleted e)
    {
        _journey = TravelJourney.FromSnapshot(e.JourneySnapshot);
    }

    internal void Apply(JourneyArrivalAcknowledged e)
    {
        _completedJourneyHistory.Add(e.JourneySnapshot);
        _journey = null;
    }

    internal void Apply(DevTravelOverrideForced e)
    {
        _pendingDevTravelOverride = new DevTravelOverride(
            e.ForcedCategory,
            e.FoeProfile,
            e.EncounterMessage);
    }

    internal void Apply(DevTravelOverrideCleared e)
    {
        _pendingDevTravelOverride = null;
    }

    internal void Apply(DevTravelOverrideConsumed e)
    {
        _pendingDevTravelOverride = null;
    }

    internal JourneyLoopResult<bool> ForceDevTravelOverride(ForceDevTravelOverrideContext context)
    {
        if (_journey is null || _journey.Status != JourneyStatus.Active)
        {
            throw new InvalidOperationException("Cannot force a travel override without an active journey.");
        }
        if (_journey.PendingEncounter is not null)
        {
            throw new InvalidOperationException("Cannot force a travel override while an encounter is pending.");
        }

        var e = new DevTravelOverrideForced
        {
            ForcedCategory = context.Override.ForcedCategory,
            FoeProfile = context.Override.FoeProfile,
            EncounterMessage = context.Override.EncounterMessage
        };
        return new JourneyLoopResult<bool>(true, [e]);
    }

    internal JourneyLoopResult<bool> ClearDevTravelOverride()
    {
        if (_pendingDevTravelOverride is null)
        {
            return new JourneyLoopResult<bool>(true, []); // No-op, idempotent
        }

        return new JourneyLoopResult<bool>(true, [new DevTravelOverrideCleared()]);
    }

    internal JourneyLoopResult<TravelJourneyStepResult> StartJourney(StartJourneyContext context)
    {
        if (_journey is not null)
        {
            return new JourneyLoopResult<TravelJourneyStepResult>(
                TravelJourneyStepResult.Failed("You are already on the trail."),
                []);
        }

        var newJourney = TravelJourney.Start(
            context.Preview,
            _nextJourneySequence,
            BuildJourneyOpeningNarration(context.Preview));
        var startMessage = $"You set out from {context.Preview.OriginTownName} toward {context.Preview.DestinationTownName} {DescribeTravelMode(context.Preview.TravelMode)}. The route is {context.Preview.RideDayDistance:0.##} ride-day unit(s) and should take {context.Preview.ExpectedDays} day(s). {DescribeCanteenCoverage(context.Preview)}.";

        var e = new JourneyStarted
        {
            JourneySnapshot = newJourney.ToSnapshot(context.TravelRules),
            DiaryMessage = startMessage,
            PursuitHeat = 0
        };

        var result = new TravelJourneyStepResult(
            true,
            JourneyStatus.Active,
            startMessage,
            startMessage,
            0,
            newJourney.ToSnapshot(context.TravelRules));

        return new JourneyLoopResult<TravelJourneyStepResult>(result, [e]);
    }

    internal JourneyLoopResult<JourneyArrivalAcknowledgementResult> AcknowledgeJourneyArrival(
        AcknowledgeJourneyArrivalContext context)
    {
        if (_journey is null)
        {
            return new JourneyLoopResult<JourneyArrivalAcknowledgementResult>(
                JourneyArrivalAcknowledgementResult.Failed("No completed journey is waiting to be acknowledged."),
                []);
        }

        if (_journey.Status != JourneyStatus.Completed)
        {
            return new JourneyLoopResult<JourneyArrivalAcknowledgementResult>(
                JourneyArrivalAcknowledgementResult.Failed(
                    "The journey is not ready to be acknowledged.",
                    _journey.ToSnapshot(context.TravelRules)),
                []);
        }

        var completedSnapshot = _journey.ToSnapshot(context.TravelRules);
        var arrivalMessage = $"You step into {completedSnapshot.DestinationTownName} and put the trail behind you.";

        var e = new JourneyArrivalAcknowledged
        {
            JourneySequence = completedSnapshot.JourneySequence,
            JourneySnapshot = completedSnapshot,
            DiaryMessage = string.Empty
        };

        var result = new JourneyArrivalAcknowledgementResult(true, arrivalMessage, completedSnapshot);
        return new JourneyLoopResult<JourneyArrivalAcknowledgementResult>(result, [e]);
    }

    internal JourneyLoopResult<TravelJourneyStepResult> AdvanceJourneyDay(AdvanceJourneyDayContext context)
    {
        if (_journey is null)
        {
            return new JourneyLoopResult<TravelJourneyStepResult>(
                TravelJourneyStepResult.Failed("No active journey is underway."), []);
        }

        if (_journey.PendingEncounter is not null)
        {
            var encounterMessage = _journey.PendingEncounter.Message;
            return new JourneyLoopResult<TravelJourneyStepResult>(
                new TravelJourneyStepResult(
                    false,
                    _journey.Status,
                    "Resolve the pending encounter before you continue on the trail.",
                    encounterMessage,
                    0,
                    _journey.ToSnapshot(context.TravelRules)), []);
        }

        if (_journey.Status != JourneyStatus.Active)
        {
            return new JourneyLoopResult<TravelJourneyStepResult>(
                new TravelJourneyStepResult(
                    false,
                    _journey.Status,
                    "The journey is not active.",
                    "The journey is not active.",
                    0,
                    _journey.ToSnapshot(context.TravelRules)), []);
        }

        var runtime = new AdvanceDayRuntime(context);
        var travelDay = PrepareTravelDayAdvance(context, runtime);
        var dayEntries = new List<string>();
        var narrationMessages = new List<string>();
        JourneyTrailEventState? lastTrailEvent = null;

        while (_journey.CurrentDayPlan is not null && !_journey.CurrentDayPlan.IsComplete)
        {
            var currentEncounter = _journey.CurrentDayPlan.CurrentEncounter;
            if (currentEncounter is null)
            {
                break;
            }

            if (currentEncounter.RequiresChoice)
            {
                var pendingEncounter = currentEncounter.PendingEncounter!;
                _journey.MarkInterrupted(pendingEncounter);
                return new JourneyLoopResult<TravelJourneyStepResult>(
                    HandleInterruptedTravelDay(context, runtime, travelDay, pendingEncounter, lastTrailEvent, dayEntries, narrationMessages),
                    runtime.Events);
            }

            if (currentEncounter.TrailEvent is not null)
            {
                var trailEventApplication = ApplyTrailEvent(
                    context, runtime,
                    currentEncounter.TrailEvent,
                    travelDay.HorseLostMessage,
                    currentEncounter.Message);
                var trailEventMessage = PrependHorseLossMessage(
                    CombineHorseLossMessage(travelDay.HorseLostMessage, trailEventApplication.HorseLossMessage),
                    currentEncounter.Message);
                dayEntries.Add(trailEventMessage);
                lastTrailEvent = currentEncounter.TrailEvent;
            }
            else if (!string.IsNullOrWhiteSpace(currentEncounter.Message))
            {
                dayEntries.Add(currentEncounter.Message);
                narrationMessages.Add(currentEncounter.Message);
            }

            _journey.AdvanceCurrentDayPlan();
        }

        var journeySnapshot = _journey.ToSnapshot(context.TravelRules);
        var result = travelDay.Progress.Completed
            ? HandleCompletedTravelDay(context, runtime, travelDay, lastTrailEvent, dayEntries, narrationMessages, travelDay.Progress)
            : HandleOngoingTravelDay(context, runtime, travelDay, lastTrailEvent, dayEntries, narrationMessages, journeySnapshot);
        return new JourneyLoopResult<TravelJourneyStepResult>(result, runtime.Events);
    }

    internal JourneyLoopResult<JourneyEncounterResolutionResult> ResolveJourneyEncounter(ResolveJourneyEncounterContext context)
    {
        if (_journey is null)
        {
            return new JourneyLoopResult<JourneyEncounterResolutionResult>(
                JourneyEncounterResolutionResult.Failed("No active journey is underway.", JourneyStatus.Failed), []);
        }

        if (_journey.PendingEncounter is null)
        {
            return new JourneyLoopResult<JourneyEncounterResolutionResult>(
                JourneyEncounterResolutionResult.Failed("There is no pending encounter to resolve.", _journey.Status, _journey.ToSnapshot(context.TravelRules)), []);
        }

        if (_journey.Status != JourneyStatus.Interrupted)
        {
            return new JourneyLoopResult<JourneyEncounterResolutionResult>(
                JourneyEncounterResolutionResult.Failed("The encounter is not waiting to be resolved.", _journey.Status, _journey.ToSnapshot(context.TravelRules)), []);
        }

        if (string.IsNullOrWhiteSpace(context.ChoiceId))
        {
            return new JourneyLoopResult<JourneyEncounterResolutionResult>(
                JourneyEncounterResolutionResult.Failed("Choose how you want to answer the encounter.", _journey.Status, _journey.ToSnapshot(context.TravelRules)), []);
        }

        var runtime = new AdvanceDayRuntime(context);
        var encounter = _journey.PendingEncounter;
        var currentDayEncounter = _journey.CurrentDayPlan?.CurrentEncounter?.PendingEncounter;
        if (encounter is null)
        {
            encounter = currentDayEncounter;
        }
        else if (encounter.FoeProfile is null && currentDayEncounter?.FoeProfile is not null)
        {
            encounter = currentDayEncounter;
        }

        if (encounter is not null && !ReferenceEquals(encounter, _journey.PendingEncounter))
        {
            _journey.UpdatePendingEncounter(encounter);
        }

        var startingState = RebuildCurrentTravelDiaryBaselineState();
        if (encounter is null)
        {
            return new JourneyLoopResult<JourneyEncounterResolutionResult>(
                JourneyEncounterResolutionResult.Failed("The encounter is not waiting to be resolved.", _journey.Status, _journey.ToSnapshot(context.TravelRules)), []);
        }

        if (encounter.Kind == "foe" && encounter.FoeProfile is null)
        {
            encounter = RecoverFoeProfile(context, encounter);
            _journey.UpdatePendingEncounter(encounter);
        }

        var resolvedChoiceId = context.ChoiceId.Trim().ToLowerInvariant();
        if (resolvedChoiceId == "bribe" && encounter.HiddenState?.BribeLockedOut == true)
        {
            return new JourneyLoopResult<JourneyEncounterResolutionResult>(
                JourneyEncounterResolutionResult.Failed("The rider will not take any more money.", _journey.Status, _journey.ToSnapshot(context.TravelRules)), []);
        }

        if (!encounter.Choices.Any(choice => string.Equals(choice.Id, context.ChoiceId, StringComparison.OrdinalIgnoreCase)))
        {
            return new JourneyLoopResult<JourneyEncounterResolutionResult>(
                JourneyEncounterResolutionResult.Failed("That is not a lawful way to answer this encounter.", _journey.Status, _journey.ToSnapshot(context.TravelRules)), []);
        }

        var resolvedChoiceLabel = encounter.Choices.First(choice => string.Equals(choice.Id, resolvedChoiceId, StringComparison.OrdinalIgnoreCase)).Label;
        var hiddenState = encounter.HiddenState ?? new JourneyEncounterHiddenState();
        var resolutionAttemptIndex = resolvedChoiceId switch
        {
            "bribe" => hiddenState.BribeOffersMade + 1,
            "run" => hiddenState.ChaseFatigue + 1,
            "fight" => 1 + hiddenState.Annoyance + (hiddenState.Shaken ? 1 : 0),
            _ => encounter.ResolutionAttempts + 1
        };
        var rollSeed = JourneyEncounterResolutionEngine.ComposeRollSeed(
            encounter,
            resolvedChoiceId,
            resolutionAttemptIndex,
            string.Join(
                "|",
                _journey.TravelMode,
                _journey.RemainingRideDayDistance,
                _journey.RemainingDays,
                runtime.Health,
                runtime.WalletCash,
                runtime.RevolverAmmo + runtime.RifleAmmo,
                context.CurrentHeat));
        var roll = context.ForcedRoll ?? JourneyEncounterResolutionEngine.Roll(rollSeed, "resolution");
        var dayEntries = new List<string>();

        JourneyEncounterResolutionResult resolutionResult;
        switch (resolvedChoiceId)
        {
            case "run":
                resolutionResult = ResolveRun(context, runtime, encounter, resolvedChoiceId, resolvedChoiceLabel, roll, startingState, dayEntries);
                break;
            case "fight":
                resolutionResult = ResolveFight(context, runtime, encounter, resolvedChoiceId, resolvedChoiceLabel, roll, startingState, dayEntries);
                break;
            case "bribe":
                resolutionResult = ResolveBribe(context, runtime, encounter, resolvedChoiceId, resolvedChoiceLabel, roll, startingState, dayEntries);
                break;
            default:
                return new JourneyLoopResult<JourneyEncounterResolutionResult>(
                    JourneyEncounterResolutionResult.Failed("That choice is not available for this encounter.", _journey.Status, _journey.ToSnapshot(context.TravelRules)), []);
        }

        return new JourneyLoopResult<JourneyEncounterResolutionResult>(resolutionResult, runtime.Events);
    }

    private JourneyEncounterResolutionResult ResolveRun(
        AdvanceJourneyDayContext baseContext,
        AdvanceDayRuntime runtime,
        JourneyEncounterState encounter,
        string resolvedChoiceId,
        string resolvedChoiceLabel,
        ulong roll,
        TravelDiaryBaselineState startingState,
        List<string> dayEntries)
    {
        var plan = JourneyEncounterResolutionEngine.ResolveRun(
            encounter,
            _journey!.TravelMode,
            _journey.HorseState,
            runtime.Health,
            baseContext.TravelRules,
            roll);

        if (plan.HealthDelta != 0)
        {
            runtime.Health += plan.HealthDelta;
        }

        var horseLossMessage = plan.HorseExhaustionDelta > 0
            ? ApplyEncounterHorsePressure(baseContext.TravelRules, runtime, plan.HorseExhaustionDelta)
            : string.Empty;

        var runMessage = PrependHorseLossMessage(horseLossMessage, plan.Message);
        dayEntries.Add(plan.Message);

        if (!plan.Resolved)
        {
            _journey.UpdatePendingEncounter(plan.UpdatedEncounter);
            PersistLatestTravelDiaryDay(baseContext.TravelRules, runtime, startingState, dayEntries, _journey.PendingEncounter);
            runtime.AddEvent(new JourneyEncounterResolved
            {
                ChoiceId = resolvedChoiceId,
                ChoiceLabel = resolvedChoiceLabel,
                Resolved = false,
                PlayerHealth = runtime.Health,
                WalletCash = runtime.WalletCash,
                AmmoSpent = 0,
                StolenItemKind = null,
                StolenItemQuantity = 0,
                PursuitHeat = baseContext.CurrentHeat,
                HorseExhaustionDelta = plan.HorseExhaustionDelta,
                ContinuedOnFoot = _journey.TravelMode == TravelMode.Foot,
                JourneySnapshot = _journey.ToSnapshot(baseContext.TravelRules),
                DiaryMessage = runMessage,
                DayCompleted = false,
                JourneyCompleted = false,
                DayEntries = _travelDiaryDays.Count > 0 ? _travelDiaryDays[^1].Entries : dayEntries
            });
            return new JourneyEncounterResolutionResult(false, true, _journey.Status, runMessage, _journey.ToSnapshot(baseContext.TravelRules));
        }

        _journey.ResumeFromEncounter();
        var resolution = new TravelDiaryEncounterResolutionState(
            resolvedChoiceId, resolvedChoiceLabel, plan.HealthDelta, 0m, 0, plan.HeatIncrease, plan.HorseExhaustionDelta, _journey.TravelMode == TravelMode.Foot);
        _journey.RecordCurrentDayEncounterResolution(resolution);
        _journey.AdvanceCurrentDayPlan();
        var resolutionResult = ContinueCurrentDayAfterEncounterResolution(baseContext, runtime, encounter, startingState, dayEntries, resolution);
        ProduceEncounterResolvedEvent(baseContext, runtime, resolvedChoiceId, resolvedChoiceLabel, 0, null, 0, plan.HorseExhaustionDelta, resolutionResult);
        return resolutionResult;
    }

    private JourneyEncounterResolutionResult ResolveFight(
        AdvanceJourneyDayContext baseContext,
        AdvanceDayRuntime runtime,
        JourneyEncounterState encounter,
        string resolvedChoiceId,
        string resolvedChoiceLabel,
        ulong roll,
        TravelDiaryBaselineState startingState,
        List<string> dayEntries)
    {
        var availableAmmo = runtime.RevolverAmmo + runtime.RifleAmmo;
        if (availableAmmo == 0 && !runtime.HasKnife)
        {
            return JourneyEncounterResolutionResult.Failed("You need a knife or firearm ammo to stand and fight.", _journey!.Status, _journey.ToSnapshot(baseContext.TravelRules));
        }

        var bulletSpend = baseContext is ResolveJourneyEncounterContext rjc ? rjc.BulletSpend : null;
        var plan = JourneyEncounterResolutionEngine.ResolveFight(
            encounter,
            runtime.Health,
            baseContext.TravelRules,
            availableAmmo,
            runtime.HasKnife,
            bulletSpend,
            roll);

        if (plan.AmmoSpent > 0)
        {
            runtime.SpendAmmo(plan.AmmoSpent);
        }

        if (plan.HealthDelta != 0)
        {
            runtime.Health += plan.HealthDelta;
        }

        dayEntries.Add(plan.Message);

        if (!plan.Resolved)
        {
            _journey!.UpdatePendingEncounter(plan.UpdatedEncounter);
            PersistLatestTravelDiaryDay(baseContext.TravelRules, runtime, startingState, dayEntries, _journey.PendingEncounter);
            runtime.AddEvent(new JourneyEncounterResolved
            {
                ChoiceId = resolvedChoiceId,
                ChoiceLabel = resolvedChoiceLabel,
                Resolved = false,
                PlayerHealth = runtime.Health,
                WalletCash = runtime.WalletCash,
                AmmoSpent = plan.AmmoSpent,
                StolenItemKind = null,
                StolenItemQuantity = 0,
                PursuitHeat = baseContext.CurrentHeat,
                HorseExhaustionDelta = plan.HorseExhaustionDelta,
                ContinuedOnFoot = plan.ContinuedOnFoot,
                JourneySnapshot = _journey.ToSnapshot(baseContext.TravelRules),
                DiaryMessage = plan.Message,
                DayCompleted = false,
                JourneyCompleted = false,
                DayEntries = _travelDiaryDays.Count > 0 ? _travelDiaryDays[^1].Entries : dayEntries
            });
            return new JourneyEncounterResolutionResult(false, true, _journey.Status, plan.Message, _journey.ToSnapshot(baseContext.TravelRules));
        }

        _journey!.ResumeFromEncounter();
        var resolution = new TravelDiaryEncounterResolutionState(
            resolvedChoiceId, resolvedChoiceLabel, plan.HealthDelta, 0m, plan.AmmoSpent, plan.HeatIncrease, plan.HorseExhaustionDelta, plan.ContinuedOnFoot);
        _journey.RecordCurrentDayEncounterResolution(resolution);
        _journey.AdvanceCurrentDayPlan();
        var resolutionResult = ContinueCurrentDayAfterEncounterResolution(baseContext, runtime, encounter, startingState, dayEntries, resolution);
        ProduceEncounterResolvedEvent(baseContext, runtime, resolvedChoiceId, resolvedChoiceLabel, plan.AmmoSpent, null, 0, plan.HorseExhaustionDelta, resolutionResult);
        return resolutionResult;
    }

    private JourneyEncounterResolutionResult ResolveBribe(
        AdvanceJourneyDayContext baseContext,
        AdvanceDayRuntime runtime,
        JourneyEncounterState encounter,
        string resolvedChoiceId,
        string resolvedChoiceLabel,
        ulong roll,
        TravelDiaryBaselineState startingState,
        List<string> dayEntries)
    {
        var bribeOffer = (baseContext is ResolveJourneyEncounterContext rjc ? rjc.BribeAmount : null) ?? baseContext.TravelRules.EncounterBribeCash;
        if (bribeOffer < 0m)
        {
            bribeOffer = 0m;
        }

        if (runtime.WalletCash < bribeOffer)
        {
            return JourneyEncounterResolutionResult.Failed($"You need ${bribeOffer:0.00} to bribe your way through.", _journey!.Status, _journey.ToSnapshot(baseContext.TravelRules));
        }

        var plan = JourneyEncounterResolutionEngine.ResolveBribe(
            encounter,
            runtime.WalletCash,
            baseContext.TravelRules,
            bribeOffer,
            _journey!.FoodRemaining,
            _journey.HorseFeedRemaining,
            runtime.RevolverAmmo,
            runtime.RifleAmmo,
            roll);

        if (plan.WalletDelta != 0m)
        {
            runtime.WalletCash += plan.WalletDelta;
        }

        if (plan.StolenItemKind is not null && plan.StolenItemQuantity > 0)
        {
            if (plan.StolenItemKind == ItemKind.RevolverAmmo)
            {
                runtime.RevolverAmmo = Math.Max(0, runtime.RevolverAmmo - plan.StolenItemQuantity);
            }
            else if (plan.StolenItemKind == ItemKind.RifleAmmo)
            {
                runtime.RifleAmmo = Math.Max(0, runtime.RifleAmmo - plan.StolenItemQuantity);
            }
        }

        if (plan.HealthDelta != 0)
        {
            runtime.Health += plan.HealthDelta;
        }

        dayEntries.Add(plan.Message);

        var retaliated = !plan.Resolved && (plan.HealthDelta < 0 || plan.StolenItemKind is not null || plan.WalletDelta < -bribeOffer);
        if (!plan.Resolved && !retaliated)
        {
            _journey!.UpdatePendingEncounter(plan.UpdatedEncounter);
            PersistLatestTravelDiaryDay(baseContext.TravelRules, runtime, startingState, dayEntries, _journey.PendingEncounter);
            runtime.AddEvent(new JourneyEncounterResolved
            {
                ChoiceId = resolvedChoiceId,
                ChoiceLabel = resolvedChoiceLabel,
                Resolved = false,
                PlayerHealth = runtime.Health,
                WalletCash = runtime.WalletCash,
                AmmoSpent = 0,
                StolenItemKind = plan.StolenItemKind,
                StolenItemQuantity = plan.StolenItemQuantity,
                PursuitHeat = baseContext.CurrentHeat,
                HorseExhaustionDelta = plan.HorseExhaustionDelta,
                ContinuedOnFoot = _journey.TravelMode == TravelMode.Foot,
                JourneySnapshot = _journey.ToSnapshot(baseContext.TravelRules),
                DiaryMessage = plan.Message,
                DayCompleted = false,
                JourneyCompleted = false,
                DayEntries = _travelDiaryDays.Count > 0 ? _travelDiaryDays[^1].Entries : dayEntries
            });
            return new JourneyEncounterResolutionResult(false, true, _journey.Status, plan.Message, _journey.ToSnapshot(baseContext.TravelRules));
        }

        _journey!.ResumeFromEncounter();
        var resolution = new TravelDiaryEncounterResolutionState(
            resolvedChoiceId, resolvedChoiceLabel, plan.HealthDelta, plan.WalletDelta, 0, plan.HeatIncrease, plan.HorseExhaustionDelta, _journey.TravelMode == TravelMode.Foot);
        _journey.RecordCurrentDayEncounterResolution(resolution);
        _journey.AdvanceCurrentDayPlan();
        var resolutionResult = ContinueCurrentDayAfterEncounterResolution(baseContext, runtime, encounter, startingState, dayEntries, resolution);
        ProduceEncounterResolvedEvent(baseContext, runtime, resolvedChoiceId, resolvedChoiceLabel, 0, plan.StolenItemKind, plan.StolenItemQuantity, plan.HorseExhaustionDelta, resolutionResult);
        return retaliated ? resolutionResult with { Success = false } : resolutionResult;
    }

    private void ProduceEncounterResolvedEvent(
        AdvanceJourneyDayContext baseContext,
        AdvanceDayRuntime runtime,
        string choiceId,
        string choiceLabel,
        int ammoSpent,
        ItemKind? stolenItemKind,
        int stolenItemQuantity,
        int horseExhaustionDelta,
        JourneyEncounterResolutionResult resolutionResult)
    {
        var journeyCompleted = resolutionResult.Status == JourneyStatus.Completed;
        var dayCompleted = journeyCompleted || (_journey?.CurrentDayPlan?.IsComplete ?? false);
        runtime.AddEvent(new JourneyEncounterResolved
        {
            ChoiceId = choiceId,
            ChoiceLabel = choiceLabel,
            Resolved = true,
            PlayerHealth = runtime.Health,
            WalletCash = runtime.WalletCash,
            AmmoSpent = ammoSpent,
            StolenItemKind = stolenItemKind,
            StolenItemQuantity = stolenItemQuantity,
            PursuitHeat = baseContext.CurrentHeat,
            HorseExhaustionDelta = horseExhaustionDelta,
            ContinuedOnFoot = _journey?.TravelMode == TravelMode.Foot,
            JourneySnapshot = _journey?.ToSnapshot(baseContext.TravelRules) ?? resolutionResult.Journey!,
            DiaryMessage = resolutionResult.Message,
            DayCompleted = dayCompleted,
            JourneyCompleted = journeyCompleted,
            AdditionalDiaryMessages = resolutionResult.AdditionalDiaryMessages ?? [],
            DayEntries = _travelDiaryDays.Count > 0 ? _travelDiaryDays[^1].Entries : []
        });
    }

    private JourneyEncounterResolutionResult ContinueCurrentDayAfterEncounterResolution(
        AdvanceJourneyDayContext baseContext,
        AdvanceDayRuntime runtime,
        JourneyEncounterState resolvedEncounter,
        TravelDiaryBaselineState startingState,
        List<string> dayEntries,
        TravelDiaryEncounterResolutionState resolution)
    {
        var narrationMessages = new List<string>();
        while (_journey is not null && _journey.CurrentDayPlan is not null && !_journey.CurrentDayPlan.IsComplete)
        {
            var currentEncounter = _journey.CurrentDayPlan.CurrentEncounter;
            if (currentEncounter is null)
            {
                break;
            }

            if (currentEncounter.RequiresChoice)
            {
                var pendingEncounter = currentEncounter.PendingEncounter!;
                _journey.MarkInterrupted(pendingEncounter);
                var pendingMessage = pendingEncounter.Message;
                dayEntries.Add(pendingMessage);

                var pendingSnapshot = _journey.ToSnapshot(baseContext.TravelRules);
                var pendingResources = CaptureTravelResources(baseContext, runtime);
                PersistLatestTravelDiaryDay(baseContext.TravelRules, runtime, startingState, dayEntries, pendingEncounter, resolution, pendingSnapshot, pendingResources);

                return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Interrupted, pendingMessage, pendingSnapshot, narrationMessages);
            }

            if (currentEncounter.TrailEvent is not null)
            {
                var trailEventApplication = ApplyTrailEvent(
                    baseContext, runtime,
                    currentEncounter.TrailEvent,
                    prefixHorseLostMessage: string.Empty,
                    currentEncounter.Message);
                var encounterMessage = PrependHorseLossMessage(trailEventApplication.HorseLossMessage, currentEncounter.Message);
                dayEntries.Add(encounterMessage);
            }
            else if (!string.IsNullOrWhiteSpace(currentEncounter.Message))
            {
                dayEntries.Add(currentEncounter.Message);
                narrationMessages.Add(currentEncounter.Message);
            }

            _journey.AdvanceCurrentDayPlan();
        }

        var journeySnapshot = _journey!.ToSnapshot(baseContext.TravelRules);
        var currentResources = CaptureTravelResources(baseContext, runtime);
        PersistLatestTravelDiaryDay(baseContext.TravelRules, runtime, startingState, dayEntries, resolvedEncounter, resolution, journeySnapshot, currentResources);

        if (_journey.CurrentDayPlan?.IsComplete == true)
        {
            _journey.SetCurrentDayPlan(null);
        }

        if (_journey.RemainingDays == 0 && _journey.RemainingRideDayDistance == 0)
        {
            var destinationTownName = _journey.Preview.DestinationTownName;
            var destinationTownId = _journey.Preview.DestinationTownId;
            var completedSnapshot = CompleteJourneyAtDestination(baseContext.TravelRules, runtime);
            runtime.AddEvent(new JourneyCompleted
            {
                JourneySnapshot = completedSnapshot,
                DestinationTownId = destinationTownId,
                DestinationTownName = destinationTownName,
                DiaryMessage = string.Empty
            });
            currentResources = CaptureTravelResources(baseContext, runtime);
            PersistLatestTravelDiaryDay(baseContext.TravelRules, runtime, startingState, Array.Empty<string>(), resolvedEncounter, resolution, completedSnapshot, currentResources);
            return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Completed, $"You clear the remaining trail and reach {destinationTownName}.", completedSnapshot, narrationMessages);
        }

        _journey.ResumeFromEncounter();
        return new JourneyEncounterResolutionResult(true, true, JourneyStatus.Active, "You push the rider behind you and keep moving.", journeySnapshot, narrationMessages);
    }

    private TravelDayAdvanceState PrepareTravelDayAdvance(AdvanceJourneyDayContext context, AdvanceDayRuntime runtime)
    {
        var startingResources = CaptureTravelResources(context, runtime);
        var startingState = new TravelDiaryBaselineState(
            _journey!.TravelMode,
            _journey.RemainingRideDayDistance,
            _journey.RemainingDays,
            _journey.DelayDays,
            startingResources);

        if (_journey.TravelMode == TravelMode.Mounted && !context.Capabilities.MountedTravelAvailable)
        {
            _journey.RecalculatePacing(TravelMode.Foot);
        }

        if (_journey.FoodRemaining > 0)
        {
            _journey.ConsumeFood();
        }

        var upkeep = JourneyUpkeepRules.ApplyDailyUpkeep(
            _journey.Preview.RouteProfile.Terrain,
            _journey.Preview.RouteProfile.WaterFeature,
            _journey.HorseState,
            context.CanteenState,
            _journey.HorseFeedRemaining,
            context.TravelRules);

        if (upkeep.HorseFeedConsumed > 0)
        {
            _journey.ConsumeHorseFeed(upkeep.HorseFeedConsumed);
        }

        if (upkeep.CanteenState is not null)
        {
            _journey.SetCanteenCharges(upkeep.CanteenState.Charges);
        }
        else
        {
            _journey.SetCanteenCharges(0);
        }

        if (upkeep.HorseState is not null)
        {
            _journey.SetHorseState(upkeep.HorseState);
        }

        var horseLostMessage = string.Empty;
        if (upkeep.MountedTravelLost && _journey.TravelMode == TravelMode.Mounted)
        {
            horseLostMessage = DescribeHorseLoss(upkeep.HorseState, context.TravelRules);
            _journey.RecalculatePacing(TravelMode.Foot);
        }

        var newDay = context.ClockDay;
        var progress = _journey.AdvanceOneDay();

        var pendingOverride = _pendingDevTravelOverride;
        TravelDayPlanState dayPlan;
        if (pendingOverride is not null)
        {
            dayPlan = TravelDayPlanFactory.CreateForcedDayPlan(pendingOverride, _journey.DaysTravelled, context.TravelRules);
            runtime.AddEvent(new DevTravelOverrideConsumed());
        }
        else
        {
            var generationContext = CreateTravelDayGenerationContext(context, runtime);
            dayPlan = TravelDayPlanGenerator.Generate(generationContext);
        }
        _journey.SetCurrentDayPlan(dayPlan);

        return new TravelDayAdvanceState(
            startingState,
            horseLostMessage,
            progress,
            newDay,
            context.CurrentHeat);
    }

    private TravelJourneyStepResult HandleInterruptedTravelDay(
        AdvanceJourneyDayContext context,
        AdvanceDayRuntime runtime,
        TravelDayAdvanceState travelDay,
        JourneyEncounterState pendingEncounter,
        JourneyTrailEventState? lastTrailEvent,
        List<string> dayEntries,
        List<string> narrationMessages)
    {
        var horseLostMessage = travelDay.HorseLostMessage;
        var encounterMessage = PrependHorseLossMessage(horseLostMessage, pendingEncounter.Message);
        dayEntries.Add(encounterMessage);
        dayEntries.Add("I could run, fight, or bribe my way through.");

        var interruptedSnapshot = _journey!.ToSnapshot(context.TravelRules);
        runtime.AddEvent(new TravelDayAdvanced
        {
            Day = travelDay.NewDay,
            JourneySnapshot = interruptedSnapshot,
            HealthDelta = 0,
            PursuitHeat = travelDay.PursuitHeat,
            DayOutcome = TravelDayOutcome.Interrupted,
            DiaryMessage = encounterMessage,
            HorseLostMessage = horseLostMessage,
            AdditionalDiaryMessages = narrationMessages,
            DayEntries = dayEntries
        });

        AppendTravelDiaryDay(context, runtime, interruptedSnapshot, travelDay.StartingState, pendingEncounter: pendingEncounter, entries: dayEntries);

        return new TravelJourneyStepResult(
            false,
            _journey.Status,
            horseLostMessage.Length == 0
                ? "Your journey is interrupted by a trail encounter."
                : $"Your journey is interrupted by a trail encounter. {horseLostMessage}",
            encounterMessage,
            0,
            interruptedSnapshot,
            lastTrailEvent);
    }

    private TravelJourneyStepResult HandleCompletedTravelDay(
        AdvanceJourneyDayContext context,
        AdvanceDayRuntime runtime,
        TravelDayAdvanceState travelDay,
        JourneyTrailEventState? lastTrailEvent,
        List<string> dayEntries,
        List<string> narrationMessages,
        JourneyProgress progress)
    {
        var horseLostMessage = travelDay.HorseLostMessage;
        var destinationTownName = _journey!.Preview.DestinationTownName;
        var destinationTownId = _journey.Preview.DestinationTownId;
        var completedSnapshot = CompleteJourneyAtDestination(context.TravelRules, runtime);
        var completionMessage = horseLostMessage.Length == 0
            ? $"You reach {destinationTownName}."
            : $"{horseLostMessage} You reach {destinationTownName}.";
        var arrivalMessage = horseLostMessage.Length == 0
            ? $"You reach {destinationTownName} after {completedSnapshot.DaysTravelled} trail day(s)."
            : $"{horseLostMessage} You reach {destinationTownName} after {completedSnapshot.DaysTravelled} trail day(s).";

        runtime.AddEvent(new TravelDayAdvanced
        {
            Day = travelDay.NewDay,
            JourneySnapshot = completedSnapshot,
            HealthDelta = 0,
            PursuitHeat = travelDay.PursuitHeat,
            DayOutcome = TravelDayOutcome.Completed,
            DiaryMessage = arrivalMessage,
            HorseLostMessage = horseLostMessage,
            AdditionalDiaryMessages = narrationMessages,
            DayEntries = dayEntries
        });

        runtime.AddEvent(new JourneyCompleted
        {
            JourneySnapshot = completedSnapshot,
            DestinationTownId = destinationTownId,
            DestinationTownName = destinationTownName,
            DiaryMessage = string.Empty
        });

        AppendTravelDiaryDay(context, runtime, completedSnapshot, travelDay.StartingState, trailEvent: lastTrailEvent, entries: dayEntries.Count == 0 ? null : dayEntries);
        _journey!.SetCurrentDayPlan(null);

        return new TravelJourneyStepResult(
            true,
            JourneyStatus.Completed,
            completionMessage,
            horseLostMessage.Length == 0
                ? $"You reach {destinationTownName} after {progress.RideDayDistanceTravelled:0.##} ride-day unit(s)."
                : $"{horseLostMessage} You reach {destinationTownName} after {progress.RideDayDistanceTravelled:0.##} ride-day unit(s).",
            travelDay.PursuitHeat,
            completedSnapshot,
            lastTrailEvent);
    }

    private TravelJourneyStepResult HandleOngoingTravelDay(
        AdvanceJourneyDayContext context,
        AdvanceDayRuntime runtime,
        TravelDayAdvanceState travelDay,
        JourneyTrailEventState? lastTrailEvent,
        List<string> dayEntries,
        List<string> narrationMessages,
        TravelJourneySnapshot journeySnapshot)
    {
        var horseLostMessage = travelDay.HorseLostMessage;
        var ongoingMessage = horseLostMessage.Length == 0
            ? $"One trail day passes. {journeySnapshot.RemainingRideDayDistance:0.##} ride-day unit(s) remain and {_journey!.RemainingDays} day(s) remain on the route. {DescribeCanteenCoverage(journeySnapshot)}."
            : $"{horseLostMessage} One trail day passes on foot. {journeySnapshot.RemainingRideDayDistance:0.##} ride-day unit(s) remain and {_journey!.RemainingDays} day(s) remain on the route. {DescribeCanteenCoverage(journeySnapshot)}.";

        runtime.AddEvent(new TravelDayAdvanced
        {
            Day = travelDay.NewDay,
            JourneySnapshot = journeySnapshot,
            HealthDelta = 0,
            PursuitHeat = travelDay.PursuitHeat,
            DayOutcome = TravelDayOutcome.Ongoing,
            DiaryMessage = ongoingMessage,
            HorseLostMessage = horseLostMessage,
            AdditionalDiaryMessages = narrationMessages,
            DayEntries = dayEntries
        });

        AppendTravelDiaryDay(context, runtime, journeySnapshot, travelDay.StartingState, trailEvent: lastTrailEvent, entries: dayEntries.Count == 0 ? null : dayEntries);
        _journey!.SetCurrentDayPlan(null);

        return new TravelJourneyStepResult(
            true,
            JourneyStatus.Active,
            ongoingMessage,
            ongoingMessage,
            travelDay.PursuitHeat,
            journeySnapshot,
            lastTrailEvent);
    }

    private TrailEventApplicationResult ApplyTrailEvent(
        AdvanceJourneyDayContext context,
        AdvanceDayRuntime runtime,
        JourneyTrailEventState trailEvent,
        string prefixHorseLostMessage,
        string encounterMessage)
    {
        ArgumentNullException.ThrowIfNull(trailEvent);

        if (trailEvent.WalletDelta != 0m)
        {
            runtime.WalletCash += trailEvent.WalletDelta;
        }

        if (trailEvent.FoodDelta != 0)
        {
            _journey!.AdjustFood(trailEvent.FoodDelta);
        }

        if (trailEvent.CanteenChargeDelta != 0)
        {
            var nextCharges = _journey!.AvailableCanteenCharges + trailEvent.CanteenChargeDelta;
            if (nextCharges < 0)
            {
                nextCharges = 0;
            }
            if (runtime.CanteenCapacity > 0)
            {
                nextCharges = Math.Min(runtime.CanteenCapacity, nextCharges);
            }
            _journey.SetCanteenCharges(nextCharges);
        }

        if (trailEvent.HorseHungerDelta != 0 || trailEvent.HorseThirstDelta != 0 || trailEvent.HorseExhaustionDelta != 0)
        {
            if (_journey!.HorseState is { } horseState)
            {
                _journey.SetHorseState(ApplyHorseDelta(horseState, trailEvent));
            }
        }

        if (trailEvent.DelayDays != 0)
        {
            _journey!.AddDelayDays(trailEvent.DelayDays);
        }

        var horseLossMessage = string.Empty;
        TravelMode? travelModeChangedTo = null;
        if (_journey!.TravelMode == TravelMode.Mounted && _journey.HorseState?.CanProvideMountedTravelFor(context.TravelRules) == false)
        {
            horseLossMessage = DescribeHorseLoss(_journey.HorseState, context.TravelRules);
            _journey.RecalculatePacing(TravelMode.Foot);
            travelModeChangedTo = TravelMode.Foot;
        }

        var fullDiaryMessage = PrependHorseLossMessage(
            CombineHorseLossMessage(prefixHorseLostMessage, horseLossMessage),
            encounterMessage);

        var postEventSnapshot = _journey.ToSnapshot(context.TravelRules);
        runtime.AddEvent(new TrailEventApplied
        {
            JourneySnapshot = postEventSnapshot,
            TrailEventKind = trailEvent.Kind,
            TrailEventId = trailEvent.Id,
            Title = trailEvent.Title,
            Message = trailEvent.Message,
            WalletDelta = trailEvent.WalletDelta,
            WalletCash = runtime.WalletCash,
            FoodDelta = trailEvent.FoodDelta,
            CanteenChargeDelta = trailEvent.CanteenChargeDelta,
            HorseHungerDelta = trailEvent.HorseHungerDelta,
            HorseThirstDelta = trailEvent.HorseThirstDelta,
            HorseExhaustionDelta = trailEvent.HorseExhaustionDelta,
            DelayDays = trailEvent.DelayDays,
            HeatIncrease = trailEvent.HeatIncrease,
            PursuitHeat = context.CurrentHeat,
            TravelModeChangedTo = travelModeChangedTo,
            DiaryMessage = fullDiaryMessage,
            HorseLostMessage = horseLossMessage
        });

        return new TrailEventApplicationResult(horseLossMessage);
    }

    private string ApplyEncounterHorsePressure(TravelRulesProfile travelRules, AdvanceDayRuntime runtime, int exhaustionIncrease)
    {
        if (exhaustionIncrease <= 0)
        {
            return string.Empty;
        }

        var horseState = _journey!.HorseState;
        if (horseState is null)
        {
            return string.Empty;
        }

        var nextHorseState = horseState.IncreaseExhaustion(exhaustionIncrease);
        _journey.SetHorseState(nextHorseState);

        if (_journey.TravelMode == TravelMode.Mounted && !nextHorseState.CanProvideMountedTravelFor(travelRules))
        {
            _journey.RecalculatePacing(TravelMode.Foot);
            return DescribeHorseLoss(nextHorseState, travelRules);
        }

        return string.Empty;
    }

    private static HorseTravelState ApplyHorseDelta(HorseTravelState horseState, JourneyTrailEventState trailEvent)
    {
        var nextHorseState = horseState;

        if (trailEvent.HorseHungerDelta > 0)
        {
            nextHorseState = nextHorseState.IncreaseHunger(trailEvent.HorseHungerDelta);
        }
        else if (trailEvent.HorseHungerDelta < 0)
        {
            nextHorseState = nextHorseState.RecoverHunger(Math.Abs(trailEvent.HorseHungerDelta));
        }

        if (trailEvent.HorseThirstDelta > 0)
        {
            nextHorseState = nextHorseState.IncreaseThirst(trailEvent.HorseThirstDelta);
        }
        else if (trailEvent.HorseThirstDelta < 0)
        {
            nextHorseState = nextHorseState.RecoverThirst(Math.Abs(trailEvent.HorseThirstDelta));
        }

        if (trailEvent.HorseExhaustionDelta > 0)
        {
            nextHorseState = nextHorseState.IncreaseExhaustion(trailEvent.HorseExhaustionDelta);
        }

        return nextHorseState;
    }

    private TravelJourneySnapshot CompleteJourneyAtDestination(TravelRulesProfile travelRules, AdvanceDayRuntime runtime)
    {
        if (_journey is null)
        {
            throw new InvalidOperationException("A journey is required to complete arrival handling.");
        }

        _journey.MarkCompleted();
        // Reflect the canteen refill that happens on arrival (Apply(JourneyCompleted)
        // calls RefillCanteenAfterArrival). The journey snapshot must capture the
        // refilled state so the diary day and replay path stay consistent.
        if (runtime.CanteenCapacity > 0)
        {
            _journey.SetCanteenCharges(runtime.CanteenCapacity);
        }
        return _journey.ToSnapshot(travelRules);
    }

    private TravelDayGenerationContext CreateTravelDayGenerationContext(
        AdvanceJourneyDayContext context,
        AdvanceDayRuntime runtime,
        int generatorVersion = TravelDayPlanGenerator.CurrentVersion,
        string? gameSeed = null,
        string? scenarioProfileId = null)
    {
        if (_journey is null)
        {
            throw new InvalidOperationException("No active journey is underway.");
        }

        var routeProfile = _journey.Preview.RouteProfile;
        var horseState = _journey.HorseState;
        var recentTrailEventKinds = _travelDiaryDays
            .Select(day => day.TrailEvent?.Kind)
            .Where(kind => kind is not null)
            .Select(kind => kind!.Value)
            .TakeLast(3)
            .ToArray();
        var recentTrailEventIds = _travelDiaryDays
            .Select(day => day.TrailEvent?.Id)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .TakeLast(3)
            .ToArray();
        var recentEncounterCategories = _travelDiaryDays
            .Select(day => day.PendingEncounter?.Kind switch
            {
                "foe" => TravelDayEncounterCategory.Foe,
                "npc" => TravelDayEncounterCategory.Npc,
                _ => (TravelDayEncounterCategory?)null
            })
            .Where(category => category is not null)
            .Select(category => category!.Value)
            .TakeLast(3)
            .ToArray();

        return new TravelDayGenerationContext(
            generatorVersion,
            gameSeed,
            scenarioProfileId,
            routeProfile.TrailId,
            _journey.Preview.OriginTownId,
            _journey.Preview.DestinationTownId,
            _journey.DaysTravelled,
            _journey.TravelMode,
            routeProfile.Risk,
            routeProfile.Terrain,
            routeProfile.WaterFeature,
            context.TravelRules.Difficulty,
            _journey.RemainingDays,
            _journey.RemainingRideDayDistance,
            CreateFoodPressureBand(_journey.FoodRemaining, _journey.RemainingDays),
            CreateCanteenPressureBand(_journey.AvailableCanteenCharges, _journey.RemainingDays, _journey.Preview.RouteProfile.WaterFeature, horseState, context.TravelRules),
            CreateHorseFeedPressureBand(_journey.HorseFeedRemaining, _journey.RemainingDays, _journey.Preview.RouteProfile.Terrain, horseState),
            CreateHorseConditionBand(horseState, context.TravelRules),
            CreateWalletBand(runtime.WalletCash, context.TravelRules),
            recentTrailEventKinds,
            recentTrailEventIds,
            recentEncounterCategories,
            HasHorse: horseState is not null && !horseState.IsDeadFor(context.TravelRules),
            context.SaltMode,
            context.Salt,
            context.GameEntropy);
    }

    private TravelDiaryBaselineState RebuildCurrentTravelDiaryBaselineState()
    {
        if (_travelDiaryDays.Count == 0)
        {
            throw new InvalidOperationException("There is no travel diary day to resume from.");
        }

        if (_journey is null)
        {
            throw new InvalidOperationException("A journey is required to rebuild the travel diary baseline.");
        }

        var latestDay = _travelDiaryDays[^1];
        var startingResources = new TravelResourceSnapshot(
            latestDay.HorseStateBefore,
            latestDay.CurrentWallet - latestDay.WalletDelta,
            latestDay.CurrentFood - latestDay.FoodDelta,
            latestDay.CurrentHorseFeed - latestDay.HorseFeedDelta,
            latestDay.CurrentCanteenCharges - latestDay.CanteenChargeDelta,
            latestDay.CurrentAmmo + latestDay.AmmoSpent,
            latestDay.CurrentHealth - latestDay.HealthDelta,
            latestDay.CurrentHeat - latestDay.HeatIncrease);

        return new TravelDiaryBaselineState(
            latestDay.StartingTravelMode,
            latestDay.StartingRideDayDistance,
            latestDay.StartingDaysRemaining,
            _journey.DelayDays - latestDay.DelayDays,
            startingResources);
    }

    private JourneyEncounterState RecoverFoeProfile(AdvanceJourneyDayContext context, JourneyEncounterState encounter)
    {
        if (_journey is null)
        {
            throw new InvalidOperationException("A journey is required to recover foe profile data.");
        }

        var generationContext = CreateTravelDayGenerationContext(context, new AdvanceDayRuntime(context), TravelDayPlanGenerator.CurrentVersion);
        var fallbackSeed = string.Join(
            "|",
            _journey.Preview.RouteProfile.TrailId,
            _journey.DaysTravelled,
            _journey.DelayDays,
            _journey.TravelMode,
            _journey.RemainingRideDayDistance,
            _journey.RemainingDays,
            context.PlayerHealth,
            context.PlayerCash,
            context.CurrentHeat,
            context.Salt,
            encounter.Message);

        var foeProfile = JourneyEncounterResolutionEngine.CreateFoeProfile(generationContext, context.TravelRules, fallbackSeed);
        return encounter with { FoeProfile = foeProfile };
    }

    private TravelResourceSnapshot CaptureTravelResources(AdvanceJourneyDayContext context, AdvanceDayRuntime runtime)
        => new(
            _journey?.HorseState,
            runtime.WalletCash,
            _journey?.FoodRemaining ?? 0,
            _journey?.HorseFeedRemaining ?? 0,
            _journey?.AvailableCanteenCharges ?? 0,
            runtime.RevolverAmmo + runtime.RifleAmmo,
            runtime.Health,
            context.CurrentHeat);

    private void AppendTravelDiaryDay(
        AdvanceJourneyDayContext context,
        AdvanceDayRuntime runtime,
        TravelJourneySnapshot journeySnapshot,
        TravelDiaryBaselineState startingState,
        JourneyTrailEventState? trailEvent = null,
        JourneyEncounterState? pendingEncounter = null,
        TravelDiaryEncounterResolutionState? encounterResolution = null,
        IReadOnlyList<string>? entries = null)
    {
        _travelDiaryDays.Add(TravelDiaryDayFactory.Create(
            journeySnapshot,
            startingState,
            CaptureTravelResources(context, runtime),
            trailEvent: trailEvent,
            pendingEncounter: pendingEncounter,
            encounterResolution: encounterResolution,
            entries: entries));
    }

    private bool PersistLatestTravelDiaryDay(
        TravelRulesProfile travelRules,
        AdvanceDayRuntime runtime,
        TravelDiaryBaselineState startingState,
        IReadOnlyList<string> newEntries,
        JourneyEncounterState? pendingEncounter = null,
        TravelDiaryEncounterResolutionState? encounterResolution = null,
        TravelJourneySnapshot? journeySnapshot = null,
        TravelResourceSnapshot? currentResources = null,
        JourneyTrailEventState? trailEvent = null)
    {
        ArgumentNullException.ThrowIfNull(startingState);
        ArgumentNullException.ThrowIfNull(newEntries);

        if (_travelDiaryDays.Count == 0)
        {
            return false;
        }

        journeySnapshot ??= _journey?.ToSnapshot(travelRules);
        if (journeySnapshot is null)
        {
            return false;
        }

        currentResources ??= CaptureTravelResources(
            new AdvanceJourneyDayContext(
                travelRules, runtime.Salt, runtime.SaltMode, runtime.GameEntropy, runtime.ClockDay, runtime.CurrentHeat,
                new PlayerCapabilities(false, false), 0, 0, null, null, runtime.WalletCash, runtime.Health, runtime.RevolverAmmo + runtime.RifleAmmo),
            runtime);
        var combinedEntries = _travelDiaryDays[^1].Entries.Concat(newEntries).ToArray();

        var lastIndex = _travelDiaryDays.Count - 1;
        _travelDiaryDays[lastIndex] = TravelDiaryDayFactory.Create(
            journeySnapshot,
            startingState,
            currentResources,
            trailEvent: trailEvent,
            pendingEncounter: pendingEncounter,
            encounterResolution: encounterResolution,
            entries: combinedEntries);
        return true;
    }

    private static string CombineHorseLossMessage(string primaryHorseLossMessage, string secondaryHorseLossMessage)
        => primaryHorseLossMessage.Length == 0
            ? secondaryHorseLossMessage
            : secondaryHorseLossMessage.Length == 0
                ? primaryHorseLossMessage
                : $"{primaryHorseLossMessage} {secondaryHorseLossMessage}";

    private static string PrependHorseLossMessage(string horseLossMessage, string message)
        => horseLossMessage.Length == 0 ? message : $"{horseLossMessage} {message}";

    private static string DescribeHorseLoss(HorseTravelState? horseState, TravelRulesProfile travelRulesProfile)
    {
        if (horseState is null)
        {
            return "Your horse could no longer carry you.";
        }

        if (horseState.IsDeadFor(travelRulesProfile))
        {
            return "Your horse died on the trail.";
        }

        if (horseState.IsLameFor(travelRulesProfile))
        {
            return "Your horse went lame and could no longer carry you.";
        }

        return "Your horse could no longer carry you.";
    }

    private static TravelPressureBand CreateFoodPressureBand(int foodRemaining, int remainingDays)
    {
        if (foodRemaining <= 0) return TravelPressureBand.Critical;
        if (foodRemaining == 1) return TravelPressureBand.High;
        if (foodRemaining <= remainingDays) return TravelPressureBand.Moderate;
        if (foodRemaining <= remainingDays + 1) return TravelPressureBand.Low;
        return TravelPressureBand.None;
    }

    private static TravelPressureBand CreateCanteenPressureBand(
        int availableCanteenCharges, int remainingDays, WaterFeature waterFeature,
        HorseTravelState? horseState, TravelRulesProfile travelRulesProfile)
    {
        if (JourneyUpkeepRules.HasRouteWater(waterFeature)) return TravelPressureBand.None;

        var chargesPerDay = JourneyUpkeepRules.WaterChargesRequiredPerDay(horseState, travelRulesProfile);
        var requiredCharges = remainingDays * chargesPerDay;
        var reserveCharges = availableCanteenCharges - requiredCharges;

        if (reserveCharges < 0) return TravelPressureBand.Critical;
        if (reserveCharges == 0) return TravelPressureBand.High;
        if (reserveCharges <= chargesPerDay) return TravelPressureBand.Moderate;
        if (reserveCharges <= chargesPerDay * 2) return TravelPressureBand.Low;
        return TravelPressureBand.None;
    }

    private static TravelPressureBand CreateHorseFeedPressureBand(
        int horseFeedRemaining, int remainingDays, TrailTerrain terrain, HorseTravelState? horseState)
    {
        if (horseState is null || JourneyUpkeepRules.HasGrazing(terrain)) return TravelPressureBand.None;
        if (horseFeedRemaining <= 0) return TravelPressureBand.Critical;
        if (horseFeedRemaining == 1) return TravelPressureBand.High;
        if (horseFeedRemaining <= remainingDays) return TravelPressureBand.Moderate;
        if (horseFeedRemaining <= remainingDays + 1) return TravelPressureBand.Low;
        return TravelPressureBand.None;
    }

    private static HorseConditionBand CreateHorseConditionBand(HorseTravelState? horseState, TravelRulesProfile travelRulesProfile)
    {
        if (horseState is null) return HorseConditionBand.None;
        if (horseState.IsDeadFor(travelRulesProfile)) return HorseConditionBand.Critical;
        if (horseState.IsLameFor(travelRulesProfile)) return HorseConditionBand.Lame;
        if (horseState.Exhaustion >= 2 || horseState.Hunger >= 2 || horseState.Thirst >= 1) return HorseConditionBand.Worn;
        return HorseConditionBand.Sound;
    }

    private static WalletBand CreateWalletBand(decimal cash, TravelRulesProfile travelRulesProfile)
    {
        if (cash <= 0m) return WalletBand.Broke;
        if (cash < travelRulesProfile.EncounterBribeCash) return WalletBand.Tight;
        if (cash < travelRulesProfile.EncounterBribeCash * 2) return WalletBand.Steady;
        if (cash < travelRulesProfile.EncounterBribeCash * 4) return WalletBand.Comfortable;
        return WalletBand.Flush;
    }

    private sealed record TrailEventApplicationResult(string HorseLossMessage);

    private sealed record TravelDayAdvanceState(
        TravelDiaryBaselineState StartingState,
        string HorseLostMessage,
        JourneyProgress Progress,
        int NewDay,
        int PursuitHeat);

    private sealed class AdvanceDayRuntime
    {
        public decimal WalletCash;
        public int Health;
        public int RevolverAmmo;
        public int RifleAmmo;
        public bool HasKnife;
        public int CanteenCapacity;
        public string Salt;
        public SaltSourceMode SaltMode;
        public GameEntropy GameEntropy;
        public int ClockDay;
        public int CurrentHeat;
        public List<IDomainEvent> Events { get; } = [];

        public AdvanceDayRuntime(AdvanceJourneyDayContext context)
        {
            WalletCash = context.PlayerCash;
            Health = context.PlayerHealth;
            RevolverAmmo = context.AvailableAmmo;
            RifleAmmo = 0;
            HasKnife = false;
            CanteenCapacity = context.CanteenState?.Capacity ?? 0;
            Salt = context.Salt;
            SaltMode = context.SaltMode;
            GameEntropy = context.GameEntropy;
            ClockDay = context.ClockDay;
            CurrentHeat = context.CurrentHeat;
        }

        public AdvanceDayRuntime(ResolveJourneyEncounterContext context)
        {
            WalletCash = context.PlayerCash;
            Health = context.PlayerHealth;
            RevolverAmmo = context.AvailableRevolverAmmo;
            RifleAmmo = context.AvailableRifleAmmo;
            HasKnife = context.HasKnife;
            CanteenCapacity = context.CanteenState?.Capacity ?? 0;
            Salt = context.Salt;
            SaltMode = context.SaltMode;
            GameEntropy = context.GameEntropy;
            ClockDay = 0;
            CurrentHeat = context.CurrentHeat;
        }

        public void AddEvent(IDomainEvent e) => Events.Add(e);

        public void SpendAmmo(int amount)
        {
            var toSpend = Math.Min(amount, RevolverAmmo + RifleAmmo);
            var fromRevolver = Math.Min(toSpend, RevolverAmmo);
            RevolverAmmo -= fromRevolver;
            RifleAmmo -= (toSpend - fromRevolver);
        }
    }

    internal void RestoreTravelDiaryDays(IReadOnlyList<TravelDiaryDayState> days)
    {
        _travelDiaryDays.Clear();
        _travelDiaryDays.AddRange(days);
    }

    internal void RestorePendingDevTravelOverride(DevTravelOverride? overrideValue)
    {
        _pendingDevTravelOverride = overrideValue;
    }

    internal void AppendTravelDiaryDay(TravelDiaryDayState travelDiaryDay)
    {
        _travelDiaryDays.Add(travelDiaryDay);
    }

    internal bool UpdateLatestTravelDiaryDay(Func<TravelDiaryDayState, TravelDiaryDayState> update)
    {
        if (_travelDiaryDays.Count == 0)
        {
            return false;
        }

        var lastIndex = _travelDiaryDays.Count - 1;
        _travelDiaryDays[lastIndex] = update(_travelDiaryDays[lastIndex]);
        return true;
    }

    private static int CalculateNextJourneySequence(
        TravelJourney? journey,
        IReadOnlyList<TravelJourneySnapshot> completedJourneyHistory)
    {
        var maxSequence = journey?.JourneySequence ?? 0;

        if (completedJourneyHistory.Count > 0)
        {
            maxSequence = Math.Max(maxSequence, completedJourneyHistory.Max(history => history.JourneySequence));
        }

        return Math.Max(1, maxSequence + 1);
    }

    private static string BuildJourneyOpeningNarration(TravelPreview preview)
    {
        var baselineRidePhrase = $"{preview.BaselineRideDays}-day {DescribeTerrain(preview.RouteProfile.Terrain)} ride";
        var travelMode = DescribeTravelMode(preview.TravelMode);
        var risk = DescribeRisk(preview.RouteProfile.Risk);
        var waterPressure = preview.WaterSecure
            ? $"I had enough water for the base trail, though the canteen still needed watching on a {preview.ExpectedDays}-day run."
            : $"This dry trail asked for {preview.CanteenChargesPerDay} canteen charge(s) a day, and I did not have much slack.";
        var foodPressure = preview.AvailableFood <= preview.ExpectedDays
            ? "My food was tight enough that I noticed every meal."
            : "My food should have held if the trail behaved itself.";
        var horsePressure = preview.HorseState is null
            ? "I was traveling without a horse, so the road had to be enough."
            : preview.MountedTravelAvailable
                ? "My horse was fit enough to carry me for now."
                : "My horse was not fit for mounted travel, so I needed to mind the pace.";

        var openingSentence = preview.TravelMode == TravelMode.Foot
            ? preview.ExpectedDays != preview.BaselineRideDays
                ? $"I set out for {preview.DestinationTownName} on a {baselineRidePhrase}, but without a horse it would take {preview.ExpectedDays} days on foot."
                : $"I set out for {preview.DestinationTownName} on a {baselineRidePhrase} on foot."
            : $"I set out for {preview.DestinationTownName} on a {baselineRidePhrase} {travelMode}.";

        return $"{openingSentence} {risk} {waterPressure} {foodPressure} {horsePressure}";
    }

    private static string DescribeTravelMode(TravelMode travelMode)
        => travelMode == TravelMode.Mounted ? "by mounted travel" : "on foot";

    private static string DescribeCanteenCoverage(TravelPreview preview)
        => DescribeCanteenCoverage(preview.RouteProfile.WaterFeature, preview.CanteenChargesPerDay, preview.CanteenReserveCharges, preview.DelayMarginDays);

    private static string DescribeCanteenCoverage(TravelJourneySnapshot snapshot)
        => DescribeCanteenCoverage(snapshot.RouteProfile.WaterFeature, snapshot.CanteenChargesPerDay, snapshot.CanteenReserveCharges, snapshot.DelayMarginDays);

    private static string DescribeCanteenCoverage(
        WaterFeature waterFeature,
        int canteenChargesPerDay,
        int canteenReserveCharges,
        int delayMarginDays)
    {
        if (JourneyUpkeepRules.HasRouteWater(waterFeature))
        {
            return "Route water is secure, so no canteen reserve is required";
        }

        if (canteenChargesPerDay <= 0)
        {
            return "No canteen water is required on this trail";
        }

        if (canteenReserveCharges == 0)
        {
            return "The canteen exactly covers the base trail and has no reserve for delays";
        }

        if (canteenReserveCharges > 0)
        {
            return $"The canteen has {canteenReserveCharges} spare charge(s) and can absorb {delayMarginDays} delay day(s)";
        }

        return $"The canteen is short by {Math.Abs(canteenReserveCharges)} charge(s) for the base trail";
    }

    private static string DescribeTerrain(TrailTerrain terrain)
        => terrain switch
        {
            TrailTerrain.OpenRange => "open-range",
            TrailTerrain.Hills => "hill country",
            TrailTerrain.Badlands => "badlands",
            TrailTerrain.Mountains => "mountain",
            _ => "trail"
        };

    private static string DescribeRisk(TrailRisk risk)
        => risk switch
        {
            TrailRisk.Low => "The route looks steady enough for now.",
            TrailRisk.Moderate => "The route has some teeth, so I will keep my eyes open.",
            TrailRisk.High => "The route looks rough enough to demand respect.",
            _ => "The route is hard to read."
        };
}
