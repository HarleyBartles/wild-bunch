using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WildBunch.GameContent.NewGame;

internal static class StartingWorldDescriptorSeedMixer
{
    private const string ResolverNamespace = "wild-bunch.gamecontent.starting-world-descriptor";
    private const string ResolverVersion = SeedWorldResolver.ResolverContractVersion;

    public static ulong CreateSeedRoot(Guid seedCode)
    {
        var canonicalSeedCode = seedCode.ToString("D");
        var material = string.Concat(ResolverNamespace, "|", ResolverVersion, "|", canonicalSeedCode);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));

        var root = BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(0, sizeof(ulong)))
            ^ BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(sizeof(ulong), sizeof(ulong)))
            ^ BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(sizeof(ulong) * 2, sizeof(ulong)))
            ^ BinaryPrimitives.ReadUInt64LittleEndian(hash.AsSpan(sizeof(ulong) * 3, sizeof(ulong)));

        return Mix64(root);
    }

    public static ulong GetFieldSeed(ulong seedRoot, string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        var labelHash = Fnv1a64(Encoding.UTF8.GetBytes(label));
        return Mix64(seedRoot ^ labelHash);
    }

    public static string CreateSeedWorldSignature(SeedWorld seedWorld)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);

        return string.Join(
            "|",
            seedWorld.WorldVariant.ToString(),
            seedWorld.TownSetKey,
            seedWorld.AccusationIndex.ToString(CultureInfo.InvariantCulture),
            seedWorld.DefaultCulpritIndex.ToString(CultureInfo.InvariantCulture),
            seedWorld.CashBonus.ToString(CultureInfo.InvariantCulture));
    }

    public static Guid CreateCandidateSeed(string descriptorSignature, ulong salt, int attempt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptorSignature);
        if (attempt < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), attempt, "Attempt must be non-negative.");
        }

        var material = string.Concat(
            ResolverNamespace,
            "|",
            ResolverVersion,
            "|representative|",
            descriptorSignature,
            "|salt=",
            salt.ToString(CultureInfo.InvariantCulture),
            "|attempt=",
            attempt.ToString(CultureInfo.InvariantCulture));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return new Guid(hash.AsSpan(0, 16).ToArray());
    }

    private static ulong Fnv1a64(ReadOnlySpan<byte> bytes)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        var hash = offsetBasis;
        foreach (var value in bytes)
        {
            hash ^= value;
            hash *= prime;
        }

        return hash;
    }

    private static ulong Mix64(ulong value)
    {
        value ^= value >> 30;
        value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27;
        value *= 0x94D049BB133111EBUL;
        value ^= value >> 31;
        return value;
    }
}
