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
2. **Data Conversion**: Convert between JSON and Ljson formats
3. **Component Information Query**: Query and search Grasshopper component information
4. **Data Execution**: Execute received data commands

## Core Data Structures

### Ljson

A unified data structure used to represent a single data item, containing name, info, time, and value.

- **Name**: Data name
- **Info**: Data description
- **Time**: Creation time, used for version identification
- **Value**: Data value (JsonElement, can be object, array, or primitive value)

**Features**:
- Supports JSON serialization and deserialization
- Supports deep cloning
- Implements IDisposable interface
- Supports parameter getting, searching, and setting (supports object and array formats)
- Provides static methods to create common types of Ljson (error, success, component info, etc.)

**LjsonHelper Utility Class**:
- `SerializeLjsonArray`: Serialize Ljson array to JSON string
- `ParseLjsonArray`: Deserialize JSON string to Ljson array

## TCP Communication Commands

GrasshopperSever supports sending various commands via TCP protocol to control Grasshopper and Rhino.

### Command Format

All commands use unified LJSON format:

```json
{
  "Name": "Command Type",
  "Info": "Command Description",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "Specific Command Name",
    "Parameter Name": "Parameter Value"
  }
}
```

**Name Field (Command Types)**:
- `COMPONENT` - Component-related commands
- `DOCUMENT` - Document-related commands
- `RHINO` - Rhino-related commands
- `DESIGN` - Design layout commands (component addition, connection, etc.)

### Component Commands

#### GETALLCOMPONENTS
Get all component information

#### FINDCOMPONENTBYGUID
Find component by GUID

#### FINDCOMPONENTBYNAME
Find component by name

#### FINDCOMPONENTBYCATEGORY
Find component by category

#### SEARCHCOMPONENTSBYNAME
Search components by name (fuzzy search)

### Document Commands

#### SAVEDOCUMENT
Save current document

#### LOADDOCUMENT
Load document

#### DATABASEPATH
Get database path

### Rhino Commands

#### RHINOSCRIPT
Run Rhino script (e.g., `_-Line 0,0,0 10,10,0`)

#### GETLASTCREATEDOBJECTS
Get last created objects

#### SELECTOBJECTS
Select objects

#### GETANDSELECTLASTOBJECTS
Get and select last created objects (composite command)

### Design Commands

Design commands are used to control component layout operations such as adding, removing, connecting, and setting values.

#### ADDCOMPONENTBYGUID
Add component via GUID

**Parameters**:
- `ComponentGuid` - Component GUID
- `X` - X coordinate (number)
- `Y` - Y coordinate (number)

**Example**:
```json
{
  "Name": "Design",
  "Command": "AddComponentByGuid",
  "ComponentGuid": "c5b7583d-7958-49f1-ae16-6272dfb9452a",
  "X": 100,
  "Y": 100
}
```

#### ADDCOMPONENTBYNAME
Add component via name

**Parameters**:
- `ComponentName` - Component name
- `X` - X coordinate (number)
- `Y` - Y coordinate (number)

**Example**:
```json
{
  "Name": "Design",
  "Command": "AddComponentByName",
  "ComponentName": "Addition",
  "X": 100,
  "Y": 100
}
```

#### REMOVECOMPONENT
Remove component

**Parameters**:
- `InstanceGuid` - Component instance GUID

**Example**:
```json
{
  "Name": "Design",
  "Command": "RemoveComponent",
  "InstanceGuid": "xxxx-xxxx-xxxx-xxxx"
}
```

#### SETCOMPONENTVALUE
Set component value

**Parameters**:
- `InstanceGuid` - Component instance GUID
- `Value` - Value to set

**Example**:
```json
{
  "Name": "Design",
  "Command": "SetComponentValue",
  "InstanceGuid": "xxxx-xxxx-xxxx-xxxx",
  "Value": "42"
}
```

#### CONNECTCOMPONENTS
Connect two component parameters

**Parameters**:
- `FromGuid` - Source component instance GUID
- `FromParameter` - Source component output parameter name
- `ToGuid` - Target component instance GUID
- `ToParameter` - Target component input parameter name

**Example**:
```json
{
  "Name": "Design",
  "Command": "ConnectComponents",
  "FromGuid": "instance-guid-1",
  "FromParameter": "Result",
  "ToGuid": "instance-guid-2",
  "ToParameter": "A"
}
```

