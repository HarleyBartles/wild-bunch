namespace WildBunch.Domain.Cases;

public readonly record struct SuspectTraitTag(string Value)
{
    public override string ToString() => Value;
}

public static class SuspectTraitTags
{
    public static readonly SuspectTraitTag Local = new("local");
    public static readonly SuspectTraitTag Armed = new("armed");
    public static readonly SuspectTraitTag Desperate = new("desperate");
    public static readonly SuspectTraitTag Bribeable = new("bribeable");
    public static readonly SuspectTraitTag Unbribeable = new("unbribeable");
    public static readonly SuspectTraitTag Tenacious = new("tenacious");
    public static readonly SuspectTraitTag Violent = new("violent");
    public static readonly SuspectTraitTag Leader = new("leader");
    public static readonly SuspectTraitTag Enforcer = new("enforcer");
    public static readonly SuspectTraitTag Lookout = new("lookout");
    public static readonly SuspectTraitTag Fence = new("fence");
    public static readonly SuspectTraitTag Rider = new("rider");
    public static readonly SuspectTraitTag Talkative = new("talkative");
    public static readonly SuspectTraitTag WellKnown = new("well-known");
    public static readonly SuspectTraitTag LocalToTurf = new("local-to-turf");
    public static readonly SuspectTraitTag GangLoyal = new("gang-loyal");
    public static readonly SuspectTraitTag Nervous = new("nervous");
    public static readonly SuspectTraitTag Cautious = new("cautious");
}
