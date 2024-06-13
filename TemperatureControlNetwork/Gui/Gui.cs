using System.Data;
using TemperatureControlNetwork.Core.Models;
using TemperatureControlNetwork.Gui.Interface;
using Terminal.Gui;

namespace TemperatureControlNetwork.Gui;

public class Gui : IGui
{
    private readonly TableView _workerTableView;
    private readonly DataTable _workerStatusTable;

    public Gui()
    {
        Application.Init();
        var top = Application.Top;

        var window = new Window("Temperature Control Network")
        {
            X = 0,
            Y = 1, // Leave one row for the top-level menu
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        top.Add(window);

        _workerTableView = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        window.Add(_workerTableView);

        _workerStatusTable = new DataTable();
        _workerStatusTable.Columns.Add("Worker ID", typeof(string));
        _workerStatusTable.Columns.Add("Status", typeof(string));
        _workerStatusTable.Columns.Add("Temperature", typeof(string));
    }

    public void DisplayWorkerStatus(List<WorkerStatus> workerStatusList, WorkerTemperatureList workerTemperatureList)
    {
        _workerStatusTable.Clear();
        foreach (var status in workerStatusList)
        {
            var temperature = workerTemperatureList.WorkerTemperatures.FirstOrDefault(wt => wt.Id == status.Id)?.Temperature ?? 0;
            _workerStatusTable.Rows.Add(status.Id.ToString(), status.Active ? "Active" : "Inactive", $"{temperature:##.#} °C");
        }

        Application.MainLoop.Invoke(() =>
        {
            _workerTableView.Table = _workerStatusTable;
            _workerTableView.SetNeedsDisplay();
        });
    }

    public void Run()
    {
        Application.Run();
    }

    public void Stop()
    {
        Application.RequestStop();
    }
}