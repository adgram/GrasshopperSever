# AI客户端教程 - 连接GrasshopperSever服务

本教程指导AI客户端如何通过TCP协议连接到GrasshopperSever插件，实现与Grasshopper的双向通信。

## ⚠️ 重要更新说明（2026-03-26）

根据实际测试结果，本教程已进行以下重要修正：

1. **数据格式**: 必须使用单个LJSON对象格式，**不要使用批量格式（Items数组）**
2. **命令格式**: 发送命令时，Name字段应为命令类型（COMPONENT/DOCUMENT/RHINO等），Value.Command为具体命令名称
3. **响应处理**: 服务器可能返回多条消息，需要正确处理UTF-8 BOM标记
4. **解码方式**: 接收数据时使用 `utf-8-sig` 编码以处理BOM

**正确的数据格式**:
```json
{
  "Name": "DOCUMENT",      // 命令类型
  "Info": "获取数据库路径",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "DATABASEPATH"  // 具体命令
  }
}
```

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

所有通信使用Ljson格式，**必须使用单个LJSON对象**（不是批量格式）：

```json
{
  "Name": "数据名称或命令类型",
  "Info": "数据说明",
  "Time": "2026-03-22T10:30:00",
  "Value": "数据值"
}
```

**重要说明**：
- 发送数据必须使用单个LJSON对象格式
- **不要使用**批量格式（Items数组）
- 发送命令时，Name字段应为命令类型（COMPONENT/DOCUMENT/RHINO等）
- Value.Command字段存放具体命令名称

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

# 发送单个LJSON数据
def send_ljson(client, name, info, value):
    data = {
        "Name": name,
        "Info": info,
        "Time": datetime.now().isoformat(),
        "Value": value
    }
    message = json.dumps(data, ensure_ascii=False)
    client.sendall((message + '\n').encode('utf-8'))

# 接收数据（处理UTF-8 BOM和多个消息）
def receive_messages(client):
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

    if total_response:
        # 使用utf-8-sig解码以处理BOM
        response = total_response.decode('utf-8-sig')
        # 分割多个消息（用BOM分隔）
        messages = [msg for msg in response.split('\ufeff') if msg.strip()]
        return [json.loads(msg.strip()) for msg in messages]
    return []

# 使用示例
client = connect_to_gh()

# 发送测试数据（单个LJSON）
send_ljson(client, "TestMessage", "测试消息", "Hello from AI!")

# 接收响应
responses = receive_messages(client)
for response in responses:
    print(f"响应: {response['Name']} - {response['Value']}")

client.close()
```

## Python客户端示例

### 完整的客户端类

```python
import socket
import json
import threading
from datetime import datetime
from typing import Optional, Callable, List, Dict, Any

class GrasshopperClient:
    """GrasshopperSever客户端"""

    def __init__(self, host='127.0.0.1', port=6879):
        self.host = host
        self.port = port
        self.client: Optional[socket.socket] = None
        self.connected = False
        self.receive_thread: Optional[threading.Thread] = None
        self.receive_callback: Optional[Callable[[Dict], None]] = None

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

    def send(self, name: str, info: str, value: Any) -> bool:
        """发送单个LJSON数据到Grasshopper"""
        if not self.connected or not self.client:
            print("未连接到服务器")
            return False

        try:
            data = {
                "Name": name,
                "Info": info,
                "Time": datetime.now().isoformat(),
                "Value": value
            }
            message = json.dumps(data, ensure_ascii=False)
            self.client.sendall((message + '\n').encode('utf-8'))
            return True
        except Exception as e:
            print(f"发送失败: {e}")
            return False

    def send_command(self, ljson_type: str, command_name: str, params: Dict) -> bool:
        """发送命令到Grasshopper

        Args:
            ljson_type: 命令类型 (COMPONENT/DOCUMENT/RHINO/SCRIPT/DESIGN)
            command_name: 具体命令名称
            params: 命令参数
        """
        return self.send(ljson_type, f"执行{command_name}", {
            "Command": command_name,
            **params
        })

    def receive(self) -> List[Dict]:
        """接收来自Grasshopper的数据（阻塞）"""
        if not self.connected or not self.client:
            return []

        try:
            self.client.settimeout(10)
            total_response = b''
            while True:
                try:
                    chunk = self.client.recv(8192)
                    if not chunk:
                        break
                    total_response += chunk
                except socket.timeout:
                    break

            if total_response:
                # 使用utf-8-sig解码以处理BOM
                response = total_response.decode('utf-8-sig')
                # 分割多个消息
                messages = [msg for msg in response.split('\ufeff') if msg.strip()]
                return [json.loads(msg.strip()) for msg in messages]
        except Exception as e:
            print(f"接收失败: {e}")
        return []

    def start_receive_thread(self, callback: Callable[[Dict], None]):
        """启动接收线程，持续接收数据"""
        self.receive_callback = callback

        def receive_loop():
            while self.connected and self.client:
                responses = self.receive()
                for response in responses:
                    if self.receive_callback:
                        self.receive_callback(response)

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
    # 发送数据（单个LJSON）
    gh.send("Point", "点坐标", {"x": 10, "y": 20, "z": 30})

    # 接收响应
    responses = gh.receive()
    for response in responses:
        print(f"收到响应: {response['Name']} - {response['Value']}")

