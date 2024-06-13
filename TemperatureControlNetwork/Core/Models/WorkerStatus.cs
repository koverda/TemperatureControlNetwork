namespace TemperatureControlNetwork.Core.Models;

public class WorkerStatus
{
    public int Id { get; set; }
    public bool Active { get; set; }

    public WorkerStatus(int id, bool active)
    {
        Id = id;
        Active = active;
    }
}