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
    private WorkerTemperatureList _workerTemperatureList = new([]);


    public Coordinator(CancellationToken cancellationToken)
    {
        _numberOfWorkers = Config.NumberOfWorkers;
        _responseChannel = Channel.CreateUnbounded<string>();
        _cancellationToken = cancellationToken;
        _random = new Random();

        for (int i = 0; i < _numberOfWorkers; i++)
        {
            var workerChannel = Channel.CreateUnbounded<string>();
            _workerChannels.Add(workerChannel);
            _workers.Add(new Worker(workerChannel.Reader, _responseChannel.Writer, i));
            _workerStatusList.Add(new WorkerStatus(i, true)); // all start as active
            _workerTemperatureList.WorkerTemperatures.Add(new WorkerTemperature(i, Config.StartingTemperature));
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
            var dataMessage = new DataMessage { Data = "Data request from coordinator" };
            var dataMessageJson = MessageJsonSerializer.Serialize(dataMessage);

            // Send data to all active workers
            foreach (var workerChannel in _workerChannels)
            {
                await workerChannel.Writer.WriteAsync(dataMessageJson, _cancellationToken);
            }

            await Task.Delay(Config.CoordinatorLoopDelay, _cancellationToken);

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
                await _workerChannels[workerId].Writer.WriteAsync(controlMessageJson, _cancellationToken);
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
        await foreach (var item in _responseChannel.Reader.ReadAllAsync(_cancellationToken))
        {
            var message = MessageJsonSerializer.Deserialize<Message>(item);

            if (message is DataResponseMessage responseMessage)
            {
                Console.WriteLine($"Coordinator received response from Worker {responseMessage.WorkerId}: {responseMessage.Temperature}");
            }

            // Workers activated / deactivated successfully
            if (message is StatusUpdateResponseMessage activationResponseMessage)
            {
                _workerStatusList.First(w => w.Id == activationResponseMessage.WorkerId).Active = activationResponseMessage.Active;

                var workerStatusUpdateMessage = new StatusUpdateMessage(_workerStatusList);
                // Send data to all active workers
                foreach (var workerChannel in _workerChannels)
                {
                    await workerChannel.Writer.WriteAsync(JsonSerializer.Serialize(workerStatusUpdateMessage), _cancellationToken);
                }
            }
        }
    }
}