using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

internal static class GameSetupSeedCodec
{
    private const string Prefix = "WB1";
    internal const int CurrentGeneratorVersion = 1;

    public static GameSetupSeed CreateCanonicalSeed(TravelDifficulty difficulty = TravelDifficulty.Normal)
        => new(CurrentGeneratorVersion, difficulty, GameSetupOptionsV1.Default, 0);

    public static GameSetupSeed GenerateRandom(GameSetupOptionsV1 options, TravelDifficulty difficulty = TravelDifficulty.Normal)
    {
        ArgumentNullException.ThrowIfNull(options);

        Span<byte> buffer = stackalloc byte[6];
        RandomNumberGenerator.Fill(buffer);
        var entropy = (ulong)buffer[0]
            | ((ulong)buffer[1] << 8)
            | ((ulong)buffer[2] << 16)
            | ((ulong)buffer[3] << 24)
            | ((ulong)buffer[4] << 32)
            | ((ulong)buffer[5] << 40);

        return new GameSetupSeed(CurrentGeneratorVersion, difficulty, options, entropy);
    }

    public static string Encode(GameSetupSeed seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        ValidateVersion(seed.GeneratorVersion);
        if (!seed.IsCanonicalEntropy)
        {
            throw new ArgumentOutOfRangeException(nameof(seed.Entropy), seed.Entropy, $"Entropy must be between 0 and {GameSetupSeed.CanonicalEntropyMaximum}.");
        }

        var difficultyCode = EncodeDifficulty(seed.Difficulty);
        var optionsCode = PackOptions(seed.Options).ToString("X2", CultureInfo.InvariantCulture);
        var entropyCode = seed.Entropy.ToString("X12", CultureInfo.InvariantCulture);
        var checksum = ComputeChecksum(seed.GeneratorVersion, seed.Difficulty, seed.Options, seed.Entropy).ToString("X4", CultureInfo.InvariantCulture);
        return $"{Prefix}-{difficultyCode}-{optionsCode}-{entropyCode}-{checksum}";
    }

