using TemperatureControlNetwork.Core;

namespace TemperatureControlNetwork;

internal static class Program
{
    private static async Task Main()
    {
        int numberOfWorkers = 3;
        var cancellationTokenSource = new CancellationTokenSource();

        var coordinator = new Coordinator(numberOfWorkers, cancellationTokenSource.Token);
        await coordinator.StartAsync();
    }
}