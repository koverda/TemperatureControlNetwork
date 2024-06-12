using System.Text.Json;
using System.Threading;
using System.Threading.Channels;

namespace TemperatureControlNetwork.Core;

public class Coordinator
{
    private readonly CancellationToken _cancellationToken;
    private readonly Random _random;

    private readonly Channel<string> _responseChannel;

    private readonly List<Channel<string>> _workerChannels = [];
    private readonly List<Worker> _workers = [];
    private readonly List<WorkerStatus> _workerStatusList = [];
    private readonly WorkerTemperatureList _workerTemperatureList = new([]);


    public Coordinator(CancellationToken cancellationToken)
    {
        _responseChannel = Channel.CreateUnbounded<string>();
        _cancellationToken = cancellationToken;
        _random = new Random();

        for (int workerId = 0; workerId < Config.NumberOfWorkers; workerId++)
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
        var workerTasks = _workers.Select(worker => worker.StartAsync()).ToList();
        var readCoordinatorChannelTask = ReadCoordinatorChannelAsync();

        await CoordinatorLoop(workerTasks, readCoordinatorChannelTask);
    }

    private async Task CoordinatorLoop(List<Task> workerTasks, Task processResponsesTask)
    {
        try
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                // Request data from all workers
                var dataMessage = new DataMessage { Data = "Data request from coordinator" };
                var dataMessageJson = MessageJsonSerializer.Serialize(dataMessage);
                foreach (var workerChannel in _workerChannels)
                {
                    if (workerChannel.Writer.TryWrite(dataMessageJson) == false)
                    {
                        await workerChannel.Writer.WriteAsync(dataMessageJson, _cancellationToken);
                    }
                }

                // check temperatures
                if (_workerTemperatureList.AverageTemperature > Config.HighTemperature)
                {
                    // turn off a worker
                    var activeWorkers = _workerStatusList.Where(ws => ws.Active).ToArray();
                    if (activeWorkers.Any())
                    {
                        int index = _random.Next(activeWorkers.Length);
                        var randomActiveWorker = activeWorkers[index];

                        var controlMessage = new ControlMessage
                        {
                            WorkerId = randomActiveWorker.Id,
                            Activate = false
                        };
                        string controlMessageJson = MessageJsonSerializer.Serialize(controlMessage);
                        if (_workerChannels[randomActiveWorker.Id].Writer.TryWrite(controlMessageJson) == false)
                        {
                            await _workerChannels[randomActiveWorker.Id].Writer.WriteAsync(controlMessageJson, _cancellationToken);
                        }
                    }
                }

                if (_workerTemperatureList.AverageTemperature < Config.LowTemperature)
                {
                    // turn on a worker
                    var inactiveWorkers = _workerStatusList.Where(ws => !ws.Active).ToArray();
                    if (inactiveWorkers.Any())
                    {
                        int index = _random.Next(inactiveWorkers.Length);
                        var randomInactiveWorker = inactiveWorkers[index];

                        var controlMessage = new ControlMessage
                        {
                            WorkerId = randomInactiveWorker.Id,
                            Activate = false
                        };
                        string controlMessageJson = MessageJsonSerializer.Serialize(controlMessage);
                        if (_workerChannels[randomInactiveWorker.Id].Writer.TryWrite(controlMessageJson) == false)
                        {
                            await _workerChannels[randomInactiveWorker.Id].Writer.WriteAsync(controlMessageJson, _cancellationToken);
                        }
                    }
                }

                // toggle random workers to destabilize system
                if (_random.Next(0, 2) == 0)
                {
                    int workerId = _random.Next(0, Config.NumberOfWorkers);
                    var controlMessage = new ControlMessage
                    {
                        WorkerId = workerId,
                        Activate = _random.Next(0, 2) == 0
                    };
                    string controlMessageJson = MessageJsonSerializer.Serialize(controlMessage);
                    if (_workerChannels[workerId].Writer.TryWrite(controlMessageJson) == false)
                    {
                        await _workerChannels[workerId].Writer.WriteAsync(controlMessageJson, _cancellationToken);
                    }
                }

                await Task.Delay(Config.CoordinatorLoopDelay, _cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Coordinator loop canceled.");
        }
        finally
        {
            await HandleShutdownAsync(workerTasks, processResponsesTask);
        }
    }

    private async Task HandleShutdownAsync(List<Task> workerTasks, Task processResponsesTask)
    {
        Console.WriteLine("Stopping all workers");
        foreach (var worker in _workerStatusList.Where(w => w.Active))
        {
            var controlMessage = new ControlMessage
            {
                WorkerId = worker.Id,
                Activate = false
            };
            string controlMessageJson = MessageJsonSerializer.Serialize(controlMessage);
            _workerChannels[worker.Id].Writer.TryWrite(controlMessageJson);
        }

        Console.WriteLine("Completing all channels");
        foreach (var workerChannel in _workerChannels)
        {
            workerChannel.Writer.Complete();
        }

        await Task.WhenAll(workerTasks);

        await processResponsesTask;
        Console.WriteLine("Done with wind down");
        Console.WriteLine("Coordinator loop finished.");
    }

    private async Task ReadCoordinatorChannelAsync()
    {
        try
        {
            await foreach (var item in _responseChannel.Reader.ReadAllAsync(_cancellationToken))
            {
                var message = MessageJsonSerializer.Deserialize<Message>(item);

                switch (message)
                {
                    case DataResponseMessage responseMessage:
                    {
                        Console.WriteLine($"Coordinator received response from Worker {responseMessage.WorkerId}: {responseMessage.Temperature:##.#}");
                        break;
                    }
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
        catch (OperationCanceledException)
        {
            // Handle the cancellation gracefully here
            Console.WriteLine("ReadCoordinatorChannelAsync canceled.");
        }
        finally
        {
            // Perform any necessary cleanup here
            Console.WriteLine("ReadCoordinatorChannelAsync finished.");
        }
    }

    private async Task StreamDataFromWorker(int workerId, CancellationToken cancellationToken)
    {
        var worker = _workers.First(w => w.Id == workerId);

        await foreach (var data in worker.GetTemperatureDataStream().WithCancellation(cancellationToken))
        {
            Console.WriteLine($"WorkerId: {data.WorkerId}, Timestamp: {data.Timestamp:O}, Temperature: {data.Temperature:F2}°C");
        }
    }
}