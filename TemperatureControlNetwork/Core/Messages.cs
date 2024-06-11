using System.ComponentModel.DataAnnotations;

namespace TemperatureControlNetwork.Core;

public enum MessageType
{
    Data,
    Control,
    Response,
    StatusUpdateResponse,
    StatusUpdate
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

public class StatusUpdateResponseMessage(int workerId, bool active) : Message
{
    public override MessageType Type => MessageType.StatusUpdateResponse;
    public int WorkerId { get; set; } = workerId;
    public bool Active { get; set; } = active;
}

public class StatusUpdateMessage(List<WorkerStatus> workerStatusList) : Message
{
    public override MessageType Type => MessageType.StatusUpdate;
    public List<WorkerStatus> WorkerStatusList { get; init; } = workerStatusList;
}
