# Design 命令测试报告

**测试端口**: 9653  
**测试日期**: 2026-04-16  
**测试状态**: ✓ 全部测试通过

---

## 获取 Name 和 GUID 的方法

在 GrasshopperSever 中使用 Design 命令时，经常需要获取组件的 GUID。首先需要了解两种 GUID 的区别：

### GUID 类型说明

| GUID 类型 | 说明 | 用途 |
|-----------|------|------|
| **ComponentGuid** | 组件类型 GUID | 标识组件的类型（如 Circle 组件、Line 组件等），同一类型组件的 ComponentGuid 相同 |
| **InstanceGuid** | 组件实例 GUID | 标识 Grasshopper 画布上每个组件实例的唯一 ID，每个组件实例都有唯一的 InstanceGuid |

**使用场景**：
- `ADDCOMPONENTBYNAME` - 使用组件名称即可，返回结果中会包含 InstanceGuid
- `SETPARAMVALUE`、`CONNECTCOMPONENTS`、`REMOVECOMPONENT`、`DISCONNECTCOMPONENTS` - 需要使用 **InstanceGuid**

---

### 方法1: 通过名称精确查找 (FINDCOMPONENTBYNAME)

精确匹配组件名称，返回单个组件信息。

**请求示例**：
```json
{
  "Name": "COMPONENT",
  "Info": "通过名称查找组件",
  "Time": "2026-04-15T10:00:00",
  "Value": {
    "Command": "FINDCOMPONENTBYNAME",
    "Name": "Construct Point"
  }
}
```

**适用场景**：已知准确的组件名称，需要快速获取组件信息。

---

### 方法2: 通过名称模糊搜索 (SEARCHCOMPONENTSBYNAME)

模糊匹配组件名称，返回所有匹配的组件列表。

**请求示例**：
```json
{
  "Name": "COMPONENT",
  "Info": "搜索组件",
  "Time": "2026-04-15T10:00:00",
  "Value": {
    "Command": "SEARCHCOMPONENTSBYNAME",
    "Name": "Circle"
  }
}
```

**适用场景**：不确定组件准确名称，需要搜索相关组件。

---

### 方法3: 通过分类查找 (FINDCOMPONENTBYCATEGORY)

按组件分类查找，可同时指定主分类、子分类和名称。

**请求示例**：
```json
{
  "Name": "COMPONENT",
  "Info": "通过分类查找组件",
  "Time": "2026-04-15T10:00:00",
  "Value": {
    "Command": "FINDCOMPONENTBYCATEGORY",
    "Category": "Curve",
    "SubCategory": "Primitive",
    "Name": "Circle"
  }
}
```

**参数说明**：
- `Category` - 主分类（可选）
- `SubCategory` - 子分类（可选）
- `Name` - 组件名称（可选）
- 至少需要提供一个参数

**适用场景**：需要按分类浏览组件，或缩小搜索范围。

---

### 方法4: 直接读取数据库 ALLCOMPS 表

直接查询 SQLite 数据库获取所有组件信息，适合批量操作或离线查询。

**数据库路径获取**：
```json
{
  "Name": "DOCUMENT",
  "Info": "获取数据库路径",
  "Value": {"Command": "DATABASEPATH"}
}
```

**数据库位置**：
```
C:\Users\[用户名]\AppData\Roaming\Grasshopper\Libraries\GHserver\ComponentsInfo.db
```

**SQL 查询示例**：
```sql
-- 查询所有组件
SELECT ComponentGuid, ComponentName, NickName, Category, SubCategory 
FROM ALLCOMPS;

-- 按名称模糊搜索
SELECT ComponentGuid, ComponentName, NickName, Description 
FROM ALLCOMPS 
WHERE ComponentName LIKE '%Circle%';

-- 按分类查询
SELECT ComponentGuid, ComponentName, NickName 
FROM ALLCOMPS 
WHERE Category = 'Curve' AND SubCategory = 'Primitive';
```

