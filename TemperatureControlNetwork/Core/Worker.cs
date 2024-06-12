using System.Runtime.CompilerServices;
using System.Threading.Channels;
using TemperatureControlNetwork.Messaging;

namespace TemperatureControlNetwork.Core;

public class Worker
{
    private readonly ChannelReader<string> _channelReader;
    private readonly Random _random = new();
    private readonly ChannelWriter<string> _responseChannelWriter;

    private bool _isActive;
    private double _temperature = Config.StartingTemperature;
    private List<WorkerStatus> _workerStatusList = [];


    public Worker(
        ChannelReader<string> channelReader
        , ChannelWriter<string> responseChannelWriter
        , int id
    )
    {
        _channelReader = channelReader;
        _responseChannelWriter = responseChannelWriter;
        Id = id;
        _isActive = true;
    }

    public int Id { get; }


    public async Task StartAsync()
    {
        _ = WorkerLoopAsync();
        await ProcessChannelMessages();
    }

    private async Task WorkerLoopAsync()
    {
        while (true)
        {
            RecalculateWorkerTemperature();

            if (_temperature >= Config.MaxTemperature) await HandleOverheat();

            await Task.Delay(Config.WorkerLoopDelay);
        }
    }

    private async Task HandleOverheat()
    {
        var inactiveWorkers = _workerStatusList.Where(w => !w.Active).ToArray();
        if (inactiveWorkers.Length > 0)
        {
            var workerToActivate = inactiveWorkers[_random.Next(inactiveWorkers.Length)];
            Console.WriteLine($"Worker {Id:000}: has reached max temperature: {_temperature:##.#}°C, needs to deactivate self and activate neighbor: {workerToActivate.Id}");

            var overheatMessage = new OverheatTakeoverMessage(Id, workerToActivate.Id);
            await _responseChannelWriter.WriteAsync(MessageJsonSerializer.Serialize(overheatMessage));
        }
    }

    private void RecalculateWorkerTemperature()
    {
        double adjustmentStep = _random.NextDouble() * Config.MaxAdjustment;

        // heat up if active, else cool down
        _temperature = _isActive
            ? Math.Min(_temperature + adjustmentStep, Config.MaxTemperature)
            : Math.Max(_temperature - adjustmentStep / 2, Config.MinTemperature); // cools down slower
        Console.WriteLine($"Worker {Id:000}: {(_isActive ? "  active" : "inactive")}: {_temperature:##.#}°C");
    }

    private async Task ProcessChannelMessages()
    {
        await foreach (string item in _channelReader.ReadAllAsync())
        {
            var message = MessageJsonSerializer.Deserialize<IMessage>(item);

            switch (message)
            {
                case ControlMessage controlMessage:
                {
                    _isActive = controlMessage.Activate;
                    Console.WriteLine($"Worker {Id} {(controlMessage.Activate ? "activated" : "deactivated")}.");
                    var activationResponseMessage = new StatusUpdateResponseMessage(Id, _isActive);
                    await _responseChannelWriter.WriteAsync(MessageJsonSerializer.Serialize(activationResponseMessage));
                    break;
                }
                case DataMessage dataMessage:
                {
                    Console.WriteLine($"Worker {Id} received: {dataMessage.Data}");
                    var responseMessage = new DataResponseMessage(Id, _temperature);
                    await _responseChannelWriter.WriteAsync(MessageJsonSerializer.Serialize(responseMessage));
                    break;
                }
                case StatusUpdateMessage statusUpdateMessage:
                {
                    Console.WriteLine($"Worker {Id} received statusUpdate");
                    _workerStatusList = statusUpdateMessage.WorkerStatusList;
                    break;
                }
            }
        }
    }


    public async IAsyncEnumerable<TemperatureData> GetTemperatureDataStream([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _temperature += (_random.NextDouble() - 0.5) / 5;

            var data = new TemperatureData
            {
                WorkerId = Id,
                Timestamp = DateTime.Now,
                Temperature = _temperature
            };

            yield return data;

            await Task.Delay(100, cancellationToken);
        }
    }
}