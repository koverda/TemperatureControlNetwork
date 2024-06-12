namespace TemperatureControlNetwork.Core;

public class WorkerTemperature
{
    public int Id { get; set; }
    public double AverageTemperature { get; set; }

    public WorkerTemperature(int id, double averageTemperature)
    {
        Id = id;
        AverageTemperature = averageTemperature;
    }
}

public class WorkerTemperatureList
{
    public List<WorkerTemperature> WorkerTemperatures { get; set; }

    public double AverageTemperature
    {
        get { return WorkerTemperatures.Average(t => t.AverageTemperature); }
    }

    public WorkerTemperatureList(List<WorkerTemperature> workerTemperatures)
    {
        WorkerTemperatures = workerTemperatures;
    }
}