#### DISCONNECTCOMPONENTS
Disconnect connection between two component parameters

**Parameters**:
- `FromGuid` - Source component instance GUID
- `FromParameter` - Source component output parameter name
- `ToGuid` - Target component instance GUID
- `ToParameter` - Target component input parameter name

**Example**:
```json
{
  "Name": "Design",
  "Command": "DisconnectComponents",
  "FromGuid": "instance-guid-1",
  "FromParameter": "Result",
  "ToGuid": "instance-guid-2",
  "ToParameter": "A"
}
```

### OUTPUT Special Key

When the Value field contains the `OUTPUT` key, its value will be output on the GHServer's Output port:

```json
{
  "Name": "TestMessage",
  "Info": "Test Message",
  "Value": {
    "OUTPUT": "Data to display on output port"
  }
}
```

### Data Communication Features

- **TCP Long Connection Support**: Send multiple messages continuously
- **Automatic Data Echo**: Server echoes received data
- **UTF-8 BOM Marker**: Response contains UTF-8 BOM, decode with `utf-8-sig`
- **Complete JSON Support**: Supports all JSON data types and nested structures
- **Unicode Support**: Full support for Chinese and special characters

## Components Description

### Data Communication Components

#### GHReceiver

Creates a TCP connection based on port and receives data. Each port accepts only one connection.

**Input Parameters**:
- `Enabled` (Boolean): Whether to enable the server, default is false
- `Port` (Integer): Listening port, default is 6879

**Output Parameters**:
- `Client` (TcpClientParam): Client connection object
- `Ljson` (LjsonParam): Incoming data
- `Status` (String): Status

**Features**:
- Receives data in background thread
- Notifies GH battery refresh via `RhinoApp.InvokeOnUiThread`
- Only receives data newer than the last received (based on time tag)

#### GHSender

Sends data using TCP connection, supports batch sending.

**Input Parameters**:
- `Client` (TcpClientParam): Client connection object
- `Ljson` (LjsonParam): Data to send, sent in order

**Output Parameters**:
- `Status` (String): Sending status

**Features**:
- Only triggers sending when Ljson.time is updated
- Automatically filters expired data

#### GHServer

Creates a TCP server based on port and receives data, executes internally and responds.

**Input Parameters**:
- `Enabled` (Boolean): Whether to enable the server, default is false
- `Port` (Integer): Listening port, default is 6879

**Output Parameters**:
- `Status` (String): Response status
- `OutPut` (Generic): Display output data

### Data Conversion Components

#### Json2Ljson

Converts JSON format to Ljson.

**Input Parameters**:
- `String` (String): JSON format string

**Output Parameters**:
- `Ljson` (LjsonParam): Generated Ljson object

#### Ljson2Json

Converts Ljson to JSON format.

**Input Parameters**:
- `Ljson` (LjsonParam): Ljson object to convert

**Output Parameters**:
- `String` (String): JSON format string

#### DataTreeLjson

Constructs Ljson from Name, Info, and Data Tree. Each branch can only contain 1 or 2 elements: 1 element converts to list, 2 elements convert to dict.

**Input Parameters**:
- `Name` (String): Ljson name
- `Info` (String): Ljson description
- `Data Tree` (Data Tree): Data Tree data

**Output Parameters**:
- `Ljson` (LjsonParam): Generated Ljson object

#### FindJdata

Finds Jdata value by name.

**Input Parameters**:
- `Ljson` (LjsonParam): Ljson object to search
- `Name` (String): Key value to find

**Output Parameters**:
- `Data` (Generic): Found value (primitive type or string)
- `DataList` (List): Found value list (primitive type or string)

### Information Query Components

#### AllComponents

Outputs information of all registered components.

**Input Parameters**:
- `Refresh` (Boolean): Refresh, value change refreshes time once

**Output Parameters**:
- `Ljson` (LjsonParam): Information of all components

**Output Structure** (Ljson.Value):
```json
{
  "categorys": "All categories",
  "count": "Component count",
  "components": "All registered components"
}
```

#### FindComponentsByGuid

Queries component information by GUID.

**Input Parameters**:
- `Guid` (String): Component GUID

**Output Parameters**:
- `ComponentInfo` (LjsonParam): Component information

