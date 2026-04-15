# GrasshopperSever 命令列表

本文档列出了GrasshopperSever插件支持的所有可用命令。

警告：不要轻易获取所有组件信息，优先使用分组或名称查询、检索，或者调用数据库。

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

### 1. RhinoObjects 表（Rhino对象信息表）

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

## Rhino命令测试记录

### RHINOSCRIPT - 运行Rhino命令

**端口**: 6655

**测试命令**: `_-Line 0,0,0 10,10,0` (创建直线)

**请求**：

```json
{
  "Name": "RHINO",
  "Info": "执行RHINOSCRIPT命令",
  "Time": "2026-03-26T...",
  "Value": {
    "Command": "RHINOSCRIPT",
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
  "Name": "RhinoCommand",
  "Info": "执行Rhino脚本成功",
  "Value": {
    "Result": "True",
    "Script": "_-Line 0,0,0 10,10,0"
  }
}
```

**说明**:

- RHINOSCRIPT命令执行成功时，会返回执行结果，包含 Result 和 Script 字段
- 命令执行失败时，会返回错误信息
- 需要在Rhino中验证命令是否实际执行成功

**测试脚本**: `test_rhinoscript_6655.py`

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

### 1. RHINOSCRIPT

**请求**：

```json
{
  "Name": "RHINO",
  "Info": "执行RHINOSCRIPT命令",
  "Value": {
    "Command": "RHINOSCRIPT",
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

### 2. GETLASTCREATEDOBJECTS（待测试）

**测试步骤**：
1. 执行 RHINOSCRIPT 命令创建对象（例如：`_-Line 0,0,0 10,10,0`）
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

### 3. SELECTOBJECTS（待测试）

**测试步骤**：
1. 执行 RHINOSCRIPT 命令创建多个对象
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

### 4. GETANDSELECTLASTOBJECTS（待测试）

**测试步骤**：
1. 执行 RHINOSCRIPT 命令创建对象
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

