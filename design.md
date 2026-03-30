### 组件开发列表

##### Ljson

统一的数据结构，表示单个数据项，包含名称、说明、时间和值。

```c#
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

```c#

/// <summary>
/// 创建组件信息Ljson
/// </summary>
public static Ljson ComponentLjson(string componentGuid, string instanceGuid,
                                   string name, string nickName, string description,
                                   string category, string subCategory, string position,
                                   string state, string inputs, string outputs)
{
    var data = new Dictionary<string, JsonElement>
    {
        { "ComponentGuid", JsonSerializer.SerializeToElement(componentGuid) },
        { "InstanceGuid", JsonSerializer.SerializeToElement(instanceGuid) },
        { "ComponentName", JsonSerializer.SerializeToElement(name) },
        { "NickName", JsonSerializer.SerializeToElement(nickName) },
        { "Description", JsonSerializer.SerializeToElement(description) },
        { "Category", JsonSerializer.SerializeToElement(category) },
        { "SubCategory", JsonSerializer.SerializeToElement(subCategory) },
        { "Position", JsonSerializer.SerializeToElement(position) },
        { "State", JsonSerializer.SerializeToElement(state) },
        { "Inputs", JsonSerializer.SerializeToElement(inputs) },
        { "Outputs", JsonSerializer.SerializeToElement(outputs) }
    };

    return new Ljson("Component", "组件信息", JsonSerializer.SerializeToElement(data));
}

/// <summary>
/// 创建组件Param信息Ljson
/// </summary>
public static Ljson ParamLjson(string paramGuid, string instanceGuid,
                               string name, string nickName, string description,
                               string typeName, bool optional, GH_ParamAccess access,
                               GH_DataMapping mapping, bool reverse, bool simplify,
                               string inputs, string outputs)
{
    var data = new Dictionary<string, object>
    {
        { "ParamGuid", paramGuid },
        { "InstanceGuid", instanceGuid },
        { "Name", name },
        { "NickName", nickName },
        { "Description", description },
        { "TypeName", typeName },
        { "Optional", optional },
        { "Access", access.ToString() },
        { "Mapping", mapping.ToString() },
        { "Reverse", reverse },
        { "Simplify", simplify },
        { "Inputs", inputs },
        { "Outputs", outputs }
    };

    return new Ljson("Param", "参数信息", JsonSerializer.SerializeToElement(data));
}
```

##### LjsonHelper

静态工具类，用于Ljson的批量操作。

```c#
// 序列化Ljson数组为JSON字符串
public static string SerializeLjsonArray(List<Ljson> ljsons)

// 从JSON字符串反序列化为Ljson数组
public static List<Ljson> ParseLjsonArray(string json)
```

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

##### DataTreeLjson

将 Name, Info 和 Data Tree 构造为 Ljson。每个 branch 只能包含 1 个或 2 个元素：1 个元素转为 list，2 个元素转为 dict。

```c#
//输入
pManager.AddTextParameter("Name", "N", "Ljson 的名称", GH_ParamAccess.item);
pManager.AddTextParameter("Info", "I", "Ljson 的说明", GH_ParamAccess.item);
pManager.AddGenericParameter("Data Tree", "DT", "Data Tree 数据。", GH_ParamAccess.tree);
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

##### FindJdata

通过名称查找Jdata的值。

```c#
//输入
pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "需要转换的Ljson", GH_ParamAccess.item);
pManager.AddTextParameter("Name", "N", "需要查找的键值", GH_ParamAccess.item);
```

```c#
// 输出
pManager.AddGenericParameter("Data", "D", "找到的值（基本类型或字符串）", GH_ParamAccess.item);
pManager.AddGenericParameter("DataList", "DL", "找到的值列表（基本类型或字符串）", GH_ParamAccess.list);
```

##### TcpClientParam

一个`System.Net.Sockets.TcpClient`连接，用于接收和传输数据。该对象由GHReceiver根据端口唯一创建。

- 默认为空。

##### GHReceiver

根据端口创建TcpClient并接收数据，并且每个端口只接受一个连接。在后台线程（Task/Thread）接收数据，然后通过 `RhinoApp.InvokeOnUiThread` 告知 GH 电池进行 `ExpireSolution(true)` 刷新。

```c#
// 输入
pManager.AddBooleanParameter("Enabled", "E", "是否启用服务器", GH_ParamAccess.item, false);
pManager.AddIntegerParameter("Port", "P", "监听的端口（1024-49151）", GH_ParamAccess.item, 6879);
```

