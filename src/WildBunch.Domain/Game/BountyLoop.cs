using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

/// <summary>
/// Child domain component inside the GameSession boundary that owns bounty-loop
/// state and behavior. Receives narrow context records, returns results plus
/// events-to-produce. Does NOT reference GameSession, produce events directly,
/// enter action context, adjust cash, or mutate CaseFile/TownVisitState/Player.
/// See BUNCH-112 and ADR-0002/ADR-0020.
/// </summary>
internal sealed class BountyLoop
{
    private readonly WantedSuspectPresenceLedger _presenceLedger;
    private UnrelatedCriminalLedger _unrelatedCriminalLedger;
    private DevSaloonOverride? _pendingDevSaloonOverride;

    internal BountyLoop(
        IReadOnlyList<WantedSuspectPresenceEntry>? presenceEntries,
        UnrelatedCriminalLedger unrelatedCriminalLedger)
    {
        _presenceLedger = new WantedSuspectPresenceLedger(presenceEntries);
        _unrelatedCriminalLedger = unrelatedCriminalLedger
            ?? throw new ArgumentNullException(nameof(unrelatedCriminalLedger));
    }

    internal IReadOnlyList<WantedSuspectPresenceEntry> PresenceEntries => _presenceLedger.Entries;
    internal UnrelatedCriminalLedger UnrelatedCriminalLedger => _unrelatedCriminalLedger;
    internal DevSaloonOverride? PendingDevSaloonOverride => _pendingDevSaloonOverride;

    internal WantedSuspectPresenceState GetWantedSuspectPresenceState(SuspectId suspectId)
        => _presenceLedger.GetState(suspectId);

    internal bool TryGetWantedSuspectPresenceState(SuspectId suspectId, out WantedSuspectPresenceState state)
        => _presenceLedger.TryGetState(suspectId, out state);

    internal void SetWantedSuspectPresenceState(SuspectId suspectId, WantedSuspectPresenceState state)
        => _presenceLedger.SetState(suspectId, state);

    // Command methods — filled in by Tasks 3–7
    // Apply methods — filled in by Task 8

    /// <summary>
    /// Saloon POI confrontation decision logic. Returns outcome with events for
    /// GameSession to produce. The armed-correct-declaration branch returns a
    /// <see cref="SaloonSettlementRequest"/> so GameSession can orchestrate
    /// EnterActionContext(SheriffOffice) + SettleSheriffTurnIn between events.
    /// </summary>
    internal SaloonConfrontationOutcome ConfrontSaloonPersonOfInterest(SaloonConfrontationContext context)
    {
        if (context.IsJourneyModal)
        {
            return new SaloonConfrontationOutcome(
                SaloonPersonOfInterestConfrontationResult.Rejected(
                    context.JourneyModalBlockMessage, context.DeclaredWantedIdentityHandle),
                [],
                null);
        }

        var activeSaloonSuspectId = context.ActiveSaloonSuspectId;
        var activeSaloonPersonOfInterestDescriptor = context.ActiveSaloonDescriptor;
        var activeSaloonPersonOfInterestKind = context.ActiveSaloonPOIKind;
        if (activeSaloonSuspectId is null && activeSaloonPersonOfInterestDescriptor is null)
        {
            return new SaloonConfrontationOutcome(
                SaloonPersonOfInterestConfrontationResult.Rejected(
                    "There is no person of interest waiting in the saloon."),
                [],
                null);
        }

        if (activeSaloonSuspectId is not null)
        {
            return ConfrontWantedSuspectInSaloon(context, activeSaloonSuspectId.Value, activeSaloonPersonOfInterestKind);
        }

        return ConfrontCitizenInSaloon(context, activeSaloonPersonOfInterestDescriptor!, activeSaloonPersonOfInterestKind);
    }

