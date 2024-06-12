using System.Threading.Channels;

namespace TemperatureControlNetwork.Core;

// todo thin out class
public class Worker
{
    private readonly ChannelReader<string> _channelReader;
    private readonly int _id;
    private readonly ChannelWriter<string> _responseChannelWriter;
    private bool _isActive;
    private int _messagesProcessed;
    private List<WorkerStatus> _workerStatusList = [];

    private double _temperature = Config.StartingTemperature;
    private readonly double _minTemperature = Config.MinTemperature;
    private readonly double _maxTemperature = Config.MaxTemperature;
    private readonly Random _random = new();

    public Worker(
        ChannelReader<string> channelReader
        , ChannelWriter<string> responseChannelWriter
        , int id
    )
    {
        _channelReader = channelReader;
        _responseChannelWriter = responseChannelWriter;
        _id = id;
        _messagesProcessed = 0;
        _isActive = true; // Workers start as active
    }


    public async Task StartAsync()
    {
        // this is the worker's loop, we want it to run continuously and not await completion
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        UpdateTemperatureLoopAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        await foreach (string item in _channelReader.ReadAllAsync())
        {
            var message = MessageJsonSerializer.Deserialize<Message>(item);

            _messagesProcessed++;
            Console.WriteLine($"Worker {_id} processed #{_messagesProcessed}");

            switch (message)
            {
                case ControlMessage controlMessage:
                {
                    _isActive = controlMessage.Activate;
                    Console.WriteLine($"Worker {_id} {(controlMessage.Activate ? "activated" : "deactivated")}.");
                    var activationResponseMessage = new StatusUpdateResponseMessage(_id, _isActive);
                    await _responseChannelWriter.WriteAsync(MessageJsonSerializer.Serialize(activationResponseMessage));
                    break;
                }
                case DataMessage dataMessage:
                {
                    Console.WriteLine($"Worker {_id} received: {dataMessage.Data}");
                    var responseMessage = new DataResponseMessage(workerId: _id, temperature: _temperature);
                    await _responseChannelWriter.WriteAsync(MessageJsonSerializer.Serialize(responseMessage));
                    break;
                }
                case StatusUpdateMessage statusUpdateMessage:
                {
                    Console.WriteLine($"Worker {_id} received statusUpdate");
                    _workerStatusList = statusUpdateMessage.WorkerStatusList;
                    break;
                }
            }
        }
    }

    private async Task UpdateTemperatureLoopAsync()
    {
        while (true)
        {
            double adjustmentStep = _random.NextDouble() * Config.MaxAdjustment;

            // heat up if active, else cool down
            _temperature = _isActive
                ? Math.Min(_temperature + adjustmentStep, _maxTemperature)
                : Math.Max(_temperature - adjustmentStep / 2, _minTemperature); // cools down slower

            Console.WriteLine($"Worker {_id:000}: {(_isActive ? "  active" : "inactive")}: {_temperature:##.#}°C");
            await Task.Delay(Config.WorkerLoopDelay);
        }
    }
}