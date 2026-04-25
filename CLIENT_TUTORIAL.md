# 客户端教程 - 连接 GrasshopperSever

本教程指导 客户端如何通过 TCP 协议连接到 GrasshopperSever，实现与 Grasshopper 的双向通信。

## 目录

- [通信协议](#通信协议)
- [命令速览](#命令速览)
- [快速开始](#快速开始)
- [GHClient 类](#grasshopperclient-类)
- [数据库访问](#数据库访问)
- [故障排除](#故障排除)

## 通信协议

### Ljson 数据结构

所有通信使用 Ljson 格式（单个 JSON 对象）：

```json
{
  "Name": "数据名称或命令类型",
  "Info": "数据说明",
  "Time": "2026-03-22T10:30:00",
  "Value": "数据值"
}
```

- `Name` 字段对应命令类型（`COMPONENT` / `DOCUMENT` / `RHINO` / `SCRIPT` / `DESIGNLIST`）
- `Value` 中通过 `Command` 字段指定具体命令
- `Value` 支持所有 JSON 数据类型（数字、字符串、布尔、数组、对象、嵌套）

### 通信模式

**推送模式（GHReceiver + GHSender）**：

```
AI ──TCP──> GHReceiver(接收) ──> GH处理 ──> GHSender(响应) ──> AI
```

**请求-响应模式（GHServer）**：

```
AI ──TCP──> GHServer(接收+执行+响应) ──> AI
```

### 注意事项

- 响应包含 UTF-8 BOM，解码时使用 `utf-8-sig`

- 服务器会回送接收到的数据

- `Value` 中包含 `OUTPUT` 键时，其值在 GHServer 的 Output 端口输出

- 使用单个 Ljson 对象，不要使用批量格式（Items 数组）

- 接收和发送消息，都是采用StreamReader.ReadLineAsync(stream, Encoding.UTF8)和StreamWriter.WriteLineAsync(stream, Encoding.UTF8)。

    1、连接成功，服务器会自动发送已连接的响应。

    2、服务器收到消息，会自动发送数据接收成功的响应。

    3、Command相关操作，会自动回复一条消息。

    客户端一次沟通，根据情况最多只需要按照顺序接收三条消息。

- 一般情况发送和接收消息，请使用标准模板[ghclient](Example/ghclient.py)类。

## 命令速览

| 类型 | 命令 | 说明 | 详细文档 |
|------|------|------|----------|
| COMPONENT | `GETALLCOMPONENTS` | 获取所有组件 | [链接](Example/CMD_COMPONENT/commands_COMPONENT.md) |
| COMPONENT | `FINDCOMPONENTBYGUID` | 按 GUID 查找组件 | 同上 |
| COMPONENT | `FINDCOMPONENTBYNAME` | 按名称查找组件 | 同上 |
| COMPONENT | `FINDCOMPONENTBYCATEGORY` | 按分类查找组件 | 同上 |
| COMPONENT | `SEARCHCOMPONENTSBYNAME` | 模糊搜索组件 | 同上 |
| DOCUMENT | `SAVEDOCUMENT` | 保存文档 | [链接](Example/CMD_DOCUMENT/gh_file_test_report.md) |
| DOCUMENT | `LOADDOCUMENT` | 加载文档 | 同上 |
| DOCUMENT | `DATABASEPATH` | 获取数据库路径 | 同上 |
| DOCUMENT | `GETALLOBJECTS` | 通过guid查找画布上组件实例  | 同上 |
| DOCUMENT | `GETOBJECT` | 获取画布上组件实例 | 同上 |
| RHINO | `RHINOSCRIPT` | 执行 Rhino 命令 | [链接](Example/CMD_RHINO/commands_RHINO.md) |
| RHINO | `GETLASTCREATEDOBJECTS` | 获取最后创建的对象 | 同上 |
| RHINO | `SELECTOBJECTS` | 选择对象 | 同上 |
| RHINO | `GETANDSELECTLASTOBJECTS` | 获取并选择对象 | 同上 |
| DESIGN | `ADDCOMPONENTBYGUID` | 通过 GUID 添加组件 | [链接](Example/CMD_DESIGN/design_test.md) |
| DESIGN | `ADDCOMPONENTBYNAME` | 通过名称添加组件 | 同上 |
| DESIGN | `ADDPARAMWITHVALUE` | 添加参数组件并设置值 | 同上 |
| DESIGN | `REMOVECOMPONENT` | 移除组件 | 同上 |
| DESIGN | `SETPARAMVALUE` | 设置参数值 | 同上 |
| DESIGN | `CONNECTCOMPONENTS` | 连接组件 | 同上 |
| DESIGN | `DISCONNECTCOMPONENTS` | 断开组件连接 | 同上 |
| DESIGNLIST |  | 批量序列化命令 | [链接](../CMD_DESIGN/design_test.md) |

警告：如果你是ai，请不要轻易获取所有组件信息(`GETALLCOMPONENTS`)，优先使用分组或名称查询、检索，或者调用数据库。

## 快速开始

### 1. Grasshopper 端设置

添加 `GHServer` 组件，设置 `Enabled = true`，端口默认 `6879`。

### 2. 最简连接

## GHClient 类

支持长连接、持续接收和自动重连的完整客户端：

见 [GHClient 类](Example/ghclient.py)

### 使用示例

见 [tcp测试](Example/tcp_test.py)

```python
# 基本用法
def send_command(port):
    responses = []
    with GHClient(port = port) as client:
        responses = client.send_command(
            name="DOCUMENT",
            info="获取数据库路径",
            value={"Command": "DATABASEPATH"}
        )
        print(responses)
    return len(responses) == 2
```

### Design 命令典型流程

```python
from ghclient import GHClient

target_guid = None
source_guid = None

# 1. 添加组件
with GHClient(port=6879) as gh:
    value = {"Command": "AddComponentByName", "ComponentName": "Addition", "X": 200, "Y": 100}
    p = gh.send_command("DESIGN", "", value)
    target_guid = gh.extract_value(p, "InstanceGuid")
    print(p)

# 2. 设置参数值
with GHClient(port=6879) as gh:
    value = {"Command": "ADDPARAMWITHVALUE", 'ParamName': "int", "Value": 42, "X": 50, "Y": 100}
    p = gh.send_command("DESIGN", "", value)
    source_guid = gh.extract_value(p, "InstanceGuid")
    print(p)

# 3. 连接组件
with GHClient(port=6879) as gh:
    value={
        "Command": "CONNECTCOMPONENTS",
        "FromGuid": source_guid,
        "FromParameter": "",
        "ToGuid": target_guid,
        "ToParameter": "A"
    }
    p = gh.send_command("DESIGN", "", value)
    print(p)
```

> Design 命令的完整参数和示例见 [design_test.md](Example/CMD_DESIGN/design_test.md)。

## 数据库访问

通过 `DATABASEPATH` 命令获取主数据库路径后可直接查询：

```python
import sqlite3
from ghclient import GHClient

# 获取路径
with GHClient(port=6879) as gh:
    responses = gh.send_command(
        name="DOCUMENT",
        info="获取数据库路径",
        value={"Command": "DATABASEPATH"}
    )
    for res in responses:
        if res.get('Name') == 'DatabasePath':
            db_path = res['Value']['DatabasePath']

# 查询组件
conn = sqlite3.connect(db_path)
cursor = conn.cursor()
cursor.execute("SELECT ComponentGuid, ComponentName, Category FROM ALLCOMPS WHERE ComponentName LIKE '%Circle%'")
for row in cursor.fetchall():
    print(row)
conn.close()
```

### 双层架构

| 数据库 | 位置 | 表 |
|--------|------|----|
| 主数据库 ComponentsInfo.db | 插件目录 | ALLCOMPS, MetaInfo |
| 文档数据库 `{名}_ghdata.db` | gh 文件同目录 | RhinoObjects, GHScriptModifyHistory, ComponentExchangeHistory |

- 主数据库存储全局组件信息，可随时重建
- 文档数据库存储文档特定数据，建议只读访问
- 未保存文档使用临时命名 `TempDocument_{GUID}.db`

> 完整表结构和 SQL 示例见 [Component 命令文档](Example/CMD_COMPONENT/commands_COMPONENT.md) 和 [Rhino 命令文档](Example/CMD_RHINO/commands_RHINO.md)。

## 故障排除

### 连接失败

- 确认 Grasshopper 正在运行
- 确认 GHReceiver/GHServer 的 `Enabled` 为 `true`
- 确认端口号正确（默认 6879）
- 检查防火墙是否阻止端口

### 数据格式错误

- 使用单个 Ljson 对象，不要使用 Items 数组
- 确保包含 `Name`, `Info`, `Time`, `Value` 四个字段
- 命令类型必须是 `COMPONENT` / `DOCUMENT` / `RHINO` / `SCRIPT` / `DESIGNLIST` 之一

### 响应解析

- 使用 `utf-8-sig` 解码响应（处理 BOM）
- 用 `\ufeff` 分割多条消息
- 设置合理的超时时间（建议 10-30 秒）

### Design 命令注意

- `SETPARAMVALUE`、`REMOVECOMPONENT`、`CONNECTCOMPONENTS` 等需要 `InstanceGuid`（组件实例 GUID），不是 `ComponentGuid`（组件类型 GUID）
- 建议每次命令都重新连接，避免缓冲区问题
- `ADDPARAMWITHVALUE` 中列表值使用字符串数组格式，如 `"[\"1.0\", \"2.0\"]"`

## 相关文档

- [主文档](README.md) - 项目概述、组件介绍、命令速览
- [主要功能](MainSectors.md) - 主要功能
- [组件开发文档](design.md) - 各组件的输入输出参数
- [TCP 通信测试](Example/tcp_test.md) - 通信协议测试记录
- [系统测试报告](Example/test_report.md) - 完整功能测试报告
- [Component 命令](Example/CMD_COMPONENT/commands_COMPONENT.md) - 组件查询命令详解
- [Design 命令](Example/CMD_DESIGN/design_test.md) - 设计布局命令详解
- [Document 命令](Example/CMD_DOCUMENT/gh_file_test_report.md) - 文档操作命令详解
- [Rhino 命令](Example/CMD_RHINO/commands_RHINO.md) - Rhino 脚本命令详解
- [Script 命令](Example/SCRIPT&CMD_SCRIPT/commands_SCRIPT.md) - 脚本编辑命令详解
- [ScriptEditor 测试](Example/SCRIPT&CMD_SCRIPT/scripteditor_test.md) - ScriptEditor 功能测试