    private SaloonConfrontationOutcome ConfrontWantedSuspectInSaloon(
        SaloonConfrontationContext context,
        SuspectId activeSaloonSuspect,
        SaloonPersonOfInterestKind? activeSaloonPersonOfInterestKind)
    {
        var events = new List<IDomainEvent>();
        var declaredWantedIdentityHandle = context.DeclaredWantedIdentityHandle;

        var targetSuspect = context.Suspects.FirstOrDefault(s => s.Id.Equals(activeSaloonSuspect));
        if (targetSuspect is null)
        {
            events.Add(BuildSaloonConfrontedEvent(
                "That person of interest is no longer available.",
                declaredWantedIdentityHandle,
                targetName: "the person of interest",
                personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                outcome: SaloonPersonOfInterestConfrontationOutcome.Rejected));
            return new SaloonConfrontationOutcome(
                SaloonPersonOfInterestConfrontationResult.Rejected(
                    "That person of interest is no longer available.",
                    declaredWantedIdentityHandle,
                    sessionChanged: true,
                    personOfInterestKind: activeSaloonPersonOfInterestKind),
                events,
                null);
        }

        var activeSaloonWarrant = context.KnownWarrants.FirstOrDefault(w => MatchesKnownWarrant(w, targetSuspect));
        if (activeSaloonWarrant is null)
        {
            events.Add(BuildSaloonConfrontedEvent(
                "You do not know any wanted identity or warrant to declare, so the opportunity has passed.",
                declaredWantedIdentityHandle,
                targetName: targetSuspect.Name,
                personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                outcome: SaloonPersonOfInterestConfrontationOutcome.Rejected));
            return new SaloonConfrontationOutcome(
                SaloonPersonOfInterestConfrontationResult.Rejected(
                    "You do not know any wanted identity or warrant to declare, so the opportunity has passed.",
                    declaredWantedIdentityHandle,
                    sessionChanged: true,
                    personOfInterestKind: activeSaloonPersonOfInterestKind),
                events,
                null);
        }

        var presenceState = context.ActiveSaloonSuspectPresenceState ?? WantedSuspectPresenceState.Unavailable;
        if (presenceState is not (WantedSuspectPresenceState.AvailableInTown or WantedSuspectPresenceState.GoneToGround))
        {
            events.Add(BuildSaloonConfrontedEvent(
                $"{targetSuspect.Name} is no longer in the saloon.",
                declaredWantedIdentityHandle,
                targetName: targetSuspect.Name,
                personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                outcome: SaloonPersonOfInterestConfrontationOutcome.Rejected));
            return new SaloonConfrontationOutcome(
                SaloonPersonOfInterestConfrontationResult.Rejected(
                    $"{targetSuspect.Name} is no longer in the saloon.",
                    declaredWantedIdentityHandle,
                    targetSuspect.Name,
                    sessionChanged: true,
                    personOfInterestKind: activeSaloonPersonOfInterestKind),
                events,
                null);
        }

        if (context.ConfrontationStates.TryGetValue(activeSaloonSuspect, out var existingState))
        {
            events.Add(BuildSaloonConfrontedEvent(
                $"{existingState.TargetName} has already been confronted.",
                declaredWantedIdentityHandle,
                targetName: existingState.TargetName,
                personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                outcome: SaloonPersonOfInterestConfrontationOutcome.Rejected));
            return new SaloonConfrontationOutcome(
                SaloonPersonOfInterestConfrontationResult.Rejected(
                    $"{existingState.TargetName} has already been confronted.",
                    declaredWantedIdentityHandle,
                    existingState.TargetName,
                    activeSaloonWarrant.Terms.Disposition,
                    sessionChanged: true,
                    personOfInterestKind: activeSaloonPersonOfInterestKind),
                events,
                null);
        }

        var hasFirearmThreatAvailable = context.FirearmThreatAvailable;
        var isDeclaredWantedIdentityForThisWarrant =
            BountyDeclarationMatchPolicy.MatchesDeclaredWantedIdentity(declaredWantedIdentityHandle, activeSaloonWarrant);

        if (hasFirearmThreatAvailable && isDeclaredWantedIdentityForThisWarrant)
        {
            // Armed + correct declaration: surrender → sheriff turn-in → saloon confronted.
            // BountyLoop produces the WantedSuspectConfronted event, then returns a
            // settlement request so GameSession can orchestrate EnterActionContext +
            // SettleSheriffTurnIn, then produce the final SaloonPersonOfInterestConfronted.
            var armedWantedEvent = BuildWantedSuspectConfrontedEvent(
                activeSaloonSuspect,
                activeSaloonWarrant,
                WantedSuspectConfrontationChoice.Surrendered,
                declaredWantedIdentityHandle);
            events.Add(armedWantedEvent);

            var armedWantedResult = WantedSuspectConfrontationResult.Surrendered(
                declaredWantedIdentityHandle,
                activeSaloonWarrant.TargetName,
                activeSaloonWarrant.Terms.Disposition,
                armedWantedEvent.Message);

            return new SaloonConfrontationOutcome(
                SaloonPersonOfInterestConfrontationResult.FromWantedSuspectResult(armedWantedResult),
                events,
                new SaloonSettlementRequest(
                    activeSaloonSuspect,
                    IsAlive: true,
                    declaredWantedIdentityHandle,
                    armedWantedEvent.Message,
                    activeSaloonWarrant.TargetName,
                    activeSaloonPersonOfInterestKind));
        }

        if (hasFirearmThreatAvailable && !string.IsNullOrWhiteSpace(declaredWantedIdentityHandle))
        {
            // Armed + wrong declaration: fine the player, no turn-in.
            var wantedWalletBefore = context.PlayerCash;
            var wantedFineAmount = BountySettlementPolicy.CalculateCappedFine(wantedWalletBefore, context.CitizenDeclarationFine);
            var publicTargetName = context.ActiveSaloonDescriptor ?? "the person of interest";
            var wrongDeclarationMessage = $"You bring {publicTargetName} to the sheriff, but the declaration is wrong. The sheriff releases them and fines you ${wantedFineAmount:0.00}.";

            events.Add(BuildSaloonConfrontedEvent(
                wrongDeclarationMessage,
                declaredWantedIdentityHandle,
                targetName: publicTargetName,
                personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                outcome: SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration,
                fineAmount: wantedFineAmount,
                walletBefore: wantedWalletBefore,
                isCitizen: false,
                isAlive: true,
                isSecured: false));

            return new SaloonConfrontationOutcome(
                SaloonPersonOfInterestConfrontationResult.WrongWantedDeclaration(
                    declaredWantedIdentityHandle,
                    publicTargetName,
                    wrongDeclarationMessage,
                    wantedFineAmount,
                    wantedWalletBefore,
                    wantedWalletBefore - wantedFineAmount,
                    isCitizen: false,
                    isAlive: true,
                    isSecured: false),
                events,
                null);
        }

        // No firearm threat or no declaration: suspect flees.
        var wantedEvent = BuildWantedSuspectConfrontedEvent(
            activeSaloonSuspect,
            activeSaloonWarrant,
            WantedSuspectConfrontationChoice.Fled,
            declaredWantedIdentityHandle);
        events.Add(wantedEvent);

        var wantedResult = WantedSuspectConfrontationResult.Fled(
            declaredWantedIdentityHandle,
            activeSaloonWarrant.TargetName,
            activeSaloonWarrant.Terms.Disposition,
            wantedEvent.Message);

        events.Add(BuildSaloonConfrontedEvent(
            wantedEvent.Message,
            declaredWantedIdentityHandle,
            targetSuspectId: activeSaloonSuspect,
            targetName: activeSaloonWarrant.TargetName,
            personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
            outcome: SaloonPersonOfInterestConfrontationOutcome.Fled,
            isAlive: true,
            isSecured: false));

        return new SaloonConfrontationOutcome(
            SaloonPersonOfInterestConfrontationResult.FromWantedSuspectResult(wantedResult),
            events,
            null);
    }

