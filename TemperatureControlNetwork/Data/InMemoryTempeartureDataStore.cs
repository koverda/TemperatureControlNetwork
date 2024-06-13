using TemperatureControlNetwork.Core.Models;
using TemperatureControlNetwork.Data.Interface;

namespace TemperatureControlNetwork.Data;

public class InMemoryTemperatureDataStore : ITemperatureDataStore
{
    private readonly List<TemperatureData> _temperatureDataList = [];

    public Task AddTemperatureDataAsync(TemperatureData data)
    {
        _temperatureDataList.Add(data);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<TemperatureData>> GetAllTemperatureDataAsync()
    {
        return Task.FromResult<IEnumerable<TemperatureData>>(_temperatureDataList);
    }

    public Task<TemperatureData> GetTemperatureDataByWorkerIdAsync(int workerId)
    {
        var data = _temperatureDataList.First(d => d.WorkerId == workerId);
        return Task.FromResult(data);
    }

    public Task<IEnumerable<TemperatureData>> GetTemperatureDataByTimeRangeAsync(DateTime startTime, DateTime endTime)
    {
        var data = _temperatureDataList.Where(d => d.Timestamp >= startTime && d.Timestamp <= endTime);
        return Task.FromResult<IEnumerable<TemperatureData>>(data);
    }
}