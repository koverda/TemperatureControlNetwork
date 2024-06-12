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


## To Do

- [x] Turning workers on and off and updating statuses everywhere
- [x] Nice shutdown
- [x] Temperature calculations
	- [x] Workers send temp data to coordinator, it keeps an average 
- [x] Triggers for turning workers on and off
	- [x] If too hot, coordinator turns off a worker
	- [x] If too cold, coordinator turns on a worker
- [ ] Streaming data
	- [ ] Get higher frequency data for a detailed view of a worker
- [ ] Saving data to long term storage
- [ ] Worker to worker communcation routed thru coordinator
	- [ ] Workers overheat and have to turn off, but they contact an inactive worker to take over
- [ ] GUI [Terminal.Gui](https://gui-cs.github.io/Terminal.Gui/index.html)