    private SaloonConfrontationOutcome ConfrontCitizenInSaloon(
        SaloonConfrontationContext context,
        string activeSaloonPersonOfInterestDescriptor,
        SaloonPersonOfInterestKind? activeSaloonPersonOfInterestKind)
    {
        var events = new List<IDomainEvent>();
        var declaredWantedIdentityHandle = context.DeclaredWantedIdentityHandle;

        var walletBefore = context.PlayerCash;
        var fineAmount = BountySettlementPolicy.CalculateCappedFine(walletBefore, context.CitizenDeclarationFine);
        var citizenTargetName = activeSaloonPersonOfInterestDescriptor;
        var citizenRoleKey = context.ActiveSaloonCitizenRole;
        var citizenNarration = BuildCitizenRevealNarration(citizenTargetName, citizenRoleKey, fineAmount);

        events.Add(BuildSaloonConfrontedEvent(
            citizenNarration,
            declaredWantedIdentityHandle,
            targetName: citizenTargetName,
            personOfInterestKind: SaloonPersonOfInterestKind.Citizen,
            outcome: SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration,
            fineAmount: fineAmount,
            walletBefore: walletBefore,
            isCitizen: true,
            citizenRole: citizenRoleKey));

        return new SaloonConfrontationOutcome(
            SaloonPersonOfInterestConfrontationResult.WrongWantedDeclaration(
                declaredWantedIdentityHandle,
                citizenTargetName,
                citizenNarration,
                fineAmount,
                walletBefore,
                walletBefore - fineAmount,
                isCitizen: true,
                isAlive: null,
                isSecured: null),
            events,
            null);
    }

