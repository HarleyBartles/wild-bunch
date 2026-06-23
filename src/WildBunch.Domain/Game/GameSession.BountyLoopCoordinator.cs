using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;

namespace WildBunch.Domain.Game;

public sealed partial class GameSession
{
    internal sealed class BountyLoopCoordinator
    {
        private readonly GameSession _session;

        internal BountyLoopCoordinator(GameSession session)
        {
            ArgumentNullException.ThrowIfNull(session);
            _session = session;
        }

        public SaloonPersonOfInterestConfrontationResult ConfrontSaloonPersonOfInterest(string? declaredWantedIdentityHandle = null)
        {
            if (_session.IsJourneyModal())
            {
                return SaloonPersonOfInterestConfrontationResult.Rejected(GameSession.JourneyModalBlockMessage, declaredWantedIdentityHandle);
            }

            var activeSaloonSuspectId = _session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId;
            var activeSaloonPersonOfInterestDescriptor = _session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor;
            var activeSaloonPersonOfInterestKind = _session.CurrentTownVisit.CurrentTownState.ResolveActiveSaloonPersonOfInterestKind();
            if (activeSaloonSuspectId is null && activeSaloonPersonOfInterestDescriptor is null)
            {
                return SaloonPersonOfInterestConfrontationResult.Rejected("There is no person of interest waiting in the saloon.");
            }

            if (activeSaloonSuspectId is not null)
            {
                var activeSaloonSuspect = activeSaloonSuspectId.Value;
                var targetSuspect = _session.CaseFile.Suspects.FirstOrDefault(suspect => suspect.Id.Equals(activeSaloonSuspect));
                if (targetSuspect is null)
                {
                    ProduceSaloonConfrontedEvent(
                        "That person of interest is no longer available.",
                        declaredWantedIdentityHandle,
                        targetName: "the person of interest",
                        personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                        outcome: SaloonPersonOfInterestConfrontationOutcome.Rejected);
                    return SaloonPersonOfInterestConfrontationResult.Rejected(
                        "That person of interest is no longer available.",
                        declaredWantedIdentityHandle,
                        sessionChanged: true,
                        personOfInterestKind: activeSaloonPersonOfInterestKind);
                }

                if (_session.TryGetKnownWarrantForSuspect(activeSaloonSuspect, out var activeSaloonWarrant))
                {
                    var presenceState = _session.GetWantedSuspectPresenceState(activeSaloonSuspect);
                    if (presenceState is not (WantedSuspectPresenceState.AvailableInTown or WantedSuspectPresenceState.GoneToGround))
                    {
                        ProduceSaloonConfrontedEvent(
                            $"{targetSuspect.Name} is no longer in the saloon.",
                            declaredWantedIdentityHandle,
                            targetName: targetSuspect.Name,
                            personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                            outcome: SaloonPersonOfInterestConfrontationOutcome.Rejected);
                        return SaloonPersonOfInterestConfrontationResult.Rejected(
                            $"{targetSuspect.Name} is no longer in the saloon.",
                            declaredWantedIdentityHandle,
                            targetSuspect.Name,
                            sessionChanged: true,
                            personOfInterestKind: activeSaloonPersonOfInterestKind);
                    }

                    if (_session.CaseFile.TryGetWantedSuspectConfrontationState(activeSaloonSuspect, out var existingState))
                    {
                        ProduceSaloonConfrontedEvent(
                            $"{existingState.TargetName} has already been confronted.",
                            declaredWantedIdentityHandle,
                            targetName: existingState.TargetName,
                            personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                            outcome: SaloonPersonOfInterestConfrontationOutcome.Rejected);
                        return SaloonPersonOfInterestConfrontationResult.Rejected(
                            $"{existingState.TargetName} has already been confronted.",
                            declaredWantedIdentityHandle,
                            existingState.TargetName,
                            activeSaloonWarrant.Terms.Disposition,
                            sessionChanged: true,
                            personOfInterestKind: activeSaloonPersonOfInterestKind);
                    }

                    var hasFirearmThreatAvailable = _session.Player.GetCapabilities(_session.TravelRules).FirearmThreatAvailable;
                    var isDeclaredWantedIdentityForThisWarrant =
                        BountyDeclarationMatchPolicy.MatchesDeclaredWantedIdentity(declaredWantedIdentityHandle, activeSaloonWarrant);

                    if (hasFirearmThreatAvailable && isDeclaredWantedIdentityForThisWarrant)
                    {
                        var armedWantedResult = ResolveWantedSuspectConfrontation(
                            activeSaloonSuspect,
                            WantedSuspectConfrontationChoice.Surrendered,
                            declaredWantedIdentityHandle);
                        if (!armedWantedResult.Success)
                        {
                            return SaloonPersonOfInterestConfrontationResult.FromWantedSuspectResult(armedWantedResult);
                        }

                        var settlementResult = SettleSheriffTurnIn(activeSaloonSuspect, isAlive: true);
                        if (!settlementResult.Success)
                        {
                            ProduceSaloonConfrontedEvent(
                                settlementResult.Message,
                                declaredWantedIdentityHandle,
                                targetName: activeSaloonWarrant.TargetName,
                                personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                                outcome: SaloonPersonOfInterestConfrontationOutcome.Rejected);
                            return SaloonPersonOfInterestConfrontationResult.Rejected(
                                settlementResult.Message,
                                declaredWantedIdentityHandle,
                                activeSaloonWarrant.TargetName,
                                activeSaloonWarrant.Terms.Disposition,
                                sessionChanged: true,
                                personOfInterestKind: activeSaloonPersonOfInterestKind);
                        }

                        var settlementMessage = $"{armedWantedResult.Message} The sheriff pays you ${settlementResult.BountyAmount:0.00}.";
                        ProduceSaloonConfrontedEvent(
                            settlementMessage,
                            declaredWantedIdentityHandle,
                            targetSuspectId: activeSaloonSuspect,
                            targetName: activeSaloonWarrant.TargetName,
                            personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                            outcome: SaloonPersonOfInterestConfrontationOutcome.Surrendered,
                            isAlive: true,
                            isSecured: true);
                        return SaloonPersonOfInterestConfrontationResult.FromWantedSuspectResult(armedWantedResult) with
                        {
                            Message = settlementMessage
                        };
                    }

                    if (hasFirearmThreatAvailable && !string.IsNullOrWhiteSpace(declaredWantedIdentityHandle))
                    {
                        var wantedWalletBefore = _session.Player.Wallet.Cash;
                        var wantedFineAmount = BountySettlementPolicy.CalculateCappedFine(wantedWalletBefore, GameSession.CitizenDeclarationFine);
                        var publicTargetName = activeSaloonPersonOfInterestDescriptor ?? "the person of interest";
                        var wrongDeclarationMessage = $"You bring {publicTargetName} to the sheriff, but the declaration is wrong. The sheriff releases them and fines you ${wantedFineAmount:0.00}.";

                        ProduceSaloonConfrontedEvent(
                            wrongDeclarationMessage,
                            declaredWantedIdentityHandle,
                            targetName: publicTargetName,
                            personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                            outcome: SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration,
                            fineAmount: wantedFineAmount,
                            walletBefore: wantedWalletBefore,
                            isCitizen: false,
                            isAlive: true,
                            isSecured: false);
                        return SaloonPersonOfInterestConfrontationResult.WrongWantedDeclaration(
                            declaredWantedIdentityHandle,
                            publicTargetName,
                            wrongDeclarationMessage,
                            wantedFineAmount,
                            wantedWalletBefore,
                            wantedWalletBefore - wantedFineAmount,
                            isCitizen: false,
                            isAlive: true,
                            isSecured: false);
                    }

                    var wantedResult = ResolveWantedSuspectConfrontation(
                        activeSaloonSuspect,
                        WantedSuspectConfrontationChoice.Fled,
                        declaredWantedIdentityHandle);
                    if (wantedResult.Success)
                    {
                        ProduceSaloonConfrontedEvent(
                            wantedResult.Message,
                            declaredWantedIdentityHandle,
                            targetSuspectId: activeSaloonSuspect,
                            targetName: activeSaloonWarrant.TargetName,
                            personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                            outcome: SaloonPersonOfInterestConfrontationOutcome.Fled,
                            isAlive: true,
                            isSecured: false);
                    }

                    return SaloonPersonOfInterestConfrontationResult.FromWantedSuspectResult(wantedResult);
                }

                ProduceSaloonConfrontedEvent(
                    "You do not know any wanted identity or warrant to declare, so the opportunity has passed.",
                    declaredWantedIdentityHandle,
                    targetName: targetSuspect.Name,
                    personOfInterestKind: activeSaloonPersonOfInterestKind ?? SaloonPersonOfInterestKind.WantedSuspect,
                    outcome: SaloonPersonOfInterestConfrontationOutcome.Rejected);
                return SaloonPersonOfInterestConfrontationResult.Rejected(
                    "You do not know any wanted identity or warrant to declare, so the opportunity has passed.",
                    declaredWantedIdentityHandle,
                    sessionChanged: true,
                    personOfInterestKind: activeSaloonPersonOfInterestKind);
            }

            var walletBefore = _session.Player.Wallet.Cash;
            var fineAmount = BountySettlementPolicy.CalculateCappedFine(walletBefore, GameSession.CitizenDeclarationFine);
            var citizenTargetName = activeSaloonPersonOfInterestDescriptor ?? throw new InvalidOperationException("A citizen person of interest descriptor is required.");
            var citizenNarration = $"You bring {citizenTargetName} to the sheriff, but the declaration is wrong. The sheriff releases them and fines you ${fineAmount:0.00}.";

            ProduceSaloonConfrontedEvent(
                citizenNarration,
                declaredWantedIdentityHandle,
                targetName: citizenTargetName,
                personOfInterestKind: SaloonPersonOfInterestKind.Citizen,
                outcome: SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration,
                fineAmount: fineAmount,
                walletBefore: walletBefore,
                isCitizen: true);
            return SaloonPersonOfInterestConfrontationResult.WrongWantedDeclaration(
                declaredWantedIdentityHandle,
                citizenTargetName,
                citizenNarration,
                fineAmount,
                walletBefore,
                walletBefore - fineAmount,
                isCitizen: true,
                isAlive: null,
                isSecured: null);
        }

