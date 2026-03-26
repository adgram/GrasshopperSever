### 算法

##### Ljson

统一的数据结构，表示单个数据项，包含名称、说明、时间和值。

```c#
public Ljson()
{
    Time = DateTime.Now;           // 每个数据创建时自动生成，用于检查数据是否被使用过
}

public Ljson(string name, string info, JsonElement value)
{
    Name = name;                   // 数据名称
    Info = info;                   // 数据说明
    Value = value;                 // 数据值（JsonElement，可以是对象、数组或原始值）
    Time = DateTime.Now;
}
```

**特性**:
- 支持JSON序列化和反序列化
- 支持深度克隆
- 实现IDisposable接口
- 支持参数的获取、搜索和设置（支持对象和数组格式）
- 提供静态方法创建常用类型的Ljson（错误、成功、组件信息等）

##### LjsonHelper

静态工具类，用于Ljson的批量操作。

```c#
// 序列化Ljson数组为JSON字符串
public static string SerializeLjsonArray(List<Ljson> ljsons)

// 从JSON字符串反序列化为Ljson数组
public static List<Ljson> ParseLjsonArray(string json)
```

##### ComponentLjson

表示组件的基本信息



### 基本数据与通信

##### LjsonParam

用于在Grasshopper电池之间传递Ljson数据的参数类型。

- 默认为空；

##### Json2Ljson

将Json转换为Ljson。

```c#
//输入
pManager.AddTextParameter("String", "S", "将Json格式转换为Ljson", GH_ParamAccess.item);
```

```c#
// 输出
pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "生成的Ljson", GH_ParamAccess.item);
```

##### StringTreeLjson

将string tree转换为Ljson。

```c#
//输入
pManager.AddTextParameter("String Tree", "ST", "将string tree转换为Ljson", GH_ParamAccess.tree);
// string tree每个branch只取前三项，非string格式转为string，项目不足则使用空值补齐。
```

```c#
// 输出
pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "生成的Ljson", GH_ParamAccess.item);
```

##### Ljson2Json

将Ljson转换为Json。

```c#
//输入
pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "需要转换的Ljson", GH_ParamAccess.item);
```

```c#
// 输出
pManager.AddTextParameter("String", "S", "Json格式", GH_ParamAccess.item);
```

##### TcpClientParam

一个`System.Net.Sockets.TcpClient`连接，用于接收和传输数据。该对象由GHReceiver根据端口唯一创建。

- 默认为空。

##### GHReceiver

根据端口创建TcpClient并接收数据，并且每个端口只接受一个连接。在后台线程（Task/Thread）接收数据，然后通过 `RhinoApp.InvokeOnUiThread` 告知 GH 电池进行 `ExpireSolution(true)` 刷新。

```c#
// 输入
pManager.AddBooleanParameter("Enabled", "E", "是否启用服务器", GH_ParamAccess.item, false);
pManager.AddIntegerParameter("Port", "P", "监听的端口", GH_ParamAccess.item, 6879);
```

```c#
// 输出
pManager.AddParameter(new TcpClientParam(), "Client", "CL", "Client连接", GH_ParamAccess.item);
pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "传入的数据", GH_ParamAccess.item);
```

##### GHSender

将需要传出的数据，使用连接进行发送。Ljson.time更新，会触发新发送，未更新则不触发。

```c#
// 输入
pManager.AddParameter(new TcpClientParam(), "Client", "CL", "Client连接", GH_ParamAccess.item);
pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "发送数据，按顺序发送", GH_ParamAccess.item);
```

```c#
// 输出
pManager.AddTextParameter("Result", "RS", "执行结果，用于显示报错或者报告", GH_ParamAccess.item);
```

##### GHServer

根据端口创建TcpClient并接收数据，并且每个端口只接受一个连接。接收到数据后在内部执行并作出响应。

```c#
// 输入
pManager.AddBooleanParameter("Enabled", "E", "是否启用服务器", GH_ParamAccess.item, false);
pManager.AddIntegerParameter("Port", "P", "监听的端口", GH_ParamAccess.item, 6879);
```

```c#
// 输出
pManager.AddTextParameter("Result", "RS", "执行结果，用于显示报错或者报告", GH_ParamAccess.item);
```

##### GHActuator

对输入的数据进行执行。

```c#
// 输入
pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "需要执行的数据", GH_ParamAccess.item);
```

```c#
// 输出
pManager.AddTextParameter("Result", "RS", "执行结果，用于显示报错或者报告", GH_ParamAccess.item);
```

##### ScriptEditor

通过输入的代码修改Script组件，支持c#、python。

```c#
// 输入
// ScriptComponent: 连接一个脚本组件
pManager.AddTextParameter("Code", "C", "需要添加到脚本的代码", GH_ParamAccess.item);
```

```c#
// 输出
pManager.AddTextParameter("Result", "RS", "执行结果，用于显示报错或者报告", GH_ParamAccess.item);
```

### 信息查询

##### AllComponents

输出所有注册的组件。

```c#
// 输入
pManager.AddBooleanParameter("Refresh", "R", "刷新，值改变就刷新一次time", GH_ParamAccess.item, false);
```

```c#
// 输出
pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "所有组件的信息", GH_ParamAccess.item);
```

- 输出结构：

```c#
// Ljson.Value为对象格式
{
  "categorys": "所有分类",
  "count": "组件数量",
  "components": "所有注册的组件"
}
```

##### FindComponentsByGuid

通过Guid查询组件信息。

```c#
// 输出
pManager.AddParameter(new LjsonParam(), "ComponentInfo", "C", "组件信息", GH_ParamAccess.item);
```

- 输出结构`ComponentLjson`（Ljson.Value为对象格式）：

```c#
{
  "ComponentGuid": "组件 GUID",
  "InstanceGuid": "实例 GUID",
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

##### FindComponentsByName

通过名称查询组件信息。输出结构`ComponentLjson`。

##### FindComponentsByCategory

通过Category查询组件信息。输出结构`ComponentLjson`。

##### SearchComponentsByName

通过名称搜索组件，可以模糊匹配。输出结构`List<ComponentLjson>`。

##### ComponentConnector

通过连接输入端，获取连接的组件的信息。

```c#
// 输入
pManager.AddBooleanParameter("Refresh", "R", "刷新输出", GH_ParamAccess.item, false);
// Input: 连接一个组件
```

```c#
// 输出
pManager.AddTextParameter("Name", "N", "名称", GH_ParamAccess.item);
pManager.AddTextParameter("GUID", "G", "组件的GUID", GH_ParamAccess.item);
pManager.AddTextParameter("Instance", "I", "组件实例的GUID", GH_ParamAccess.item);
```

### 计划

- 增加help

- 再次尝试静默运行脚本。
- 序列化xml