    /// <summary>
    /// Direct wanted-suspect confrontation decision logic. Returns result plus
    /// the WantedSuspectConfronted event for GameSession to produce.
    /// </summary>
    internal BountyLoopResult<WantedSuspectConfrontationResult> ResolveWantedSuspectConfrontation(
        WantedSuspectConfrontationContext context)
    {
        var events = new List<IDomainEvent>();

        if (context.IsJourneyModal)
        {
            return new BountyLoopResult<WantedSuspectConfrontationResult>(
                WantedSuspectConfrontationResult.Rejected(context.JourneyModalBlockMessage, context.DeclaredWantedIdentityHandle),
                events);
        }

        if (!context.CanConfrontInCurrentContext)
        {
            return new BountyLoopResult<WantedSuspectConfrontationResult>(
                WantedSuspectConfrontationResult.Rejected(
                    "You can only confront a wanted suspect who is present in your current location.",
                    context.DeclaredWantedIdentityHandle),
                events);
        }

        var targetSuspect = context.Suspects.FirstOrDefault(s => s.Id.Equals(context.TargetSuspectId));
        if (targetSuspect is null)
        {
            return new BountyLoopResult<WantedSuspectConfrontationResult>(
                WantedSuspectConfrontationResult.Rejected(
                    "That person is not part of this case.",
                    context.DeclaredWantedIdentityHandle),
                events);
        }

        var warrant = context.KnownWarrants.FirstOrDefault(w => MatchesKnownWarrant(w, targetSuspect));
        if (warrant is null)
        {
            return new BountyLoopResult<WantedSuspectConfrontationResult>(
                WantedSuspectConfrontationResult.Rejected(
                    $"There is no wanted notice for {targetSuspect.Name}.",
                    context.DeclaredWantedIdentityHandle,
                    targetSuspect.Name),
                events);
        }

        if (context.ConfrontationStates.TryGetValue(context.TargetSuspectId, out var existingState))
        {
            return new BountyLoopResult<WantedSuspectConfrontationResult>(
                WantedSuspectConfrontationResult.Rejected(
                    $"{existingState.TargetName} has already been confronted.",
                    context.DeclaredWantedIdentityHandle,
                    existingState.TargetName,
                    existingState.Disposition),
                events);
        }

        if (context.Choice == WantedSuspectConfrontationChoice.Abandoned)
        {
            var abandonNarration = DescribeConfrontationNarration(warrant.TargetName, context.Choice, context.DeclaredWantedIdentityHandle);
            events.Add(new WantedSuspectConfronted
            {
                TargetSuspectId = context.TargetSuspectId,
                TargetName = warrant.TargetName,
                Disposition = warrant.Terms.Disposition,
                Choice = WantedSuspectConfrontationChoice.Abandoned,
                Outcome = WantedSuspectConfrontationOutcome.Abandoned,
                IsAlive = true,
                IsSecured = false,
                Message = abandonNarration,
                DeclaredWantedIdentityHandle = context.DeclaredWantedIdentityHandle
            });
            return new BountyLoopResult<WantedSuspectConfrontationResult>(
                WantedSuspectConfrontationResult.Abandoned(
                    context.DeclaredWantedIdentityHandle,
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    abandonNarration),
                events);
        }

        var (isAlive, isSecured) = context.Choice switch
        {
            WantedSuspectConfrontationChoice.Surrendered => (true, true),
            WantedSuspectConfrontationChoice.Fled => (true, false),
            WantedSuspectConfrontationChoice.Killed => (false, true),
            _ => ((bool?)null, (bool?)null)
        };

        if (isAlive is null)
        {
            return new BountyLoopResult<WantedSuspectConfrontationResult>(
                WantedSuspectConfrontationResult.Rejected(
                    $"The confrontation choice for {targetSuspect.Name} is not supported.",
                    context.DeclaredWantedIdentityHandle,
                    targetSuspect.Name,
                    warrant.Terms.Disposition),
                events);
        }

        var narration = DescribeConfrontationNarration(warrant.TargetName, context.Choice, context.DeclaredWantedIdentityHandle);
        events.Add(new WantedSuspectConfronted
        {
            TargetSuspectId = context.TargetSuspectId,
            TargetName = warrant.TargetName,
            Disposition = warrant.Terms.Disposition,
            Choice = context.Choice,
            Outcome = (WantedSuspectConfrontationOutcome)context.Choice,
            IsAlive = isAlive!.Value,
            IsSecured = isSecured!.Value,
            Message = narration,
            DeclaredWantedIdentityHandle = context.DeclaredWantedIdentityHandle
        });

        var result = context.Choice switch
        {
            WantedSuspectConfrontationChoice.Surrendered => WantedSuspectConfrontationResult.Surrendered(
                context.DeclaredWantedIdentityHandle, warrant.TargetName, warrant.Terms.Disposition, narration),
            WantedSuspectConfrontationChoice.Fled => WantedSuspectConfrontationResult.Fled(
                context.DeclaredWantedIdentityHandle, warrant.TargetName, warrant.Terms.Disposition, narration),
            WantedSuspectConfrontationChoice.Killed => WantedSuspectConfrontationResult.Killed(
                context.DeclaredWantedIdentityHandle, warrant.TargetName, warrant.Terms.Disposition, narration),
            _ => WantedSuspectConfrontationResult.Rejected(
                $"The confrontation choice for {targetSuspect.Name} is not supported.",
                context.DeclaredWantedIdentityHandle, targetSuspect.Name, warrant.Terms.Disposition)
        };

        return new BountyLoopResult<WantedSuspectConfrontationResult>(result, events);
    }

    /// <summary>
    /// Sheriff turn-in assessment decision logic. Pure decision — no events.
    /// </summary>
    internal SheriffTurnInResult AssessSheriffTurnIn(SheriffTurnInContext context)
    {
        if (context.IsJourneyModal)
        {
            return SheriffTurnInResult.Rejected(context.JourneyModalBlockMessage);
        }

        var targetSuspect = context.Suspects.FirstOrDefault(s => s.Id.Equals(context.TargetSuspectId));
        if (targetSuspect is null)
        {
            return context.IsAlive
                ? SheriffTurnInResult.WrongPersonAlive("That person is not part of this case.")
                : SheriffTurnInResult.WrongPersonDead("That person is not part of this case.");
        }

        var warrant = context.KnownWarrants.FirstOrDefault(w => MatchesKnownWarrant(w, targetSuspect));
        if (warrant is null)
        {
            return context.IsAlive
                ? SheriffTurnInResult.WrongPersonAlive($"There is no wanted notice for {targetSuspect.Name}.", targetSuspect.Name)
                : SheriffTurnInResult.WrongPersonDead($"There is no wanted notice for {targetSuspect.Name}.", targetSuspect.Name);
        }

        if (!context.ConfrontationStates.TryGetValue(context.TargetSuspectId, out var confrontationState))
        {
            return SheriffTurnInResult.Rejected(
                $"You have not secured {warrant.TargetName} for turn-in yet.",
                warrant.TargetName,
                warrant.Terms.Disposition,
                warrant.Terms.BountyAmount);
        }

        if (!confrontationState.IsTurnInEligible)
        {
            return SheriffTurnInResult.Rejected(
                $"{warrant.TargetName} got away and is not secured for turn-in.",
                warrant.TargetName,
                warrant.Terms.Disposition,
                warrant.Terms.BountyAmount);
        }

        if (context.IsAlive && !confrontationState.IsAlive)
        {
            return SheriffTurnInResult.Rejected(
                $"{warrant.TargetName} is not alive anymore.",
                warrant.TargetName,
                warrant.Terms.Disposition,
                warrant.Terms.BountyAmount);
        }

        if (!context.IsAlive && confrontationState.IsAlive)
        {
            return SheriffTurnInResult.Rejected(
                $"{warrant.TargetName} was secured alive and cannot be turned in dead.",
                warrant.TargetName,
                warrant.Terms.Disposition,
                warrant.Terms.BountyAmount);
        }

        if (context.IsAlive)
        {
            return SheriffTurnInResult.AcceptedAlive(
                warrant.TargetName,
                warrant.Terms.Disposition,
                warrant.Terms.BountyAmount,
                $"You bring in {warrant.TargetName} alive under a {DescribeWarrantDisposition(warrant.Terms.Disposition)} warrant.");
        }

        if (warrant.Terms.Disposition == WarrantDisposition.DeadOrAlive)
        {
            return SheriffTurnInResult.AcceptedDead(
                warrant.TargetName,
                warrant.Terms.Disposition,
                warrant.Terms.BountyAmount,
                $"You turn in the body of {warrant.TargetName} under a {DescribeWarrantDisposition(warrant.Terms.Disposition)} warrant.");
        }

        return SheriffTurnInResult.Rejected(
            $"The warrant for {warrant.TargetName} requires an alive turn-in.",
            warrant.TargetName,
            warrant.Terms.Disposition,
            warrant.Terms.BountyAmount);
    }

