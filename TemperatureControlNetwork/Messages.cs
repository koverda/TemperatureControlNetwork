namespace TemperatureControlNetwork;

public enum MessageType
{
    Data,
    Control
}

public abstract class Message
{
    public abstract MessageType Type { get; }
}

public class DataMessage(string data) : Message
{
    public DataMessage() : this("")
    {
    }

    public override MessageType Type => MessageType.Data;
    public string Data { get; init; } = data;
}

public class ControlMessage : Message
{
    public override MessageType Type => MessageType.Control;
    public int WorkerId { get; init; }
    public bool Activate { get; init; }
}