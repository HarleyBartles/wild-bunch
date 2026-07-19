using System.Text.Json;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public string Serialize(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return JsonSerializer.Serialize(GameSessionSnapshot.FromDomain(session), Options);
    }

    internal GameSession Deserialize(string stateJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateJson);

        return Deserialize<GameSessionSnapshot>(stateJson).ToDomain();
    }

    private static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, Options)
            ?? throw new InvalidOperationException($"Unable to deserialize {typeof(T).Name}.");

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new OutlawGangIdJsonConverter());
        return options;
    }

    private sealed class OutlawGangIdJsonConverter : System.Text.Json.Serialization.JsonConverter<OutlawGangId>
    {
        public override OutlawGangId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Gang ids must be serialized as strings.");
            }

            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonException("Gang ids must be non-empty strings.");
            }

            return new OutlawGangId(value);
        }

        public override void Write(Utf8JsonWriter writer, OutlawGangId value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            writer.WriteStringValue(value.Value);
        }
    }
}
