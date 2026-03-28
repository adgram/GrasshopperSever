# GHScriptEditor 测试文档

## 测试概述
测试GrasshopperSever的ScriptEditor功能，用于执行GHPython3代码。

## 测试环境
- 端口: 6895
- 数据格式: LJSON
- 编码: UTF-8

## 测试方法

### 1. 发送数据格式

发送包含GHPython3代码的LJSON数据：

```json
{
  "Name": "ScriptEditor",
  "Info": "测试GHPython3代码执行",
  "Time": "2026-03-26T16:32:42",
  "Value": {
    "Code": "import Rhino.Geometry as rg\nimport math\n\n# 创建一个点\nx = 10.0\ny = 20.0\nz = 30.0\n\npoint = rg.Point3d(x, y, z)\n\n# 返回点\na = point"
  }
}
```

**关键点**：
- Name字段可以自定义，这里使用"ScriptEditor"
- Value字段可以自定义，但值需要手动提取，建议使用”OUTPUT“自动提取。
- Code的值是完整的GHPython3代码字符串
- 代码中定义输出变量（如`a`），用于返回结果

### 2. 接收响应格式

服务器会返回多条响应消息：

#### 响应1: SCRIPTRESULT（代码执行结果）
```json
{
  "Name": "SCRIPTRESULT",
  "Info": "SCRIPTRESULT",
  "Value": [{
    "Code": "# GH_COMPONENT_IO_START\n# INPUT_PARAMS: {...}\n# OUTPUT_PARAMS: {...}\n# GH_COMPONENT_IO_END\r\nimport Rhino.Geometry as rg\nimport math\n\n# 创建一个点\nx = 10.0\ny = 20.0\nz = 30.0\n\npoint = rg.Point3d(x, y, z)\n\n# 返回点\na = point"
  }]
}
```

**说明**：
- 服务器会将用户代码包装成完整的Grasshopper Python组件格式
- 自动添加输入参数信息（INPUT_PARAMS）
- 自动添加输出参数信息（OUTPUT_PARAMS）
- 代码被包裹在`# GH_COMPONENT_IO_START`和`# GH_COMPONENT_IO_END`之间
- 输入输出必须是ScriptVariableParam类型。

#### 响应2: OK（连接确认）
```json
{
  "Name": "OK",
  "Info": "成功响应",
  "Value": "客户端已连接"
}
```

#### 响应3: OK（数据接收确认）
```json
{
  "Name": "OK",
  "Info": "成功响应",
  "Value": "数据接收成功"
}
```

#### 响应4-5: SCRIPTRESULT（重复返回）
- 服务器会多次返回相同的SCRIPTRESULT
- 通常返回3次

### 3. Python测试代码

```python
import socket
import json
from datetime import datetime

def send_data(port):
    """发送ScriptEditor数据"""
    data = {
        "Name": "ScriptEditor",
        "Info": "测试GHPython3代码执行",
        "Time": datetime.now().isoformat(),
        "Value": {
            "Code": "import Rhino.Geometry as rg\nimport math\n\n# 创建一个点\nx = 10.0\ny = 20.0\nz = 30.0\n\npoint = rg.Point3d(x, y, z)\n\n# 返回点\na = point"
        }
    }

    try:
        client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        client.connect(('127.0.0.1', port))
        message = json.dumps(data, ensure_ascii=False)
        client.sendall((message + '\n').encode('utf-8'))

        # 接收响应
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

        # 解析响应（使用utf-8-sig处理BOM）
        if total_response:
            response = total_response.decode('utf-8-sig')
            messages = [msg for msg in response.split('\ufeff') if msg.strip()]
            results = [json.loads(msg.strip()) for msg in messages]
            return results
        return []

    except Exception as e:
        print(f"连接失败: {e}")
        return []

# 使用示例
results = send_data(6895)
for i, result in enumerate(results):
    print(f"响应{i+1}: {result.get('Name')} - {result.get('Value')}")
```

## C# Script测试

### 标准C#模板

C# Script必须使用标准的Grasshopper C# Script模板：

```csharp
// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    #region Notes
    /* 
      Members:
        RhinoDoc RhinoDocument
        GH_Document GrasshopperDocument
        IGH_Component Component
        int Iteration

      Methods (Virtual & overridable):
        Print(string text)
        Print(string format, params object[] args)
        Reflect(object obj)
        Reflect(object obj, string method_name)
    */
    #endregion

    private void RunScript(object x, object y, ref object a)
    {
        // Write your logic here
        a = null;
    }
}
```

### C# Script测试结果

**发送数据格式**：

```json
{
  "Name": "ScriptEditor",
  "Info": "测试C#代码执行",
  "Time": "2026-03-26T16:49:43",
  "Value": {
    "Code": "// Grasshopper Script Instance\n..."
  }
}
```

**响应特点**：

- ✓ 连接成功
- ✓ 服务器正确识别标准C#模板
- ✓ 保留完整的模板结构（using声明、Script_Instance类等）
- ✓ 只在顶部添加输入输出参数注释
- ✓ 注释使用C#语法（`//`而不是Python的`#`）
- ✓ 响应2条SCRIPTRESULT消息

### C#与Python的区别

| 特性     | Python Script   | C# Script       |
| -------- | --------------- | --------------- |
| 注释语法 | `#`             | `//`            |
| 响应次数 | 3条SCRIPTRESULT | 2条SCRIPTRESULT |
| 代码包装 | 添加IO注释      | 保留完整模板    |
| 输出参数 | 直接使用变量名  | ref参数         |

### 调试技巧

#### 使用out端口调试

`out`端口是固定的输出端口，用于捕获脚本的调试信息：

**功能**：

