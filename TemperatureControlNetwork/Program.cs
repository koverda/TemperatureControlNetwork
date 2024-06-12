using Microsoft.Extensions.DependencyInjection;
using TemperatureControlNetwork.Core;
using TemperatureControlNetwork.Data;
using TemperatureControlNetwork.Data.Interface;

namespace TemperatureControlNetwork;

internal static class Program
{
    private static async Task Main()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var coordinator = serviceProvider.GetRequiredService<Coordinator>();

        await coordinator.StartAsync();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<ITemperatureDataStore>(_ =>
            {
                const string filePath = "path/to/file.csv";
                return new CsvTemperatureDataStore(filePath);
            })
            // .AddSingleton<ITemperatureDataStore>(_ => new InMemoryTemperatureDataStore()) // can pick either csv or in-memory
            .AddSingleton(provider =>
            {
                var cancellationToken = new CancellationTokenSource().Token;
                var temperatureDataStore = provider.GetRequiredService<ITemperatureDataStore>();
                return new Coordinator(cancellationToken, temperatureDataStore);
            });
    }
}