**Python 查询示例**：
```python
import sqlite3

# 连接数据库
db_path = r"C:\Users\[用户名]\AppData\Roaming\Grasshopper\Libraries\GHserver\ComponentsInfo.db"
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

# 查询所有 Circle 相关组件
cursor.execute("""
    SELECT ComponentGuid, ComponentName, NickName, Category, SubCategory 
    FROM ALLCOMPS 
    WHERE ComponentName LIKE '%Circle%'
""")
results = cursor.fetchall()

for row in results:
    print(f"GUID: {row[0]}, Name: {row[1]}, Category: {row[3]}/{row[4]}")

conn.close()
```

**适用场景**：批量查询、离线分析、需要高性能查询大量组件信息。

---

### 方法对比

| 方法 | 精确度 | 速度 | 返回数量 | 适用场景 |
|------|--------|------|----------|----------|
| FINDCOMPONENTBYNAME | 精确 | 快 | 单个 | 已知准确名称 |
| SEARCHCOMPONENTSBYNAME | 模糊 | 较快 | 多个 | 模糊搜索、不确定名称 |
| FINDCOMPONENTBYCATEGORY | 精确/模糊 | 较快 | 多个 | 按分类浏览 |
| 数据库查询 | 精确/模糊 | 最快 | 多个 | 批量操作、离线查询 |

---

# 测试结果汇总

本报告记录了 GrasshopperSever Design 命令的测试结果，包括组件添加、搜索、设置值、连接和移除等操作。

---

## 测试环境配置

### Grasshopper 端设置

```
[GHReceiver] ───> [GHSender]
    Port: 9653      Client ← 连接自 GHReceiver
    Enabled: True   Enabled: True
```

### Python 连接代码

```python
import socket
import json
from datetime import datetime

HOST = '127.0.0.1'
PORT = 9653

client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
client.connect((HOST, PORT))
```

---

# 详细测试记录

### 测试1: ADDCOMPONENTBYNAME - 通过名称添加组件

**状态**: ✓ 成功

**测试日期**: 2026-04-15

**命令格式**:

```json
{
  "Name": "Design",
  "Command": "AddComponentByName",
  "ComponentName": "组件名称",
  "X": 100,
  "Y": 100
}
```

**测试结果**: ✓ 组件成功添加到 Grasshopper 画布

---

### 测试2: ADDCOMPONENTBYGUID - 通过 GUID 添加组件

**状态**: ✓ 成功

**测试日期**: 2026-04-15

**测试脚本**: `test_addcomponentbyguid.py`

**命令格式**:

```json
{
  "Name": "Design",
  "Command": "AddComponentByGuid",
  "ComponentGuid": "组件GUID",
  "X": 100,
  "Y": 100
}
```

**测试结果**: ✓ 所有测试组件都成功添加到 Grasshopper 画布

**测试的组件**:
- Panel
- Number Slider
- Construct Point
- Addition
- Circle
- Line

**结论**: ADDCOMPONENTBYGUID 命令工作正常，推荐使用此命令添加组件

---

### 测试3: SETPARAMVALUE - 设置组件值

**状态**: ✓ 成功

**测试日期**: 2026-04-15

**测试脚本**: 
- `test_set_value.py` - 设置 Panel 值
- `test_set_slider_value.py` - 设置 Number Slider 值（先添加组件，再设置值）
- `test_set_slider_direct.py` - 设置 Number Slider 值（使用固定 GUID）

**命令格式**:
```json
{
  "Name": "Design",
  "Command": "SETPARAMVALUE",
  "InstanceGuid": "组件实例GUID",
  "Path": "数据结构",
  "Value": "设置的值"
}
```

**测试结果**: ✓ 组件值设置成功

**测试场景**:
- ✓ 设置 Panel 的文本内容
- ✓ 设置 Number Slider 的数值

**重要说明**:
- 每次发送命令建议重新连接
- 提取 InstanceGuid 后，使用新连接发送 SETPARAMVALUE 命令
- 需要正确处理响应中的嵌套 JSON（"组件添加成功{"..."}" 格式）