        /// <summary>
        /// Produces a <see cref="SaloonPersonOfInterestConfronted"/> event via the session's
        /// event-sourcing pipeline. The Apply method clears the active saloon person and
        /// applies any fine. WalletAfter is computed from WalletBefore - FineAmount.
        /// </summary>
        private void ProduceSaloonConfrontedEvent(
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
            bool isCitizen = false)
        {
            var e = new SaloonPersonOfInterestConfronted
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
                IsCitizen = isCitizen
            };
            _session.ProduceEvent(e);
        }

        public WantedSuspectConfrontationResult ConfrontSaloonWantedSuspect(string? declaredWantedIdentityHandle = null)
        {
            var activeSaloonSuspectId = _session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId;
            if (activeSaloonSuspectId is null)
            {
                return WantedSuspectConfrontationResult.Rejected(
                    "There is no wanted suspect waiting in the saloon.",
                    declaredWantedIdentityHandle);
            }

            var targetSuspect = _session.CaseFile.Suspects.FirstOrDefault(suspect => suspect.Id.Equals(activeSaloonSuspectId));
            if (targetSuspect is null)
            {
                return WantedSuspectConfrontationResult.Rejected(
                    "That person is not part of this case.",
                    declaredWantedIdentityHandle);
            }

            if (!_session.TryGetKnownWarrantForSuspect(targetSuspect.Id, out _))
            {
                ProduceSaloonConfrontedEvent(
                    $"There is no wanted notice for {targetSuspect.Name}.",
                    declaredWantedIdentityHandle,
                    targetSuspectId: targetSuspect.Id,
                    targetName: targetSuspect.Name,
                    personOfInterestKind: SaloonPersonOfInterestKind.WantedSuspect,
                    outcome: SaloonPersonOfInterestConfrontationOutcome.Rejected);
                return WantedSuspectConfrontationResult.Rejected(
                    $"There is no wanted notice for {targetSuspect.Name}.",
                    declaredWantedIdentityHandle,
                    targetSuspect.Name,
                    sessionChanged: true);
            }

            return GameSession.ResolveSaloonPersonOfInterestCompatibilityResult(ConfrontSaloonPersonOfInterest(declaredWantedIdentityHandle));
        }