**Output Structure** (ComponentLjson - Ljson.Value):
```json
{
  "ComponentGuid": "Component GUID",
  "ComponentName": "Component name",
  "NickName": "Component nickname",
  "Description": "Component description",
  "Category": "Main category",
  "SubCategory": "Sub-category",
  "Prototype": "funtion info"
}
```

#### FindComponentsByName

Queries component information by name.

**Input Parameters**:
- `Name` (String): Component name

**Output Parameters**:
- `ComponentInfo` (LjsonParam): Component information

#### FindComponentsByCategory

Queries component information by Category.

**Input Parameters**:
- `Category` (String): Main category name

**Output Parameters**:
- `ComponentInfo` (LjsonParam): Component information

#### SearchComponentsByName

Searches components by name, supports fuzzy matching.

**Input Parameters**:
- `Keyword` (String): Search keyword

**Output Parameters**:
- `ComponentInfo` (LjsonParam): Component information list

#### ComponentConnector

Retrieves information about the connected component via its input port.

**Input Parameters**:
- `Input` (Generic): Connect a component

**Output Parameters**:
- `Name` (String): Component name
- `GUID` (String): Component GUID
- `InsGUID` (String): Component object GUID
- `Instance` (Generic): Component object

#### SearchDataBase

Queries database.

**Input Parameters**:
- `SQL` (String): Complete SQL query statement

**Output Parameters**:
- `Result` (String): Query result, returned in JSON format

### Execution Components

#### GHActuator

Executes input data.

**Input Parameters**:
- `Ljson` (LjsonParam): Data to execute

**Output Parameters**:
- `Status` (String): Execution result
- `Result` (LjsonParam): Processed Ljson result
- `OutPut` (Generic): Display output data

#### ScriptEditor

Modifies a Script component via input code, supports C# and Python.

**Input Parameters**:
- `ScriptComponent` (Generic): Rhino8 Grasshopper script component, supports only one component
- `Code` (String): Script code
- `IntputParams` (String): Input parameter definitions
- `OutputParams` (String): Output parameter definitions

**Output Parameters**:
- `Result` (String): Display runtime information
- `ComponentType` (String): Display component information
- `IsSDKMode` (Boolean): Whether code is SDK mode
- `SourceCode` (String): Code
- `InputParams` (String): Current input parameter information
- `OutputParams` (String): Current output parameter information

![scripteditor_test](Example/SCRIPT&CMD_SCRIPT/scripteditor_test.png)

![scripteditor_test](Example/SCRIPT&CMD_SCRIPT/scripteditor_test2.png)

#### RunScript

Runs C# script internally. This component is reserved for AI to execute scripts directly.

**Input Parameters**:
- `Code` (String): Script

**Output Parameters**:
- `Ljson` (LjsonParam): Data output
- `Out` (String): Debug output

#### CommandRhino

Executes Rhino script.

**Input Parameters**:
- `Ljson` (LjsonParam): Rhino command Ljson data to execute, must contain Command field

**Output Parameters**:
- `Result` (LjsonParam): Ljson result after execution

## Database Features

The plugin uses SQLite database to store data with a dual-database architecture:

### Database Architecture

#### 1. Main Database (ComponentsInfo.db)
- **Location**: Plugin directory
- **Purpose**: Stores global component information, shared by all Grasshopper documents
- **Tables**: ALLCOMPS, MetaInfo

#### 2. Document Database ({_ghdata.db)
- **Location**:
  - If document is saved: Same directory as gh file, named `{document_name}_ghdata.db`
  - If document is not saved: Plugin directory, named `TempDocument_{GUID}.db`
- **Purpose**: Stores document-specific data, tightly associated with the document
- **Tables**: GHScriptModifyHistory, RhinoObjects

**Advantages**:
- Global component information shared, improved performance
- Document-specific data bound to document, easy to share and manage
- Automatic cleanup of temporary data for unsaved documents

### DatabaseManager

Provides the following features:

- Manage main database and document database
- Automatic database initialization
- Create and manage data tables
- Track table update times (main database)
- Provide database connection objects
- Execute SQL commands with timestamp updates (main database)

### Main Database Tables

#### MetaInfo Table
Used to track table update times in the main database, contains the following fields:

| Field Name | Data Type | Constraint | Description |
|------------|-----------|------------|-------------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | Primary key, auto-increment |
| TableName | TEXT | NOT NULL UNIQUE | Table name |
| LastUpdateTime | DATETIME | DEFAULT CURRENT_TIMESTAMP | Last update time |
| Description | TEXT | - | Table description |

