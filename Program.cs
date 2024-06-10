using System.Threading.Channels;

namespace TemperatureControlNetwork;

internal static class Program
{
    private static async Task Main()
    {
        int numberOfWorkers = 3;
        var cancellationTokenSource = new CancellationTokenSource();

        var coordinator = new Coordinator(numberOfWorkers, cancellationTokenSource.Token);
        await coordinator.StartAsync();
    }
}

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
            string data = GenerateRandomData();
            await _channel.Writer.WriteAsync(data, _cancellationToken);
            await Task.Delay(1000, _cancellationToken);

            // Randomly activate or deactivate workers
            if (_random.Next(0, 2) == 0)
            {
                int workerId = _random.Next(0, _numberOfWorkers);
                string controlMessage = _random.Next(0, 2) == 0
                    ? $"ActivateWorker:{workerId}"
                    : $"DeactivateWorker:{workerId}";
                await _channel.Writer.WriteAsync(controlMessage, _cancellationToken);
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

public class Worker(ChannelReader<string> channelReader, int id)
{
    private bool _isActive = true; // Workers start as active

    public async Task StartAsync()
    {
        await foreach (string item in channelReader.ReadAllAsync())
            if (item.StartsWith("ActivateWorker:"))
            {
                int workerId = int.Parse(item.Split(':')[1]);
                if (workerId == id)
                {
                    _isActive = true;
                    Console.WriteLine($"Worker {id} activated.");
                }
            }
            else if (item.StartsWith("DeactivateWorker:"))
            {
                int workerId = int.Parse(item.Split(':')[1]);
                if (workerId == id)
                {
                    _isActive = false;
                    Console.WriteLine($"Worker {id} deactivated.");
                }
            }
            else if (_isActive)
            {
                Console.WriteLine($"Worker {id} received: {item}");
                // Simulate processing
                await Task.Delay(500);
                Console.WriteLine($"Worker {id} processed: {item}");
            }
    }
}