    public static GameSetupSeedDecodeResult Decode(string? seedCode)
    {
        if (string.IsNullOrWhiteSpace(seedCode))
        {
            return GameSetupSeedDecodeResult.Failed("Seed code is required.");
        }

        var parts = seedCode.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 5 || !string.Equals(parts[0], Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return GameSetupSeedDecodeResult.Failed("Seed code format is invalid.");
        }

        if (!TryDecodeDifficulty(parts[1], out var difficulty))
        {
            return GameSetupSeedDecodeResult.Failed("Seed difficulty is invalid.");
        }

        if (!byte.TryParse(parts[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var optionsBits))
        {
            return GameSetupSeedDecodeResult.Failed("Seed options are invalid.");
        }

        if (parts[3].Length != 12 || !ulong.TryParse(parts[3], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var entropy))
        {
            return GameSetupSeedDecodeResult.Failed("Seed entropy is invalid.");
        }

        if (entropy > GameSetupSeed.CanonicalEntropyMaximum)
        {
            return GameSetupSeedDecodeResult.Failed("Seed entropy is out of range.");
        }

        if (!ushort.TryParse(parts[4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var checksum))
        {
            return GameSetupSeedDecodeResult.Failed("Seed checksum is invalid.");
        }

        var options = UnpackOptions(optionsBits);
        var seed = new GameSetupSeed(CurrentGeneratorVersion, difficulty, options, entropy);
        var expectedChecksum = ComputeChecksum(seed.GeneratorVersion, seed.Difficulty, seed.Options, seed.Entropy);
        if (checksum != expectedChecksum)
        {
            return GameSetupSeedDecodeResult.Failed("Seed checksum does not match.");
        }

        return GameSetupSeedDecodeResult.Ok(seed);
    }

    public static GameSetupSeed WithDifficulty(GameSetupSeed seed, TravelDifficulty difficulty)
    {
        ArgumentNullException.ThrowIfNull(seed);
        return seed with { Difficulty = difficulty, GeneratorVersion = CurrentGeneratorVersion };
    }

    public static GameSetupSeed WithOption(GameSetupSeed seed, GameSetupOption option, int value)
    {
        ArgumentNullException.ThrowIfNull(seed);

        var nextOptions = option switch
        {
            GameSetupOption.StartWithHorse => seed.Options with { StartWithHorse = value != 0 },
            GameSetupOption.LoadoutProfile => seed.Options with { LoadoutProfile = ParseLoadoutProfile(value) },
            GameSetupOption.JourneyRandomness => seed.Options with { JourneyRandomnessMode = ParseJourneyRandomnessMode(value) },
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, "Unsupported setup option.")
        };

        return seed with { Options = nextOptions, GeneratorVersion = CurrentGeneratorVersion };
    }

    public static string GetStableKey(GameSetupSeed seed)
        => Encode(seed);

    private static void ValidateVersion(int generatorVersion)
    {
        if (generatorVersion != CurrentGeneratorVersion)
        {
            throw new NotSupportedException($"Unsupported game setup generator version {generatorVersion}.");
        }
    }

    private static char EncodeDifficulty(TravelDifficulty difficulty)
        => difficulty switch
        {
            TravelDifficulty.Normal => 'N',
            TravelDifficulty.Easy => 'E',
            TravelDifficulty.Hard => 'H',
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unsupported travel difficulty.")
        };

    private static bool TryDecodeDifficulty(string code, out TravelDifficulty difficulty)
    {
        difficulty = TravelDifficulty.Normal;
        if (code.Length != 1)
        {
            return false;
        }

        difficulty = char.ToUpperInvariant(code[0]) switch
        {
            'N' => TravelDifficulty.Normal,
            'E' => TravelDifficulty.Easy,
            'H' => TravelDifficulty.Hard,
            _ => TravelDifficulty.Normal
        };

        return difficulty is TravelDifficulty.Normal or TravelDifficulty.Easy or TravelDifficulty.Hard;
    }

    private static byte PackOptions(GameSetupOptionsV1 options)
    {
        var bits = 0;
        if (options.StartWithHorse)
        {
            bits |= 1;
        }

        bits |= ((int)options.LoadoutProfile & 0x03) << 1;
        if (options.JourneyRandomnessMode == TravelRandomnessMode.Deterministic)
        {
            bits |= 1 << 3;
        }
        return (byte)bits;
    }

    private static GameSetupOptionsV1 UnpackOptions(byte bits)
    {
        var startWithHorse = (bits & 0x01) != 0;
        var loadoutProfile = ParseLoadoutProfile((bits >> 1) & 0x03);
        var journeyRandomnessMode = (bits & (1 << 3)) != 0
            ? TravelRandomnessMode.Deterministic
            : TravelRandomnessMode.RuntimeSalted;
        return new GameSetupOptionsV1(startWithHorse, loadoutProfile, journeyRandomnessMode);
    }

    private static StartingLoadoutProfile ParseLoadoutProfile(int value)
        => value switch
        {
            0 => StartingLoadoutProfile.Standard,
            1 => StartingLoadoutProfile.Light,
            2 => StartingLoadoutProfile.Stocked,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported loadout profile.")
        };

    private static TravelRandomnessMode ParseJourneyRandomnessMode(int value)
        => value switch
        {
            0 => TravelRandomnessMode.RuntimeSalted,
            1 => TravelRandomnessMode.Deterministic,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported journey randomness mode.")
        };

    private static ushort ComputeChecksum(int generatorVersion, TravelDifficulty difficulty, GameSetupOptionsV1 options, ulong entropy)
    {
        var payload = string.Join(
            "|",
            Prefix,
            generatorVersion,
            difficulty,
            PackOptions(options),
            entropy.ToString("X12", CultureInfo.InvariantCulture));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return BitConverter.ToUInt16(hash, 0);
    }
}
