namespace WildBunch.Application.Dev.Models;

/// <summary>
/// Request DTO for the lock-rng dev endpoint.
/// Salt contract:
///   - null / empty / whitespace → handler generates a fresh 32-char hex fixed salt.
///   - Non-empty string after trimming → handler uses the trimmed value verbatim
///     as the reproducibility token.
/// SaltSource.CreateFixed validates non-null only (no length/format constraint today).
/// If format validation is added to SaltSource later, the handler and tests must surface
/// that error to the caller rather than silently generating.
/// </summary>
public sealed record LockRngRequestDto(string? Salt);
