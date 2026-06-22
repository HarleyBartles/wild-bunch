namespace WildBunch.Domain.Events;

/// <summary>
/// Marker interface for typed domain events — immutable facts produced by aggregate command methods.
/// Domain events carry only decision data. Envelope metadata (event id, sequence, timestamp, etc.)
/// is infrastructure and lives in WildBunch.Persistence, not here. See ADR-0028.
/// </summary>
public interface IDomainEvent { }
