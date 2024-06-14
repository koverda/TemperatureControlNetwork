using TemperatureControlNetwork.Core.Models;

namespace TemperatureControlNetwork.Gui.Interface;

public interface IGui
{
    void DisplayWorkerStatus(List<WorkerStatus> workerStatusList, WorkerTemperatureList workerTemperatureList);
    void Run();
}