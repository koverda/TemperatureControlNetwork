using TemperatureControlNetwork.Core.Models;

namespace TemperatureControlNetwork.Messaging;

public enum MessageType
{
    Data,
    Control,
    Response,
    StatusUpdateResponse,
    StatusUpdate,
    OverheatTakeover
}

public interface IMessage
{
    public MessageType Type { get; }
}

public class DataMessage(string data) : IMessage
{
    public DataMessage() : this("")
    {
    }

    public MessageType Type => MessageType.Data;
    public string Data { get; init; } = data;
}

public class ControlMessage : IMessage
{
    public MessageType Type => MessageType.Control;
    public int WorkerId { get; init; }
    public bool Activate { get; init; }
}

public class DataResponseMessage(int workerId, double temperature) : IMessage
{
    public MessageType Type => MessageType.Response;
    public int WorkerId { get; init; } = workerId;
    public double Temperature { get; init; } = temperature;
}

public class StatusUpdateResponseMessage(int workerId, bool active) : IMessage
{
    public MessageType Type => MessageType.StatusUpdateResponse;
    public int WorkerId { get; init; } = workerId;
    public bool Active { get; init; } = active;
}

public class StatusUpdateMessage(List<WorkerStatus> workerStatusList) : IMessage
{
    public MessageType Type => MessageType.StatusUpdate;
    public List<WorkerStatus> WorkerStatusList { get; init; } = workerStatusList;
}

public class OverheatTakeoverMessage(int workerToDeactivate, int workerToActivate) : IMessage
{
    public MessageType Type => MessageType.OverheatTakeover;
    public int WorkerToDeactivate { get; init; } = workerToDeactivate;
    public int WorkerToActivate { get; init; } = workerToActivate;
}
