namespace TemperatureControlNetwork.Core;

public class WorkerTemperature
{
    public int Id { get; set; }
    public double Temperature { get; set; }

    public WorkerTemperature(int id, double temperature)
    {
        Id = id;
        Temperature = temperature;
    }
}

public class WorkerTemperatureList
{
    public List<WorkerTemperature> WorkerTemperatures { get; set; }

    public double AverageTemperature
    {
        get { return WorkerTemperatures.Average(t => t.Temperature); }
    }

    public WorkerTemperatureList(List<WorkerTemperature> workerTemperatures)
    {
        WorkerTemperatures = workerTemperatures;
    }
}