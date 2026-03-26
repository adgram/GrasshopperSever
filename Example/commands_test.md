# GrasshopperSever 命令列表

本文档列出了GrasshopperSever插件支持的所有可用命令。

## 命令格式

所有命令通过LJSON格式发送，必须包含以下结构：

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

**Name字段（命令类型）**：
- `COMPONENT` - 组件相关命令
- `DOCUMENT` - 文档相关命令
- `RHINO` - Rhino相关命令
- `SCRIPT` - 脚本相关命令
- `DESIGN` - 设计相关命令

---

## 数据库表结构

GrasshopperSever 使用 SQLite 数据库存储组件信息和对象信息。数据库文件位于：
- **路径**：`C:\Users\[用户名]\AppData\Roaming\Grasshopper\Libraries\GHserver\GrasshopperSever.db`
- **查询命令**：使用 DATABASEPATH 命令获取具体路径

### 1. MetaInfo 表（元信息表）

用于跟踪数据库表的更新时间和描述信息。

| 字段名 | 数据类型 | 约束 | 说明 |
|--------|----------|------|------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | 主键，自增 |
| TableName | TEXT | NOT NULL UNIQUE | 表名 |
| LastUpdateTime | DATETIME | DEFAULT CURRENT_TIMESTAMP | 最后更新时间 |
| Description | TEXT | - | 表描述 |

**SQL 创建语句**：
```sql
CREATE TABLE IF NOT EXISTS MetaInfo (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TableName TEXT NOT NULL UNIQUE,
    LastUpdateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
    Description TEXT
)
```

**示例查询**：
```sql
-- 查看所有表及其最后更新时间
SELECT TableName, LastUpdateTime, Description FROM MetaInfo;

-- 查看某个表的最后更新时间
SELECT LastUpdateTime FROM MetaInfo WHERE TableName = 'AllComponents';
```

---

### 2. AllComponents 表（组件信息表）

存储所有 Grasshopper 组件的详细信息。

| 字段名 | 数据类型 | 约束 | 说明 |
|--------|----------|------|------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | 主键，自增 |
| ComponentGuid | TEXT | NOT NULL UNIQUE | 组件的 GUID（唯一标识） |
| ComponentName | TEXT | NOT NULL | 组件名称 |
| NickName | TEXT | - | 组件昵称 |
| Description | TEXT | - | 组件描述 |
| Category | TEXT | NOT NULL | 主分类 |
| SubCategory | TEXT | NOT NULL | 子分类 |
| Inputs | TEXT | DEFAULT '' | 输入参数定义（JSON格式） |
| Outputs | TEXT | DEFAULT '' | 输出参数定义（JSON格式） |

**SQL 创建语句**：
```sql
CREATE TABLE IF NOT EXISTS AllComponents (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ComponentGuid TEXT NOT NULL UNIQUE,
    ComponentName TEXT NOT NULL,
    NickName TEXT,
    Description TEXT,
    Category TEXT NOT NULL,
    SubCategory TEXT NOT NULL,
    Inputs TEXT DEFAULT '',
    Outputs TEXT DEFAULT ''
)
```

**示例查询**：
```sql
-- 查询所有组件
SELECT ComponentGuid, ComponentName, NickName, Category, SubCategory FROM AllComponents;

-- 按分类查询组件
SELECT ComponentName, NickName, Description FROM AllComponents WHERE Category = 'Curve';

-- 模糊搜索组件
SELECT ComponentName, NickName, Description FROM AllComponents WHERE ComponentName LIKE '%Circle%';

-- 统计组件数量
SELECT Category, COUNT(*) as Count FROM AllComponents GROUP BY Category;
```

**注意事项**：
- `Inputs` 和 `Outputs` 字段存储的是参数定义的 JSON 字符串
- 使用 `INSERT OR REPLACE` 语句进行插入或更新
- 该表在插件初始化时自动填充，并在每次启动时更新

---

### 3. RhinoObjects 表（Rhino对象信息表）

