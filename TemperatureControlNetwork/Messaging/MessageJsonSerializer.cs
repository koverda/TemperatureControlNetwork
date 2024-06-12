using System.Text.Json;

namespace TemperatureControlNetwork.Messaging;

public static class MessageJsonSerializer
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new MessageJsonConverter() },
        WriteIndented = true
    };

    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _jsonOptions);
    }

    public static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? throw new JsonException($"Could not deserialize json: {json}");
    }
}