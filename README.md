# TemperatureControlNetwork

This project demonstrates a .NET application using channels for concurrent, high-performance communication between a coordinator and multiple workers.

## Features

- **Coordinator:**
  - Manages multiple worker instances.
  - Sends data and control messages to workers.
  - Receives responses from workers.

- **Workers:**
  - Each has its own channel for receiving messages.
  - Processes data when active.
  - Sends responses back to the coordinator.

- **Bidirectional Communication:**
  - Uses channels for efficient, concurrent message passing.

## Running the Application

1. Clone the repository
2. Build and run the application
3. Ctrl + C to stop the application


## To DO

- [ ] Turning workers on and off and updating statuses everywhere
- [ ] Streaming data
	- [ ] Maybe workers store higher frequency temperature data and stream it to the coordinator once they run low on space
	- [ ] Manual way to get all workers to stream their data
- [ ] Saving data to long term storage
- [ ] Temperature calculations
	- [ ] Workers send temp data to coordinator, it keeps an average 
	- [ ] Workers track how many other workers are on and use that to adjust their temperature readings
- [ ] Triggers for turning workers on and off
	- [ ] If too hot, coordinator turns off a worker
	- [ ] If too cold, coordinator turns on a worker
- [ ] Worker to worker communcation routed thru coordinator
	- [ ] Workers randomly fail and have to self repair - thus they contact an in-active worker to take over
- [ ] GUI [Terminal.Gui](https://gui-cs.github.io/Terminal.Gui/index.html)