namespace WildBunch.GameContent.NewGame;

public static class StartingWorldDescriptorCodeValidator
{
    public static bool TryValidate(string? seedCode, out string? errorMessage)
    {
        if (!SeedWorldResolver.TryParseSeedCode(seedCode, out _))
        {
            errorMessage = "Seed code must be a UUID-shaped string.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
