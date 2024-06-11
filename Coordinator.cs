using System.Text.Json;
using System.Threading.Channels;

namespace TemperatureControlNetwork;

public class Coordinator
{
    private readonly CancellationToken _cancellationToken;
    private readonly Channel<string> _channel;
    private readonly int _numberOfWorkers;
    private readonly Random _random;
    private readonly List<Worker> _workers;

    public Coordinator(int numberOfWorkers, CancellationToken cancellationToken)
    {
        _numberOfWorkers = numberOfWorkers;
        _workers = new List<Worker>();
        _channel = Channel.CreateUnbounded<string>();
        _cancellationToken = cancellationToken;
        _random = new Random();

        for (int i = 0; i < _numberOfWorkers; i++) _workers.Add(new Worker(_channel.Reader, i));
    }

    public async Task StartAsync()
    {
        var workerTasks = _workers.Select(worker => worker.StartAsync()).ToList();

        while (!_cancellationToken.IsCancellationRequested)
        {
            string dataMessage = JsonSerializer.Serialize(new DataMessage { Data = GenerateRandomData() });
            await _channel.Writer.WriteAsync(dataMessage, _cancellationToken);

            await Task.Delay(1000, _cancellationToken);

            // Randomly activate or deactivate workers
            if (_random.Next(0, 2) == 0)
            {
                int workerId = _random.Next(0, _numberOfWorkers);
                var controlMessage = new ControlMessage
                {
                    WorkerId = workerId,
                    Activate = _random.Next(0, 2) == 0
                };
                string controlMessageJson = JsonSerializer.Serialize(controlMessage);
                await _channel.Writer.WriteAsync(controlMessageJson, _cancellationToken);
            }
        }

        _channel.Writer.Complete();
        await Task.WhenAll(workerTasks);
    }

    private string GenerateRandomData()
    {
        return _random.Next(0, 2) == 0
            ? $"Temperature: {_random.Next(15, 30)}°C"
            : "Message: Hello from Coordinator!";
    }
}