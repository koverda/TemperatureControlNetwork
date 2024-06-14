using System.Text.Json;
using System.Threading.Channels;
using TemperatureControlNetwork.Core.Interface;
using TemperatureControlNetwork.Core.Models;
using TemperatureControlNetwork.Data.Interface;
using TemperatureControlNetwork.Gui.Interface;
using TemperatureControlNetwork.Messaging;

namespace TemperatureControlNetwork.Core;

/// <summary>
/// The Coordinator class manages the interaction between workers, the GUI, and the temperature data store.
/// It also handles control messages and worker status updates.
/// </summary>
public class Coordinator : ICoordinator
{
    private readonly CancellationToken _cancellationToken;
    private readonly ITemperatureDataStore _temperatureDataStore;
    private readonly IGui _gui;
    private readonly Random _random;
    private readonly Channel<string> _coordinatorChannel;
    private readonly List<Channel<string>> _workerChannels = [];
    private readonly List<Worker> _workers = [];
    private readonly List<WorkerStatus> _workerStatusList = [];
    private readonly WorkerTemperatureList _workerTemperatureList = new([]);

    /// <summary>
    /// Initializes a new instance of the <see cref="Coordinator"/> class.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <param name="temperatureDataStore">The temperature data store.</param>
    /// <param name="gui">The GUI interface to display worker statuses.</param>
    public Coordinator(CancellationToken cancellationToken, ITemperatureDataStore temperatureDataStore, IGui gui)
    {
        _coordinatorChannel = Channel.CreateUnbounded<string>();
        _cancellationToken = cancellationToken;
        _temperatureDataStore = temperatureDataStore;
        _gui = gui;
        _random = new Random();

        for (int workerId = 0; workerId < Config.NumberOfWorkers; workerId++)
        {
            var workerChannel = Channel.CreateUnbounded<string>();
            _workerChannels.Add(workerChannel);
            _workers.Add(new Worker(workerChannel.Reader, _coordinatorChannel.Writer, workerId));
            _workerStatusList.Add(new WorkerStatus(workerId, active: true));
            _workerTemperatureList.WorkerTemperatures.Add(new WorkerTemperature(workerId, Config.StartingTemperature));
        }

        DisplayWorkerStatus();
    }

    /// <summary>
    /// Starts the coordinator and begins the process of managing workers and handling messages.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StartAsync()
    {
        var workerTasks = _workers.Select(worker => worker.StartAsync()).ToList();
        var readCoordinatorChannelTask = ReadCoordinatorChannelAsync();

        await CoordinatorLoop(workerTasks, readCoordinatorChannelTask);
    }

    /// <summary>
    /// The main loop of the Coordinator that manages worker tasks and processes responses.
    /// </summary>
    /// <param name="workerTasks">A list of tasks representing the worker operations.</param>
    /// <param name="processResponsesTask">A task for processing responses from the workers.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task CoordinatorLoop(List<Task> workerTasks, Task processResponsesTask)
    {
        try
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"Average Temperature {_workerTemperatureList.AverageTemperature:##.#}");

                // Request data from all workers
                var dataMessage = new DataMessage { Data = "Data request from coordinator" };
                var dataMessageJson = MessageJsonSerializer.Serialize(dataMessage);
                await MessageAllWorkers(dataMessageJson);

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
                var activate = _random.Next(0, 4) == 0;
                int workerId = _random.Next(0, Config.NumberOfWorkers);
                if (_workerStatusList.Any(w => w.Id == workerId && w.Active != activate))
                {
                    await ActivateWorker(workerId, activate);
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

    private async Task MessageAllWorkers(string messageJson)
    {
        foreach (var workerChannel in _workerChannels)
        {
            if (workerChannel.Writer.TryWrite(messageJson) == false)
            {
                await workerChannel.Writer.WriteAsync(messageJson, _cancellationToken);
            }
        }
    }

    /// <summary>
    /// Sends an activation or deactivation command to a specific worker.
    /// </summary>
    /// <param name="workerId">The ID of the worker to activate or deactivate.</param>
    /// <param name="activate">A boolean value indicating whether to activate (<c>true</c>) or deactivate (<c>false</c>) the worker.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ActivateWorker(int workerId, bool activate)
    {
        var controlMessage = new ControlMessage
        {
            WorkerId = workerId,
            Activate = activate
        };
        string controlMessageJson = MessageJsonSerializer.Serialize(controlMessage);
        if (_workerChannels[workerId].Writer.TryWrite(controlMessageJson) == false)
        {
            await _workerChannels[workerId].Writer.WriteAsync(controlMessageJson, _cancellationToken);
        }
    }

    /// <summary>
    /// Handles the shutdown process by stopping all workers and completing all channels.
    /// </summary>
    /// <param name="workerTasks">The list of worker tasks to wait for completion.</param>
    /// <param name="processResponsesTask">The task for processing responses.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Reads and processes messages from the coordinator's response channel.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ReadCoordinatorChannelAsync()
    {
        try
        {
            await foreach (string item in _coordinatorChannel.Reader.ReadAllAsync(_cancellationToken))
            {
                var message = MessageJsonSerializer.Deserialize<IMessage>(item);

                await HandleIncomingMessage(message);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("ReadCoordinatorChannelAsync canceled.");
        }
        finally
        {
            Console.WriteLine("ReadCoordinatorChannelAsync finished.");
        }
    }

    /// <summary>
    /// Handles incoming messages and performs actions based on the message type.
    /// </summary>
    /// <param name="message">The incoming message to handle.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task HandleIncomingMessage(IMessage message)
    {
        switch (message)
        {
            case DataResponseMessage responseMessage:
            {
                Console.WriteLine($"Coordinator received response from Worker {responseMessage.WorkerId}: {responseMessage.Temperature:##.#}");
                _workerTemperatureList.WorkerTemperatures.First(w => w.Id == responseMessage.WorkerId).Temperature = responseMessage.Temperature;
                break;
            }
            case StatusUpdateResponseMessage statusUpdateResponseMessage:
            {
                _workerStatusList.First(w => w.Id == statusUpdateResponseMessage.WorkerId).Active = statusUpdateResponseMessage.Active;

                var workerStatusUpdateMessage = new StatusUpdateMessage(_workerStatusList);
                await MessageAllWorkers(JsonSerializer.Serialize(workerStatusUpdateMessage));

                DisplayWorkerStatus();

                break;
            }
            case OverheatTakeoverMessage overheatTakeoverMessage:
            {
                await ActivateWorker(overheatTakeoverMessage.WorkerToDeactivate, false);
                await ActivateWorker(overheatTakeoverMessage.WorkerToActivate, true);
                break;
            }
        }
    }

    /// <summary>
    /// Streams data from a specified worker and stores it in the temperature data store.
    /// </summary>
    /// <param name="workerId">The ID of the worker to stream data from.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task StreamDataFromWorker(int workerId, CancellationToken cancellationToken)
    {
        var worker = _workers.First(w => w.Id == workerId);

        await foreach (var data in worker.GetTemperatureDataStream().WithCancellation(cancellationToken))
        {
            Console.WriteLine($"WorkerId: {data.WorkerId}, Timestamp: {data.Timestamp:O}, Temperature: {data.Temperature:F2}°C");
            await _temperatureDataStore.AddTemperatureDataAsync(data);
        }
    }

    /// <summary>
    /// Displays the current status of all workers using the GUI.
    /// </summary>
    private void DisplayWorkerStatus()
    {
        _gui.DisplayWorkerStatus(_workerStatusList, _workerTemperatureList);
    }
}