# 示例2: 持续接收数据
def on_receive(data):
    print(f"收到数据: {data['Name']}")
    # 处理接收到的数据
    print(f"  值: {data['Value']}")

gh = GrasshopperClient()
gh.connect()
gh.start_receive_thread(on_receive)

# 继续发送数据...
gh.send("Radius", "半径", 5.0)
gh.send("Height", "高度", 10.0)

# 示例3: 发送几何数据
with GrasshopperClient() as gh:
    # 发送线段
    gh.send("Line", "线段", {
        "type": "Line",
        "start": [0, 0, 0],
        "end": [10, 10, 10]
    })

    # 发送圆
    gh.send("Circle", "圆", {
        "type": "Circle",
        "center": [5, 5, 0],
        "radius": 3.0
    })
```

## 高级功能

### 1. 发送命令

AI可以发送各种命令到GrasshopperSever：

```python
# 获取数据库路径
with GrasshopperClient() as gh:
    gh.send_command("DOCUMENT", "DATABASEPATH", {})
    responses = gh.receive()
    for response in responses:
        if response['Name'] == 'DatabasePath':
            db_path = response['Value']['DatabasePath']
            print(f"数据库路径: {db_path}")

# 搜索组件
with GrasshopperClient() as gh:
    gh.send_command("COMPONENT", "SEARCHCOMPONENTSBYNAME", {"Name": "Circle"})
    responses = gh.receive()
    for response in responses:
        if response['Name'] == 'SearchComponentsByName':
            count = response['Value']['Count']
            print(f"找到 {count} 个Circle组件")

# 获取最后创建的Rhino对象
with GrasshopperClient() as gh:
    gh.send_command("RHINO", "GETLASTCREATEDOBJECTS", {})
    responses = gh.receive()
    for response in responses:
        if response['Name'] == 'GetLastCreatedObjects':
            print(f"最后创建的对象: {response['Value']}")
```

### 2. 批量操作

批量发送多个数据（通过多次send调用）：

```python
def batch_send(gh: GrasshopperClient, operations: List[Dict]):
    """批量发送操作"""
    for op in operations:
        gh.send(
            op.get("name", "Operation"),
            op.get("description", ""),
            op.get("data", {})
        )

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
- 确保使用单个LJSON对象格式（不是Items数组）
- 检查Name、Info、Time、Value字段是否存在
- 发送命令时使用正确的格式（Name=命令类型，Value.Command=具体命令）

```python
def validate_ljson(data):
    """验证Ljson数据格式"""
    if not isinstance(data, dict):
        return False
    # 单个LJSON对象必须有这4个字段
    required_fields = ["Name", "Info", "Time", "Value"]
    if not all(key in data for key in required_fields):
        return False
    return True

# 使用示例
data = {
    "Name": "TestMessage",
    "Info": "测试消息",
    "Time": datetime.now().isoformat(),
    "Value": "Hello"
}
if validate_ljson(data):
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
- **重要**: 解码响应时使用 `utf-8-sig` 以处理UTF-8 BOM标记

```python
# 正确的中文处理（发送）
message = json.dumps(data, ensure_ascii=False)
encoded_message = message.encode('utf-8')

# 正确的解码（接收）- 使用utf-8-sig处理BOM
decoded_message = data.decode('utf-8-sig')

# 示例
total_response = b''
while True:
    chunk = client.recv(4096)
    if not chunk:
        break
    total_response += chunk

# 使用utf-8-sig解码
response = total_response.decode('utf-8-sig')
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
    def send(self, name, info, value):
        logging.debug(f"发送数据: Name={name}, Info={info}, Value={value}")
        result = super().send(name, info, value)
        logging.debug(f"发送结果: {result}")
        return result

    def receive(self):
        data = super().receive()
        logging.debug(f"接收数据: {len(data)} 条消息")
        for i, msg in enumerate(data):
            logging.debug(f"  消息{i}: {msg['Name']}")
        return data
```

#### 数据包监控

```python
def monitor_traffic(gh: GrasshopperClient):
    """监控网络流量"""
    sent_count = 0
    received_count = 0

    def wrapped_send(name, info, value):
        nonlocal sent_count
        sent_count += 1
        print(f"[发送 #{sent_count}] Name={name}")
        return gh.send(name, info, value)

    def wrapped_receive():
        nonlocal received_count
        data = gh.receive()
        if data:
            received_count += 1
            print(f"[接收 #{received_count}] {len(data)} 条消息")
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