存储 Rhino 中创建的对象信息。

| 字段名 | 数据类型 | 约束 | 说明 |
|--------|----------|------|------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | 主键，自增 |
| ObjectId | TEXT | NOT NULL | 对象 ID（GUID 字符串） |
| ObjectType | TEXT | - | 对象类型（如：Curve, Surface, Mesh 等） |
| LayerName | TEXT | - | 图层名称 |
| ObjectName | TEXT | - | 对象名称 |
| CreateTime | DATETIME | DEFAULT CURRENT_TIMESTAMP | 创建时间 |
| DocumentSerialNumber | TEXT | - | 文档序列号 |
| Description | TEXT | - | 描述信息 |

**SQL 创建语句**：
```sql
CREATE TABLE IF NOT EXISTS RhinoObjects (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ObjectId TEXT NOT NULL,
    ObjectType TEXT,
    LayerName TEXT,
    ObjectName TEXT,
    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
    DocumentSerialNumber TEXT,
    Description TEXT
)
```

**示例查询**：
```sql
-- 查询所有对象
SELECT ObjectId, ObjectType, LayerName, ObjectName, CreateTime FROM RhinoObjects;

-- 按类型查询对象
SELECT ObjectId, LayerName FROM RhinoObjects WHERE ObjectType = 'Curve';

-- 查询最近创建的对象
SELECT * FROM RhinoObjects ORDER BY CreateTime DESC LIMIT 10;

-- 按图层统计对象数量
SELECT LayerName, COUNT(*) as Count FROM RhinoObjects GROUP BY LayerName;
```

**注意事项**：
- 该表在第一次调用 `GETLASTCREATEDOBJECTS` 命令时自动创建
- 每次调用 `GETLASTCREATEDOBJECTS` 命令时，新获取的对象会自动插入到表中
- 该表是暂存文件，不会和 Grasshopper 文件同步

---

### 4. GHScriptModifyHistory 表（GHScript组件修改历史表）

存储 GHScript 组件（C# Script、Python Script 等）的修改历史记录。

| 字段名 | 数据类型 | 约束 | 说明 |
|--------|----------|------|------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | 主键，自增 |
| InstanceGuid | TEXT | NOT NULL | 组件实例 GUID（用于标识具体的组件实例） |
| ComponentGuid | TEXT | NOT NULL | 组件类型 GUID（用于标识组件类型） |
| ComponentName | TEXT | - | 组件名称 |
| ModifyType | TEXT | NOT NULL | 修改类型（CODE_CHANGE：代码修改，PARAM_CHANGE：参数修改） |
| ModifyContent | TEXT | - | 修改内容（JSON 格式） |
| Description | TEXT | - | 描述信息 |
| ModifyTime | DATETIME | DEFAULT CURRENT_TIMESTAMP | 修改时间 |

**SQL 创建语句**：
```sql
CREATE TABLE IF NOT EXISTS GHScriptModifyHistory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    InstanceGuid TEXT NOT NULL,
    ComponentGuid TEXT NOT NULL,
    ComponentName TEXT,
    ModifyType TEXT NOT NULL,
    ModifyContent TEXT,
    Description TEXT,
    ModifyTime DATETIME DEFAULT CURRENT_TIMESTAMP
)
```

**示例查询**：
```sql
-- 查询所有修改历史
SELECT * FROM GHScriptModifyHistory ORDER BY ModifyTime DESC;

-- 查询特定实例的修改历史
SELECT * FROM GHScriptModifyHistory WHERE InstanceGuid = '{instance_guid}' ORDER BY ModifyTime DESC;

-- 查询代码修改历史
SELECT * FROM GHScriptModifyHistory WHERE ModifyType = 'CODE_CHANGE' ORDER BY ModifyTime DESC;

-- 查询参数修改历史
SELECT * FROM GHScriptModifyHistory WHERE ModifyType = 'PARAM_CHANGE' ORDER BY ModifyTime DESC;

-- 按组件类型统计修改次数
SELECT ComponentName, COUNT(*) as ModifyCount FROM GHScriptModifyHistory GROUP BY ComponentName;

-- 查询最近的修改
SELECT ComponentName, ModifyType, Description, ModifyTime FROM GHScriptModifyHistory ORDER BY ModifyTime DESC LIMIT 20;
```

