﻿﻿﻿# GrasshopperSever

Rhino Grasshopper 插件，通过 TCP 协议提供与 Grasshopper/Rhino 的双向通信，支持 AI 客户端远程控制组件布局、执行脚本和查询数据。
中文 | [English](README_EN.md)

## 项目结构

```
GrasshopperSever/
├── README.md                         # 本文档
├── CLIENT_TUTORIAL.md                # 客户端连接教程
├── design.md                         # 组件开发技术文档
├── MainSectors.md                    # 主要功能
└── Example/
    ├── tcp_test.md                   # TCP 通信测试记录
    ├── test_report.md                # 系统测试报告
    ├── CMD_COMPONENT/
    │   └── commands_COMPONENT.md     # Component 命令详解
    ├── CMD_DESIGN/
    │   └── design_test.md            # Design 命令测试报告
    ├── CMD_DOCUMENT/
    │   └── gh_file_test_report.md    # Document 命令测试报告
    ├── CMD_RHINO/
    │   └── commands_RHINO.md         # Rhino 命令详解
    └── SCRIPT&CMD_SCRIPT/
        ├── commands_SCRIPT.md        # Script 命令详解
        └── scripteditor_test.md      # ScriptEditor 测试文档
```

## 功能概述

| 功能 | 说明 |
|------|------|
| TCP 通信 | GHReceiver/GHSender 推送模式，GHServer 请求 - 响应模式 |
| 组件信息查询 | 按名称/GUID/分类查询和模糊搜索组件 |
| 设计布局控制 | 添加、移除、连接组件，设置参数值 |
| Rhino 脚本执行 | 远程执行 Rhino 命令，获取和选择对象 |
| GH 脚本执行 | 通过 ScriptEditor 修改脚本组件，或直接运行 C# 脚本 |
| 文档操作 | 保存/加载 Grasshopper 文档 |
| 数据库 | SQLite 双层架构存储组件信息和操作历史 |

## Grasshopper 组件

### 数据通信

| 组件 | 说明 |
|------|------|
| **GHReceiver** | 按端口创建 TCP 连接并接收数据，后台线程接收，通过 `InvokeOnUiThread` 刷新 |
| **GHSender** | 使用 TCP 连接发送数据，Ljson.time 更新时触发发送 |
| **GHServer** | 按端口创建 TCP 服务端，接收数据后内部执行并响应，请求 - 响应模式 |

### 数据转换

| 组件 | 说明 |
|------|------|
| **Json2Ljson** | JSON 字符串→Ljson 对象 |
| **Ljson2Json** | Ljson 对象→JSON 字符串 |
| **DataTreeLjson** | Name + Info + Data Tree→Ljson |
| **FindJdata** | 按名称查找 Ljson 中的值 |

### 信息查询

| 组件 | 说明 |
|------|------|
| **AllComponents** | 输出所有注册组件信息（需 Refresh=True） |
| **FindComponentsByGuid** | 按 GUID 查询组件 |
| **FindComponentsByName** | 按名称查询组件 |
| **FindComponentsByCategory** | 按分类查询组件 |
| **SearchComponentsByName** | 模糊搜索组件 |
| **ComponentConnector** | 通过连接输入端获取组件信息 |
| **SearchDataBase** | 执行 SQL 查询数据库 |

### 执行组件

| 组件 | 说明 |
|------|------|
| **GHActuator** | 执行输入的 Ljson 数据 |
| **ScriptEditor** | 修改脚本组件代码，支持 C# 和 Python |
| **RunScript** | 内部嵌入 Rhino8 C# 组件，直接执行 C# 脚本 |
| **RunScript2** | 内部嵌入 Rhino7 C# 组件，右键可打开代码编辑器 |
| **CommandRhino** | 执行 Rhino 脚本命令 |

> 组件的详细输入输出参数见 [design.md](design.md)。

## TCP 命令

所有命令使用 Ljson 格式，通过 TCP 发送：

```json
{
  "Name": "命令类型",
  "Info": "命令描述",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "具体命令名称",
    "参数名": "参数值"
  }
}
```

**命令Name 字段**：`COMPONENT` | `DOCUMENT` | `RHINO` | `SCRIPT` | `DESIGN`

### 命令速览

