﻿using System;
using System.Text.Json;
using System.Threading.Channels;

namespace TemperatureControlNetwork.Core;

public class Worker
{
    private readonly ChannelReader<string> _channelReader;
    private readonly int _id;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ChannelWriter<string> _responseChannelWriter;
    private bool _isActive;
    private int _messagesProcessed;
    private List<WorkerStatus> _neighborStatusList = [];

    private double _temperature = 20.0;
    private readonly double _minTemperature = 10.0;
    private readonly double _maxTemperature = 30.0;
    private readonly Random _random = new Random();

    public Worker(
        ChannelReader<string> channelReader
        , ChannelWriter<string> responseChannelWriter
        , int id, JsonSerializerOptions jsonOptions
    )
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
        UpdateTemperatureLoopAsync();

        await foreach (string item in _channelReader.ReadAllAsync())
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


    private async Task UpdateTemperatureLoopAsync()
    {
        while (true)
        {
            double adjustmentStep = _random.NextDouble() * 0.5; // Random adjustment step between 0 and 0.5

            if (_isActive)
            {
                var activeNeighbors = _neighborStatusList.Count(w => w.Active);
                var averageActive = _neighborStatusList.Count / 2.0;

                if (activeNeighbors > averageActive)
                {
                    _temperature = Math.Min(_temperature + adjustmentStep, _maxTemperature);
                }
                else
                {
                    _temperature = Math.Max(_temperature - adjustmentStep, _minTemperature);
                }
            }
            else
            {
                _temperature = Math.Max(_temperature - adjustmentStep, _minTemperature);
            }

            Console.WriteLine($"Worker {_id} temperature: {_temperature:##.#}°C");
            await Task.Delay(1000); // Update temperature every second
        }
    }
}