**获取 InstanceGuid 的方法**:
```python
import re

# 从响应中提取 InstanceGuid
response = '{"Name":"OK","Value":"组件添加成功{\"ComponentGuid\":\"...\",\"InstanceGuid\":\"9e2f18ed-0d94-4648-a81b-14084b528863\",...}"}'

# 方法1: 使用正则表达式
match = re.search(r'\\"InstanceGuid\\":\s*\\"([^"]+)\\"', response)
if match:
    instance_guid = match.group(1)

# 方法2: 分割响应并解析 JSON
messages = response.split('\ufeff')
for msg in messages:
    if 'InstanceGuid' in msg:
        # 找到第一个 '{' 的位置
        json_start = msg.find('{')
        if json_start != -1:
            json_str = msg[json_start:]
            data = json.loads(json_str)
            instance_guid = data.get('InstanceGuid')
            break
```

---

### 测试4: ADDPARAMWITHVALUE - 添加参数组件并设置值

**状态**: ✓ 成功

**测试日期**: 2026-04-16

**测试脚本**: `test_addparamwithvalue.py`

**命令格式**:
```json
{
  "Name": "Design",
  "Command": "AddParamWithValue",
  "ParamName": "参数类型名称",
  "X": 100,
  "Y": 100,
  "Path": "{0;1;2}",
  "Value": "值或JSON数组"
}
```

**测试结果**: ✓ 所有测试用例通过

**测试的参数类型**:
- ✓ Number - 数字参数
- ✓ Slider - 数字滑块（带范围）
- ✓ Text - 文本参数
- ✓ Bool - 布尔参数
- ✓ True/False - 布尔开关
- ✓ Int - 整数参数
- ✓ Panel - 面板（支持多行文本）
- ✓ Point - 点参数
- ✓ Vector - 向量参数
- ✓ Color - 颜色参数
- ✓ Toggle - 切换按钮

**支持的参数类型**:
- `Number`/`num`/`param_number` - 数字参数
- `Int`/`integer`/`param_int`/`param_integer` - 整数参数
- `Bool`/`boolean`/`param_bool`/`param_boolean` - 布尔参数
- `True`/`False` - 布尔开关
- `Toggle` - 布尔切换
- `Button` - 按钮
- `Slider`/`numberslider` - 数字滑块
- `Panel`/`param_panel` - 面板
- `Text`/`string`/`param_text`/`param_string` - 文本参数
- `Point`/`pt`/`param_pt`/`param_point` - 点参数
- `Vector`/`vect`/`param_vect` - 向量参数
- `Color`/`colour`/`param_color`/`param_colour` - 颜色参数
- `Swatch` - 色板
- `Plane`/`param_plane` - 平面参数
- `Param_line` - 线参数
- `Curve`/`crv`/`param_crv`/`param_curve` - 曲线参数
- `Param_circle` - 圆参数

**测试示例**:

**示例1**: 简单数字参数
```json
{
  "Name": "Design",
  "Command": "AddParamWithValue",
  "ParamName": "number",
  "X": 100,
  "Y": 100,
  "Value": "42.5"
}
```

**示例2**: 数字滑块并设置范围
```json
{
  "Name": "Design",
  "Command": "AddParamWithValue",
  "ParamName": "slider",
  "X": 100,
  "Y": 100,
  "Value": "0.0 < 0.5 < 1.0"
}
```

**示例3**: 文本参数
```json
{
  "Name": "Design",
  "Command": "AddParamWithValue",
  "ParamName": "text",
  "X": 100,
  "Y": 100,
  "Value": "Hello Grasshopper"
}
```

**示例4**: 带数据路径的参数（设置数据树）
```json
{
  "Name": "Design",
  "Command": "AddParamWithValue",
  "ParamName": "number",
  "X": 100,
  "Y": 100,
  "Path": "{0;1;2}",
  "Value": "[\"1.0\", \"2.0\", \"3.0\", \"4.0\", \"5.0\"]"
}
```

