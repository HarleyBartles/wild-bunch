using System.Security.Cryptography;

namespace WildBunch.Domain.Travel;

public enum TravelRandomnessMode
{
    RuntimeSalted = 0,
    Deterministic = 1
}

public sealed record TravelRandomnessState(TravelRandomnessMode Mode, string Salt)
{
    public static TravelRandomnessState CreateRuntimeSalted()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return new TravelRandomnessState(TravelRandomnessMode.RuntimeSalted, Convert.ToHexString(bytes));
    }

    public static TravelRandomnessState CreateDeterministic(string salt)
    {
        ArgumentNullException.ThrowIfNull(salt);
        return new TravelRandomnessState(TravelRandomnessMode.Deterministic, salt);
    }
}
