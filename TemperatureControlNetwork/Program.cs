using TemperatureControlNetwork.Core;

namespace TemperatureControlNetwork;

internal static class Program
{
    private static async Task Main()
    {
        var coordinator = new Coordinator(new CancellationTokenSource().Token);
        await coordinator.StartAsync();
    }
}