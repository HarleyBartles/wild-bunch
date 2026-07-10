using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Layout-scoped deterministic source for town hub layout generation.
/// Uses layout salts to influence only layout-specific decisions, ensuring
/// that dev layout overrides do not leak into unrelated game setup decisions
/// like case file choices or mystery truth.
/// </summary>
internal sealed class LayoutDeterministicSource
{
    public LayoutDeterministicSource(
        string seedCode,
        TownId townId,
        int townSlot,
        string resolverVersion,
        LayoutSalts layoutSalts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seedCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolverVersion);
        ArgumentNullException.ThrowIfNull(layoutSalts);
        
        SeedCode = seedCode;
        TownId = townId;
        TownSlot = townSlot;
        ResolverVersion = resolverVersion;
        LayoutSalts = layoutSalts;
    }

    public string SeedCode { get; }
    public TownId TownId { get; }
    public int TownSlot { get; }
    public string ResolverVersion { get; }
    public LayoutSalts LayoutSalts { get; }

    /// <summary>
    /// Roll a deterministic value for a layout-specific concern.
    /// The concern salt determines which layout salt influences the roll.
    /// </summary>
    public ulong Roll(string label, LayoutConcern concern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        
        var salt = concern switch
        {
            LayoutConcern.Buildings => LayoutSalts.BuildingsSalt ?? "default-buildings",
            LayoutConcern.Roads => LayoutSalts.RoadsSalt ?? "default-roads",
            LayoutConcern.Dirt => LayoutSalts.DirtSalt ?? "default-dirt",
            LayoutConcern.Props => LayoutSalts.PropsSalt ?? "default-props",
            _ => throw new ArgumentOutOfRangeException(nameof(concern), concern, null)
        };
        
        // Hash includes seed, town identity, resolver version, concern-specific salt, and label
        var hashInput = $"{SeedCode}|{TownId.Value}|{TownSlot}|{ResolverVersion}|{salt}|{label}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));
        return BitConverter.ToUInt64(bytes, 0);
    }

    public int PickIndex(string label, LayoutConcern concern, int count)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be at least 1.");
        }

        return (int)(Roll(label, concern) % (ulong)count);
    }
}

/// <summary>
/// Layout concerns that can be influenced by specific salts.
/// </summary>
internal enum LayoutConcern
{
    Buildings,
    Roads,
    Dirt,
    Props
}
