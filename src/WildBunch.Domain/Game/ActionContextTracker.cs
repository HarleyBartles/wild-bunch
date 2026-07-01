using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

/// <summary>
/// Child domain component inside the session boundary that owns town action-context
/// state and turn-advancement tracking. Receives narrow context records, returns events
/// to produce. Does NOT reference the parent aggregate, produce events directly, enter
/// action context (it IS the action context), or mutate Clock/PursuitState/CaseFile/Player.
/// See BUNCH-120 and ADR-0002/ADR-0020.
/// </summary>
internal sealed class ActionContextTracker
{
    internal TownActionContext CurrentActionContext { get; private set; } = TownActionContext.None;
    internal TownId? CurrentActionContextTownId { get; private set; }

    /// <summary>
    /// Decides whether entering the given context produces a TownActionContextEntered event.
    /// Returns the event to produce, or null if no-op (None context, or same context in same town).
    /// Does NOT mutate Clock or PursuitState — the event carries the computed values and
    /// the parent aggregate's Apply handler sets them.
    /// </summary>
    internal TownActionContextEntered? EnterActionContext(
        TownActionContext context,
        ActionContextEnterInputs inputs)
    {
        if (context == TownActionContext.None)
        {
            return null;
        }

        // Same context only suppresses time advancement if it was entered in the same town.
        if (context == CurrentActionContext && inputs.CurrentTownId.Equals(CurrentActionContextTownId))
        {
            return null;
        }

        // Compute resulting clock state (do NOT mutate Clock directly — Apply does that).
        var newTurn = inputs.Clock.Turn + 1;
        var newDay = inputs.Clock.Day;
        var newHeat = inputs.PursuitState.Heat;
        if (newTurn >= 4)
        {
            newDay++;
            newTurn = 0;
            // A full day passed in town — heat increases by 1 (lawman pressure).
            newHeat = inputs.PursuitState.Heat + 1;
        }

        return new TownActionContextEntered
        {
            Context = context,
            TownId = inputs.CurrentTownId,
            Day = newDay,
            Turn = newTurn,
            TimeOfDay = (TimeOfDay)newTurn,
            PursuitHeat = newHeat
        };
    }

    /// <summary>
    /// Named predicate expressing the invariant for direct wanted-suspect confrontation:
    /// confrontation itself does not advance time and is only valid when the player is
    /// already in an appropriate active POI/location context. For this first version the
    /// only supported confrontation context is the saloon POI loop.
    /// </summary>
    internal bool CanConfrontWantedSuspectInCurrentContext(CanConfrontInContextInputs inputs)
    {
        if (CurrentActionContext != TownActionContext.Saloon)
        {
            return false;
        }

        if (CurrentActionContextTownId is null || !CurrentActionContextTownId.Equals(inputs.CurrentTownId))
        {
            return false;
        }

        return inputs.ActiveSaloonPersonOfInterestId is not null
            && inputs.ActiveSaloonPersonOfInterestId.Equals(inputs.TargetSuspectId);
    }

    /// <summary>
    /// Resets CurrentActionContext and CurrentActionContextTownId to None/null.
    /// Called by the parent aggregate when the current town changes.
    /// </summary>
    internal void Reset()
    {
        CurrentActionContext = TownActionContext.None;
        CurrentActionContextTownId = null;
    }

    /// <summary>
    /// Applies a TownActionContextEntered event to mutate owned state.
    /// The parent aggregate's Apply handler calls this for the owned portion, then
    /// applies cross-owner mutations (Clock.Set, PursuitState.SetHeat, _version++).
    /// </summary>
    internal void Apply(TownActionContextEntered e)
    {
        CurrentActionContext = e.Context;
        CurrentActionContextTownId = e.TownId;
    }

    /// <summary>
    /// Restores owned state from a persisted snapshot. Called by the parent aggregate
    /// during rehydration after the constructor builds a fresh ActionContextTracker.
    /// </summary>
    internal void RestoreState(TownActionContext context, TownId? townId)
    {
        CurrentActionContext = context;
        CurrentActionContextTownId = townId;
    }
}

/// <summary>Read-only inputs for an enter-action-context decision.</summary>
internal sealed record ActionContextEnterInputs(
    GameClock Clock,
    PursuitState PursuitState,
    TownId CurrentTownId);

/// <summary>Read-only inputs for a can-confront-wanted-suspect check.</summary>
internal sealed record CanConfrontInContextInputs(
    SuspectId TargetSuspectId,
    TownId CurrentTownId,
    SuspectId? ActiveSaloonPersonOfInterestId);
