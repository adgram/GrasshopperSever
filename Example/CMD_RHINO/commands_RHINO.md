# GrasshopperSever Rhino 命令文档

本文档列出了 GrasshopperSever 插件支持的 Rhino 相关命令。

## 命令格式

所有命令通过 LJSON 格式发送，必须包含以下结构：

```json
{
  "Name": "RHINO",
  "Info": "命令描述",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "具体命令名称",
    "参数名": "参数值"
  }
}
```

**端口**：6655

---

## 数据库表结构

### RhinoObjects 表（Rhino 对象信息表）

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

## Rhino 命令

### 1. RHINOSCRIPT - 运行 Rhino 命令

执行 Rhino 脚本命令。

**请求**：
```json
{
  "Name": "RHINO",
  "Info": "执行 RHINOSCRIPT 命令",
  "Time": "2026-03-26T...",
  "Value": {
    "Command": "RHINOSCRIPT",
    "Script": "_-Line 0,0,0 10,10,0"
  }
}
```

**响应示例**：
```json
{
  "Name": "RhinoCommand",
  "Info": "执行 Rhino 脚本成功",
  "Value": {
    "Result": "True",
    "Script": "_-Line 0,0,0 10,10,0"
  }
}
```

**说明**：
- RHINOSCRIPT 命令执行成功时，会返回执行结果，包含 Result 和 Script 字段
- 命令执行失败时，会返回错误信息
- 需要在 Rhino 中验证命令是否实际执行成功

---

### 2. GETLASTCREATEDOBJECTS - 获取最后创建的对象

获取 Rhino 中最后创建的对象信息，并将对象信息存入数据库。

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

**响应示例（成功）**：
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

**响应示例（无对象）**：
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

---

### 3. SELECTOBJECTS - 选择对象

根据对象 ID 列表选择 Rhino 中的对象。

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
- `Objects`：对象 ID 列表，支持以下分隔符：逗号 (,)、分号 (;)、空格

**响应示例（成功选择 3 个对象）**：
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

**响应示例（全部失败）**：
```json
{
  "Name": "SelectObjects",
  "Info": "选择对象",
  "Value": {
    "TotalRequested": "2",
    "TotalSelected": "0",
    "InvalidIdCount": "2",
    "NotFoundCount": "0",
    "Message": "所有 ID 均无效或未找到对象（无效 ID: 2, 未找到：0）"
  }
}
```

**说明**：
- 支持批量选择多个对象
- 自动清除之前的选择
- 验证每个 ID 的格式和有效性
- 自动刷新视图以显示选择结果
- 返回详细的统计信息

---

### 4. GETANDSELECTLASTOBJECTS - 获取并选择最后创建的对象

一次性完成"获取最后创建的对象"和"选择它们"两个操作。

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

**响应示例（成功）**：
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

## 使用示例（标准接收方式）

### Python 示例（使用 readline()）

