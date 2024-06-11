using System.Text.Json;
using System.Threading.Channels;

namespace TemperatureControlNetwork;

public class Worker(ChannelReader<string> channelReader, int id)
{
    private bool _isActive = true;

    public async Task StartAsync()
    {
        await foreach (var item in channelReader.ReadAllAsync())
        {
            var message = JsonSerializer.Deserialize<Message>(item);

            if (message is ControlMessage controlMessage)
            {
                if (controlMessage.WorkerId == id)
                {
                    _isActive = controlMessage.Activate;
                    Console.WriteLine($"Worker {id} {(controlMessage.Activate ? "activated" : "deactivated")}.");
                }
            }
            else if (message is DataMessage dataMessage && _isActive)
            {
                Console.WriteLine($"Worker {id} received: {dataMessage.Data}");
                // Simulate processing
                await Task.Delay(500);
                Console.WriteLine($"Worker {id} processed: {dataMessage.Data}");
            }
        }
    }
}