        public WantedSuspectConfrontationResult ResolveWantedSuspectConfrontation(
            SuspectId targetSuspectId,
            WantedSuspectConfrontationChoice choice,
            string? declaredWantedIdentityHandle = null)
        {
            if (_session.IsJourneyModal())
            {
                return WantedSuspectConfrontationResult.Rejected(GameSession.JourneyModalBlockMessage, declaredWantedIdentityHandle);
            }

            // BUNCH-80 review feedback: direct confrontation must not bypass the active
            // POI/location precondition. The confrontation itself is a same-context action
            // and does not advance time, but it is only valid when the player is already in
            // an appropriate active POI context with the target present. For this first
            // version that means the saloon POI loop. The rule lives behind
            // <see cref="GameSession.CanConfrontWantedSuspectInCurrentContext"/> so future
            // non-saloon POI locations can extend it without weakening the call-site check.
            if (!_session.CanConfrontWantedSuspectInCurrentContext(targetSuspectId))
            {
                return WantedSuspectConfrontationResult.Rejected(
                    "You can only confront a wanted suspect who is present in your current location.",
                    declaredWantedIdentityHandle);
            }

            var targetSuspect = _session.CaseFile.Suspects.FirstOrDefault(suspect => suspect.Id.Equals(targetSuspectId));
            if (targetSuspect is null)
            {
                return WantedSuspectConfrontationResult.Rejected(
                    "That person is not part of this case.",
                    declaredWantedIdentityHandle);
            }

            var warrant = _session.CaseFile.KnownWarrants.FirstOrDefault(candidate => GameSession.MatchesKnownWarrant(candidate, targetSuspect));
            if (warrant is null)
            {
                return WantedSuspectConfrontationResult.Rejected(
                    $"There is no wanted notice for {targetSuspect.Name}.",
                    declaredWantedIdentityHandle,
                    targetSuspect.Name);
            }

            if (_session.CaseFile.TryGetWantedSuspectConfrontationState(targetSuspectId, out var existingState))
            {
                return WantedSuspectConfrontationResult.Rejected(
                    $"{existingState.TargetName} has already been confronted.",
                    declaredWantedIdentityHandle,
                    existingState.TargetName,
                    existingState.Disposition);
            }

            if (choice == WantedSuspectConfrontationChoice.Abandoned)
            {
                var abandonNarration = GameSession.DescribeConfrontationNarration(warrant.TargetName, choice, declaredWantedIdentityHandle);
                var abandonEvent = new WantedSuspectConfronted
                {
                    TargetSuspectId = targetSuspectId,
                    TargetName = warrant.TargetName,
                    Disposition = warrant.Terms.Disposition,
                    Choice = WantedSuspectConfrontationChoice.Abandoned,
                    Outcome = WantedSuspectConfrontationOutcome.Abandoned,
                    IsAlive = true,
                    IsSecured = false,
                    Message = abandonNarration,
                    DeclaredWantedIdentityHandle = declaredWantedIdentityHandle
                };
                _session.ProduceEvent(abandonEvent);
                return WantedSuspectConfrontationResult.Abandoned(
                    declaredWantedIdentityHandle,
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    abandonNarration);
            }

            var (isAlive, isSecured) = choice switch
            {
                WantedSuspectConfrontationChoice.Surrendered => (true, true),
                WantedSuspectConfrontationChoice.Fled => (true, false),
                WantedSuspectConfrontationChoice.Killed => (false, true),
                _ => ((bool?)null, (bool?)null)
            };

            if (isAlive is null)
            {
                return WantedSuspectConfrontationResult.Rejected(
                    $"The confrontation choice for {targetSuspect.Name} is not supported.",
                    declaredWantedIdentityHandle,
                    targetSuspect.Name,
                    warrant.Terms.Disposition);
            }

            var narration = GameSession.DescribeConfrontationNarration(warrant.TargetName, choice, declaredWantedIdentityHandle);
            var confrontationEvent = new WantedSuspectConfronted
            {
                TargetSuspectId = targetSuspectId,
                TargetName = warrant.TargetName,
                Disposition = warrant.Terms.Disposition,
                Choice = choice,
                Outcome = (WantedSuspectConfrontationOutcome)choice,
                IsAlive = isAlive!.Value,
                IsSecured = isSecured!.Value,
                Message = narration,
                DeclaredWantedIdentityHandle = declaredWantedIdentityHandle
            };
            _session.ProduceEvent(confrontationEvent);

            return choice switch
            {
                WantedSuspectConfrontationChoice.Surrendered => WantedSuspectConfrontationResult.Surrendered(
                    declaredWantedIdentityHandle,
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    narration),
                WantedSuspectConfrontationChoice.Fled => WantedSuspectConfrontationResult.Fled(
                    declaredWantedIdentityHandle,
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    narration),
                WantedSuspectConfrontationChoice.Killed => WantedSuspectConfrontationResult.Killed(
                    declaredWantedIdentityHandle,
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    narration),
                WantedSuspectConfrontationChoice.Abandoned => WantedSuspectConfrontationResult.Abandoned(
                    declaredWantedIdentityHandle,
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    narration),
                _ => WantedSuspectConfrontationResult.Rejected(
                    $"The confrontation choice for {targetSuspect.Name} is not supported.",
                    declaredWantedIdentityHandle,
                    targetSuspect.Name,
                    warrant.Terms.Disposition)
            };
        }

