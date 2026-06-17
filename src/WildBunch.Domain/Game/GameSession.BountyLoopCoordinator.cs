using WildBunch.Domain.Cases;

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
                    _session.CurrentTownVisit.CurrentTownState.ClearActiveSaloonPersonOfInterest();
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
                        _session.CurrentTownVisit.CurrentTownState.ClearActiveSaloonPersonOfInterest();
                        return SaloonPersonOfInterestConfrontationResult.Rejected(
                            $"{targetSuspect.Name} is no longer in the saloon.",
                            declaredWantedIdentityHandle,
                            targetSuspect.Name,
                            sessionChanged: true,
                            personOfInterestKind: activeSaloonPersonOfInterestKind);
                    }

                    if (_session.CaseFile.TryGetWantedSuspectConfrontationState(activeSaloonSuspect, out var existingState))
                    {
                        _session.CurrentTownVisit.CurrentTownState.ClearActiveSaloonPersonOfInterest();
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
                            _session.CurrentTownVisit.CurrentTownState.ClearActiveSaloonPersonOfInterest();
                            return SaloonPersonOfInterestConfrontationResult.Rejected(
                                settlementResult.Message,
                                declaredWantedIdentityHandle,
                                activeSaloonWarrant.TargetName,
                                activeSaloonWarrant.Terms.Disposition,
                                sessionChanged: true,
                                personOfInterestKind: activeSaloonPersonOfInterestKind);
                        }

                        _session.CurrentTownVisit.CurrentTownState.ClearActiveSaloonPersonOfInterest();
                        var settlementMessage = $"{armedWantedResult.Message} The sheriff pays you ${settlementResult.BountyAmount:0.00}.";
                        return SaloonPersonOfInterestConfrontationResult.FromWantedSuspectResult(armedWantedResult) with
                        {
                            Message = settlementMessage
                        };
                    }

                    if (hasFirearmThreatAvailable && !string.IsNullOrWhiteSpace(declaredWantedIdentityHandle))
                    {
                        var wantedWalletBefore = _session.Player.Wallet.Cash;
                        var wantedFineAmount = BountySettlementPolicy.CalculateCappedFine(wantedWalletBefore, 10m);
                        if (wantedFineAmount > 0m)
                        {
                            _session.Player.AdjustCash(-wantedFineAmount);
                        }

                        _session.CurrentTownVisit.CurrentTownState.ClearActiveSaloonPersonOfInterest();
                        var publicTargetName = activeSaloonPersonOfInterestDescriptor ?? "the person of interest";
                        var wrongDeclarationMessage = $"You bring {publicTargetName} to the sheriff, but the declaration is wrong. The sheriff releases them and fines you ${wantedFineAmount:0.00}.";
                        return SaloonPersonOfInterestConfrontationResult.WrongWantedDeclaration(
                            declaredWantedIdentityHandle,
                            publicTargetName,
                            wrongDeclarationMessage,
                            wantedFineAmount,
                            wantedWalletBefore,
                            _session.Player.Wallet.Cash,
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
                        _session.CurrentTownVisit.CurrentTownState.ClearActiveSaloonPersonOfInterest();
                    }

                    return SaloonPersonOfInterestConfrontationResult.FromWantedSuspectResult(wantedResult);
                }

                _session.CurrentTownVisit.CurrentTownState.ClearActiveSaloonPersonOfInterest();
                return SaloonPersonOfInterestConfrontationResult.Rejected(
                    "You do not know any wanted identity or warrant to declare, so the opportunity has passed.",
                    declaredWantedIdentityHandle,
                    sessionChanged: true,
                    personOfInterestKind: activeSaloonPersonOfInterestKind);
            }

            var walletBefore = _session.Player.Wallet.Cash;
            var fineAmount = BountySettlementPolicy.CalculateCappedFine(walletBefore, 10m);
            if (fineAmount > 0m)
            {
                _session.Player.AdjustCash(-fineAmount);
            }

            var citizenTargetName = activeSaloonPersonOfInterestDescriptor ?? throw new InvalidOperationException("A citizen person of interest descriptor is required.");
            _session.CurrentTownVisit.CurrentTownState.ClearActiveSaloonPersonOfInterest();
            var citizenNarration = $"You bring {citizenTargetName} to the sheriff, but the declaration is wrong. The sheriff releases them and fines you ${fineAmount:0.00}.";
            return SaloonPersonOfInterestConfrontationResult.WrongWantedDeclaration(
                declaredWantedIdentityHandle,
                citizenTargetName,
                citizenNarration,
                fineAmount,
                walletBefore,
                _session.Player.Wallet.Cash,
                isCitizen: true,
                isAlive: null,
                isSecured: null);
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
                _session.CurrentTownVisit.CurrentTownState.ClearActiveSaloonPersonOfInterest();
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
                _session.RecordCaseUpdate(abandonNarration);
                return WantedSuspectConfrontationResult.Abandoned(
                    declaredWantedIdentityHandle,
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    abandonNarration);
            }

            WantedSuspectConfrontationState? nextState = choice switch
            {
                WantedSuspectConfrontationChoice.Surrendered => new WantedSuspectConfrontationState(
                    targetSuspect.Id,
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    WantedSuspectConfrontationOutcome.Surrendered,
                    IsAlive: true,
                    IsSecured: true,
                    _session.Clock.Day,
                    _session.Clock.Turn + 1),
                WantedSuspectConfrontationChoice.Fled => new WantedSuspectConfrontationState(
                    targetSuspect.Id,
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    WantedSuspectConfrontationOutcome.Fled,
                    IsAlive: true,
                    IsSecured: false,
                    _session.Clock.Day,
                    _session.Clock.Turn + 1),
                WantedSuspectConfrontationChoice.Killed => new WantedSuspectConfrontationState(
                    targetSuspect.Id,
                    warrant.TargetName,
                    warrant.Terms.Disposition,
                    WantedSuspectConfrontationOutcome.Killed,
                    IsAlive: false,
                    IsSecured: true,
                    _session.Clock.Day,
                    _session.Clock.Turn + 1),
                _ => null
            };

            if (nextState is null)
            {
                return WantedSuspectConfrontationResult.Rejected(
                    $"The confrontation choice for {targetSuspect.Name} is not supported.",
                    declaredWantedIdentityHandle,
                    targetSuspect.Name,
                    warrant.Terms.Disposition);
            }

            var narration = GameSession.DescribeConfrontationNarration(warrant.TargetName, choice, declaredWantedIdentityHandle);
            _session.RecordCaseUpdate(narration);
            var resolvedState = nextState! with { Day = _session.Clock.Day, Turn = _session.Clock.Turn };
            _session.CaseFile.RecordWantedSuspectConfrontationState(resolvedState);
            _session.UpdateWantedSuspectPresence(targetSuspectId, choice);

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
            var assessment = AssessSheriffTurnIn(targetSuspectId, isAlive);
            if (!assessment.Success)
            {
                return assessment;
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
                return rejectionResult;
            }

            _session.Player.AdjustCash(settlementState.BountyAmount);
            _session.CaseFile.RecordSheriffTurnInSettlementState(settlementState);

            return assessment with { SessionChanged = true };
        }
    }
}
