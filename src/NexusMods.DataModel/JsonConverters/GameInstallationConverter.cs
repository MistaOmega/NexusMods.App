﻿using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Games;

namespace NexusMods.DataModel.JsonConverters;

public class GameInstallationConverter : JsonConverter<GameInstallation>
{
    private readonly Dictionary<(string Slug, Version Version), GameInstallation> _games;

    public GameInstallationConverter(IEnumerable<IGame> games)
    {
        _games = games.SelectMany(g => g.Installations.Select(i => (g.Slug, Install: i)))
            .ToDictionary(r => (r.Slug, r.Install.Version), r => r.Install);
    }
    
    public override GameInstallation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array start");
        reader.Read();
        var slug = reader.GetString()!;
        reader.Read();
        var version = JsonSerializer.Deserialize<Version>(ref reader, options)!;
        reader.Read();

        if (_games.TryGetValue((slug, version), out var found)) return found;
        return new UnknownGame(slug, version).Installations.First();
    }

    public override void Write(Utf8JsonWriter writer, GameInstallation value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStringValue(value.Game.Slug);
        JsonSerializer.Serialize(writer, value.Version, options);
        writer.WriteEndArray();
    }
}