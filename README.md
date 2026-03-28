# GrasshopperSever

一个用于Rhino Grasshopper的插件，提供TCP通信、数据转换和组件信息查询功能。

中文 | [English](README_EN.md)

## 项目信息

- **版本**: 1.0
- **支持的框架**: .NET Framework 4.8, .NET 7.0, .NET 7.0-windows
- **插件GUID**: 0171a275-7e22-4b2a-9f82-b80f07a08b08

## 功能概述

GrasshopperSever插件为Grasshopper提供了以下核心功能：

1. **数据通信**: 通过TCP协议接收和发送数据
2. **数据转换**: JSON与Ljson格式互相转换
3. **组件信息查询**: 查询和搜索Grasshopper组件信息
4. **数据执行**: 执行接收到的数据命令

## 核心数据结构

### Ljson

统一的数据结构，用于表示单个数据项，包含名称、说明、时间和值。

- **Name**: 数据名称
- **Info**: 数据说明
- **Time**: 创建时间，用于标识数据版本
- **Value**: 数据值（JsonElement，可以是对象、数组或原始值）

**特性**:
- 支持JSON序列化和反序列化
- 支持深度克隆
- 实现IDisposable接口
- 支持参数的获取、搜索和设置（支持对象和数组格式）
- 提供静态方法创建常用类型的Ljson（错误、成功、组件信息等）

**LjsonHelper工具类**:
- `SerializeLjsonArray`: 序列化Ljson数组为JSON字符串
- `ParseLjsonArray`: 从JSON字符串反序列化为Ljson数组

## 组件说明

### 数据通信组件

#### GHReceiver

根据端口创建TCP连接并接收数据，每个端口只接受一个连接。

**输入参数**:
- `Enabled` (Boolean): 是否启用服务器，默认为 false
- `Port` (Integer): 监听的端口，默认为 6879

**输出参数**:
- `Client` (TcpClientParam): Client连接对象
- `Ljson` (LjsonParam): 传入的数据

**特性**:
- 在后台线程接收数据
- 通过 `RhinoApp.InvokeOnUiThread` 通知GH电池刷新
- 只接收比上次更新的数据（基于time标签）

#### GHSender

使用TCP连接发送数据，支持批量发送。

**输入参数**:
- `Client` (TcpClientParam): Client连接对象
- `Ljson` (LjsonParam): 发送数据，按顺序发送

**输出参数**:
- `Result` (String): 执行结果，用于显示报错或报告

**特性**:
- 只有Ljson.time更新时才会触发发送
- 自动过滤过期数据

#### GHServer

根据端口创建TCP服务器并接收数据，接收到数据后在内部执行并作出响应。

**输入参数**:
- `Enabled` (Boolean): 是否启用服务器，默认为 false
- `Port` (Integer): 监听的端口，默认为 6879

**输出参数**:
- `Result` (String): 执行结果，用于显示报错或报告

### 数据转换组件

#### Json2Ljson

将JSON格式转换为Ljson。

**输入参数**:
- `String` (String): JSON格式字符串

**输出参数**:
- `Ljson` (LjsonParam): 生成的Ljson对象

#### Ljson2Json

将Ljson转换为JSON格式。

**输入参数**:
- `Ljson` (LjsonParam): 需要转换的Ljson对象

**输出参数**:
- `String` (String): JSON格式字符串

#### DataTreeLjson

将Data Tree转换为Ljson。

**输入参数**:

- `String Tree` (GH_Structure\<string\>): String Tree结构

**输出参数**:
- `Ljson` (LjsonParam): 生成的Ljson对象

**特性**:
- 每个branch只取前三项
- 非string格式转为string
- 项目不足则使用空值补齐

### 信息查询组件

#### AllComponents

输出所有注册的组件信息。

**输入参数**:
- `Refresh` (Boolean): 刷新，值改变就刷新一次time

**输出参数**:
- `Ljson` (LjsonParam): 所有组件的信息

**输出结构** (Ljson.Value):
```json
{
  "categorys": "所有分类",
  "count": "组件数量",
  "components": "所有注册的组件"
}
```

#### FindComponentsByGuid

通过GUID查询组件信息。

**输入参数**:
- `Guid` (String): 组件的GUID

**输出参数**:
- `ComponentInfo` (LjsonParam): 组件信息

**输出结构** (Ljson.Value):
```json
{
  "ComponentGuid": "组件GUID",
  "InstanceGuid": "实例GUID",
  "ComponentName": "组件名称",
  "NickName": "组件昵称",
  "Description": "组件描述",
  "Category": "主分类",
  "SubCategory": "子分类",
  "Position": "位置信息",
  "State": "状态信息",
  "Inputs": "输入端信息",
  "Outputs": "输出端信息"
}
```

