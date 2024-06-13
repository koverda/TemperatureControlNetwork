using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using TemperatureControlNetwork.Core.Models;
using TemperatureControlNetwork.Data.Interface;

namespace TemperatureControlNetwork.Data;

public class CsvTemperatureDataStore : ITemperatureDataStore
{
    private readonly string _filePath;
    private readonly CsvConfiguration _csvConfig;

    public CsvTemperatureDataStore(string filePath)
    {
        _filePath = filePath;
        _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
    }

    public async Task AddTemperatureDataAsync(TemperatureData data)
    {
        var records = new List<TemperatureData> { data };

        bool append = File.Exists(_filePath);
        await using var writer = new StreamWriter(_filePath, append);
        await using var csv = new CsvWriter(writer, _csvConfig);

        if (!append)
        {
            csv.WriteHeader<TemperatureData>();
            await csv.NextRecordAsync();
        }

        await csv.WriteRecordsAsync(records);
    }

    public async Task<IEnumerable<TemperatureData>> GetAllTemperatureDataAsync()
    {
        if (!File.Exists(_filePath))
        {
            return Enumerable.Empty<TemperatureData>();
        }

        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, _csvConfig);
        var asyncRecords = csv.GetRecordsAsync<TemperatureData>();
        return await asyncRecords.ToListAsync();
    }

    public async Task<TemperatureData> GetTemperatureDataByWorkerIdAsync(int workerId)
    {
        var allData = await GetAllTemperatureDataAsync();
        return allData.First(data => data.WorkerId == workerId);
    }

    public async Task<IEnumerable<TemperatureData>> GetTemperatureDataByTimeRangeAsync(DateTime startTime, DateTime endTime)
    {
        var allData = await GetAllTemperatureDataAsync();
        return allData.Where(data => data.Timestamp >= startTime && data.Timestamp <= endTime);
    }
}