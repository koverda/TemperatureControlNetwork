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
    private readonly List<WorkerStatus> _workerStatusList = [];
    private readonly WorkerTemperatureList _workerTemperatureList = new([]);


    public Coordinator(CancellationToken cancellationToken)
    {
        _numberOfWorkers = Config.NumberOfWorkers;
        _responseChannel = Channel.CreateUnbounded<string>();
        _cancellationToken = cancellationToken;
        _random = new Random();

        for (int workerId = 0; workerId < _numberOfWorkers; workerId++)
        {
            var workerChannel = Channel.CreateUnbounded<string>();
            _workerChannels.Add(workerChannel);
            _workers.Add(new Worker(workerChannel.Reader, _responseChannel.Writer, workerId));
            _workerStatusList.Add(new WorkerStatus(workerId, active: true));
            _workerTemperatureList.WorkerTemperatures.Add(new WorkerTemperature(workerId, Config.StartingTemperature));
        }
    }

    public async Task StartAsync()
    {
        var workerTasks = new List<Task>();

        foreach (var worker in _workers)
        {
            workerTasks.Add(worker.StartAsync());
        }

        var processResponsesTask = WatchCoordinatorChannelsAsync();
        await CoordinatorLoop();

        foreach (var workerChannel in _workerChannels)
        {
            workerChannel.Writer.Complete();
        }

        await Task.WhenAll(workerTasks);
        await processResponsesTask;
    }

    private async Task CoordinatorLoop()
    {
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
    }

    private async Task WatchCoordinatorChannelsAsync()
    {
        await foreach (var item in _responseChannel.Reader.ReadAllAsync(_cancellationToken))
        {
            var message = MessageJsonSerializer.Deserialize<Message>(item);

            switch (message)
            {
                case DataResponseMessage responseMessage:
                {
                    Console.WriteLine($"Coordinator received response from Worker {responseMessage.WorkerId}: {responseMessage.Temperature:##.#}");
                }
                    break;
                case StatusUpdateResponseMessage activationResponseMessage:
                {
                    _workerStatusList.First(w => w.Id == activationResponseMessage.WorkerId).Active = activationResponseMessage.Active;

                    var workerStatusUpdateMessage = new StatusUpdateMessage(_workerStatusList);
                    foreach (var workerChannel in _workerChannels)
                    {
                        await workerChannel.Writer.WriteAsync(JsonSerializer.Serialize(workerStatusUpdateMessage), _cancellationToken);
                    }

                    break;
                }
            }
        }
    }


    private async Task WorkerLoopAsync()
    {
        while (true)
        {
        }

        // This is the worker's main loop, we want it to run indefinitely and there's nothing to return
        // ReSharper disable once FunctionNeverReturns
    }
}