using System.Data;
using TemperatureControlNetwork.Core;
using TemperatureControlNetwork.Core.Models;
using TemperatureControlNetwork.Gui.Interface;
using Terminal.Gui;

namespace TemperatureControlNetwork.Gui;

public class Gui : IGui
{
    private readonly TableView _workerTableView;
    private readonly DataTable _workerStatusTable;
    private readonly Label _averageTemperatureLabel;

    public Gui()
    {
        Application.Init();
        var top = Application.Top;

        var menu = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_File", new MenuItem[]
            {
                new MenuItem("_Close", "", () => { Application.RequestStop(); })
            }),
        });


        var window = new Window("Temperature Control Network")
        {
            X = 0,
            Y = 1, // Leave one row for the top-level menu
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        top.Add(window);
        top.Add(menu);


        _averageTemperatureLabel = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Text = "Average Temperature: 0 °C"
        };

        _workerTableView = new TableView
        {
            X = 0,
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };


        window.Add(_workerTableView);
        window.Add(_averageTemperatureLabel);

        _workerStatusTable = new DataTable();
        _workerStatusTable.Columns.Add("Worker ID", typeof(int));
        _workerStatusTable.Columns.Add("Status", typeof(string));
        _workerStatusTable.Columns.Add("Temperature", typeof(string));
        for (int i = 0; i < Config.NumberOfWorkers; i++)
        {
            _workerStatusTable.Rows.Add(i);
        }
    }

    public void DisplayWorkerStatus(List<WorkerStatus> workerStatusList, WorkerTemperatureList workerTemperatureList)
    {
        foreach (DataRow dr in _workerStatusTable.Rows)
        {
            var workerStatus = workerStatusList.First(w => w.Id == (int)dr["Worker Id"]);
            dr["Status"] = workerStatus.Active ? "Active" : "Inactive";

            var temperature = workerTemperatureList.WorkerTemperatures.FirstOrDefault(wt => wt.Id == workerStatus.Id)?.Temperature ?? 0;
            dr["Temperature"] = $"{temperature:##.#} °C";
        }

        Application.MainLoop.Invoke(() =>
        {
            try
            {
                _workerTableView.Table = _workerStatusTable;
                _workerTableView.SetNeedsDisplay();
                _averageTemperatureLabel.Text = $"Average Temperature: {workerTemperatureList.AverageTemperature:##.#} °C";
            }
            catch (RowNotInTableException e)
            {
                Console.WriteLine(e);
            }
        });
    }

    public void Run()
    {
        Application.Run();
    }

    public void Stop()
    {
        Application.Shutdown();
    }
}