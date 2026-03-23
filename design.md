### 算法

##### JData

表示基本数据单元

```c#
public JData(string name, string description, string value)
{
    Name = name;                // 数据名称
    Description = description;  // 数据描述
    Value = value;              // 数据值
}
```

##### JList

即`List<JData>`，表示一组数据。作为数据传输的基本单元。

```c#
public JList(JData[] items)
{
    time = DateTime.Now;           // 每个数据创建时自动生成，用于检查数据是否被使用过
    if (items != null)
    {
        foreach (var item in items)
        {
        	queue.Enqueue(item);
        }
    }
}
```

##### ComponentJList

表示组件的基本信息



### 基本数据与通信

##### JListParam

由`JList = (DateTime time, List<JData(string name，string description，string data)>)`组成的数据，表示tcp一次消息的多个JData。

- 默认为空；

##### Json2JList

将Json转换为JList。

```c#
//输入
pManager.AddTextParameter("String", "S", "将Json格式转换为JList", GH_ParamAccess.item);
```

```c#
// 输出
pManager.AddParameter(new JListParam(), "JList", "JQ", "生成的JList", GH_ParamAccess.item);
```

##### StringTreeJList

将string tree转换为JList。

```c#
//输入
pManager.AddTextParameter("String Tree", "ST", "将string tree转换为JList", GH_ParamAccess.tree);
// string tree每个branch只取前三项，非string格式转为string，项目不足则使用空值补齐。
```

```c#
// 输出
pManager.AddParameter(new JListParam(), "JList", "JQ", "生成的JList", GH_ParamAccess.item);
```

##### JList2Json

将JList转换为Json。

```c#
//输入
pManager.AddParameter(new JListParam(), "JList", "JQ", "需要转换的JList", GH_ParamAccess.item);
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
pManager.AddParameter(new JListParam(), "JList", "JQ", "传入的数据", GH_ParamAccess.item);
```

##### GHSender

将需要传出的数据，使用连接进行发送。JList.time更新，会触发新发送，未更新则不触发。

```c#
// 输入
pManager.AddParameter(new TcpClientParam(), "Client", "CL", "Client连接", GH_ParamAccess.item);
pManager.AddParameter(new JListParam(), "JList", "JQ", "发送数据，按顺序发送", GH_ParamAccess.item);
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
pManager.AddParameter(new JListParam(), "JList", "JQ", "需要执行的数据", GH_ParamAccess.item);
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
pManager.AddParameter(new JListParam(), "JList", "JQ", "所有组件的信息", GH_ParamAccess.item);
```

- 输出结构：

```c#
outdata = [categorys, count, components];
categorys = JData("AllCategorys", "所有分类", string(list(name)));
count = JData("Count", "组件数量", string(components count));
components = JData("AllComponents", "所有注册的组件", string({category : {subCategory : component}}}));
component = string(guid, name, nickname, description);
```

##### FindComponentsByGuid

通过Guid查询组件信息。

```c#
// 输出
pManager.AddParameter(new JListParam(), "ComponentInfo", "C", "组件信息", GH_ParamAccess.item);
```

- 输出结构`ComponentJList`：

```c#
[new JData("ComponentGuid", "组件 GUID", componentGuid),
new JData("InstanceGuid", "实例 GUID", instanceGuid),
new JData("ComponentName", "组件名称", name),
new JData("NickName", "组件昵称", nickName),
new JData("Description", "组件描述", description),
new JData("Category", "主分类", category),
new JData("SubCategory", "子分类", subCategory),
new JData("Position", "位置信息", position),
new JData("State", "状态信息", state),
new JData("Inputs", "输入端信息", inputs),
new JData("Outputs", "输出端信息", outputs)]
```

##### FindComponentsByName

通过名称查询组件信息。输出结构`ComponentJList`。

##### FindComponentsByCategory

通过Category查询组件信息。输出结构`ComponentJList`。

##### SearchComponentsByName

通过名称搜索组件，可以模糊匹配。输出结构`List<ComponentJList>`。

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



