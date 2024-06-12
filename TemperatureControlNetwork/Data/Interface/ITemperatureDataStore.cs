using TemperatureControlNetwork.Core;

namespace TemperatureControlNetwork.Data.Interface;

public interface ITemperatureDataStore
{
    Task AddTemperatureDataAsync(TemperatureData data);
    Task<IEnumerable<TemperatureData>> GetAllTemperatureDataAsync();
    Task<TemperatureData> GetTemperatureDataByWorkerIdAsync(int workerId);
    Task<IEnumerable<TemperatureData>> GetTemperatureDataByTimeRangeAsync(DateTime startTime, DateTime endTime);
}