**ModifyContent 字段说明**：

代码修改（CODE_CHANGE）：
```json
{
  "OldCodeLength": 1234,
  "NewCodeLength": 1500,
  "CodeChanged": true,
  "ComponentType": "C# Script"
}
```

参数修改（PARAM_CHANGE）：
```json
{
  "OldInputParams": "[...]",
  "OldOutputParams": "[...]",
  "NewInputParams": "[...]",
  "NewOutputParams": "[...]",
  "InputParamCount": 3,
  "OutputParamCount": 2,
  "ComponentType": "Python 3 Script"
}
```

**注意事项**：
- 该表在第一次修改 GHScript 组件时自动创建
- 每次修改代码或参数时，会自动记录修改历史
- 该表是暂存文件，不会和 Grasshopper 文件同步
- InstanceGuid 用于区分不同的组件实例
- ComponentGuid 用于标识组件类型（如：C# Script、Python 3 Script 等）

---

### 数据库使用建议

**只读操作**：
- ✅ 可以安全地读取数据库中的数据
- ✅ 可以使用 SQL 查询组件信息和对象信息
- ✅ 可以统计数据用于分析

**写操作**：
- ⚠️ 不建议手动写入数据
- ⚠️ 数据库会在插件运行时自动更新
- ⚠️ 手动修改可能影响插件功能

**性能优化**：
- 使用 WAL（Write-Ahead Logging）模式
- 使用连接池减少连接开销
- 批量操作使用事务提高性能

---

## Component 命令（组件相关）

### 1. GETALLCOMPONENTS
获取所有组件信息

**请求参数**：
```json
{
  "Name": "COMPONENT",
  "Info": "获取所有组件",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "GETALLCOMPONENTS"
  }
}
```

**响应**：返回所有组件的列表

---

### 2. FINDCOMPONENTBYGUID
通过GUID查找组件

**请求参数**：
```json
{
  "Name": "COMPONENT",
  "Info": "通过GUID查找组件",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "FINDCOMPONENTBYGUID",
    "Guid": "组件的GUID字符串"
  }
}
```

**响应**：返回匹配的组件信息

**错误**：如果未找到会返回错误信息

---

### 3. FINDCOMPONENTBYNAME
通过名称查找组件

**请求参数**：
```json
{
  "Name": "COMPONENT",
  "Info": "通过名称查找组件",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "FINDCOMPONENTBYNAME",
    "Name": "组件名称"
  }
}
```

**响应**：返回匹配的组件信息

**错误**：如果未找到会返回错误信息

---

### 4. FINDCOMPONENTBYCATEGORY
通过分类查找组件

**请求参数**：
```json
{
  "Name": "COMPONENT",
  "Info": "通过分类查找组件",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "FINDCOMPONENTBYCATEGORY",
    "Category": "主分类（可选）",
    "SubCategory": "子分类（可选）",
    "Name": "组件名称（可选）"
  }
}
```

**说明**：至少需要提供Category、SubCategory或Name中的一个参数

**响应**：返回符合条件的组件列表

**错误**：如果未找到会返回错误信息

---

### 5. SEARCHCOMPONENTSBYNAME
通过名称搜索组件（模糊搜索）

**请求参数**：
```json
{
  "Name": "COMPONENT",
  "Info": "搜索组件",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "SEARCHCOMPONENTSBYNAME",
    "Name": "搜索关键词"
  }
}
```

**响应**：
```json
{
  "Name": "SearchComponentsByName",
  "Info": "搜索组件",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Count": "匹配数量",
    "Components": [组件列表]
  }
}
```

**错误**：如果未找到会返回错误信息

---

## Document 命令（文档相关）

