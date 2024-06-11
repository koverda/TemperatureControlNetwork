using System.Text.Json;

namespace TemperatureControlNetwork.Core;

public static class MessageJsonSerializer
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
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
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }
}