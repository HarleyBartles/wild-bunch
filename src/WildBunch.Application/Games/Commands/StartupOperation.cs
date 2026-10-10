using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WildBunch.Application.Games.Commands;

public enum StartupOperationKind
{
    Setup = 1,
    FirstArrival = 2
}

/// <summary>
/// Persisted transport retry identity for the two one-time startup operations.
/// This record does not represent game state; the referenced event stream does.
/// </summary>
public sealed record StartupOperation(
    Guid OperationId,
    StartupOperationKind Kind,
    string RequestFingerprint,
    Guid SessionId);

internal static class StartupOperationFingerprint
{
    public static string ForSetup(CompletePlayerSetupCommand command) => Hash(new
    {
        Version = "wild-bunch-startup-operation-v1",
        Kind = "setup",
        command.PlayerName,
        GameDifficulty = (int)command.GameDifficulty,
        GameEntropy = (int)command.GameEntropy,
        command.SeedCode
    });

    public static string ForFirstArrival(CompleteGameStartCommand command) => Hash(new
    {
        Version = "wild-bunch-startup-operation-v1",
        Kind = "first-arrival",
        SessionId = command.SessionId.Value,
        command.StartingTownId
    });

    private static string Hash<T>(T request)
    {
        var json = JsonSerializer.Serialize(request);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}
