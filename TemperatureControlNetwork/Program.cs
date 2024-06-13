using System.ComponentModel;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using TemperatureControlNetwork.Core;
using TemperatureControlNetwork.Core.Interface;
using TemperatureControlNetwork.Data;
using TemperatureControlNetwork.Data.Interface;
using TemperatureControlNetwork.Gui.Interface;
using Terminal.Gui;

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
        ConfigureServices(serviceCollection, cancellationTokenSource.Token);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var coordinator = serviceProvider.GetRequiredService<Coordinator>();
        var gui = serviceProvider.GetRequiredService<IGui>();

        Task coordinatorTask = Task.Run(async () => await coordinator.StartAsync());
        Task uiTask = Task.Run(() => gui.Run());

        await Task.WhenAll(coordinatorTask, uiTask);
    }

    private static void ConfigureServices(IServiceCollection services, CancellationToken cancellationToken)
    {
        services.AddSingleton<IGui, Gui.Gui>();
        // services.AddSingleton<ITemperatureDataStore>(_ => new InMemoryTemperatureDataStore()) // can pick either csv or in-memory
        services.AddSingleton<ITemperatureDataStore>(_ =>
        {
            const string filePath = "path/to/file.csv";
            return new CsvTemperatureDataStore(filePath);
        });
        services.AddSingleton<Coordinator>(provider =>
        {
            var temperatureDataStore = provider.GetRequiredService<ITemperatureDataStore>();
            var gui = provider.GetRequiredService<IGui>();
            return new Coordinator(cancellationToken, temperatureDataStore, gui);
        });
    }
}