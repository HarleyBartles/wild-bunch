namespace WildBunch.Domain.Cases;

public readonly record struct OutlawGangId(string Value);

public static class OutlawGangIds
{
    public static readonly OutlawGangId WildBunch = new("wild-bunch");
}
