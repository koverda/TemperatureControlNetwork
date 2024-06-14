# Temperature Control Network

## 1. Introduction

The Temperature Control Network is designed to monitor and control the temperature of 
multiple workers (e.g., heaters) in a coordinated manner. The system involves a central 
coordinator that communicates with individual workers to manage their states 
(active/inactive) and receive temperature data. This document outlines the architecture, 
components, data flow, and design considerations for the Temperature Control Network.

## 2. Architecture Overview

- Coordinator: Central controller that manages the workers, requests data, and adjusts worker states based on temperature thresholds.
- Workers: Individual units that maintain and report their temperature, and adjust their state (active/inactive) based on commands from the coordinator.
- Channels: Mechanism for communication between the coordinator and workers, using channels for message passing.
- Streams: Mechanism for sending real time temperature data from a worker to the coordinator using IAsyncEnumerable. 

## 3. Components

**3.1 Coordinator:**
	- Request temperature data from all workers at regular intervals.
	- Process temperature data and decide whether to activate or deactivate workers based on predefined thresholds.
	- Can stream data from worker for real time data storage. 
	- Handle system shutdown gracefully, ensuring all workers are properly deactivated.
	
**3.2 Workers:**
	- Maintain its own temperature.
	- Adjust temperature based on its state (active/inactive).
	- Respond to data requests from the coordinator.
	- Change state based on commands from the coordinator.
	- Track itself for overheating, if it overheats it finds an inactive worker to take over and messages to the coordinator to facilitate.
	
**3.3 Channels:**
	- Used for communication between the coordinator and workers.
	- Implemented using System.Threading.Channels.

## 4. Data Flow

**4.1 Coordinator Requesting Data**
	- The coordinator sends a DataMessage to all workers at regular intervals.
	- Workers receive the DataMessage and respond with their current temperature.

**4.2 Worker Responding with Temperature Data**
	- Workers receive a DataMessage and process it.
	- Workers send a DataResponseMessage back to the coordinator with their current temperature.

**4.3 Coordinator Processing Responses**
	- The coordinator receives DataResponseMessage from workers.
	- The coordinator updates the temperature list and decides whether to activate or deactivate workers based on the average temperature.

**4.4 Coordinator Adjusting Worker States**
	- If the average temperature exceeds a high threshold, the coordinator deactivates a random active worker.
	- If the average temperature falls below a low threshold, the coordinator activates a random inactive worker.
	- The coordinator sends ControlMessage to the selected worker to change its state.
	
**4.4 Worker Streaming to Coordinator**
	- If requested through the UI (TODO), the selected worker streams real time data to the coordiantor.


## 6. Design Considerations

- **Scalability:** The system should be able to scale with an increasing number of workers.
- **Fault Tolerance:** The system should handle worker failures gracefully and ensure the coordinator can still manage the remaining workers.
- **Performance:** Efficient message passing and handling to ensure timely updates and adjustments.
- **Configurability:** Parameters like temperature thresholds, adjustment steps, and delays should be configurable.


## 7. Running the Application

1. Clone the repository
2. Build and run the application
3. Ctrl + C to stop the application


## 8. To Do

- [x] Turning workers on and off and updating statuses everywhere
- [x] Nice shutdown
- [x] Temperature calculations
	- [x] Workers send temp data to coordinator, it keeps an average 
- [x] Triggers for turning workers on and off
	- [x] If too hot, coordinator turns off a worker
	- [x] If too cold, coordinator turns on a worker
- [x] Streaming data
	- [x] Get higher frequency data for a detailed view of a worker
- [x] Saving data to long term storage
- [x] Worker to worker communcation routed thru coordinator
	- [x] Workers overheat and have to turn off, but they contact an inactive worker to take over
- [-] Improve project structure
- [ ] Better handling of status updates
- [-] Documentation
- [-] GUI [Terminal.Gui](https://gui-cs.github.io/Terminal.Gui/index.html)
	- [x] Base GUI
	- [ ] Wind-down app from GUI
	- [ ] Show coordinator average
	- [ ] Show graphs
	- [ ] Streaming data for GUI
- [ ] Tests 
	- [ ] for data stores
	- [ ] for other components
- [ ] Encrypt stored data 
- [-] System architecture document
