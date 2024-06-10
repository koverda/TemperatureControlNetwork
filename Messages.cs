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

public class DataMessage : Message
{
    public override MessageType Type => MessageType.Data;
    public string Data { get; set; }
}

public class ControlMessage : Message
{
    public override MessageType Type => MessageType.Control;
    public int WorkerId { get; set; }
    public bool Activate { get; set; }
}