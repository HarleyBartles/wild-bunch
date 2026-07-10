using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal sealed class GameSetupDeterministicSource
{
    public GameSetupDeterministicSource(string seedCode, LayoutSalts? layoutSalts = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedCode);
        SeedCode = seedCode;
        LayoutSalts = layoutSalts;
    }

    public string SeedCode { get; }
    public LayoutSalts? LayoutSalts { get; }

    public ulong Roll(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        
        // Include layout salts in the hash when available for deterministic layout control
        var hashInput = LayoutSalts is not null
            ? $"{SeedCode}|{LayoutSalts.BuildingsSalt}|{LayoutSalts.RoadsSalt}|{LayoutSalts.DirtSalt}|{LayoutSalts.PropsSalt}|{label}"
            : $"{SeedCode}|{label}";
            
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));
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