- 输出错误信息
- 输出print相关内容
- 捕获运行时异常

**Python Script调试示例**：

```python
import Rhino.Geometry as rg

# 使用print输出调试信息
print("开始执行脚本")
print("输入值: x=" + str(x))

try:
    point = rg.Point3d(x, y, z)
    print("点创建成功: " + str(point))
    a = point
except Exception as e:
    print("错误: " + str(e))
    a = None
```

**C# Script调试示例**：

```csharp
private void RunScript(object x, object y, ref object a)
{
    // 使用Print方法输出调试信息
    Print("开始执行脚本");
    Print("输入值: x=" + x.ToString());

    try
    {
        Rhino.Geometry.Point3d point = new Rhino.Geometry.Point3d(10.0, 20.0, 30.0);
        Print("点创建成功: " + point.ToString());
        a = point;
    }
    catch (Exception ex)
    {
        Print("错误: " + ex.Message);
        a = null;
    }
}
```

**调试信息查看**：

- 所有print/Print输出都会显示在out端口
- 错误信息和异常也会自动输出到out端口
- 建议在代码中添加适当的调试信息以便排查问题

## 测试结果

### 成功测试
- ✓ 端口6895连接成功
- ✓ Code字段被正确识别和处理
- ✓ 代码被自动包装成Grasshopper Python组件格式
- ✓ 返回完整的IO参数信息
- ✓ 响应使用utf-8-sig编码（包含BOM）

### 响应特点
1. 返回5条响应消息
2. SCRIPTRESULT消息会重复3次
3. 所有消息都包含Name、Info、Value字段
4. 响应使用BOM标记，需要用utf-8-sig解码

## 代码示例

### 示例1: 创建点
```python
import Rhino.Geometry as rg

x = 10.0
y = 20.0
z = 30.0
point = rg.Point3d(x, y, z)
a = point
```

### 示例2: 创建圆
```python
import Rhino.Geometry as rg

center = rg.Point3d(0, 0, 0)
radius = 10.0
circle = rg.Circle(center, radius)
a = circle
```

### 示例3: 创建直线
```python
import Rhino.Geometry as rg

start_point = rg.Point3d(0, 0, 0)
end_point = rg.Point3d(10, 10, 10)
line = rg.Line(start_point, end_point)
a = line
```

## 注意事项

1. **端口选择**: 必须使用6895端口
2. **编码**: 使用UTF-8编码，接收时使用utf-8-sig处理BOM
3. **Code格式**: Code必须是字符串，包含完整的Python代码
4. **输出变量**: 需要定义输出变量（如`a`）来返回结果
5. **重复响应**: 服务器会多次返回SCRIPTRESULT，这是正常行为
6. **IO包装**: 服务器会自动包装代码，添加Grasshopper组件的IO信息

## 输入输出端口声明分析

### 声明格式

输入输出端口通过注释在代码顶部声明，格式为：

```python
# GH_COMPONENT_IO_START
# INPUT_PARAMS: {JSON对象}
# OUTPUT_PARAMS: {JSON对象}
# GH_COMPONENT_IO_END
```

### INPUT_PARAMS（输入参数）

**JSON结构**：
```json
{
  "Time": "时间戳",
  "Items": [
    {
      "Name": "Param",
      "Info": "参数信息",
      "Time": "时间戳",
      "Value": {
        "ParamGuid": "参数GUID",
        "InstanceGuid": "实例GUID",
        "Name": "x",
        "NickName": "x",
        "Description": "rhinoscriptsyntax geometry",
        "TypeName": "Generic Data",
        "Optional": true,
        "Access": "item",
        "Mapping": "None",
        "Reverse": false,
        "Simplify": false,
        "Inputs": "[]",
        "Outputs": "[]"
      }
    }
  ]
}
```

**关键字段**：
- `Name`: 参数名称（如"x"、"y"）
- `NickName`: 显示名称
- `Description`: 参数描述
- `TypeName`: 数据类型（如"Generic Data"、"Text"）
- `Optional`: 是否可选
- `Access`: 访问方式（"item"列表项）

### OUTPUT_PARAMS（输出参数）

**JSON结构**：
```json
{
  "Time": "时间戳",
  "Items": [
    {
      "Name": "Param",
      "Info": "参数信息",
      "Time": "时间戳",
      "Value": {
        "ParamGuid": "参数GUID",
        "InstanceGuid": "实例GUID",
        "Name": "out",
        "NickName": "out",
        "Description": "Standard output and error contents collected during script run",
        "TypeName": "Text",
        "Optional": false,
        "Access": "item",
        "Mapping": "None",
        "Reverse": false,
        "Simplify": false,
        "Inputs": "[]",
        "Outputs": "[\"guid\"]"
      }
    }
  ]
}
```

**关键字段**：
- `Name`: 输出变量名（如"out"、"a"）
- `TypeName`: 输出类型
- `Optional`: 输出参数通常不可选

### 特点

1. **自动生成**: 服务器自动根据代码内容生成这些注释
2. **默认参数**: 即使代码中只定义了输出变量，服务器也会自动添加输入参数
3. **固定端口**: `out`端口是固定的，用于捕获脚本输出和错误信息
4. **GUID标识**: 每个参数都有唯一的GUID用于识别
5. **JSON格式**: 参数信息以JSON字符串形式存储在注释中

### 功能说明

可以通过API调整ghscript的输入输出端，但是比较复杂，故而只是提供这个功能。主要作用是通过API查看代码是否符合输入输出需求。

- 查看输入参数配置
- 查看输出参数配置
- 验证代码接口是否符合预期
- 获取参数的GUID和其他元数据

## 测试文件
- `test_script_editor.py` - Python测试脚本
- `test_csharp_script.py` - C#测试脚本