#### FindComponentsByName

通过名称查询组件信息。

**输入参数**:
- `Name` (String): 组件名称

**输出参数**:
- `ComponentInfo` (LjsonParam): 组件信息

#### FindComponentsByCategory

通过Category查询组件信息。

**输入参数**:
- `Category` (String): 主分类名称

**输出参数**:
- `ComponentInfo` (LjsonParam): 组件信息

#### SearchComponentsByName

通过名称搜索组件，支持模糊匹配。

**输入参数**:
- `Keyword` (String): 搜索关键词

**输出参数**:
- `ComponentInfo` (LjsonParam): 组件信息列表

#### ComponentConnector

通过连接输入端，获取连接的组件的信息。

**输入参数**:

- Input: 连接一个组件

**输出参数**:

- `Name` (string): 名称
- `GUID` (string): 组件的GUID
- `Instance` (string): 组件实例的GUID

### 执行组件

#### GHActuator

对输入的数据进行执行。

**输入参数**:
- `Ljson` (LjsonParam): 需要执行的数据

**输出参数**:
- `Result` (String): 执行结果，用于显示报错或报告

#### ScriptEditor

通过输入的代码修改Script组件，支持c#、python。

**输入参数**:

- `ScriptComponent` : 连接一个脚本组件
- `Code` (String): 需要添加到脚本

**输出参数**:

- `Result` (String): 执行结果，用于显示报错或报告

## 数据库功能

插件使用SQLite数据库存储元数据，数据库文件位于插件目录下（`GrasshopperSever.db`）。

### DatabaseManager

提供以下功能：

- 自动初始化数据库
- 创建和管理数据表
- 跟踪表的更新时间
- 提供数据库连接对象
- 执行带时间戳更新的SQL命令

### MetaInfo表

用于跟踪表的更新时间，包含以下字段：

- `Id`: 主键
- `TableName`: 表名
- `LastUpdateTime`: 最后更新时间
- `Description`: 表描述

## 参数类型

### LjsonParam

用于在Grasshopper电池之间传递Ljson数据的参数类型。

### TcpClientParam

用于传递TCP客户端连接对象的参数类型，由GHReceiver根据端口唯一创建。

## 构建和安装

### 构建要求

- .NET Framework 4.8 或 .NET 7.0 SDK
- Grasshopper 8.29.26063.11001 或更高版本

### 构建步骤

1. 使用Visual Studio打开 `GrasshopperSever.sln`
2. 选择目标框架（net4.8, net7.0, 或 net7.0-windows）
3. 构建解决方案

### 安装

1. 将构建生成的 `.gha` 文件复制到Grasshopper组件目录
2. 重启Rhino/Grasshopper
3. 插件将自动加载

## 使用示例

### TCP通信示例

1. 创建一个 `GHReceiver` 组件，设置端口号（例如6879）
2. 将 `Enabled` 设置为 `true` 启动接收器
3. 通过TCP客户端发送JSON数据到指定端口
4. 数据将被接收并转换为Ljson格式输出

### 组件查询示例

1. 使用 `AllComponents` 获取所有组件列表
2. 使用 `FindComponentsByName` 查找特定组件
3. 使用 `SearchComponentsByName` 进行模糊搜索

### 数据转换示例

1. 创建 `Json2Ljson` 组件
2. 输入JSON字符串
3. 获取转换后的Ljson对象

## 注意事项

1. 每个端口只能创建一个TCP接收器
2. Ljson的time标签用于版本控制，只接收/发送更新的数据
3. 数据库文件位于插件目录，确保有写入权限
4. TCP通信使用UTF-8编码
5. 建议使用防火墙规则保护TCP端口

## 依赖项

- Grasshopper 8.29.26063.11001
- Microsoft.Data.Sqlite 10.0.5
- System.Data.SQLite 1.0.119
- System.Text.Json 10.0.5（仅net4.8）
- System.Resources.Extensions 10.0.5

## 许可证

请查看项目许可证文件。

## 贡献

欢迎提交问题和拉取请求。

## 联系方式

如有问题或建议，请联系插件作者。

## 相关文档

- [English Documentation](README_EN.md) - 英文版文档
- [AI客户端教程](AI_CLIENT_TUTORIAL.md) - AI客户端连接和交互指南
- [插件开发文档](插件开发.md) - 插件开发技术文档