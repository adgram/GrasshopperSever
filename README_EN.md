# GrasshopperSever

Rhino Grasshopper plugin that provides bidirectional communication with Grasshopper/Rhino via TCP protocol, supporting AI client remote control of component layouts, script execution, and data queries.

English | [中文](README.md)

## Project Structure

```
GrasshopperSever/
├── README_EN.md                      # This document
├── CLIENT_TUTORIAL.md             # AI Client Connection Tutorial
├── design.md                         # Component Development Technical Documentation
├── MainSectors.md                    # Main Features
└── Example/
    ├── tcp_test.md                   # TCP Communication Test Records
    ├── test_report.md                # System Test Report
    ├── CMD_COMPONENT/
    │   └── commands_COMPONENT.md     # Component Commands Details
    ├── CMD_DESIGN/
    │   └── design_test.md            # Design Commands Test Report
    ├── CMD_DOCUMENT/
    │   └── gh_file_test_report.md    # Document Commands Test Report
    ├── CMD_RHINO/
    │   └── commands_RHINO.md         # Rhino Commands Details
    └── SCRIPT&CMD_SCRIPT/
        ├── commands_SCRIPT.md        # Script Commands Details
        └── scripteditor_test.md      # ScriptEditor Test Documentation
```

## Feature Overview

| Feature | Description |
|---------|-------------|
| TCP Communication | GHReceiver/GHSender push mode, GHServer request-response mode |
| Component Information Query | Query and fuzzy search components by name/GUID/category |
| Design Layout Control | Add, remove, connect components, set parameter values |
| Rhino Script Execution | Execute Rhino commands remotely, get and select objects |
| GH Script Execution | Modify script components via ScriptEditor, or run C# scripts directly |
| Document Operations | Save/Load Grasshopper documents |
| Database | SQLite dual-layer architecture storing component info and operation history |

## Grasshopper Components

### Data Communication

| Component | Description |
|-----------|-------------|
| **GHReceiver** | Creates TCP connection by port and receives data, background thread reception, refreshes via `InvokeOnUiThread` |
| **GHSender** | Sends data using TCP connection, triggers sending when Ljson.time updates |
| **GHServer** | Creates TCP server by port, receives data, executes internally and responds, request-response mode |

### Data Conversion

| Component | Description |
|-----------|-------------|
| **Json2Ljson** | JSON string → Ljson object |
| **Ljson2Json** | Ljson object → JSON string |
| **DataTreeLjson** | Name + Info + Data Tree → Ljson |
| **FindJdata** | Find values in Ljson by name |

### Information Query

| Component | Description |
|-----------|-------------|
| **AllComponents** | Output all registered component info (requires Refresh=True) |
| **FindComponentsByGuid** | Query components by GUID |
| **FindComponentsByName** | Query components by name |
| **FindComponentsByCategory** | Query components by category |
| **SearchComponentsByName** | Fuzzy search components |
| **ComponentConnector** | Get component info through input connection |
| **SearchDataBase** | Execute SQL queries on database |

### Execution Components

| Component | Description |
|-----------|-------------|
| **GHActuator** | Execute input Ljson data |
| **ScriptEditor** | Modify script component code, supports C# and Python |
| **RunScript** | Internally embedded Rhino8 C# component, executes C# scripts directly |
| **RunScript2** | Internally embedded Rhino7 C# component, right-click to open code editor |
| **CommandRhino** | Execute Rhino script commands |

> For detailed input/output parameters of each component, see [design.md](design.md).

## TCP Commands

All commands use Ljson format and are sent via TCP:

```json
{
  "Name": "Command Type",
  "Info": "Command Description",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "Specific Command Name",
    "parameter_name": "parameter_value"
  }
}
```

**Name Field**: `COMPONENT` | `DOCUMENT` | `RHINO` | `SCRIPT` | `DESIGN`

### Command Quick Reference

| Type | Command | Description |
|------|---------|-------------|
| COMPONENT | `GETALLCOMPONENTS` | Get all components |
| COMPONENT | `FINDCOMPONENTBYGUID` | Find component by GUID |
| COMPONENT | `FINDCOMPONENTBYNAME` | Find component by name |
| COMPONENT | `FINDCOMPONENTBYCATEGORY` | Find component by category |
| COMPONENT | `SEARCHCOMPONENTSBYNAME` | Fuzzy search components |
| DOCUMENT | `SAVEDOCUMENT` | Save current document |
| DOCUMENT | `LOADDOCUMENT` | Load document |
| DOCUMENT | `DATABASEPATH` | Get database path |
| DOCUMENT | `GETALLOBJECTS` | get all objects |
| DOCUMENT | `GETOBJECT` | get component info by GUID |
| RHINO | `RHINOSCRIPT` | Execute Rhino script commands |
| RHINO | `GETLASTCREATEDOBJECTS` | Get last created Rhino objects |
| RHINO | `SELECTOBJECTS` | Select Rhino objects |
| RHINO | `GETANDSELECTLASTOBJECTS` | Get and select last created objects |
| DESIGN | `ADDCOMPONENTBYGUID` | Add component by GUID |
| DESIGN | `ADDCOMPONENTBYNAME` | Add component by name |
| DESIGN | `ADDPARAMWITHVALUE` | Add parameter component and set value |
| DESIGN | `REMOVECOMPONENT` | Remove component |
| DESIGN | `SETPARAMVALUE` | Set parameter value |
| DESIGN | `CONNECTCOMPONENTS` | Connect components |
| DESIGN | `DISCONNECTCOMPONENTS` | Disconnect components |
| SCRIPT |  | Unrealized methods |