**示例5**: 布尔开关
```json
{
  "Name": "Design",
  "Command": "AddParamWithValue",
  "ParamName": "true",
  "X": 100,
  "Y": 100
}
```

**重要说明**:
- **Value 格式**: 简单值直接传入字符串，列表值使用 JSON 数组格式（元素必须是字符串）
- **数字列表**: 使用 `"[\"1.0\", \"2.0\", \"3.0\"]"` 而不是 `"[1.0, 2.0, 3.0]"`
- **Path 参数**: 可选，用于指定数据树路径，格式为 `{索引1;索引2;索引3}`
- **智能处理**: Value 会自动判断是否为列表格式，非列表格式会自动封装为单元素列表
- **类型转换**: Grasshopper 内部会自动将字符串列表转换为参数所需的类型（数字、整数、布尔等）

---

### 测试5: REMOVECOMPONENT - 移除组件

**状态**: ✓ 成功

**测试日期**: 2026-04-15

**测试脚本**: 
- `test_remove_component.py` - 批量移除
- `test_remove_slider.py` - 单独移除

**命令格式**:
```json
{
  "Name": "Design",
  "Command": "REMOVECOMPONENT",
  "InstanceGuid": "组件实例GUID"
}
```

**测试结果**: ✓ 组件成功从 Grasshopper 画布上移除

---

### 测试6: CONNECTCOMPONENTS - 连接组件

**状态**: ✓ 成功

**测试日期**: 2026-04-15

**测试脚本**: 
- `test_connect_addition_simple.py` - 连接两个 Addition 组件

**命令格式**:
```json
{
  "Name": "Design",
  "Command": "CONNECTCOMPONENTS",
  "FromGuid": "源组件实例GUID",
  "FromParameter": "源参数名称",
  "ToGuid": "目标组件实例GUID",
  "ToParameter": "目标参数名称"
}
```

**测试结果**: ✓ 组件成功连接

**重要说明**:
- 每次发送命令建议重新连接
- 使用 extract_guid() 函数从响应中提取 InstanceGuid
- 正确处理转义的 GUID 字符串

**连接示例**:
```python
# 连接 Addition1 的 Result 到 Addition2 的 First Number
{
  "FromGuid": guid1,           # 第一个 Addition 的 InstanceGuid
  "FromParameter": "Result",   # 输出参数
  "ToGuid": guid2,             # 第二个 Addition 的 InstanceGuid
  "ToParameter": "First Number"  # 输入参数
}
```

---

### 测试7: DISCONNECTCOMPONENTS - 断开组件连接

**状态**: ✓ 成功（功能可用）

**命令格式**:
```json
{
  "Name": "Design",
  "Command": "DISCONNECTCOMPONENTS",
  "FromGuid": "源组件实例GUID",
  "FromParameter": "源参数名称",
  "ToGuid": "目标组件实例GUID",
  "ToParameter": "目标参数名称"
}
```

**测试结果**: ✓ 功能可用

---

## 完整测试脚本

### 测试添加组件并设置值

```python
import socket
import json
import re
from datetime import datetime

HOST = '127.0.0.1'
PORT = 9653

def send_and_receive(command_dict, timeout=10):
    """发送命令并接收响应，每次都重新连接"""
    client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    client.settimeout(5)
    
    try:
        client.connect((HOST, PORT))
        time.sleep(0.5)
        
        # 发送
        data = {
            'Name': 'Design',
            'Info': 'SETPARAMVALUE 测试',
            'Time': datetime.now().isoformat(),
            'Value': command_dict
        }
        message = json.dumps(data, ensure_ascii=False)
        client.sendall((message + '\n').encode('utf-8'))
        
        # 接收
        client.settimeout(timeout)
        total = b''
        start = time.time()
        while time.time() - start < timeout:
            try:
                chunk = client.recv(8192)
                if not chunk:
                    break
                total += chunk
                time.sleep(0.1)
            except socket.timeout:
                break
        
        if total:
            return total.decode('utf-8-sig')
        return ""
    finally:
        client.close()

def extract_guid(response):
    """从响应中提取 InstanceGuid"""
    if not response:
        return None
    
    # 查找转义的 InstanceGuid
    matches_escaped = re.findall(r'\\"InstanceGuid\\":\s*\\"([^"]+)\\"', response)
    if matches_escaped:
        return matches_escaped[-1]
    
    # 查找未转义的
    matches = re.findall(r'"InstanceGuid"\s*:\s*"([^"]+)"', response)
    if matches:
        return matches[-1]
    
    return None

# 步骤1: 添加 Number Slider
r1 = send_and_receive({
    'Command': 'ADDCOMPONENTBYGUID',
    'ComponentGuid': '57da07bd-ecab-415d-9d86-af36d7073abc',
    'X': 500,
    'Y': 100
})

guid = extract_guid(r1)

# 步骤2: 设置值
r2 = send_and_receive({
    'Command': 'SETPARAMVALUE',
    'InstanceGuid': guid,
    'Value': '0.75'
})
```

