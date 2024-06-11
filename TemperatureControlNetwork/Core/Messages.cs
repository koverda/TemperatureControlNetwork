namespace TemperatureControlNetwork.Core;

public enum MessageType
{
    Data,
    Control,
    Response,
    ActivationResponseMessage
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

public class ActivationResponseMessage(int workerId, bool success) : Message
{
    public override MessageType Type => MessageType.ActivationResponseMessage;
    public int WorkerId { get; set; } = workerId;
    public bool Success { get; set; } = success;
}

public class NeighborUpdateMessage(string data) : Message
{
    public NeighborUpdateMessage() : this("")
    {
    }

    public override MessageType Type => MessageType.Data;
    public string Data { get; init; } = data;
}
