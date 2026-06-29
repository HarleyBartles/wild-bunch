using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Pressure-owned difficulty envelope. Owns the player-selected difficulty
/// and the difficulty-derived pressure fields (starting cash, loadout posture,
/// horse/saddle posture, travel rules profile). The seed codec does NOT encode
/// this — it is applied downstream of <see cref="SeedWorld"/> by
/// <see cref="GameSetupResolver"/>.
/// BUNCH-94 will expand the difficulty -> pressure mapping here (full
/// horse/saddle envelope, loadout envelope, travel harshness, clue pressure,
/// false-lead pressure, consequence severity).
/// </summary>
public sealed record DifficultyEnvelope(
    GameDifficulty Difficulty,
    decimal StartingCash,
    StartingLoadoutProfile LoadoutProfile,
    bool StartWithHorse,
    bool IncludeSaddle,
    TravelRulesProfile TravelRules)
{
    /// <summary>
    /// Resolves the pressure-owned difficulty envelope for the requested difficulty.
    /// BUNCH-107 ships transitional defaults: ALL difficulties get horse+saddle
    /// and Standard loadout. The only difficulty-owned variation is base cash
    /// and travel rules. BUNCH-94 will expand this mapping to add the full
    /// horse/saddle/loadout/travel/clue pressure envelope.
    /// </summary>
    public static DifficultyEnvelope For(GameDifficulty difficulty)
    {
        var baseCash = difficulty switch
        {
            GameDifficulty.Easy => 28m,
            GameDifficulty.Challenging => 18m,
            GameDifficulty.Brutal => 13m,
            _ => 23m
        };

        // Transitional: all difficulties get Standard loadout (+0) and horse (+2).
        var startingCash = baseCash + 2m;

        return new DifficultyEnvelope(
            difficulty,
            startingCash,
            LoadoutProfile: StartingLoadoutProfile.Standard,
            StartWithHorse: true,
            IncludeSaddle: true,
            TravelRules: TravelRulesProfile.For(difficulty));
    }
}