### 1. SAVEDOCUMENT
保存当前文档

**请求参数**：
```json
{
  "Name": "DOCUMENT",
  "Info": "保存文档",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "SAVEDOCUMENT",
    "FilePath": "文件路径（可选）"
  }
}
```

**响应**：保存操作的结果

**错误**：如果保存失败会返回错误信息

---

### 2. LOADDOCUMENT
加载文档

**请求参数**：
```json
{
  "Name": "DOCUMENT",
  "Info": "加载文档",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "LOADDOCUMENT",
    "FilePath": "文件路径（必需）"
  }
}
```

**错误**：如果未提供FilePath会返回错误信息

**响应**：加载操作的结果

---

### 3. DATABASEPATH
获取数据库路径

**请求参数**：
```json
{
  "Name": "DOCUMENT",
  "Info": "获取数据库路径",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "DATABASEPATH"
  }
}
```

**响应**：
```json
{
  "Name": "DatabasePath",
  "Info": "获取数据库路径",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "DatabasePath": "数据库的完整路径"
  }
}
```

**注意事项**：
- 可以直接读取数据库，但请勿写入（数据库只是暂存文件，不会和gh同步）
- 建议使用 SQLite 工具（如 DB Browser for SQLite）查看数据库内容
- 数据库会在插件运行时自动更新

**快速查询示例**：

```sql
-- 1. 查看所有表及其最后更新时间
SELECT TableName, LastUpdateTime, Description FROM MetaInfo;

-- 2. 查询所有组件（常用）
SELECT ComponentGuid, ComponentName, NickName, Category, SubCategory FROM AllComponents;

-- 3. 按分类查询组件
SELECT ComponentName, NickName, Description FROM AllComponents WHERE Category = 'Curve';

-- 4. 模糊搜索组件
SELECT ComponentName, NickName, Description FROM AllComponents WHERE ComponentName LIKE '%Circle%';

-- 5. 查询所有 Rhino 对象
SELECT ObjectId, ObjectType, LayerName, ObjectName, CreateTime FROM RhinoObjects;

-- 6. 按图层统计对象数量
SELECT LayerName, COUNT(*) as Count FROM RhinoObjects GROUP BY LayerName;

-- 7. 查询最近创建的对象
SELECT * FROM RhinoObjects ORDER BY CreateTime DESC LIMIT 10;

-- 8. 统计组件数量
SELECT Category, COUNT(*) as Count FROM AllComponents GROUP BY Category ORDER BY Count DESC;

-- 9. 查询所有 GHScript 修改历史
SELECT ComponentName, ModifyType, Description, ModifyTime FROM GHScriptModifyHistory ORDER BY ModifyTime DESC;

-- 10. 查询特定实例的修改历史（需要替换 {instance_guid}）
SELECT * FROM GHScriptModifyHistory WHERE InstanceGuid = '{instance_guid}' ORDER BY ModifyTime DESC;
```

---

## Rhino命令测试记录

### RUNSCRIPT - 运行Rhino命令

**端口**: 6655

**测试命令**: `_-Line 0,0,0 10,10,0` (创建直线)

**请求**：

```json
{
  "Name": "RHINO",
  "Info": "执行RUNSCRIPT命令",
  "Time": "2026-03-26T...",
  "Value": {
    "Command": "RUNSCRIPT",
    "Script": "_-Line 0,0,0 10,10,0"
  }
}
```

**测试结果**:

- ✓ 成功：命令执行成功，直线已创建
- ✓ 响应：返回3条消息（客户端已连接、数据接收成功、命令执行结果）
- ✓ 命令执行结果包含 `Result: True` 和执行的 `Script` 内容

**响应示例**：

```json
{
  "Name": "RunScript",
  "Info": "执行Rhino脚本成功",
  "Value": {
    "Result": "True",
    "Script": "_-Line 0,0,0 10,10,0"
  }
}
```

**说明**:

