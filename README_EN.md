# GrasshopperSever

A plugin for Rhino Grasshopper providing TCP communication, data conversion, and component information query capabilities.

[中文文档](README.md) | English

## Project Information

- **Version**: 1.0
- **Supported Frameworks**: .NET Framework 4.8, .NET 7.0, .NET 7.0-windows
- **Plugin GUID**: 0171a275-7e22-4b2a-9f82-b80f07a08b08

## Features Overview

GrasshopperSever plugin provides the following core features for Grasshopper:

1. **Data Communication**: Receive and send data via TCP protocol
2. **Data Conversion**: Convert between JSON and JQueue formats
3. **Component Information Query**: Query and search Grasshopper component information
4. **Data Execution**: Execute received data commands

## Core Data Structures

### JQueue

A data structure composed of `(DateTime time, Queue<JData>)`, used to represent multiple JData items in a single TCP message.

- **time**: Queue creation time, used for version identification
- **queue**: Queue of JData objects

**Features**:
- Thread-safe queue implementation
- Supports JSON serialization and deserialization
- Supports deep cloning
- Supports timeout waiting and cancellation tokens

### JData

Basic data unit containing three properties:

- **Name**: Data name
- **Description**: Data description
- **Value**: Data content (string format)

## Components Description

### Data Communication Components

#### GHReceiver

Creates a TCP connection based on port and receives data. Each port accepts only one connection.

**Input Parameters**:
- `Enabled` (Boolean): Whether to enable the server, default is false
- `Port` (Integer): Listening port, default is 6879

**Output Parameters**:
- `Client` (TcpClientParam): Client connection object
- `JQueue` (JQueueParam): Incoming data

**Features**:
- Receives data in background thread
- Notifies GH battery refresh via `RhinoApp.InvokeOnUiThread`
- Only receives data newer than the last received (based on time tag)

#### GHSender

Sends data using TCP connection, supports batch sending.

**Input Parameters**:
- `Client` (TcpClientParam): Client connection object
- `JQueue` (JQueueParam): Data to send, sent in order

**Output Parameters**:
- `Result` (String): Execution result, used for displaying errors or reports

**Features**:
- Only triggers sending when JQueue.time is updated
- Automatically filters expired data

#### GHServer

Creates a TCP server based on port and receives data, executes internally and responds.

**Input Parameters**:
- `Enabled` (Boolean): Whether to enable the server, default is false
- `Port` (Integer): Listening port, default is 6879

**Output Parameters**:
- `Result` (String): Execution result, used for displaying errors or reports

### Data Conversion Components

#### Json2JQueue

Converts JSON format to JQueue.

**Input Parameters**:
- `String` (String): JSON format string

**Output Parameters**:
- `JQueue` (JQueueParam): Generated JQueue object

#### JQueue2Json

Converts JQueue to JSON format.

**Input Parameters**:
- `JQueue` (JQueueParam): JQueue object to convert

**Output Parameters**:
- `String` (String): JSON format string

#### StringTreeJQueue

Converts String Tree to JQueue.

**Input Parameters**:
- `String Tree` (GH_Structure<string>): String Tree structure

**Output Parameters**:
- `JQueue` (JQueueParam): Generated JQueue object

**Features**:
- Takes only the first three items from each branch
- Converts non-string formats to string
- Fills missing items with empty values

### Information Query Components

#### AllComponents

Outputs information of all registered components.

**Input Parameters**:
- `Refresh` (Boolean): Refresh, value change refreshes time once

**Output Parameters**:
- `JQueue` (JQueueParam): Information of all components

**Output Structure**:
```
[
  categorys,                    // All categories
  count,                        // Component count
  components                    // All registered components
]
```

#### FindComponentsByGuid

Queries component information by GUID.

**Input Parameters**:
- `Guid` (String): Component GUID

**Output Parameters**:
- `ComponentInfo` (JQueueParam): Component information

**Output Structure** (ComponentJQueue):
```
[
  ComponentGuid,      // Component GUID
  InstanceGuid,       // Instance GUID
  ComponentName,      // Component name
  NickName,           // Component nickname
  Description,        // Component description
  Category,           // Main category
  SubCategory,        // Sub-category
  Position,           // Position information
  State,              // State information
  Inputs,             // Input parameters information
  Outputs             // Output parameters information
]
```

