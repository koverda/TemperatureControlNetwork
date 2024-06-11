using System.Text.Json;
using FluentAssertions;
using TemperatureControlNetwork.Core;

namespace Test;

public class MessageJsonConverterTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new MessageJsonConverter() }
    };

    [Fact]
    public void ShouldSerializeAndDeserializeDataMessage()
    {
        // Arrange
        var originalMessage = new DataMessage("Test Data");

        // Act
        string json = JsonSerializer.Serialize(originalMessage, _jsonOptions);
        var deserializedMessage = JsonSerializer.Deserialize<Message>(json, _jsonOptions);

        // Assert
        deserializedMessage.Should().BeOfType<DataMessage>();
        deserializedMessage.As<DataMessage>().Data.Should().Be("Test Data");
    }

    [Fact]
    public void ShouldSerializeAndDeserializeControlMessage()
    {
        // Arrange
        var originalMessage = new ControlMessage
        {
            WorkerId = 1,
            Activate = true
        };

        // Act
        string json = JsonSerializer.Serialize(originalMessage, _jsonOptions);
        var deserializedMessage = JsonSerializer.Deserialize<Message>(json, _jsonOptions);

        // Assert
        deserializedMessage.Should().BeOfType<ControlMessage>();
        deserializedMessage.As<ControlMessage>().WorkerId.Should().Be(1);
        deserializedMessage.As<ControlMessage>().Activate.Should().BeTrue();
    }

    [Fact]
    public void ShouldSerializeAndDeserializeActivationResponseMessage()
    {
        // Arrange
        var originalMessage = new ActivationResponseMessage(1, true);

        // Act
        string json = JsonSerializer.Serialize(originalMessage, _jsonOptions);
        var deserializedMessage = JsonSerializer.Deserialize<Message>(json, _jsonOptions);

        // Assert
        deserializedMessage.Should().BeOfType<ActivationResponseMessage>();
        deserializedMessage.As<ActivationResponseMessage>().WorkerId.Should().Be(1);
        deserializedMessage.As<ActivationResponseMessage>().Success.Should().BeTrue();
    }

    [Fact]
    public void ShouldThrowExceptionForUnknownMessageType()
    {
        // Arrange
        string json = "{\"Type\":\"Unknown\",\"Data\":\"Test Data\"}";

        // Act
        Action act = () => JsonSerializer.Deserialize<Message>(json, _jsonOptions);

        // Assert
        act.Should().Throw<JsonException>();
    }
}