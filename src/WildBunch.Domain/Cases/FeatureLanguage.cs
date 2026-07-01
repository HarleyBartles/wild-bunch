namespace WildBunch.Domain.Cases;

public enum FeatureCategory
{
    Limp = 0,
    MissingPart = 1,
    Scar = 2,
    Absence = 3
}

public enum FeatureSide
{
    None = 0,
    Left = 1,
    Right = 2
}

public sealed record FeatureDescriptor(FeatureCategory Category, string BodyPart, FeatureSide Side);

public sealed record FeatureLanguage(
    string HasForm,
    string WithForm,
    string WhoForm,
    string? OpeningLeadForm)
{
    /// <summary>
    /// Constructs a FeatureLanguage from explicit forms, for test fixtures
    /// and non-feature-pool identity facts that don't have structured tokens.
    /// </summary>
    public static FeatureLanguage Raw(string hasForm, string withForm, string? whoForm = null)
        => new(hasForm, withForm, whoForm ?? hasForm.ToLowerInvariant(), null);
}
