# GrasshopperSever 命令列表

本文档列出了 GrasshopperSever 插件支持的所有可用命令。
警告：不要轻易获取所有组件信息，优先使用分组或名称查询、检索，或者调用数据库。
注意：默认数据库是不会创建的，使用查找或查询组件的命令或组件返回为空。建议在第一次使用时，手动运行一次 AllComponents 组件进行初始化。

## 命令格式

所有命令通过 LJSON 格式发送，必须包含以下结构：

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

**Name 字段（命令类型）**：
- `COMPONENT` - 组件相关命令
- `DOCUMENT` - 文档相关命令
- `RHINO` - Rhino 相关命令
- `SCRIPT` - 脚本相关命令
- `DESIGN` - 设计相关命令

---

## 数据库表结构

GrasshopperSever 使用 SQLite 数据库存储组件信息和对象信息。数据库文件位于：
- **路径**：`C:\Users\[用户名]\AppData\Roaming\Grasshopper\Libraries\GHserver\ComponentsInfo.db`
- **查询命令**：使用 `DATABASEPATH` 命令获取具体路径

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
SELECT LastUpdateTime FROM MetaInfo WHERE TableName = 'ALLCOMPS';
```

---

### 2. ALLCOMPS 表（组件信息表）

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
| Prototype | TEXT | DEFAULT '' | 包含输入输出的函数签名（JSON 格式） |

**SQL 创建语句**：
```sql
CREATE TABLE IF NOT EXISTS ALLCOMPS (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ComponentGuid TEXT NOT NULL UNIQUE,
    ComponentName TEXT NOT NULL,
    NickName TEXT,
    Description TEXT,
    Category TEXT NOT NULL,
    SubCategory TEXT NOT NULL,
    Prototype TEXT DEFAULT ''
)
```

**示例查询**：
```sql
-- 查询所有组件
SELECT ComponentGuid, ComponentName, NickName, Category, SubCategory FROM ALLCOMPS;

-- 按分类查询组件
SELECT ComponentName, NickName, Description FROM ALLCOMPS WHERE Category = 'Curve';

-- 模糊搜索组件
SELECT ComponentName, NickName, Description FROM ALLCOMPS WHERE ComponentName LIKE '%Circle%';

-- 统计组件数量
SELECT Category, COUNT(*) as Count FROM ALLCOMPS GROUP BY Category;
```

**注意事项**：
- `Inputs` 和 `Outputs` 字段存储的是参数定义的 JSON 字符串
- 使用 `INSERT OR REPLACE` 语句进行插入或更新
- 该表在插件初始化时自动填充，并在每次启动时更新

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

通过 GUID 查找组件

**请求参数**：
```json
{
  "Name": "COMPONENT",
  "Info": "通过 GUID 查找组件",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "FINDCOMPONENTBYGUID",
    "Guid": "组件的 GUID 字符串"
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

**说明**：至少需要提供 Category、SubCategory 或 Name 中的一个参数

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


def send_command(port, ljson_type, command_name, params, max_responses=2):
    """
    发送命令到 GrasshopperSever
    
    Args:
        port: 端口号
        ljson_type: 命令类型 (COMPONENT/DOCUMENT/RHINO/SCRIPT/DESIGN)
        command_name: 具体命令名称
        params: 命令参数字典
        max_responses: 预期接收的响应数量
                     - 普通消息：1 条（成功接收确认）
                     - 可执行命令：2 条（成功接收确认 + 命令处理结果）
    
    Returns:
        响应消息列表
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

    # 使用 readline() 接收响应
    responses = receive_responses(client, max_count=max_responses)
    
    client.close()
    return responses


# 示例 1：获取数据库路径
print("=== 获取数据库路径 ===")
results = send_command(6655, "DOCUMENT", "DATABASEPATH", {}, max_responses=2)
for result in results:
    if result.get('Name') == 'DatabasePath':
        print(f"数据库路径：{result['Value']['DatabasePath']}")

# 示例 2：搜索组件
print("\n=== 搜索 Circle 组件 ===")
results = send_command(6655, "COMPONENT", "SEARCHCOMPONENTSBYNAME", 
                       {"Name": "Circle"}, max_responses=2)
for result in results:
    if result.get('Name') == 'SearchComponentsByName':
        print(f"找到 {result['Value']['Count']} 个组件")

# 示例 3：获取所有 Maths 分类的组件
print("\n=== 获取 Maths 分类组件 ===")
results = send_command(6655, "COMPONENT", "FINDCOMPONENTBYCATEGORY", 
                       {"Category": "Maths"}, max_responses=2)
for result in results:
    print(f"响应：{result}")
```

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
- 未找到命令类型
- 未知的命令
- 缺少必需参数
- 执行命令时出错

---

## 响应消息说明

### 普通数据发送
发送普通 LJSON 数据（不包含 Command 字段）时，会收到 **1 条响应**：

```json
{
  "Name": "OK",
  "Info": "成功响应",
  "Time": "2026-03-26T10:00:00",
  "Value": "数据接收成功"
}
```

### 可执行命令
发送可执行命令（包含 Command 字段）时，会收到 **2 条响应**：

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
  "Name": "SearchComponentsByName",
  "Info": "搜索组件",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Count": "5",
    "Components": [...]
  }
}
```

### 注意事项
1. **BOM 处理**：服务器响应包含 UTF-8 BOM 标记，需要手动去除
2. **消息边界**：使用 `readline()` 按行接收，每条消息以换行符分隔
3. **响应数量**：根据消息类型设置正确的 `max_responses` 参数
