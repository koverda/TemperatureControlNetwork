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