- RUNSCRIPT命令执行成功时，会返回执行结果，包含 Result 和 Script 字段
- 命令执行失败时，会返回错误信息
- 需要在Rhino中验证命令是否实际执行成功

**测试脚本**: `test_runscript_6655.py`

---

### GETLASTCREATEDOBJECTS - 获取最后创建的对象

**功能**：获取Rhino中最后创建的对象信息，并将对象信息存入数据库

**请求**：

```json
{
  "Name": "RHINO",
  "Info": "获取最后创建的对象",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "GETLASTCREATEDOBJECTS"
  }
}
```

**响应示例**（成功）：

```json
{
  "Name": "GetLastCreatedObjects",
  "Info": "获取最后创建的对象",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Object_0": {
      "Id": "00000000-0000-0000-0000-000000000000",
      "Guid": "00000000-0000-0000-0000-000000000000",
      "Type": "Curve",
      "Layer": "Default",
      "Name": "",
      "DatabaseRecordId": "1"
    },
    "Count": "1"
  }
}
```

**响应示例**（无对象）：

```json
{
  "Name": "GetLastCreatedObjects",
  "Info": "未找到对象",
  "Time": "2026-03-26T10:00:00",
  "Value": {}
}
```

**说明**：

- 自动初始化对象表（如果不存在）
- 使用 `_SelLast` 命令选择最后创建的对象
- 获取对象的详细信息：ID、类型、图层、名称
- 将对象信息存入数据库的 `RhinoObjects` 表
- 返回对象数量和每个对象的详细信息

**数据库表结构**：

```sql
CREATE TABLE RhinoObjects (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ObjectId TEXT NOT NULL,
    ObjectType TEXT,
    LayerName TEXT,
    ObjectName TEXT,
    CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
    DocumentSerialNumber TEXT,
    Description TEXT
)
```

---

### SELECTOBJECTS - 选择对象

**功能**：根据对象ID列表选择Rhino中的对象

**请求**：

```json
{
  "Name": "RHINO",
  "Info": "选择对象",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "SELECTOBJECTS",
    "Objects": "guid1,guid2,guid3"
  }
}
```

**参数说明**：
- `Objects`：对象ID列表，支持以下分隔符：逗号(,)、分号(;)、空格

**响应示例**（成功）：

```json
{
  "Name": "SelectObjects",
  "Info": "选择对象",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "TotalRequested": "3",
    "TotalSelected": "2",
    "InvalidIdCount": "0",
    "NotFoundCount": "1",
    "Message": "部分对象选择成功（成功: 2, 无效ID: 0, 未找到: 1）"
  }
}
```

**响应示例**（全部失败）：

```json
{
  "Name": "SelectObjects",
  "Info": "选择对象",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "TotalRequested": "2",
    "TotalSelected": "0",
    "InvalidIdCount": "2",
    "NotFoundCount": "0",
    "Message": "所有ID均无效或未找到对象（无效ID: 2, 未找到: 0）"
  }
}
```

**说明**：

- 支持批量选择多个对象
- 自动清除之前的选择
- 验证每个ID的格式和有效性
- 自动刷新视图以显示选择结果
- 返回详细的统计信息

**使用示例**：

```python
# 从 GETLASTCREATEDOBJECTS 的结果中提取 Guid
objects_result = send_command(6655, "RHINO", "GETLASTCREATEDOBJECTS", {})
guids = []
for key, value in objects_result[0]['Value'].items():
    if key.startswith('Object_'):
        guids.append(value['Guid'])

# 选择这些对象
select_result = send_command(6655, "RHINO", "SELECTOBJECTS", {
    "Objects": ",".join(guids)
})
```

---

### GETANDSELECTLASTOBJECTS - 获取并选择最后创建的对象

**功能**：一次性完成"获取最后创建的对象"和"选择它们"两个操作

**请求**：

```json
{
  "Name": "RHINO",
  "Info": "获取并选择最后创建的对象",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "GETANDSELECTLASTOBJECTS"
  }
}
```

**响应示例**（成功）：