```python
import socket
import json
from datetime import datetime

def receive_responses(client, max_count=10, timeout=10):
    """
    按行接收服务器响应（标准方式）
    
    Args:
        client: TCP socket 连接对象
        max_count: 最多接收的消息数量
        timeout: 超时时间（秒）
    
    Returns:
        响应消息列表
    """
    if not client:
        return []
    
    client.settimeout(timeout)
    reader = client.makefile('r', encoding='utf-8')
    messages = []
    
    for i in range(max_count):
        try:
            line = reader.readline()
            if not line:
                break
            
            line = line.strip()
            if not line:
                continue
            
            # 尝试解析 JSON
            try:
                msg = json.loads(line)
                messages.append(msg)
            except json.JSONDecodeError as e:
                # 尝试去除 BOM 标记
                if line.startswith('\ufeff'):
                    try:
                        msg = json.loads(line[1:])
                        messages.append(msg)
                    except json.JSONDecodeError:
                        pass
        
        except Exception as e:
            break
    
    reader.close()
    return messages


def send_command(command_name, params, max_responses=2):
    """
    发送 Rhino 命令到 GrasshopperSever
    
    Args:
        command_name: 具体命令名称
        params: 命令参数字典
        max_responses: 预期接收的响应数量
                     - Rhino 命令：2 条（接收确认 + 命令结果）
    
    Returns:
        响应消息列表
    """
    data = {
        "Name": "RHINO",
        "Info": f"执行{command_name}命令",
        "Time": datetime.now().isoformat(),
        "Value": {
            "Command": command_name,
            **params
        }
    }

    client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    client.connect(('127.0.0.1', 6655))
    
    message = json.dumps(data, ensure_ascii=False)
    client.sendall((message + '\n').encode('utf-8'))

    # 使用 readline() 接收响应
    responses = receive_responses(client, max_count=max_responses)
    
    client.close()
    return responses


# 示例 1：执行 Rhino 命令创建直线
print("=== 创建直线 ===")
results = send_command("RHINOSCRIPT", 
                       {"Script": "_-Line 0,0,0 10,10,0"}, 
                       max_responses=2)
for result in results:
    print(f"响应：{result}")

# 示例 2：获取最后创建的对象
print("\n=== 获取最后创建的对象 ===")
results = send_command("GETLASTCREATEDOBJECTS", {}, max_responses=2)
for result in results:
    if result.get('Name') == 'GetLastCreatedObjects':
        print(f"找到 {result['Value'].get('Count', 0)} 个对象")
        for key, value in result['Value'].items():
            if key.startswith('Object_'):
                print(f"  对象 ID: {value.get('Guid')}")
                print(f"  类型：{value.get('Type')}")

# 示例 3：选择对象
print("\n=== 选择对象 ===")
# 先获取对象
obj_results = send_command("GETLASTCREATEDOBJECTS", {}, max_responses=2)
guids = []
for result in obj_results:
    if result.get('Name') == 'GetLastCreatedObjects':
        for key, value in result['Value'].items():
            if key.startswith('Object_'):
                guids.append(value['Guid'])

# 选择这些对象
if guids:
    results = send_command("SELECTOBJECTS", 
                           {"Objects": ",".join(guids)}, 
                           max_responses=2)
    for result in results:
        if result.get('Name') == 'SelectObjects':
            print(f"选择成功：{result['Value'].get('TotalSelected')} 个对象")

# 示例 4：获取并选择最后创建的对象（复合命令）
print("\n=== 获取并选择最后创建的对象 ===")
results = send_command("GETANDSELECTLASTOBJECTS", {}, max_responses=2)
for result in results:
    if result.get('Name') == 'GetAndSelectLastObjects':
        print(f"对象数量：{result['Value']['Objects'].get('Count', 0)}")
        print(f"选择成功：{result['Value']['Selection']['TotalSelected']} 个")
```

---

## 错误处理

所有命令在执行失败时会返回错误格式的 LJSON：

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
- 未知的命令
- 缺少必需参数
- 执行命令时出错
- 对象 ID 无效或不存在

---

## 响应消息说明

### Rhino 命令响应
执行 Rhino 命令时，会收到 **2 条响应**：

**响应 1 - 接收确认**：
```json
{
  "Name": "OK",
  "Info": "成功响应",
  "Time": "2026-03-26T10:00:00",
  "Value": "客户端已连接"
}
```

**响应 2 - 命令结果**：
```json
{
  "Name": "RhinoCommand",
  "Info": "执行 Rhino 脚本成功",
  "Value": {
    "Result": "True",
    "Script": "_-Line 0,0,0 10,10,0"
  }
}
```

### 注意事项
1. **BOM 处理**：服务器响应包含 UTF-8 BOM 标记，需要手动去除
2. **消息边界**：使用 `readline()` 按行接收，每条消息以换行符分隔
3. **响应数量**：Rhino 命令固定返回 2 条响应

---

## 测试总结

### 已测试命令（端口 6655）

| 命令 | 状态 | 测试结果 |
|------|------|----------|
| RHINOSCRIPT | ✅ 已测试 | 成功执行 Rhino 脚本，返回正确结果 |
| GETLASTCREATEDOBJECTS | ✅ 已测试 | 成功获取对象信息，数据库集成正常 |
| SELECTOBJECTS | ✅ 已测试 | 成功选择多个对象，错误处理正常 |
| GETANDSELECTLASTOBJECTS | ✅ 已测试 | 成功复合操作，获取并选择对象 |

### 测试覆盖范围
- ✅ 连接测试：成功连接到 GrasshopperSever
- ✅ 命令执行：所有 Rhino 命令正常工作
- ✅ 对象创建：能够成功创建 Rhino 对象（点、圆、直线）
- ✅ 对象获取：能够正确获取最后创建的对象信息
- ✅ 对象选择：能够正确选择多个对象，错误处理正常
- ✅ 复合操作：GETANDSELECTLASTOBJECTS 正常工作
- ✅ 数据库集成：对象信息正确存储到数据库
- ✅ 响应格式：返回格式符合 LJSON 规范
- ✅ 错误处理：能够正确识别和处理无效对象 ID