        public SheriffTurnInResult AssessSheriffTurnIn(SuspectId targetSuspectId, bool isAlive)
        {
            if (_session.IsJourneyModal())
            {
                return SheriffTurnInResult.Rejected(GameSession.JourneyModalBlockMessage);
            }

            var targetSuspect = _session.CaseFile.Suspects.FirstOrDefault(suspect => suspect.Id.Equals(targetSuspectId));
            if (targetSuspect is null)
            {
                return isAlive
                    ? SheriffTurnInResult.WrongPersonAlive("That person is not part of this case.")
                    : SheriffTurnInResult.WrongPersonDead("That person is not part of this case.");
            }

            var warrant = _session.CaseFile.KnownWarrants.FirstOrDefault(candidate => GameSession.MatchesKnownWarrant(candidate, targetSuspect));
            if (warrant is null)
            {
                return isAlive
                    ? SheriffTurnInResult.WrongPersonAlive($"There is no wanted notice for {targetSuspect.Name}.", targetSuspect.Name)
                    : SheriffTurnInResult.WrongPersonDead($"There is no wanted notice for {targetSuspect.Name}.", targetSuspect.Name);
            }

            if (!_session.CaseFile.TryGetWantedSuspectConfrontationState(targetSuspectId, out var confrontationState))
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

            if (isAlive && !confrontationState.IsAlive)
            {
                return SheriffTurnInResult.Rejected(
                    $"{warrant.TargetName} is not alive anymore.",
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    warrant.Terms.BountyAmount);
            }

            if (!isAlive && confrontationState.IsAlive)
            {
                return SheriffTurnInResult.Rejected(
                    $"{warrant.TargetName} was secured alive and cannot be turned in dead.",
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    warrant.Terms.BountyAmount);
            }

            if (isAlive)
            {
                return SheriffTurnInResult.AcceptedAlive(
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    warrant.Terms.BountyAmount,
                    $"You bring in {warrant.TargetName} alive under a {GameSession.DescribeWarrantDisposition(warrant.Terms.Disposition)} warrant.");
            }

            if (warrant.Terms.Disposition == WarrantDisposition.DeadOrAlive)
            {
                return SheriffTurnInResult.AcceptedDead(
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    warrant.Terms.BountyAmount,
                    $"You turn in the body of {warrant.TargetName} under a {GameSession.DescribeWarrantDisposition(warrant.Terms.Disposition)} warrant.");
            }

            return SheriffTurnInResult.Rejected(
                $"The warrant for {warrant.TargetName} requires an alive turn-in.",
                warrant.TargetName,
                warrant.Terms.Disposition,
                warrant.Terms.BountyAmount);
        }

