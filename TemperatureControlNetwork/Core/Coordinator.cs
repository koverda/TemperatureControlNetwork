using System.Text.Json;
using System.Threading.Channels;

namespace TemperatureControlNetwork.Core;

public class Coordinator
{
    private readonly CancellationToken _cancellationToken;
    private readonly Random _random;

    private readonly Channel<string> _responseChannel;
    private readonly int _numberOfWorkers;

    private readonly List<Channel<string>> _workerChannels = [];
    private readonly List<Worker> _workers = [];
    private List<WorkerStatus> _workerStatusList = [];


    public Coordinator(int numberOfWorkers, CancellationToken cancellationToken)
    {
        _numberOfWorkers = numberOfWorkers;
        _responseChannel = Channel.CreateUnbounded<string>();
        _cancellationToken = cancellationToken;
        _random = new Random();

        for (int i = 0; i < _numberOfWorkers; i++)
        {
            var workerChannel = Channel.CreateUnbounded<string>();
            _workerChannels.Add(workerChannel);
            _workers.Add(new Worker(workerChannel.Reader, _responseChannel.Writer, i));
            _workerStatusList.Add(new WorkerStatus(i, true)); // all start as active
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
            var dataMessageJson = MessageJsonSerializer.Serialize(dataMessage);

            // Send data to all active workers
            foreach (var workerChannel in _workerChannels)
            {
                await workerChannel.Writer.WriteAsync(dataMessageJson);
            }

            await Task.Delay(3000); // Simulate a delay between sending data

            // Randomly activate or deactivate workers
            if (_random.Next(0, 2) == 0)
            {
                int workerId = _random.Next(0, _numberOfWorkers);
                var controlMessage = new ControlMessage
                {
                    WorkerId = workerId,
                    Activate = _random.Next(0, 2) == 0
                };
                string controlMessageJson = MessageJsonSerializer.Serialize(controlMessage);
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
            var message = MessageJsonSerializer.Deserialize<Message>(item);

            if (message is ResponseMessage responseMessage)
            {
                Console.WriteLine($"Coordinator received response from Worker {responseMessage.WorkerId}: {responseMessage.Response}");
            }

            // Workers activated / deactivated successfully
            if (message is StatusUpdateResponseMessage activationResponseMessage)
            {
                _workerStatusList.First(w => w.Id == activationResponseMessage.WorkerId).Active = activationResponseMessage.Active;

                var workerStatusUpdateMessage = new StatusUpdateMessage(_workerStatusList);
                // Send data to all active workers
                foreach (var workerChannel in _workerChannels)
                {
                    await workerChannel.Writer.WriteAsync(JsonSerializer.Serialize(workerStatusUpdateMessage));
                }
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