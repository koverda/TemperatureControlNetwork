using TemperatureControlNetwork.Core;

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

public class DataResponseMessage(int workerId, double temperature) : Message
{
    public override MessageType Type => MessageType.Response;
    public int WorkerId { get; set; } = workerId;
    public double Temperature { get; set; } = temperature;
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

public class OverheatTakeoverMessage(int workerToDeactivate, int workerToActivate) : Message
{
    public override MessageType Type => MessageType.OverheatTakeover;
    public int WorkerToDeactivate { get; set; } = workerToDeactivate;
    public int WorkerToActivate { get; set; } = workerToActivate;
}
