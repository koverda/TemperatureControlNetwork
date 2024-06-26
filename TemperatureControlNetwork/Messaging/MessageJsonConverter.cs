﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace TemperatureControlNetwork.Messaging;

// todo can i reduce access modifiers?
public class MessageJsonConverter : JsonConverter<IMessage>
{
    public override IMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions? options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var raw = doc.RootElement.GetRawText();
        if (!doc.RootElement.TryGetProperty("Type", out var typeElement)) throw new JsonException("Missing Type property.");

        var type = (MessageType)typeElement.GetInt32();

        switch (type)
        {
            case MessageType.Data:
                return JsonSerializer.Deserialize<DataMessage>(doc.RootElement.GetRawText(), options);
            case MessageType.Control:
                return JsonSerializer.Deserialize<ControlMessage>(doc.RootElement.GetRawText(), options);
            case MessageType.Response:
                return JsonSerializer.Deserialize<DataResponseMessage>(doc.RootElement.GetRawText(), options);
            case MessageType.StatusUpdateResponse:
                return JsonSerializer.Deserialize<StatusUpdateResponseMessage>(doc.RootElement.GetRawText(), options);
            case MessageType.StatusUpdate:
                return JsonSerializer.Deserialize<StatusUpdateMessage>(doc.RootElement.GetRawText(), options);
            case MessageType.OverheatTakeover:
                return JsonSerializer.Deserialize<OverheatTakeoverMessage>(doc.RootElement.GetRawText(), options);
            default:
                throw new JsonException($"Unknown message type: {type}");
        }
    }

    public override void Write(Utf8JsonWriter writer, IMessage value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}