# AI客户端教程 - 连接GrasshopperSever服务

本教程指导AI客户端如何通过TCP协议连接到GrasshopperSever插件，实现与Grasshopper的双向通信。

## 目录

- [服务概述](#服务概述)
- [通信协议](#通信协议)
- [快速开始](#快速开始)
- [Python客户端示例](#python客户端示例)
- [高级功能](#高级功能)
- [常见用例](#常见用例)
- [故障排除](#故障排除)

## 服务概述

GrasshopperSever提供TCP服务，允许外部客户端（如AI程序）与Grasshopper进行双向通信。

### 默认配置

- **默认端口**: 6879
- **通信协议**: TCP
- **数据格式**: JSON
- **编码**: UTF-8

### 连接模式

1. **客户端模式**: AI作为客户端，GHReceiver接收数据，GHSender发送响应
2. **服务模式**: AI作为客户端，连接到GHServer，请求-响应模式

## 通信协议

### Ljson数据结构

所有通信使用Ljson格式，单个数据项包含名称、说明、时间和值：

```json
{
  "Name": "数据名称",
  "Info": "数据说明",
  "Time": "2026-03-22T10:30:00",
  "Value": "数据值"
}
```

**批量数据结构**（用于TCP传输，使用LjsonHelper）：
```json
{
  "Time": "2026-03-22T10:30:00",
  "Items": [
    {
      "Name": "数据名称",
      "Info": "数据说明",
      "Time": "2026-03-22T10:30:00",
      "Value": "数据值"
    }
  ]
}
```

### 通信流程

#### 推送模式（GHReceiver + GHSender）

```
AI客户端                    Grasshopper
    |                            |
    |-------- TCP连接 ---------->|
    |                            |
    |----- 发送Ljson数据 ------>|  GHReceiver接收
    |                            |
    |<----- 发送响应Ljson ------|  GHSender发送
    |                            |
```

#### 请求-响应模式（GHServer）

```
AI客户端                    Grasshopper
    |                            |
    |-------- TCP连接 ---------->|
    |                            |
    |----- 发送请求Ljson ------>|  GHServer接收并执行
    |                            |
    |<----- 返回结果Ljson ------|  返回执行结果
    |                            |
```

## 快速开始

### 1. 准备Grasshopper定义

在Grasshopper中创建以下组件：

**接收端设置**:
1. 添加 `GHReceiver` 组件
2. 设置 `Enabled` = `true`
3. 设置 `Port` = `6879`

**发送端设置**:
1. 添加 `GHSender` 组件
2. 连接 `Client` 输入到GHReceiver的输出
3. 准备要发送的数据

### 2. 基本连接测试

```python
import socket
import json
from datetime import datetime

# 建立TCP连接
def connect_to_gh(host='127.0.0.1', port=6879):
    client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    client.connect((host, port))
    return client

# 发送数据
def send_jlist(client, data_items):
    jlist = {
        "Time": datetime.now().isoformat(),
        "Items": data_items
    }
    message = json.dumps(jlist, ensure_ascii=False)
    client.sendall((message + '\n').encode('utf-8'))

# 接收数据
def receive_jlist(client):
    data = client.recv(4096).decode('utf-8')
    if data:
        return json.loads(data.strip())
    return None

# 使用示例
client = connect_to_gh()

# 发送测试数据
test_data = [
    {
        "Name": "Test",
        "Info": "测试消息",
        "Value": "Hello from AI!"
    }
]
send_jlist(client, test_data)

# 接收响应
response = receive_jlist(client)
print("响应:", response)

client.close()
```

## Python客户端示例

### 完整的客户端类

```python
import socket
import json
import threading
from datetime import datetime
from typing import List, Dict, Optional, Callable

class GrasshopperClient:
    """GrasshopperSever客户端"""
    
    def __init__(self, host='127.0.0.1', port=6879):
        self.host = host
        self.port = port
        self.client: Optional[socket.socket] = None
        self.connected = False
        self.receive_thread: Optional[threading.Thread] = None
        self.receive_callback: Optional[Callable] = None
        
    def connect(self) -> bool:
        """连接到Grasshopper服务器"""
        try:
            self.client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.client.connect((self.host, self.port))
            self.connected = True
            print(f"已连接到Grasshopper服务器 {self.host}:{self.port}")
            return True
        except Exception as e:
            print(f"连接失败: {e}")
            return False
    
    def disconnect(self):
        """断开连接"""
        if self.receive_thread:
            self.receive_thread = None
        if self.client:
            self.client.close()
            self.client = None
        self.connected = False
        print("已断开连接")
    
    def send(self, data_items: List[Dict[str, str]]) -> bool:
        """发送数据到Grasshopper"""
        if not self.connected or not self.client:
            print("未连接到服务器")
            return False
        
        try:
            jlist = {
                "Time": datetime.now().isoformat(),
                "Items": data_items
            }
            message = json.dumps(jlist, ensure_ascii=False)
            self.client.sendall((message + '\n').encode('utf-8'))
            print(f"已发送: {len(data_items)} 个数据项")
            return True
        except Exception as e:
            print(f"发送失败: {e}")
            return False
    
    def receive(self) -> Optional[Dict]:
        """接收来自Grasshopper的数据（阻塞）"""
        if not self.connected or not self.client:
            return None
        
        try:
            data = self.client.recv(4096).decode('utf-8')
            if data:
                return json.loads(data.strip())
        except Exception as e:
            print(f"接收失败: {e}")
        return None
    
    def start_receive_thread(self, callback: Callable[[Dict], None]):
        """启动接收线程，持续接收数据"""
        self.receive_callback = callback
        
        def receive_loop():
            while self.connected and self.client:
                data = self.receive()
                if data and self.receive_callback:
                    self.receive_callback(data)
        
        self.receive_thread = threading.Thread(target=receive_loop, daemon=True)
        self.receive_thread.start()
        print("接收线程已启动")
    
    def __enter__(self):
        self.connect()
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        self.disconnect()
```

### 使用示例

```python
# 示例1: 基本发送和接收
with GrasshopperClient() as gh:
    # 发送数据
    gh.send([
        {"Name": "Point", "Description": "点坐标", "Value": "10,20,30"},
        {"Name": "Radius", "Description": "半径", "Value": "5.0"}
    ])
    
    # 接收响应
    response = gh.receive()
    if response:
        print("收到响应:", response)

# 示例2: 持续接收数据
def on_receive(data):
    print(f"收到数据: {data['Items'][0]['Name']}")
    # 处理接收到的数据
    for item in data['Items']:
        print(f"  - {item['Name']}: {item['Value']}")

gh = GrasshopperClient()
gh.connect()
gh.start_receive_thread(on_receive)

# 继续发送数据...
# gh.send([...])

# 示例3: 发送几何数据
gh.send([
    {
        "Name": "Line",
        "Info": "线段",
        "Value": json.dumps({
            "Start": [0, 0, 0],
            "End": [10, 10, 10]
        })
    },
    {
        "Name": "Circle",
        "Info": "圆",
        "Value": json.dumps({
            "Center": [5, 5, 0],
            "Radius": 3.0
        })
    }
])
```

## 高级功能

### 1. 组件查询

AI可以查询Grasshopper中的组件信息：

```python
def query_components(gh: GrasshopperClient, component_name: str):
    """查询指定组件"""
    gh.send([
        {
            "Name": "Query",
            "Info": "组件查询",
            "Value": component_name
        }
    ])
    
    response = gh.receive()
    if response:
        for item in response['Items']:
            print(f"{item['Name']}: {item['Value']}")

# 使用示例
with GrasshopperClient() as gh:
    query_components(gh, "Circle")
```

### 2. 参数控制

控制Grasshopper参数：

```python
def set_parameter(gh: GrasshopperClient, param_name: str, value: str):
    """设置参数值"""
    gh.send([
        {
            "Name": "SetParameter",
            "Info": "设置参数",
            "Value": json.dumps({
                "name": param_name,
                "value": value
            })
        }
    ])

# 使用示例
with GrasshopperClient() as gh:
    set_parameter(gh, "radius", "10.5")
    set_parameter(gh, "segments", "32")
```

### 3. 几何操作

发送几何数据到Grasshopper：

```python
import numpy as np

def send_curve(gh: GrasshopperClient, points: List[List[float]]):
    """发送曲线数据"""
    gh.send([
        {
            "Name": "Curve",
            "Info": "控制点曲线",
            "Value": json.dumps({
                "type": "NurbsCurve",
                "points": points,
                "degree": 3
            })
        }
    ])

def send_surface(gh: GrasshopperClient, u_points: int, v_points: int):
    """生成并发送曲面"""
    # 生成网格点
    u = np.linspace(0, 1, u_points)
    v = np.linspace(0, 1, v_points)
    U, V = np.meshgrid(u, v)
    
    # 创建曲面点
    points = []
    for i in range(u_points):
        for j in range(v_points):
            points.append([U[i, j], V[i, j], np.sin(U[i, j]) * np.cos(V[i, j])])
    
    gh.send([
        {
            "Name": "Surface",
            "Info": "参数化曲面",
            "Value": json.dumps({
                "type": "GridSurface",
                "u_count": u_points,
                "v_count": v_points,
                "points": points
            })
        }
    ])

# 使用示例
with GrasshopperClient() as gh:
    # 发送曲线
    curve_points = [[0, 0, 0], [1, 1, 0], [2, 0, 1], [3, 1, 0]]
    send_curve(gh, curve_points)
    
    # 发送曲面
    send_surface(gh, 10, 10)
```

### 4. 批量操作

批量发送多个数据项：

```python
def batch_send(gh: GrasshopperClient, operations: List[Dict]):
    """批量发送操作"""
    items = []
    for op in operations:
        items.append({
            "Name": op.get("name", "Operation"),
            "Info": op.get("description", ""),
            "Value": json.dumps(op.get("data", {}))
        })
    
    gh.send(items)

# 使用示例
operations = [
    {
        "name": "CreatePoint",
        "description": "创建点",
        "data": {"x": 10, "y": 20, "z": 0}
    },
    {
        "name": "CreateCircle",
        "description": "创建圆",
        "data": {"center": [10, 20, 0], "radius": 5}
    },
    {
        "name": "Extrude",
        "description": "拉伸",
        "data": {"distance": 10}
    }
]

with GrasshopperClient() as gh:
    batch_send(gh, operations)
```

## 常见用例

### 用例1: AI生成设计

```python
import random

def generate_design(gh: GrasshopperClient):
    """AI生成随机设计"""
    # 生成随机参数
    radius = random.uniform(5, 20)
    height = random.uniform(10, 50)
    segments = random.randint(8, 32)
    
    # 发送到Grasshopper
    gh.send([
        {"Name": "Radius", "Info": "半径", "Value": str(radius)},
        {"Name": "Height", "Info": "高度", "Value": str(height)},
        {"Name": "Segments", "Info": "分段数", "Value": str(segments)}
    ])
    
    # 等待Grasshopper生成结果
    response = gh.receive()
    return response

# 使用示例
with GrasshopperClient() as gh:
    for i in range(10):
        result = generate_design(gh)
        print(f"生成方案 {i+1}: {result}")
```

### 用例2: 参数优化

```python
def optimize_parameter(gh: GrasshopperClient, param_name: str, target_value: float):
    """优化参数以接近目标值"""
    current_value = 0
    step = 1.0
    
    for iteration in range(100):
        # 发送当前参数值
        gh.send([
            {
                "Name": param_name,
                "Info": "优化参数",
                "Value": str(current_value)
            }
        ])
        
        # 接收结果
        response = gh.receive()
        if not response:
            break
        
        # 获取计算结果
        result_value = float(response['Items'][0]['Value'])
        
        # 调整参数
        error = target_value - result_value
        if abs(error) < 0.01:
            print(f"优化完成: {param_name} = {current_value}")
            return current_value
        
        current_value += error * step
    
    print("优化未收敛")
    return current_value

# 使用示例
with GrasshopperClient() as gh:
    optimize_parameter(gh, "radius", 15.0)
```

### 用例3: 实时交互

```python
import time

def interactive_mode(gh: GrasshopperClient):
    """交互式控制模式"""
    print("进入交互模式 (输入 'quit' 退出)")
    
    while True:
        # 获取用户输入
        command = input("> ").strip()
        
        if command == 'quit':
            break
        
        if command == 'radius':
            radius = input("输入半径: ")
            gh.send([
                {"Name": "Radius", "Info": "半径", "Value": radius}
            ])
        
        elif command == 'height':
            height = input("输入高度: ")
            gh.send([
                {"Name": "Height", "Info": "高度", "Value": height}
            ])
        
        elif command == 'random':
            gh.send([
                {"Name": "Random", "Info": "随机生成", "Value": "true"}
            ])
        
        # 接收响应
        response = gh.receive()
        if response:
            print("响应:", response['Items'][0]['Value'])

# 使用示例
with GrasshopperClient() as gh:
    interactive_mode(gh)
```

### 用例4: 数据可视化

```python
import matplotlib.pyplot as plt

def visualize_data(gh: GrasshopperClient, data_points: List[List[float]]):
    """发送数据并可视化结果"""
    # 发送数据点
    gh.send([
        {
            "Name": "DataPoints",
            "Info": "数据点",
            "Value": json.dumps(data_points)
        }
    ])
    
    # 接收处理结果
    response = gh.receive()
    if response:
        # 解析结果
        for item in response['Items']:
            if item['Name'] == 'ProcessedData':
                points = json.loads(item['Value'])
                
                # 可视化
                plt.figure(figsize=(10, 8))
                plt.scatter(*zip(*points))
                plt.title("Grasshopper处理结果")
                plt.xlabel("X")
                plt.ylabel("Y")
                plt.grid(True)
                plt.show()

# 使用示例
with GrasshopperClient() as gh:
    data = [[i, i**2] for i in range(10)]
    visualize_data(gh, data)
```

## 故障排除

### 常见问题

#### 1. 连接失败

**问题**: 无法连接到Grasshopper服务器

**解决方案**:
- 确认Grasshopper正在运行
- 检查GHReceiver的 `Enabled` 是否为 `true`
- 验证端口号是否正确（默认6879）
- 检查防火墙设置

```python
# 测试连接
def test_connection(host='127.0.0.1', port=6879):
    try:
        test_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        test_socket.settimeout(5)
        test_socket.connect((host, port))
        test_socket.close()
        print(f"端口 {port} 可访问")
        return True
    except Exception as e:
        print(f"端口 {port} 不可访问: {e}")
        return False

test_connection()
```

#### 2. 数据格式错误

**问题**: 发送的数据无法被正确解析

**解决方案**:
- 确保JSON格式正确
- 检查Time字段格式
- 验证Items数组结构

```python
def validate_jlist(data):
    """验证Ljson数据格式"""
    if not isinstance(data, dict):
        return False
    if "Time" not in data or "Items" not in data:
        return False
    if not isinstance(data["Items"], list):
        return False
    for item in data["Items"]:
        if not all(key in item for key in ["Name", "Info", "Time", "Value"]):
            return False
    return True

# 使用
jlist = {
    "Time": datetime.now().isoformat(),
    "Items": [...]
}
if validate_jlist(jlist):
    print("数据格式正确")
```

#### 3. 接收超时

**问题**: 等待响应超时

**解决方案**:
- 添加超时处理
- 检查Grasshopper定义是否正确计算
- 确认GHSender已连接到GHReceiver

```python
def receive_with_timeout(gh: GrasshopperClient, timeout=5.0):
    """带超时的接收"""
    gh.client.settimeout(timeout)
    try:
        return gh.receive()
    except socket.timeout:
        print("接收超时")
        return None
    finally:
        gh.client.settimeout(None)  # 重置超时
```

#### 4. 编码问题

**问题**: 中文字符显示乱码

**解决方案**:
- 确保使用UTF-8编码
- 在JSON序列化时使用 `ensure_ascii=False`

```python
# 正确的中文处理
message = json.dumps(jlist, ensure_ascii=False)
encoded_message = message.encode('utf-8')

# 解码时指定编码
decoded_message = data.decode('utf-8')
```

### 调试技巧

#### 启用详细日志

```python
import logging

logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(levelname)s - %(message)s'
)

class DebugGrasshopperClient(GrasshopperClient):
    def send(self, data_items):
        logging.debug(f"发送数据: {data_items}")
        result = super().send(data_items)
        logging.debug(f"发送结果: {result}")
        return result
    
    def receive(self):
        data = super().receive()
        logging.debug(f"接收数据: {data}")
        return data
```

#### 数据包监控

```python
def monitor_traffic(gh: GrasshopperClient):
    """监控网络流量"""
    sent_count = 0
    received_count = 0
    
    def wrapped_send(data_items):
        nonlocal sent_count
        sent_count += 1
        print(f"[发送 #{sent_count}] {len(data_items)} 项")
        return gh.send(data_items)
    
    def wrapped_receive():
        nonlocal received_count
        data = gh.receive()
        if data:
            received_count += 1
            print(f"[接收 #{received_count}] {len(data['Items'])} 项")
        return data
    
    gh.send = wrapped_send
    gh.receive = wrapped_receive
```

## 最佳实践

1. **错误处理**: 始终包含适当的错误处理
2. **资源管理**: 使用上下文管理器确保连接正确关闭
3. **超时设置**: 为所有网络操作设置合理的超时
4. **数据验证**: 在发送前验证数据格式
5. **日志记录**: 记录所有通信以便调试
6. **重连机制**: 实现自动重连以应对连接丢失

## 扩展阅读

- [GrasshopperSever主文档](./README.md)
- [Ljson数据结构详解](./design.md)
- [Grasshopper API文档](https://developer.rhino3d.com/guides/grasshopper/)

## 支持

如有问题，请查阅故障排除部分或联系插件开发者。