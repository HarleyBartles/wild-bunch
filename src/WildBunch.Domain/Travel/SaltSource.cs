using System.Security.Cryptography;

namespace WildBunch.Domain.Travel;

public enum SaltSourceMode
{
    Runtime = 0,
    Fixed = 1
}

public sealed record SaltSource(SaltSourceMode Mode, string Salt)
{
    public static SaltSource CreateRuntime()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return new SaltSource(SaltSourceMode.Runtime, Convert.ToHexString(bytes));
    }

    public static SaltSource CreateFixed(string salt)
    {
        ArgumentNullException.ThrowIfNull(salt);
        return new SaltSource(SaltSourceMode.Fixed, salt);
    }
}
