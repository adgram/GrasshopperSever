# AI教程

本教程用于指导ai客户端，如何如何通过 TCP 协议连接到 GrasshopperSever，实现与 Grasshopper 的双向通信，进行信息获取与执行相关命令。

## 目录

- [通信连接](#通信连接)
- [命令速览](#命令速览)
- [快速开始](#快速开始)
- [GHClient 类](#grasshopperclient-类)
- [数据库访问](#数据库访问)
- [故障排除](#故障排除)

## 通信连接

### 通信数据结构

所有接收与发送数据均是采用单个Ljson 格式完成。

1. 连接成功，服务端会自动返回一条已连接的响应。

2. 发送消息给服务端，服务端会自动返回一条已收到的响应。

3. 发送Command相关操作给服务端，服务端会自动返回一条已收到的响应和一条执行结果的消息。

因此，当你执行命令时，你可以解析服务端返回到数据。通过返回消息，你可以判断是否执行成功。

### 通信方式

一般情况发送和接收消息，请使用标准模板[ghclient](../ghclient.py)类，不要自建连接函数。

这个类提供了如何连接、如何发送消息、如何发送命令的标准方法。

当前发送的命令和关键词是大小写不敏感的，但相关值是大小写敏感的。

## 命令速览

下面是所有支持的相关命令，具体如何执行你可以查看详细文档或者文档所在的文件夹的py文件。

文档里面的代码可能是过时的，以py文件为准。

| 类型 | 命令 | 说明 | 详细文档 |
|------|------|------|----------|
| COMPONENT | `GETALLCOMPONENTS` | 获取所有组件 | [链接](../CMD_COMPONENT/commands_COMPONENT.md) |
| COMPONENT | `FINDCOMPONENTBYGUID` | 按 GUID 查找组件 | 同上 |
| COMPONENT | `FINDCOMPONENTBYNAME` | 按名称查找组件 | 同上 |
| COMPONENT | `FINDCOMPONENTBYCATEGORY` | 按分类查找组件 | 同上 |
| COMPONENT | `SEARCHCOMPONENTSBYNAME` | 模糊搜索组件 | 同上 |
| DOCUMENT | `SAVEDOCUMENT` | 保存文档 | [链接](../CMD_DOCUMENT/gh_file_test_report.md) |
| DOCUMENT | `LOADDOCUMENT` | 加载文档 | 同上 |
| DOCUMENT | `DATABASEPATH` | 获取数据库路径 | 同上 |
| DOCUMENT | `GETALLOBJECTS` | 通过guid查找画布上组件实例 | 同上 |
| DOCUMENT | `GETOBJECT` | 获取画布上组件实例 | 同上 |
| RHINO | `RHINOSCRIPT` | 执行 Rhino 命令 | [链接](../CMD_RHINO/commands_RHINO.md) |
| RHINO | `GETLASTCREATEDOBJECTS` | 获取最后创建的对象 | 同上 |
| RHINO | `SELECTOBJECTS` | 选择对象 | 同上 |
| RHINO | `GETANDSELECTLASTOBJECTS` | 获取并选择对象 | 同上 |
| DESIGN | `ADDCOMPONENTBYGUID` | 通过 GUID 添加组件 | [链接](../CMD_DESIGN/design_test.md) |
| DESIGN | `ADDCOMPONENTBYNAME` | 通过名称添加组件 | 同上 |
| DESIGN | `ADDPARAMWITHVALUE` | 添加参数组件并设置值 | 同上 |
| DESIGN | `REMOVECOMPONENT` | 移除组件 | 同上 |
| DESIGN | `SETPARAMVALUE` | 设置参数值 | 同上 |
| DESIGN | `CONNECTCOMPONENTS` | 连接组件 | 同上 |
| DESIGN | `DISCONNECTCOMPONENTS` | 断开组件连接 | 同上 |
| SCRIPT |  | 未实现的命令，改为RunScript组件 |  |

警告：请不要轻易获取所有组件信息(`GETALLCOMPONENTS`)，优先使用分组或名称查询、检索，或者调用数据库。

## 数据库访问

通过 `DATABASEPATH` 命令获取主数据库路径后可直接查询gh所有已经注册的组件相关信息：

```python
import sqlite3
from ghclient import GHClient

# 获取路径
with GHClient(port=6879) as gh:
    responses = gh.send_command(
        name="DOCUMENT",
        info="获取数据库路径",
        value={"Command": "DATABASEPATH"}
    )
    for res in responses:
        if res.get('Name') == 'DatabasePath':
            db_path = res['Value']['DatabasePath']

# 查询组件
conn = sqlite3.connect(db_path)
cursor = conn.cursor()
cursor.execute("SELECT ComponentGuid, ComponentName, Category FROM ALLCOMPS WHERE ComponentName LIKE '%Circle%'")
for row in cursor.fetchall():
    print(row)
conn.close()
```

## 故障排除

### 连接失败

- 确认 Grasshopper 正在运行
- 确认 GHReceiver/GHServer 的 `Enabled` 为 `true`
- 确认端口号正确

### Design 命令注意

- `SETPARAMVALUE`、`REMOVECOMPONENT`、`CONNECTCOMPONENTS` 等需要 `InstanceGuid`（组件实例 GUID），不是 `ComponentGuid`（组件类型 GUID）
- `ADDPARAMWITHVALUE` 中列表值使用字符串数组格式，如 `"[\"1.0\", \"2.0\"]"`