#### ALLCOMPS Table
Stores detailed information for all Grasshopper components (global cache).

| Field Name | Data Type | Constraint | Description |
|------------|-----------|------------|-------------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | Primary key, auto-increment |
| ComponentGuid | TEXT | NOT NULL UNIQUE | Component GUID (unique identifier) |
| ComponentName | TEXT | NOT NULL | Component name |
| NickName | TEXT | - | Component nickname |
| Description | TEXT | - | Component description |
| Category | TEXT | NOT NULL | Main category |
| SubCategory | TEXT | NOT NULL | Sub-category |
| Prototype | TEXT | DEFAULT '' | Function signature containing input and output parameters (JSON format) |

### Document Database Tables

#### RhinoObjects Table
Stores information about objects created in Rhino (document-specific).

| Field Name | Data Type | Constraint | Description |
|------------|-----------|------------|-------------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | Primary key, auto-increment |
| ObjectId | TEXT | NOT NULL | Object ID (GUID string) |
| ObjectType | TEXT | - | Object type (e.g., Curve, Surface, Mesh, etc.) |
| LayerName | TEXT | - | Layer name |
| ObjectName | TEXT | - | Object name |
| CreateTime | DATETIME | DEFAULT CURRENT_TIMESTAMP | Creation time |
| DocumentSerialNumber | TEXT | - | Document serial number |
| Description | TEXT | - | Description |

#### GHScriptModifyHistory Table
Stores modification history records for GHScript components (document-specific).

| Field Name | Data Type | Constraint | Description |
|------------|-----------|------------|-------------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | Primary key, auto-increment |
| InstanceGuid | TEXT | NOT NULL | Component instance GUID |
| ComponentGuid | TEXT | NOT NULL | Component type GUID |
| ComponentName | TEXT | - | Component name |
| ModifyType | TEXT | NOT NULL | Modification type (CODE_CHANGE or PARAM_CHANGE) |
| ModifyContent | TEXT | - | Modification content (JSON format) |
| Description | TEXT | - | Description |
| ModifyTime | DATETIME | DEFAULT CURRENT_TIMESTAMP | Modification time |

#### ComponentExchangeHistory Table
Stores component exchange operation history (document-specific), including add, remove, connect, disconnect operations.

| Field Name | Data Type | Constraint | Description |
|------------|-----------|------------|-------------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | Primary key, auto-increment |
| OperationType | TEXT | NOT NULL | Operation type (AddComponent, RemoveComponent, SetComponentValue, ConnectComponents, DisconnectComponents) |
| ComponentGuid | TEXT | - | Component GUID |
| InstanceGuid | TEXT | - | Component instance GUID |
| ComponentName | TEXT | - | Component name |
| PositionX | REAL | - | X coordinate (when adding component) |
| PositionY | REAL | - | Y coordinate (when adding component) |
| Value | TEXT | - | Set value (when setting component value) |
| FromInstanceGuid | TEXT | - | Source component instance GUID (for connect/disconnect operations) |
| FromParameter | TEXT | - | Source parameter name (for connect/disconnect operations) |
| ToInstanceGuid | TEXT | - | Target component instance GUID (for connect/disconnect operations) |
| ToParameter | TEXT | - | Target parameter name (for connect/disconnect operations) |
| OperationTime | DATETIME | DEFAULT CURRENT_TIMESTAMP | Operation time |
| Description | TEXT | - | Description |

**Notes**:
- Main database (ComponentsInfo.db) stores global component information, can be rebuilt at any time
- Document database is in the same directory as gh file, easy to share and version control
- Unsaved documents use temporary naming to avoid conflicts
- Read-only operations are recommended, manual write operations are not advised
- Can use SQL queries for component information and object information

## Parameter Types

### LjsonParam

Parameter type used to pass Ljson data between Grasshopper batteries.

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
4. Data will be received and converted to Ljson format output

### Component Query Example

1. Use `AllComponents` to get all component lists
2. Use `FindComponentsByName` to find specific components
3. Use `SearchComponentsByName` for fuzzy search

### Data Conversion Example

1. Create a `Json2Ljson` component
2. Input JSON string
3. Get the converted Ljson object

## Notes

1. Each port can only create one TCP receiver
2. Ljson's time tag is used for version control, only receives/sends updated data
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