```json
{
  "Name": "GetAndSelectLastObjects",
  "Info": "获取并选择最后创建的对象",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Objects": {
      "Object_0": {
        "Id": "00000000-0000-0000-0000-000000000000",
        "Guid": "00000000-0000-0000-0000-000000000000",
        "Type": "Curve",
        "Layer": "Default",
        "Name": "",
        "DatabaseRecordId": "1"
      },
      "Count": "1"
    },
    "Selection": {
      "TotalRequested": "1",
      "TotalSelected": "1",
      "InvalidIdCount": "0",
      "NotFoundCount": "0"
    }
  }
}
```

**说明**：

- 复合命令，自动执行 GETLASTCREATEDOBJECTS 和 SELECTOBJECTS
- 自动处理数据格式转换
- 返回包含对象信息和选择结果的合并数据
- 适用于需要立即选择刚创建对象的场景

**使用建议**：

- 如果只需要获取对象信息：使用 `GETLASTCREATEDOBJECTS`
- 如果只需要选择已知对象：使用 `SELECTOBJECTS`
- 如果需要创建对象后立即选择：使用 `GETANDSELECTLASTOBJECTS`

## 命令分类汇总

| 分类 | 命令数量 | 命令列表 |
|------|----------|----------|
| Component | 5 | GETALLCOMPONENTS, FINDCOMPONENTBYGUID, FINDCOMPONENTBYNAME, FINDCOMPONENTBYCATEGORY, SEARCHCOMPONENTSBYNAME |
| Document | 3 | SAVEDOCUMENT, LOADDOCUMENT, DATABASEPATH |
| Rhino | 4 | RUNSCRIPT, GETLASTCREATEDOBJECTS, SELECTOBJECTS, GETANDSELECTLASTOBJECTS |
| **总计** | **12** | |

---

## 错误处理

所有命令在执行失败时会返回错误格式的LJSON：

```json
{
  "Name": "Error",
  "Info": "错误描述",
  "Time": "2026-03-26T10:00:00",
  "Value": "错误详情信息"
}
```

常见错误类型：
- 输入数据为空
- 未找到命令类型
- 未知的命令
- 缺少必需参数
- 执行命令时出错

---

## 使用示例

### Python示例

```python
import socket
import json
from datetime import datetime

def send_command(port, ljson_type, command_name, params):
    """发送命令到GrasshopperSever

    Args:
        port: 端口号
        ljson_type: 命令类型 (COMPONENT/DOCUMENT/RHINO/SCRIPT/DESIGN)
        command_name: 具体命令名称
        params: 命令参数字典
    """
    data = {
        "Name": ljson_type,  # 命令类型
        "Info": f"执行{command_name}命令",
        "Time": datetime.now().isoformat(),
        "Value": {
            "Command": command_name,  # 具体命令
            **params
        }
    }

    client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    client.connect(('127.0.0.1', port))
    message = json.dumps(data, ensure_ascii=False)
    client.sendall((message + '\n').encode('utf-8'))

    client.settimeout(10)
    total_response = b''
    while True:
        try:
            chunk = client.recv(4096)
            if not chunk:
                break
            total_response += chunk
        except socket.timeout:
            break

    client.close()

    # 解析响应（可能有多个消息）
    response = total_response.decode('utf-8-sig')
    messages = [msg for msg in response.split('\ufeff') if msg.strip()]
    results = [json.loads(msg.strip()) for msg in messages]

    return results

# 示例：获取数据库路径
results = send_command(6879, "DOCUMENT", "DATABASEPATH", {})
for result in results:
    if result.get('Name') == 'DatabasePath':
        print(f"数据库路径: {result['Value']['DatabasePath']}")

# 示例：搜索组件
results = send_command(6879, "COMPONENT", "SEARCHCOMPONENTSBYNAME", {"Name": "Circle"})
for result in results:
    if result.get('Name') == 'SearchComponentsByName':
        print(f"找到 {result['Value']['Count']} 个组件")
```

## 测试结果验证

