using System.Security.Cryptography;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Domain.Game;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Commands;

public sealed class ForceDevSaltSourceHandler : GameSessionCommandHandler
{
    public ForceDevSaltSourceHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task HandleAsync(ForceDevSaltSourceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        // Salt contract:
        //   null / empty / whitespace → generate a fresh 32-char hex fixed salt.
        //   Non-empty string after trimming → use the trimmed value verbatim.
        // This trimming is deliberate and tested. Do not drift between
        // "preserve exactly" and "trim" — the contract is trim.
        var salt = string.IsNullOrWhiteSpace(command.Salt)
            ? Convert.ToHexString(RandomNumberGenerator.GetBytes(16))
            : command.Salt.Trim();

        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            session.ForceDevSaltSource(SaltSource.CreateFixed(salt));
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);
    }
}
