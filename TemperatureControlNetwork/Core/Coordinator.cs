using System.Text.Json;
using System.Threading.Channels;

namespace TemperatureControlNetwork.Core;

public class Coordinator
{
    private readonly CancellationToken _cancellationToken;
    private readonly List<Channel<string>> _workerChannels;
    private readonly Channel<string> _responseChannel;
    private readonly int _numberOfWorkers;
    private readonly Random _random;
    private readonly List<Worker> _workers;

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        Converters = { new MessageJsonConverter() }
    };

    public Coordinator(int numberOfWorkers, CancellationToken cancellationToken)
    {
        _numberOfWorkers = numberOfWorkers;
        _workers = new List<Worker>();
        _responseChannel = Channel.CreateUnbounded<string>();
        _workerChannels = new List<Channel<string>>();
        _cancellationToken = cancellationToken;
        _random = new Random();

        for (int i = 0; i < _numberOfWorkers; i++)
        {
            var workerChannel = Channel.CreateUnbounded<string>();
            _workerChannels.Add(workerChannel);
            _workers.Add(new Worker(workerChannel.Reader, _responseChannel.Writer, i, _jsonOptions));
        }
    }

    public async Task StartAsync()
    {
        var workerTasks = new List<Task>();

        foreach (var worker in _workers)
        {
            workerTasks.Add(worker.StartAsync());
        }

        var processResponsesTask = ProcessResponsesAsync();

        // Run indefinitely until cancellation is requested
        while (!_cancellationToken.IsCancellationRequested)
        {
            var dataMessage = new DataMessage { Data = GenerateRandomData() };
            var dataMessageJson = JsonSerializer.Serialize(dataMessage, _jsonOptions);

            // Send data to all active workers
            foreach (var workerChannel in _workerChannels)
            {
                await workerChannel.Writer.WriteAsync(dataMessageJson);
            }

            await Task.Delay(1); // Simulate a delay between sending data

            // Randomly activate or deactivate workers
            if (_random.Next(0, 2) == 0)
            {
                int workerId = _random.Next(0, _numberOfWorkers);
                var controlMessage = new ControlMessage
                {
                    WorkerId = workerId,
                    Activate = _random.Next(0, 2) == 0
                };
                string controlMessageJson = JsonSerializer.Serialize(controlMessage, _jsonOptions);
                await _workerChannels[workerId].Writer.WriteAsync(controlMessageJson);
            }
        }

        foreach (var workerChannel in _workerChannels)
        {
            workerChannel.Writer.Complete();
        }

        await Task.WhenAll(workerTasks);
        await processResponsesTask;
    }

    private async Task ProcessResponsesAsync()
    {
        await foreach (var item in _responseChannel.Reader.ReadAllAsync())
        {
            var message = JsonSerializer.Deserialize<Message>(item, _jsonOptions);

            if (message is ResponseMessage responseMessage)
            {
                Console.WriteLine($"Coordinator received response from Worker {responseMessage.WorkerId}: {responseMessage.Response}");
            }
        }
    }

    private string GenerateRandomData()
    {
        return _random.Next(0, 2) == 0
            ? $"Temperature: {_random.Next(15, 30)}°C"
            : "Message: Hello from Coordinator!";
    }
}