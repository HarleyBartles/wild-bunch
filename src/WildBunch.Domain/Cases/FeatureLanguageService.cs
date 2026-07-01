namespace WildBunch.Domain.Cases;

public static class FeatureLanguageService
{
    public static FeatureLanguage For(FeatureDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Category switch
        {
            FeatureCategory.Limp => ForLimp(descriptor),
            FeatureCategory.MissingPart => ForMissingPart(descriptor),
            FeatureCategory.Scar => ForScar(descriptor),
            FeatureCategory.Absence => ForAbsence(descriptor),
            _ => throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor.Category, "Unsupported feature category.")
        };
    }

    private static string Location(FeatureDescriptor d)
        => d.Side == FeatureSide.None ? d.BodyPart : $"{SideWord(d.Side)} {d.BodyPart}";

    private static string SideWord(FeatureSide side) => side switch
    {
        FeatureSide.Left => "left",
        FeatureSide.Right => "right",
        _ => string.Empty
    };

    private static FeatureLanguage ForLimp(FeatureDescriptor d)
    {
        var location = Location(d);
        return new FeatureLanguage(
            HasForm: $"Has a limp in the {location}.",
            WithForm: $"a limp in the {location}",
            WhoForm: $"has a limp in the {location}",
            OpeningLeadForm: $"The culprit walks with a limp in the {location}.");
    }

    private static FeatureLanguage ForMissingPart(FeatureDescriptor d)
    {
        var location = Location(d);
        return new FeatureLanguage(
            HasForm: $"Is missing the {location}.",
            WithForm: $"a missing {location}",
            WhoForm: $"is missing the {location}",
            OpeningLeadForm: $"The culprit is missing the {location}.");
    }

    private static FeatureLanguage ForScar(FeatureDescriptor d)
    {
        var location = Location(d);
        return new FeatureLanguage(
            HasForm: $"Has a scar on the {location}.",
            WithForm: $"a scar on the {location}",
            WhoForm: $"has a scar on the {location}",
            OpeningLeadForm: $"The culprit has a scar on the {location}.");
    }

    private static FeatureLanguage ForAbsence(FeatureDescriptor d)
        => new(
            HasForm: $"Has no {d.BodyPart}.",
            WithForm: $"no {d.BodyPart}",
            WhoForm: $"has no {d.BodyPart}",
            OpeningLeadForm: $"The culprit has no {d.BodyPart}.");
}
