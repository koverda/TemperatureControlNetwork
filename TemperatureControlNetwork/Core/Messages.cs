namespace TemperatureControlNetwork.Core;

public enum MessageType
{
    Data,
    Control,
    Response
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

public class ResponseMessage(string response) : Message
{
    public ResponseMessage() : this("")
    {
    }

    public override MessageType Type => MessageType.Response;
    public int WorkerId { get; set; }
    public string Response { get; set; } = response;
}