| 类型 | 命令 | 说明 |
|------|------|------|
| COMPONENT | `GETALLCOMPONENTS` | 获取所有组件 |
| COMPONENT | `FINDCOMPONENTBYGUID` | 按 GUID 查找组件 |
| COMPONENT | `FINDCOMPONENTBYNAME` | 按名称查找组件 |
| COMPONENT | `FINDCOMPONENTBYCATEGORY` | 按分类查找组件 |
| COMPONENT | `SEARCHCOMPONENTSBYNAME` | 模糊搜索组件 |
| DOCUMENT | `SAVEDOCUMENT` | 保存当前文档 |
| DOCUMENT | `LOADDOCUMENT` | 加载文档 |
| DOCUMENT | `DATABASEPATH` | 获取数据库路径 |
| DOCUMENT | `GETALLOBJECTS` | 获取画布上所有组件实例 |
| DOCUMENT | `GETOBJECT` | 通过guid查找画布上组件实例 |
| RHINO | `RHINOSCRIPT` | 执行 Rhino 命令命令 |
| RHINO | `GETLASTCREATEDOBJECTS` | 获取最后创建的 Rhino 对象 |
| RHINO | `SELECTOBJECTS` | 选择 Rhino 对象 |
| RHINO | `GETANDSELECTLASTOBJECTS` | 获取并选择最后创建的对象 |
| DESIGN | `ADDCOMPONENTBYGUID` | 通过 GUID 添加组件 |
| DESIGN | `ADDCOMPONENTBYNAME` | 通过名称添加组件 |
| DESIGN | `ADDPARAMWITHVALUE` | 添加参数组件并设置值 |
| DESIGN | `REMOVECOMPONENT` | 移除组件 |
| DESIGN | `SETPARAMVALUE` | 设置参数值 |
| DESIGN | `CONNECTCOMPONENTS` | 连接组件 |
| DESIGN | `DISCONNECTCOMPONENTS` | 断开组件连接 |
| SCRIPT |  | 未实现的命令，改为 RunScript 组件 |

> 各命令的详细参数、示例和响应格式见对应文档：[Component 命令](Example/CMD_COMPONENT/commands_COMPONENT.md)、[Design 命令](Example/CMD_DESIGN/design_test.md)、[Document 命令](Example/CMD_DOCUMENT/gh_file_test_report.md)、[Rhino 命令](Example/CMD_RHINO/commands_RHINO.md)、[Script 命令](Example/SCRIPT&CMD_SCRIPT/commands_SCRIPT.md)。

> 警告：如果你是 ai，请不要轻易获取所有组件信息 (`GETALLCOMPONENTS`)，优先使用分组或名称查询、检索，或者调用数据库。

## 通信模式

### 推送模式（GHReceiver + GHSender）
```
客户端──TCP──> GHReceiver(接收)──> GH 处理 ──> GHSender(响应) ──> 客户端
```

### 请求 - 响应模式（GHServer）
```
客户端──TCP──> GHServer(接收 + 执行 + 响应) ──> 客户端
```

> 详细通信协议和 Python 客户端代码见 [CLIENT_TUTORIAL.md](CLIENT_TUTORIAL.md)。

## 数据库

采用 SQLite 双层架构：

| 数据库 | 位置 | 说明 |
|--------|------|------|
| **主数据库** ComponentsInfo.db | `AppData\Roaming\Grasshopper\Libraries\GHserver\` | 全局组件信息，所有文档共享 |
| **文档数据库** `{名字}_ghdata.db` | 与 gh 文件同目录 | 文档特定数据（Rhino 对象、脚本修改历史、组件操作历史） |

主数据库包含 ALLCOMPS（组件信息）和 MetaInfo（元信息）表；文档数据库包含 RhinoObjects、GHScriptModifyHistory、ComponentExchangeHistory 表。建议只读访问。

> 完整表结构和查询示例见 [Component 命令文档](Example/CMD_COMPONENT/commands_COMPONENT.md) 和 [Rhino 命令文档](Example/CMD_RHINO/commands_RHINO.md)。

## 快速开始

1. 安装 `.gha` 插件到 Grasshopper 组件目录
2. 在 Grasshopper 中添加 `GHServer` 组件，设置 `Enabled = true`，端口默认 `6879`
3. 使用 Python 客户端连接：

```python
from ghclient import GHClient

with GHClient(port = 6879) as client:
    responses = client.send_command(
        name="DOCUMENT",
        info="获取数据库路径",
        value={"Command": "DATABASEPATH"}
    )
    print(responses)
```

> 更完整的客户端类和高级用法见 [客户端教程](CLIENT_TUTORIAL.md) 和 [主要功能](MainSectors.md)。

## 相关文档

- [主要功能](MainSectors.md) - 主要功能
- [客户端教程](CLIENT_TUTORIAL.md) - 通信协议、客户端代码和故障排除
- [组件开发文档](design.md) - 各组件的输入输出参数和技术细节
- [TCP 通信测试](Example/tcp_test.md) - 通信协议测试记录
- [系统测试报告](Example/test_report.md) - 完整功能测试报告
- [Component 命令](Example/CMD_COMPONENT/commands_COMPONENT.md) - 组件查询命令详解
- [Design 命令](Example/CMD_DESIGN/design_test.md) - 设计布局命令详解
- [Document 命令](Example/CMD_DOCUMENT/gh_file_test_report.md) - 文档操作命令详解
- [Rhino 命令](Example/CMD_RHINO/commands_RHINO.md) - Rhino 脚本命令详解
- [Script 命令](Example/SCRIPT&CMD_SCRIPT/commands_SCRIPT.md) - 脚本编辑命令详解
- [ScriptEditor 测试](Example/SCRIPT&CMD_SCRIPT/scripteditor_test.md) - ScriptEditor 功能测试

## 项目信息

- **版本**: 1.0
- **框架**: .NET 7.0 / .NET 7.0-windows/ .NET 8.0 / .NET 8.0-windows
- **插件 GUID**: `0171a275-7e22-4b2a-9f82-b80f07a08b08`

## 依赖项
- Rhino 8.29.26063.11001
- System.Data.SQLite 1.0.119
