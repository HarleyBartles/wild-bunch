using System.Security.Cryptography;
using System.Text;

namespace WildBunch.GameContent.NewGame;

public sealed class GameSetupDeterministicSource
{
    public GameSetupDeterministicSource(string seedCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedCode);
        SeedCode = seedCode;
    }

    public string SeedCode { get; }

    public ulong Roll(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{SeedCode}|{label}"));
        return BitConverter.ToUInt64(bytes, 0);
    }

    public int PickIndex(string label, int count)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be at least 1.");
        }

        return (int)(Roll(label) % (ulong)count);
    }
}