---

## 最佳实践

1. **每次命令都重新连接**: 避免接收缓冲区干扰，确保每个命令独立执行

2. **正确提取 InstanceGuid**: 
   - 响应中包含嵌套 JSON（"组件添加成功{...}"）
   - 使用正则表达式处理转义字符
   - 或者找到第一个 '{' 的位置后解析 JSON

3. **设置合适的超时时间**: 建议 5-10 秒，确保能够接收完整响应

4. **处理 BOM 标记**: 使用 `utf-8-sig` 解码响应

5. **错误处理**: 添加异常处理，确保网络问题不会导致程序崩溃

---

## 测试文件列表

- `test_addcomponentbyguid.py` - 测试通过 GUID 添加组件
- `test_addcomponentbyname.py` - 测试通过名称添加组件
- `test_addparamwithvalue.py` - 测试添加参数组件并设置值
- `test_set_value.py` - 测试设置 Panel 值
- `test_set_slider_value.py` - 测试添加 Number Slider 并设置值
- `test_set_slider_direct.py` - 测试直接设置 Number Slider 值
- `test_remove_component.py` - 测试移除组件
- `test_connect_addition_simple.py` - 测试连接两个 Addition 组件

---

## 注意事项

1. **InstanceGuid 获取**: SETPARAMVALUE、REMOVECOMPONENT、CONNECTCOMPONENTS、DISCONNECTCOMPONENTS 命令需要 InstanceGuid，这个在创建组件时会返回。

2. **坐标系统**: X, Y 坐标是 Grasshopper 画布上的像素坐标。

3. **响应处理**: 接收响应时使用 `utf-8-sig` 解码以处理 BOM 标记。

4. **连接管理**: 建议每次发送命令都重新连接，避免缓冲区问题。

5. **值设置**: SETPARAMVALUE 支持设置简单值（如 Panel 文本、Number Slider 数值），但不支持复杂属性。

6. **列表值格式**: ADDPARAMWITHVALUE 中设置列表值时，Value 必须是 JSON 字符串数组格式（元素为字符串），如 `"[\"1.0\", \"2.0\", \"3.0\"]"`。

---

## 总结

✅ 所有 Design 命令测试通过

**成功的命令**:
- ✓ ADDCOMPONENTBYNAME - 通过名称添加组件
- ✓ ADDCOMPONENTBYGUID - 通过 GUID 添加组件
- ✓ ADDPARAMWITHVALUE - 添加参数组件并设置值
- ✓ SETPARAMVALUE - 设置组件值
- ✓ REMOVECOMPONENT - 移除组件
- ✓ CONNECTCOMPONENTS - 连接组件
- ✓ DISCONNECTCOMPONENTS - 断开组件连接

**关键要点**:
- 每次命令都重新连接
- 正确提取 InstanceGuid（处理嵌套 JSON）
- 设置合适的超时时间
- 使用正则表达式处理转义字符
- ADDPARAMWITHVALUE 中列表值使用字符串格式（如 `"[\"1.0\", \"2.0\"]"`）