#### FindComponentsByName

Queries component information by name.

**Input Parameters**:
- `Name` (String): Component name

**Output Parameters**:
- `ComponentInfo` (JQueueParam): Component information

#### FindComponentsByCategory

Queries component information by Category.

**Input Parameters**:
- `Category` (String): Main category name

**Output Parameters**:
- `ComponentInfo` (JQueueParam): Component information

#### SearchComponentsByName

Searches components by name, supports fuzzy matching.

**Input Parameters**:
- `Keyword` (String): Search keyword

**Output Parameters**:
- `ComponentInfo` (JQueueParam): Component information list

#### ComponentConnector

Retrieves information about the connected component via its input port.

**Input Parameters**:
- `Refresh` (bool): Refresh output
- Input: Connect a component

**Output Parameters**:
- `Name` (string): Component name
- `GUID` (string): Component GUID
- `Instance` (string): Component instance GUID

### Execution Components

#### GHActuator

Executes input data.

**Input Parameters**:
- `JQueue` (JQueueParam): Data to execute

**Output Parameters**:
- `Result` (String): Execution result, used for displaying errors or reports

#### ScriptEditor

Modifies a Script component via input code, supports C# and Python.

**Input Parameters**:
- `ScriptComponent`: Connect a script component
- `Code` (String): Code to be added to the script

**Output Parameters**:
- `Result` (String): Execution result, used for displaying errors or reports

## Database Features

The plugin uses SQLite database to store metadata. The database file is located in the plugin directory (`GrasshopperSever.db`).

### DatabaseManager

Provides the following features:

- Automatic database initialization
- Create and manage data tables
- Track table update times
- Provide database connection objects
- Execute SQL commands with timestamp updates

### MetaInfo Table

Used to track table update times, contains the following fields:

- `Id`: Primary key
- `TableName`: Table name
- `LastUpdateTime`: Last update time
- `Description`: Table description

## Parameter Types

### JQueueParam

Parameter type used to pass JQueue data between Grasshopper batteries.

### TcpClientParam

Parameter type used to pass TCP client connection objects, uniquely created by GHReceiver based on port.

## Build and Installation

### Build Requirements

- .NET Framework 4.8 or .NET 7.0 SDK
- Grasshopper 8.29.26063.11001 or higher

### Build Steps

1. Open `GrasshopperSever.sln` with Visual Studio
2. Select target framework (net4.8, net7.0, or net7.0-windows)
3. Build the solution

### Installation

1. Copy the built `.gha` file to the Grasshopper components directory
2. Restart Rhino/Grasshopper
3. The plugin will be automatically loaded

## Usage Examples

### TCP Communication Example

1. Create a `GHReceiver` component and set the port number (e.g., 6879)
2. Set `Enabled` to `true` to start the receiver
3. Send JSON data to the specified port via TCP client
4. Data will be received and converted to JQueue format output

### Component Query Example

1. Use `AllComponents` to get all component lists
2. Use `FindComponentsByName` to find specific components
3. Use `SearchComponentsByName` for fuzzy search

### Data Conversion Example

1. Create a `Json2JQueue` component
2. Input JSON string
3. Get the converted JQueue object

## Notes

1. Each port can only create one TCP receiver
2. JQueue's time tag is used for version control, only receives/sends updated data
3. Database file is located in the plugin directory, ensure write permission
4. TCP communication uses UTF-8 encoding
5. Recommend using firewall rules to protect TCP ports

## Dependencies

- Grasshopper 8.29.26063.11001
- Microsoft.Data.Sqlite 10.0.5
- System.Data.SQLite 1.0.119
- System.Text.Json 10.0.5 (net4.8 only)
- System.Resources.Extensions 10.0.5

## License

Please refer to the project license file.

## Contributing

Issues and pull requests are welcome.

## Contact

For questions or suggestions, please contact the plugin author.

## Additional Documentation

- [AI Client Tutorial](AI_CLIENT_TUTORIAL.md) - Guide for AI clients to connect and interact with the plugin
- [插件开发文档](插件开发.md) - Plugin development documentation (Chinese)