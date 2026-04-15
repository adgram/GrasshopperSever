### 一、创建连接

在grasshopper中，使用GHServer创建一个tcp连接，Enabled端口为True，Port端口为5695。

警告：不要轻易获取所有组件信息，优先使用分组或名称查询、检索，或者调用数据库。

### 二、连接测试

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

### 三、模拟任务

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

