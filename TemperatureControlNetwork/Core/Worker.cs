using System.Text.Json;
using System.Threading.Channels;

namespace TemperatureControlNetwork.Core;

public class Worker
{
    private readonly ChannelReader<string> _channelReader;
    private readonly ChannelWriter<string> _responseChannelWriter;
    private readonly int _id;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _isActive;

    public Worker(ChannelReader<string> channelReader, ChannelWriter<string> responseChannelWriter, int id, JsonSerializerOptions jsonOptions)
    {
        _channelReader = channelReader;
        _responseChannelWriter = responseChannelWriter;
        _id = id;
        _jsonOptions = jsonOptions;
        _isActive = true; // Workers start as active
    }

    public async Task StartAsync()
    {
        await foreach (var item in _channelReader.ReadAllAsync())
        {
            var message = JsonSerializer.Deserialize<Message>(item, _jsonOptions);

            if (message is ControlMessage controlMessage)
            {
                if (controlMessage.WorkerId == _id)
                {
                    _isActive = controlMessage.Activate;
                    Console.WriteLine($"Worker {_id} {(controlMessage.Activate ? "activated" : "deactivated")}.");
                }
            }
            else if (message is DataMessage dataMessage && _isActive)
            {
                Console.WriteLine($"Worker {_id} received: {dataMessage.Data}");
                // Simulate processing
                await Task.Delay(500);
                Console.WriteLine($"Worker {_id} processed: {dataMessage.Data}");

                var responseMessage = new ResponseMessage(_id, $"Processed: {dataMessage.Data}");
                await _responseChannelWriter.WriteAsync(JsonSerializer.Serialize(responseMessage, _jsonOptions));
            }
        }
    }
}