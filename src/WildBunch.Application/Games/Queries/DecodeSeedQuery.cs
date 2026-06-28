namespace WildBunch.Application.Games.Queries;

/// <summary>
/// Query to decode a seed and return the encoded difficulty and entropy.
/// Used by the frontend's seed editor to reflect the seed's encoded values.
/// </summary>
public sealed record DecodeSeedQuery(string SeedCode);