> For detailed parameters, examples and response formats of each command, see the respective documentation: [Component Commands](Example/CMD_COMPONENT/commands_COMPONENT.md), [Design Commands](Example/CMD_DESIGN/design_test.md), [Document Commands](Example/CMD_DOCUMENT/gh_file_test_report.md), [Rhino Commands](Example/CMD_RHINO/commands_RHINO.md), [Script Commands](Example/SCRIPT&CMD_SCRIPT/commands_SCRIPT.md).
>
> Warning: If you are an AI, please avoid getting all component information (`GETALLCOMPONENTS`) easily. Prioritize using category or name queries, searches, or database calls.

## Communication Modes

### Push Mode (GHReceiver + GHSender)

```
AI Client ──TCP──> GHReceiver(Receive) ──> GH Processing ──> GHSender(Response) ──> AI Client
```

### Request-Response Mode (GHServer)

```
AI Client ──TCP──> GHServer(Receive+Execute+Response) ──> AI Client
```

> For detailed communication protocol and Python client code, see [CLIENT_TUTORIAL.md](CLIENT_TUTORIAL.md).

## Database

Uses SQLite dual-layer architecture:

| Database | Location | Description |
|----------|----------|-------------|
| **Main Database** ComponentsInfo.db | `AppData\Roaming\Grasshopper\Libraries\GHserver\` | Global component info, shared across all documents |
| **Document Database** `{name}_ghdata.db` | Same directory as gh file | Document-specific data (Rhino objects, script modification history, component operation history) |

The main database contains ALLCOMPS (component info) and MetaInfo tables; the document database contains RhinoObjects, GHScriptModifyHistory, and ComponentExchangeHistory tables. Recommended for read-only access.

> For complete table structure and query examples, see [Component Commands Documentation](Example/CMD_COMPONENT/commands_COMPONENT.md) and [Rhino Commands Documentation](Example/CMD_RHINO/commands_RHINO.md).

## Quick Start

1. Install the `.gha` plugin to the Grasshopper components directory
2. Add `GHServer` component in Grasshopper, set `Enabled = true`, port default `6879`
3. Use Python client to connect:

```python
from ghclient import GHClient

with GHClient(port = 6879) as client:
    responses = client.send_command(
        name="DOCUMENT",
        info="get database path",
        value={"Command": "DATABASEPATH"}
    )
    print(responses)
```

> For complete client class and advanced usage, see [AI Client Tutorial](CLIENT_TUTORIAL.md) and [Main Features](MainSectors.md).

## Related Documentation

- [Main Features](MainSectors.md) - Main Features
- [AI Client Tutorial](CLIENT_TUTORIAL.md) - Communication Protocol, Client Code and Troubleshooting
- [Component Development Documentation](design.md) - Input/Output Parameters and Technical Details for Each Component
- [TCP Communication Test](Example/tcp_test.md) - Communication Protocol Test Records
- [System Test Report](Example/test_report.md) - Complete Functionality Test Report
- [Component Commands](Example/CMD_COMPONENT/commands_COMPONENT.md) - Component Query Commands Details
- [Design Commands](Example/CMD_DESIGN/design_test.md) - Design Layout Commands Details
- [Document Commands](Example/CMD_DOCUMENT/gh_file_test_report.md) - Document Operation Commands Details
- [Rhino Commands](Example/CMD_RHINO/commands_RHINO.md) - Rhino Script Commands Details
- [Script Commands](Example/SCRIPT&CMD_SCRIPT/commands_SCRIPT.md) - Script Editor Commands Details
- [ScriptEditor Test](Example/SCRIPT&CMD_SCRIPT/scripteditor_test.md) - ScriptEditor Functionality Test

## Project Information

- **Version**: 1.0
- **Framework**: .NET 7.0 / .NET 7.0-windows / .NET 8.0 / .NET 8.0-windows
- **Plugin GUID**: `0171a275-7e22-4b2a-9f82-b80f07a08b08`

## Dependencies

- Rhino 8.29.26063.11001
- System.Data.SQLite 1.0.119