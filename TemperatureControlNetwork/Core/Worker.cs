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
    private int _messagesProcessed;
    public List<(int Id, bool IsActive)> NeighborWorkerStatuses { get; set; }

    public Worker(ChannelReader<string> channelReader, ChannelWriter<string> responseChannelWriter, int id, JsonSerializerOptions jsonOptions)
    {
        _channelReader = channelReader;
        _responseChannelWriter = responseChannelWriter;
        _id = id;
        _jsonOptions = jsonOptions;
        _messagesProcessed = 0;
        _isActive = true; // Workers start as active
    }

    public async Task StartAsync()
    {
        await foreach (var item in _channelReader.ReadAllAsync())
        {
            var message = JsonSerializer.Deserialize<Message>(item, _jsonOptions);

            _messagesProcessed++;

            switch (message)
            {
                case ControlMessage controlMessage:
                {
                    if (controlMessage.WorkerId == _id)
                    {
                        _isActive = controlMessage.Activate;
                        Console.WriteLine($"Worker {_id} {(controlMessage.Activate ? "activated" : "deactivated")}.");
                    }

                    break;
                }
                case DataMessage dataMessage when _isActive:
                {
                    Console.WriteLine($"Worker {_id} received: {dataMessage.Data}");
                    Console.WriteLine($"Worker {_id} processed #{_messagesProcessed}: {dataMessage.Data}");

                    // Send a response back to the coordinator
                    var responseMessage = new ResponseMessage { WorkerId = _id, Response = $"Processed: {dataMessage.Data}" };
                    await _responseChannelWriter.WriteAsync(JsonSerializer.Serialize(responseMessage, _jsonOptions));
                    break;
                }
            }
        }
    }
}