    /// <summary>
    /// Tries to create a sheriff turn-in settlement state from the assessment.
    /// Returns true + settlement state on success, false + rejection result on failure.
    /// </summary>
    internal bool TryCreateSettlementState(
        SheriffTurnInContext context,
        SheriffTurnInResult assessment,
        out SheriffTurnInSettlementState settlementState,
        out SheriffTurnInResult rejectionResult)
        => BountySettlementPolicy.TryCreateSheriffTurnInSettlementState(
            context.ExistingSettlements,
            assessment,
            context.TargetSuspectId,
            context.IsAlive,
            context.ClockDay,
            context.ClockTurn,
            out settlementState,
            out rejectionResult);

    // --- Helpers ---

    internal bool IsEligibleSaloonPersonOfInterestCandidate(Suspect suspect, SuspectId trueCulpritId, KillerReleaseState killerRelease)
    {
        ArgumentNullException.ThrowIfNull(suspect);

        if (suspect.Id.Equals(trueCulpritId))
        {
            return killerRelease.IsReleased;
        }

        return true;
    }

    internal string? GetSaloonPoiIneligibilityReason(Suspect suspect, SuspectId trueCulpritId, KillerReleaseState killerRelease)
    {
        ArgumentNullException.ThrowIfNull(suspect);

        if (suspect.Id.Equals(trueCulpritId))
        {
            if (killerRelease.IsReleased)
            {
                return null;
            }

            return killerRelease.StatusText;
        }

        return null;
    }

