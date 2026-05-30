namespace WildBunch.GameContent.NewGame;

public static class GameSetupSeedCodeValidator
{
    public static bool TryValidate(string? seedCode, out string? errorMessage)
    {
        var decodeResult = GameSetupSeedCodec.Decode(seedCode);
        errorMessage = decodeResult.Success ? null : decodeResult.ErrorMessage;
        return decodeResult.Success;
    }
}