### 1. DATABASEPATH（已测试）

**请求**：
```json
{
  "Name": "DOCUMENT",
  "Info": "获取数据库路径",
  "Value": {"Command": "DATABASEPATH"}
}
```

**响应**：
```json
{
  "Name": "DatabasePath",
  "Info": "获取数据库路径",
  "Value": {
    "DatabasePath": "C:\\Users\\SZ\\AppData\\Roaming\\Grasshopper\\Libraries\\GHserver\\GrasshopperSever.db"
  }
}
```

**测试状态**: ✓ 成功

---

### 2. RUNSCRIPT（已测试）

**请求**：
```json
{
  "Name": "RHINO",
  "Info": "执行RUNSCRIPT命令",
  "Value": {
    "Command": "RUNSCRIPT",
    "Script": "_-Line 0,0,0 10,10,0"
  }
}
```

**响应**：
```json
{
  "Name": "RunScript",
  "Info": "执行Rhino脚本成功",
  "Value": {
    "Result": "True",
    "Script": "_-Line 0,0,0 10,10,0"
  }
}
```

**测试状态**: ✓ 成功

---

### 3. GETLASTCREATEDOBJECTS（待测试）

**测试步骤**：
1. 执行 RUNSCRIPT 命令创建对象（例如：`_-Line 0,0,0 10,10,0`）
2. 执行 GETLASTCREATEDOBJECTS 命令
3. 验证返回的对象信息是否正确
4. 检查数据库中是否已存储对象记录

**预期响应**：
```json
{
  "Name": "GetLastCreatedObjects",
  "Info": "获取最后创建的对象",
  "Value": {
    "Object_0": {
      "Id": "{guid}",
      "Guid": "{guid}",
      "Type": "Curve",
      "Layer": "Default",
      "Name": "",
      "DatabaseRecordId": "1"
    },
    "Count": "1"
  }
}
```

**测试状态**: ⏳ 待测试

---

### 4. SELECTOBJECTS（待测试）

**测试步骤**：
1. 执行 RUNSCRIPT 命令创建多个对象
2. 执行 GETLASTCREATEDOBJECTS 获取对象ID
3. 执行 SELECTOBJECTS 命令选择对象
4. 在Rhino中验证对象是否被选中

**请求示例**：
```json
{
  "Name": "RHINO",
  "Info": "选择对象",
  "Value": {
    "Command": "SELECTOBJECTS",
    "Objects": "{guid1},{guid2},{guid3}"
  }
}
```

**预期响应**：
```json
{
  "Name": "SelectObjects",
  "Info": "选择对象",
  "Value": {
    "TotalRequested": "3",
    "TotalSelected": "3",
    "InvalidIdCount": "0",
    "NotFoundCount": "0"
  }
}
```

**测试状态**: ⏳ 待测试

---

### 5. GETANDSELECTLASTOBJECTS（待测试）

**测试步骤**：
1. 执行 RUNSCRIPT 命令创建对象
2. 执行 GETANDSELECTLASTOBJECTS 命令
3. 验证返回的对象信息和选择结果
4. 在Rhino中验证对象是否被选中

**请求示例**：
```json
{
  "Name": "RHINO",
  "Info": "获取并选择最后创建的对象",
  "Value": {
    "Command": "GETANDSELECTLASTOBJECTS"
  }
}
```

**预期响应**：
```json
{
  "Name": "GetAndSelectLastObjects",
  "Info": "获取并选择最后创建的对象",
  "Value": {
    "Objects": {
      "Object_0": {
        "Id": "{guid}",
        "Guid": "{guid}",
        "Type": "Curve",
        "Layer": "Default",
        "Name": "",
        "DatabaseRecordId": "1"
      },
      "Count": "1"
    },
    "Selection": {
      "TotalRequested": "1",
      "TotalSelected": "1",
      "InvalidIdCount": "0",
      "NotFoundCount": "0"
    }
  }
}
```

**测试状态**: ⏳ 待测试

