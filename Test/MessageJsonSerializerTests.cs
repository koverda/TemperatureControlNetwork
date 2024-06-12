using System.Text.Json;
using FluentAssertions;
using TemperatureControlNetwork.Messaging;

namespace Test;

public class MessageJsonSerializerTests
{

    [Fact]
    public void ShouldSerializeAndDeserializeDataMessage()
    {
        // Arrange
        var originalMessage = new DataMessage("Test Data");

        // Act
        string json = MessageJsonSerializer.Serialize(originalMessage);
        var deserializedMessage = MessageJsonSerializer.Deserialize<IMessage>(json);

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
        string json = MessageJsonSerializer.Serialize(originalMessage);
        var deserializedMessage = MessageJsonSerializer.Deserialize<IMessage>(json);

        // Assert
        deserializedMessage.Should().BeOfType<ControlMessage>();
        deserializedMessage.As<ControlMessage>().WorkerId.Should().Be(1);
        deserializedMessage.As<ControlMessage>().Activate.Should().BeTrue();
    }

    [Fact]
    public void ShouldSerializeAndDeserializeActivationResponseMessage()
    {
        // Arrange
        var originalMessage = new StatusUpdateResponseMessage(1, true);

        // Act
        string json = MessageJsonSerializer.Serialize(originalMessage);
        var deserializedMessage = MessageJsonSerializer.Deserialize<IMessage>(json);

        // Assert
        deserializedMessage.Should().BeOfType<StatusUpdateResponseMessage>();
        deserializedMessage.As<StatusUpdateResponseMessage>().WorkerId.Should().Be(1);
        deserializedMessage.As<StatusUpdateResponseMessage>().Active.Should().BeTrue();
    }

    [Fact]
    public void ShouldThrowExceptionForUnknownMessageType()
    {
        // Arrange
        string json = "{\"Type\":\"Unknown\",\"Data\":\"Test Data\"}";

        // Act
        Action act = () => MessageJsonSerializer.Deserialize<IMessage>(json);

        // Assert
        act.Should().Throw<JsonException>();
    }
}