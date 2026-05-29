namespace WildBunch.Domain.Economy;

public sealed record StorePurchaseResult(bool Success, string Message)
{
    public static StorePurchaseResult Succeeded(string message) => new(true, message);

    public static StorePurchaseResult Failed(string message) => new(false, message);
}
