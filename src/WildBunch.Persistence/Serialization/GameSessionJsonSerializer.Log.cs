using WildBunch.Domain.Game;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    private sealed record GameLogEntrySnapshot(GameLogEntryKind Kind, string Message, int Day, int Turn)
    {
        public static GameLogEntrySnapshot FromDomain(GameLogEntry entry)
            => new(entry.Kind, entry.Message, entry.Day, entry.Turn);

        public static GameLogEntry ToDomain(GameLogEntrySnapshot snapshot)
            => new(snapshot.Kind, snapshot.Message, snapshot.Day, snapshot.Turn);
    }
}