```c#
// 输出
pManager.AddParameter(new TcpClientParam(), "Client", "CL", "Client连接", GH_ParamAccess.item);
pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "传入的数据", GH_ParamAccess.item);
pManager.AddTextParameter("Status", "ST", "状态", GH_ParamAccess.item);
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
pManager.AddTextParameter("Status", "ST", "发送状态", GH_ParamAccess.item);
```

##### GHServer

根据端口创建TcpClient并接收数据，并且每个端口只接受一个连接。接收到数据后在内部执行并作出响应。

```c#
// 输入
pManager.AddBooleanParameter("Enabled", "E", "是否启用服务器", GH_ParamAccess.item, false);
pManager.AddIntegerParameter("Port", "P", "监听的端口（1024-49151）", GH_ParamAccess.item, 6879);
```

```c#
// 输出
pManager.AddTextParameter("Status", "ST", "回复状态", GH_ParamAccess.item);
pManager.AddGenericParameter("OutPut", "O", "显示输出数据", GH_ParamAccess.item);
```

##### GHActuator

对输入的数据进行执行。

```c#
// 输入
pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "需要执行的数据", GH_ParamAccess.item);
```

```c#
// 输出
pManager.AddTextParameter("Status", "ST", "执行结果", GH_ParamAccess.item);
pManager.AddParameter(new LjsonParam(), "Result", "R", "处理后的Ljson结果", GH_ParamAccess.item);
pManager.AddGenericParameter("OutPut", "O", "显示输出数据", GH_ParamAccess.item);
```

##### ScriptEditor

通过输入的代码修改Script组件，支持c#、python。

```c#
// 输入
pManager.AddGenericParameter("ScriptComponent", "SC", "Rhino8 Grasshopper 的脚本组件，仅支持操作一个组件", GH_ParamAccess.tree);
pManager.AddTextParameter("Code", "C", "脚本代码", GH_ParamAccess.item, "");
pManager.AddTextParameter("IntputParams", "IP", "输入端参数定义", GH_ParamAccess.item);
pManager.AddTextParameter("OutputParams", "OP", "输出端参数定义", GH_ParamAccess.item);
```

```c#
// 输出
pManager.AddTextParameter("Result", "R", "显示运行信息", GH_ParamAccess.item);
pManager.AddTextParameter("ComponentType", "T", "显示组件信息", GH_ParamAccess.item);
pManager.AddBooleanParameter("IsSDKMode", "SDK", "代码是否是SDK模式", GH_ParamAccess.item);
pManager.AddTextParameter("SourceCode", "SC", "代码code", GH_ParamAccess.item);
pManager.AddTextParameter("InputParams", "IP", "当前输入端参数信息", GH_ParamAccess.item);
pManager.AddTextParameter("OutputParams", "OP", "当前输出端参数信息", GH_ParamAccess.item);
```

##### RunScript

在内部运行c#脚本。本组件预留给ai直接执行脚本。

```c#
// 输入
pManager.AddTextParameter("Code", "C", "脚本", GH_ParamAccess.item, "");
```

```c#
// 输出
pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "数据输出", GH_ParamAccess.item);
pManager.AddTextParameter("Out", "O", "调试输出", GH_ParamAccess.item);
```

##### CommandRhino

执行rhino脚本。

```c#
// 输入
pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "要执行的Rhino命令Ljson数据，必须包含Command字段", GH_ParamAccess.item);
```

```c#
// 输出
pManager.AddParameter(new LjsonParam(), "Result", "R", "执行后的Ljson结果", GH_ParamAccess.item);
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
pManager.AddGenericParameter("Input", "I", "连接一个组件", GH_ParamAccess.tree);
```

```c#
// 输出
pManager.AddTextParameter("Name", "N", "组件名字", GH_ParamAccess.list);
pManager.AddTextParameter("GUID", "ID", "组件的GUID", GH_ParamAccess.list);
pManager.AddTextParameter("InsGUID", "TS", "组件对象的GUID", GH_ParamAccess.list);
pManager.AddGenericParameter("Instance", "IT", "组件对象", GH_ParamAccess.list);
```

##### SearchDataBase

查询数据库。

```c#
// 输入
pManager.AddTextParameter("SQL", "SQL", "完整的SQL查询语句", GH_ParamAccess.item);
```

```c#
// 输出
pManager.AddTextParameter("Result", "R", "查询结果，以JSON格式返回", GH_ParamAccess.item);
```

### 计划

- 增加help

- 序列化xml
