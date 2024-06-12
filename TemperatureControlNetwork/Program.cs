using System.Threading;
using TemperatureControlNetwork.Core;

namespace TemperatureControlNetwork;

internal static class Program
{
    private static async Task Main()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        var coordinator = new Coordinator(cancellationTokenSource.Token);
        await coordinator.StartAsync();
    }
}