        public SheriffTurnInResult SettleSheriffTurnIn(SuspectId targetSuspectId, bool isAlive)
        {
            // Enter SheriffOffice context BEFORE assessment. This emits a TownActionContextEntered
            // event if the context changed (advances turn). Even rejected turn-ins produce the
            // context event — going to the sheriff's office takes time regardless of outcome.
            // Track whether the context entry mutated the session so rejected results can
            // truthfully report SessionChanged. See BUNCH-80 review feedback.
            var contextChanged = _session.EnterActionContext(TownActionContext.SheriffOffice);

            var assessment = AssessSheriffTurnIn(targetSuspectId, isAlive);
            if (!assessment.Success)
            {
                return contextChanged ? assessment.WithSessionChanged() : assessment;
            }

            if (!BountySettlementPolicy.TryCreateSheriffTurnInSettlementState(
                    _session.CaseFile,
                    assessment,
                    targetSuspectId,
                    isAlive,
                    _session.Clock.Day,
                    _session.Clock.Turn,
                    out var settlementState,
                    out var rejectionResult))
            {
                return contextChanged ? rejectionResult.WithSessionChanged() : rejectionResult;
            }

            var settledEvent = new SheriffTurnInSettled
            {
                TargetSuspectId = targetSuspectId,
                TargetName = assessment.TargetName!,
                Disposition = assessment.Disposition!.Value,
                IsAlive = isAlive,
                BountyAmount = settlementState.BountyAmount,
                Message = assessment.Message!,
                Day = settlementState.Day,
                Turn = settlementState.Turn
            };
            _session.ProduceEvent(settledEvent);

            return assessment with { SessionChanged = true };
        }
    }
}
