using System.Threading.Channels;

namespace TemperatureControlNetwork;

internal static class TemperatureControlNetwork
{
    private static async Task Main()
    {
        const int numberOfWorkers = 3;
        var cancellationTokenSource = new CancellationTokenSource();

        var coordinator = new Coordinator(numberOfWorkers, cancellationTokenSource.Token);
        await coordinator.StartAsync();
    }
}

public class Coordinator
{
    private readonly List<Worker> _workers = [];
    private readonly Channel<string> _channel;
    private readonly CancellationToken _cancellationToken;
    private readonly Random _random;

    public Coordinator(int numberOfWorkers, CancellationToken cancellationToken)
    {
        _channel = Channel.CreateUnbounded<string>();
        _cancellationToken = cancellationToken;
        _random = new Random();

        for (int i = 0; i < numberOfWorkers; i++)
        {
            _workers.Add(new Worker(_channel.Reader, i));
        }
    }

    public async Task StartAsync()
    {
        var workerTasks = new List<Task>();

        foreach (var worker in _workers)
        {
            workerTasks.Add(worker.StartAsync());
        }

        while (!_cancellationToken.IsCancellationRequested)
        {
            string data = GenerateRandomData();
            await _channel.Writer.WriteAsync(data, _cancellationToken);
            await Task.Delay(1000, _cancellationToken);
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
    public async Task StartAsync()
    {
        await foreach (string item in channelReader.ReadAllAsync())
        {
            Console.WriteLine($"Worker {id} received: {item}");
            // Simulate processing
            await Task.Delay(500);
            Console.WriteLine($"Worker {id} processed: {item}");
        }
    }
}