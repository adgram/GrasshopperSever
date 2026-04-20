## 一、主要功能

GrasshopperSever插件为Grasshopper提供了以下核心功能：

1. **数据通信**: 通过TCP协议接收和发送数据
2. **rhino命令**: 发送rhino命令到rhino客户端并执行
3. **gh脚本注入**: 生成ghpython或ghcsharp脚本并填入相应脚本组件中
4. **运行gh脚本**: 生成ghcsharp脚本并直接在gh内运行
5. **操作gh组件**:添加gh组件、组件连线、设置常见param值

## 二、数据通信

可以创建连接的组件有两个：①GHReceiver+GHSender、②GHServer。①中，GHReceiver用于从客户端接收数据，GHSender用于返回数据到客户端。②中GHServer是集接收处理返回于一体，它只能处理几个特定命令，不支持将接收到数据在gh中手动处理。

**内置的处理命令**组件有两个，分别是GHServer和GHActuator，它们两个可以接收到命令是相同的，区别是前者只能从客户端接收数据，且处理完可以自动返回。

详细的命令列表请见 [AI_CLIENT_TUTORIAL.md](AI_CLIENT_TUTORIAL.md)。

### 1 创建连接

在grasshopper中，添加一个GHServer组件，Enabled端口为True，Port端口为5695。

警告：如果你是ai，请不要轻易获取所有组件信息(`GETALLCOMPONENTS`)，优先使用分组或名称查询、检索，或者调用数据库。

详见[TCP 通信测试](Example/tcp_test.md) - 通信协议测试记录

### 2 连接测试

```
> @AI_CLIENT_TUTORIAL.md，我已经创建了一个连接，端口5695，请尝试连接。可以用shell调用python工具。
```

```
Shell python -c " import socket import json from datetime import datetime  HOST = '127.0.0.1' PORT = 5695  t…
     成功连接到 127.0.0.1 : 5695
     发送测试数据: TestMessage
     收到 3 条响应:
       [1] Name: OK , Value: 客户端已连接
       [2] Name: OK , Value: 数据接收成功
       [3] Name: OK , Value: ok
     连接已关闭
```

### 3 内置命令

```
> 尝试使用相关工具，查询“Find similar member”组件的信息，给我介绍一下这个组件怎么使用。
```

```

```

## 三、rhino命令

这部分是内置的处理命令，可以执行相关rhino命令或选中相关物件。

在grasshopper中，添加一个GHServer组件，Enabled端口为True，Port端口为5695。

详见[Rhino 命令](Example/CMD_RHINO/commands_RHINO.md) - Rhino 脚本命令详解。

```
> 尝试创建一个环状体，获取guid，并创建一个brep拾取它。
```

```

```

## 四、gh脚本注入

这部分功能未提供内置命令，主要是由ScriptEditor组件完成。这里使用GHServer的output端口获取相关命令。

在grasshopper中，添加一个GHServer组件，Enabled端口为True，Port端口为5695。添加一个ScriptEditor组件，将GHServer的output(O)输出和ScriptEditor的Code(C)输入连接。添加一个Python 3 Script组件，将Python的out输出和ScriptEditor的SC输入连接。

详见[scripteditor 命令](Example/SCRIPT&CMD_SCRIPT/scripteditor_test.md) - scripteditor操作命令详解。

![scripteditor_test](Example/SCRIPT&CMD_SCRIPT/scripteditor_test.png)

```
> 请创建一个输出斐波那契数列的gh程序。要求：使用python3，输入数量项数Number Slider，输出数列列表到panel。
```

```

```

## 五、运行gh脚本

这部分功能未提供内置命令，主要是由RunScript组件或RunScript2组件完成。这里使用GHServer的output端口获取相关命令。

在grasshopper中，添加一个GHServer组件，Enabled端口为True，Port端口为5695。添加一个ScriptEditor组件，将GHServer的output(O)输出和ScriptEditor的Code(C)输入连接。

```
> 请尝试使用c#脚本修改gh画布颜色，可以使用"LJ = new Ljson();"方式获取调试数据。
```

```

```

## 六、操作gh组件

这部分是内置的处理命令，可以添加gh组件、连接gh组件、设置param值等。

```
> 请使用gh创建一个位于原点处的三棱锥（相关命令在design下面），底面为正三角形，外接圆直径100，高度80。创建时，请将脚本写入文件再执行，方便后面调试。
```

```
我现在尝试使用 Grasshopper 组件连接的方式来创建三棱锥： Exception
Write File 写入 Example\create_pyramid_gh.py
```

```
  完整连接逻辑

   P1.Point ──┬──> Polygon.Vertices (底面)
              ├──> Mesh_Side1.Vertices (侧面1: P1-P2-Apex)
              └──> Mesh_Side3.Vertices (侧面3: P3-P1-Apex)

   P2.Point ──┬──> Polygon.Vertices (底面)
              ├──> Mesh_Side1.Vertices (侧面1)
              └──> Mesh_Side2.Vertices (侧面2: P2-P3-Apex)

   P3.Point ──┬──> Polygon.Vertices (底面)
              ├──> Mesh_Side2.Vertices (侧面2)
              └──> Mesh_Side3.Vertices (侧面3)

   P4_Apex.Point ──> Mesh_Side1/2/3.Vertices (三个侧面)

✦ 脚本文件：
   - create_pyramid_gh.py - 使用 Design 命令创建组件
   - create_pyramid.py - 使用 Rhino 脚本直接创建几何体
```