    /// <summary>
    /// Dev command: forces a saloon override. Validates suspect/citizen eligibility
    /// and returns the DevSaloonOverrideForced event for GameSession to produce.
    /// </summary>
    internal BountyLoopResult<bool> ForceDevSaloonOverride(DevSaloonOverrideContext context)
    {
        var overrideValue = context.Override;

        if (overrideValue.ForcedKind is DevSaloonPoiKind.Suspect && overrideValue.ForcedSuspectId is not null)
        {
            var suspectId = overrideValue.ForcedSuspectId.Value;

            if (!context.Suspects.Any(s => s.Id == overrideValue.ForcedSuspectId))
            {
                throw new InvalidOperationException(
                    $"Unknown suspect ID: {suspectId.Value}. Cannot force a saloon override for a suspect that does not exist.");
            }

            var suspect = context.Suspects.First(s => s.Id == overrideValue.ForcedSuspectId);
            if (!IsEligibleSaloonPersonOfInterestCandidate(suspect, context.TrueCulpritId, context.KillerReleaseState))
            {
                var reason = GetSaloonPoiIneligibilityReason(suspect, context.TrueCulpritId, context.KillerReleaseState);
                throw new InvalidOperationException(
                    $"Cannot force a saloon override for suspect {suspectId.Value} ({suspect.Name}). " +
                    $"{reason ?? "Suspect is not eligible as a saloon POI candidate."}");
            }
        }

        if (overrideValue.ForcedKind is DevSaloonPoiKind.Citizen && overrideValue.ForcedCitizenRoleKey is not null)
        {
            if (!context.CitizenRoleKeys.Any(key => string.Equals(key, overrideValue.ForcedCitizenRoleKey, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Unknown citizen role key: {overrideValue.ForcedCitizenRoleKey}. Cannot force a saloon override for a citizen role that does not exist.");
            }
        }

        var e = new DevSaloonOverrideForced
        {
            ForcedKind = overrideValue.ForcedKind,
            ForcedSuspectId = overrideValue.ForcedSuspectId,
            ForcedCitizenRoleKey = overrideValue.ForcedCitizenRoleKey
        };
        return new BountyLoopResult<bool>(true, [e]);
    }

    /// <summary>
    /// Dev command: clears any pending saloon override.
    /// Returns the DevSaloonOverrideCleared event for GameSession to produce.
    /// </summary>
    internal BountyLoopResult<bool> ClearDevSaloonOverride()
    {
        return new BountyLoopResult<bool>(true, [new DevSaloonOverrideCleared()]);
    }

    private static bool MatchesKnownWarrant(Warrant warrant, Suspect targetSuspect)
    {
        ArgumentNullException.ThrowIfNull(warrant);
        ArgumentNullException.ThrowIfNull(targetSuspect);

        if (string.Equals(warrant.TargetName, targetSuspect.Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return warrant.Terms.KnownAliases.Any(alias => string.Equals(alias, targetSuspect.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static string DescribeWarrantDisposition(WarrantDisposition disposition)
        => disposition switch
        {
            WarrantDisposition.AliveOnly => "alive-only",
            WarrantDisposition.DeadOrAlive => "dead-or-alive",
            _ => $"disposition {disposition}"
        };

    private static string DescribeConfrontationNarration(
        string targetName,
        WantedSuspectConfrontationChoice choice,
        string? declaredWantedIdentityHandle = null)
        => choice switch
        {
            WantedSuspectConfrontationChoice.Surrendered => declaredWantedIdentityHandle is null
                ? $"You confront {targetName} and bring them in alive."
                : $"You confront {targetName} as {declaredWantedIdentityHandle} and bring them in alive.",
            WantedSuspectConfrontationChoice.Fled => declaredWantedIdentityHandle is null
                ? $"You confront {targetName}, but they get away."
                : $"You confront {targetName} as {declaredWantedIdentityHandle}, but they get away.",
            WantedSuspectConfrontationChoice.Killed => declaredWantedIdentityHandle is null
                ? $"You confront {targetName} and secure the body."
                : $"You confront {targetName} as {declaredWantedIdentityHandle} and secure the body.",
            WantedSuspectConfrontationChoice.Abandoned => declaredWantedIdentityHandle is null
                ? $"You back away before confronting {targetName}."
                : $"You back away before confronting {targetName} as {declaredWantedIdentityHandle}.",
            _ => declaredWantedIdentityHandle is null
                ? $"You confront {targetName}."
                : $"You confront {targetName} as {declaredWantedIdentityHandle}."
        };

    private static string BuildCitizenRevealNarration(string concealmentDescriptor, string? roleKey, decimal fineAmount)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            return $"You bring {concealmentDescriptor} to the sheriff, but the declaration is wrong. The sheriff releases them and fines you ${fineAmount:0.00}.";
        }

        var role = CitizenCast.GetRoleByKey(roleKey);
        return $"You bring {concealmentDescriptor} to the sheriff. The sheriff identifies them as {role.DisplayName}, releases them, and fines you ${fineAmount:0.00}.";
    }

    private static WantedSuspectConfronted BuildWantedSuspectConfrontedEvent(
        SuspectId targetSuspectId,
        Warrant warrant,
        WantedSuspectConfrontationChoice choice,
        string? declaredWantedIdentityHandle)
    {
        var (isAlive, isSecured) = choice switch
        {
            WantedSuspectConfrontationChoice.Surrendered => (true, true),
            WantedSuspectConfrontationChoice.Fled => (true, false),
            WantedSuspectConfrontationChoice.Killed => (false, true),
            _ => ((bool?)null, (bool?)null)
        };

        return new WantedSuspectConfronted
        {
            TargetSuspectId = targetSuspectId,
            TargetName = warrant.TargetName,
            Disposition = warrant.Terms.Disposition,
            Choice = choice,
            Outcome = (WantedSuspectConfrontationOutcome)choice,
            IsAlive = isAlive ?? false,
            IsSecured = isSecured ?? false,
            Message = DescribeConfrontationNarration(warrant.TargetName, choice, declaredWantedIdentityHandle),
            DeclaredWantedIdentityHandle = declaredWantedIdentityHandle
        };
    }

    private static SaloonPersonOfInterestConfronted BuildSaloonConfrontedEvent(
        string message,
        string? declaredWantedIdentityHandle,
        SuspectId? targetSuspectId = null,
        string targetName = "",
        SaloonPersonOfInterestKind personOfInterestKind = SaloonPersonOfInterestKind.WantedSuspect,
        SaloonPersonOfInterestConfrontationOutcome outcome = SaloonPersonOfInterestConfrontationOutcome.Rejected,
        bool? isAlive = null,
        bool? isSecured = null,
        decimal? fineAmount = null,
        decimal? walletBefore = null,
        bool isCitizen = false,
        string? citizenRole = null)
        => new()
        {
            Message = message,
            TargetSuspectId = targetSuspectId,
            TargetName = targetName,
            PersonOfInterestKind = personOfInterestKind,
            Outcome = outcome,
            IsAlive = isAlive,
            IsSecured = isSecured,
            FineAmount = fineAmount,
            WalletBefore = walletBefore,
            WalletAfter = fineAmount is { } fine && walletBefore is { } before ? before - fine : walletBefore,
            DeclaredWantedIdentityHandle = declaredWantedIdentityHandle,
            IsCitizen = isCitizen,
            CitizenRole = citizenRole
        };

    internal void RestoreUnrelatedCriminalLedger(UnrelatedCriminalLedger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        _unrelatedCriminalLedger = ledger;
    }

    internal void RestorePendingDevSaloonOverride(DevSaloonOverride? overrideValue)
    {
        _pendingDevSaloonOverride = overrideValue;
    }

    // --- Apply methods for owned-state mutations ---

    internal void Apply(WantedSuspectConfronted e)
    {
        if (e.Outcome is not WantedSuspectConfrontationOutcome.Abandoned)
        {
            UpdateWantedSuspectPresence(e.TargetSuspectId, e.Choice);
        }
    }

    internal void Apply(SheriffTurnInSettled e)
    {
        _unrelatedCriminalLedger.RecordGangMemberTakenIn();
    }

    internal void Apply(UnrelatedCriminalTurnInSettled e)
    {
        _unrelatedCriminalLedger.MarkWarrantCollected(e.WarrantId);
        _unrelatedCriminalLedger.RecordTakenIn(e.WarrantId);
    }

    internal void Apply(DevSaloonOverrideForced e)
    {
        _pendingDevSaloonOverride = new DevSaloonOverride(
            e.ForcedKind,
            e.ForcedSuspectId,
            e.ForcedCitizenRoleKey);
    }

    internal void Apply(DevSaloonOverrideCleared e)
    {
        _pendingDevSaloonOverride = null;
    }

    internal void Apply(DevSaloonOverrideConsumed e)
    {
        _pendingDevSaloonOverride = null;
    }

    private void UpdateWantedSuspectPresence(SuspectId suspectId, WantedSuspectConfrontationChoice choice)
    {
        var nextPresenceState = choice switch
        {
            WantedSuspectConfrontationChoice.Surrendered => WantedSuspectPresenceState.SecuredAlive,
            WantedSuspectConfrontationChoice.Fled => WantedSuspectPresenceState.GoneToGround,
            WantedSuspectConfrontationChoice.Killed => WantedSuspectPresenceState.SecuredDead,
            _ => WantedSuspectPresenceState.Unavailable
        };

        if (nextPresenceState != WantedSuspectPresenceState.Unavailable)
        {
            _presenceLedger.SetState(suspectId, nextPresenceState);
        }
    }

    /// <summary>
    /// Saloon look-around decision logic. Receives narrow context, returns the
    /// investigation result plus events for GameSession to produce. Consumes
    /// the pending dev override (owned state) if present.
    /// </summary>
    internal BountyLoopResult<CaseInvestigationResult> LookAroundSaloon(SaloonLookAroundContext context)
    {
        var events = new List<IDomainEvent>();

        // Dev override: capture the pending override before producing the consumed event,
        // because GameSession's Apply(DevSaloonOverrideConsumed) will clear
        // _pendingDevSaloonOverride. The forced POI must be built from the captured value.
        // See BUNCH-90.
        var pendingDevOverride = context.PendingDevOverride;

        if (pendingDevOverride is not null)
        {
            events.Add(new DevSaloonOverrideConsumed());

            // Build the forced POI from the captured override value.
            if (pendingDevOverride.ForcedKind is DevSaloonPoiKind.Suspect)
            {
                Suspect? forcedSuspect = null;
                if (pendingDevOverride.ForcedSuspectId is not null)
                {
                    // Specific suspect was forced - validated at force time.
                    forcedSuspect = context.EligibleSuspects.FirstOrDefault(s => s.Id == pendingDevOverride.ForcedSuspectId);
                }

                // If no specific suspect or the specific suspect is not found, use normal candidate selection.
                if (forcedSuspect is null && pendingDevOverride.ForcedSuspectId is null)
                {
                    forcedSuspect = context.EligibleSuspects.FirstOrDefault();
                }

                if (forcedSuspect is not null)
                {
                    var descriptor = SaloonPersonOfInterestDescriptor.Describe(forcedSuspect, context.KnownWarrants);
                    var spotMessage = $"You look around the saloon and spot {descriptor}.";
                    events.Add(new SaloonPersonOfInterestSpotted
                    {
                        SourceKind = InvestigationSourceKind.SaloonLookAround,
                        TownId = context.TownId,
                        Message = spotMessage,
                        SuspectId = forcedSuspect.Id,
                        Descriptor = descriptor,
                        PersonOfInterestKind = SaloonPersonOfInterestKind.WantedSuspect,
                        RecordLog = true
                    });
                    return new BountyLoopResult<CaseInvestigationResult>(
                        CaseInvestigationResult.Succeeded(spotMessage, sessionChanged: true), events);
                }
            }
            else if (pendingDevOverride.ForcedKind is DevSaloonPoiKind.None)
            {
                // Nobody of interest — the saloon is quiet.
                var nobodyMessage = "You look around the saloon, but nobody of interest catches your eye.";
                events.Add(new SaloonPersonOfInterestSpotted
                {
                    SourceKind = InvestigationSourceKind.SaloonLookAround,
                    TownId = context.TownId,
                    Message = nobodyMessage,
                    RecordLog = true
                });
                return new BountyLoopResult<CaseInvestigationResult>(
                    CaseInvestigationResult.Succeeded(nobodyMessage, sessionChanged: true), events);
            }
            else
            {
                // Citizen - spots a citizen from the source-backed cast.
                // The false-lead outcome comes from the normal confrontation flow
                // when the player declares a wrong wanted identity on a citizen POI.
                // Citizen features are drawn from the shared suspect feature vocabulary.
                var forcedFeatureDescriptions = context.SuspectFeatureDescriptions;
                CitizenEncounter forcedEncounter;
                if (pendingDevOverride.ForcedCitizenRoleKey is not null)
                {
                    forcedEncounter = context.CitizenSelectByRoleKey(pendingDevOverride.ForcedCitizenRoleKey, forcedFeatureDescriptions);
                }
                else
                {
                    forcedEncounter = context.CitizenSelect(context.TownId, context.Day, context.Turn, context.VisitNumber, forcedFeatureDescriptions);
                }
                var forcedCitizenDescriptor = context.CitizenDescriptorResolver(forcedEncounter);
                var forcedCitizenMessage = $"You look around the saloon and spot {forcedCitizenDescriptor}.";
                events.Add(new SaloonPersonOfInterestSpotted
                {
                    SourceKind = InvestigationSourceKind.SaloonLookAround,
                    TownId = context.TownId,
                    Message = forcedCitizenMessage,
                    Descriptor = forcedCitizenDescriptor,
                    PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
                    CitizenRole = forcedEncounter.Role.Key,
                    RecordLog = false
                });
                return new BountyLoopResult<CaseInvestigationResult>(
                    CaseInvestigationResult.Succeeded(forcedCitizenMessage, sessionChanged: true), events);
            }
        }

        // Normal path: no dev override active.
        if (context.IsSaloonSourceSpent)
        {
            var repeatMessage = "You look around the saloon again, but nobody of interest is here.";
            events.Add(new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = context.TownId,
                Message = repeatMessage,
                RecordLog = true
            });
            return new BountyLoopResult<CaseInvestigationResult>(
                CaseInvestigationResult.Succeeded(repeatMessage, sessionChanged: true), events);
        }

        // BUNCH-106: Simplified saloon POI selection.
        // The candidate pool is: each eligible non-culprit suspect + each citizen role + one "nobody" slot.
        // Any non-culprit suspect can walk into any saloon — no town presence, warrant, or poster gates.
        // The true killer is excluded until the killer-release gate opens.
        // The roll is deterministic using the salt source + town + day + turn + visit number.
        var eligibleSuspects = context.EligibleSuspects;
        var citizenRoleCount = context.CitizenRoleCount;
        var poolSize = eligibleSuspects.Count + citizenRoleCount + 1; // +1 for "nobody"
        var rollHash = StableSaloonRollHash(context.TownId, context.Day, context.Turn, context.VisitNumber, context.Salt);
        var rollIndex = rollHash % poolSize;

        // Nobody of interest.
        if (rollIndex == poolSize - 1)
        {
            var nobodyMessage = "You look around the saloon, but nobody of interest catches your eye.";
            events.Add(new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = context.TownId,
                Message = nobodyMessage,
                RecordLog = true
            });
            return new BountyLoopResult<CaseInvestigationResult>(
                CaseInvestigationResult.Succeeded(nobodyMessage, sessionChanged: true), events);
        }

        // Suspect slot.
        if (rollIndex < eligibleSuspects.Count)
        {
            var suspect = eligibleSuspects[rollIndex];
            var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, context.KnownWarrants);
            var spotMessage = $"You look around the saloon and spot {descriptor}.";
            events.Add(new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = context.TownId,
                Message = spotMessage,
                SuspectId = suspect.Id,
                Descriptor = descriptor,
                PersonOfInterestKind = SaloonPersonOfInterestKind.WantedSuspect,
                RecordLog = true
            });
            return new BountyLoopResult<CaseInvestigationResult>(
                CaseInvestigationResult.Succeeded(spotMessage, sessionChanged: true), events);
        }

        // Citizen slot.
        var citizenFeatureDescriptions = context.SuspectFeatureDescriptions;
        var citizenEncounter = context.CitizenSelect(context.TownId, context.Day, context.Turn, context.VisitNumber, citizenFeatureDescriptions);
        var citizenDescriptor = context.CitizenDescriptorResolver(citizenEncounter);
        var citizenMessage = $"You look around the saloon and spot {citizenDescriptor}.";
        events.Add(new SaloonPersonOfInterestSpotted
        {
            SourceKind = InvestigationSourceKind.SaloonLookAround,
            TownId = context.TownId,
            Message = citizenMessage,
            Descriptor = citizenDescriptor,
            PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
            CitizenRole = citizenEncounter.Role.Key,
            RecordLog = false
        });
        return new BountyLoopResult<CaseInvestigationResult>(
            CaseInvestigationResult.Succeeded(citizenMessage, sessionChanged: true), events);
    }

    /// <summary>
    /// Stable manual hash for deterministic saloon POI rolls. Uses the salt source
    /// so different sessions get different rolls for the same town/day/turn/visit.
    /// Does NOT use <see cref="string.GetHashCode()"/> (not stable across restarts).
    /// </summary>
    private static int StableSaloonRollHash(TownId townId, int day, int turn, int visitNumber, string salt)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in salt)
            {
                hash = (hash * 31) + c;
            }
            foreach (var c in townId.Value)
            {
                hash = (hash * 31) + c;
            }
            hash = (hash * 31) + day;
            hash = (hash * 31) + turn;
            hash = (hash * 31) + visitNumber;
            return Math.Abs(hash);
        }
    }
}
