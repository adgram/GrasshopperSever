# 服务发送数据一览

### Ljson结构

所有命令使用 Ljson 格式，通过 TCP 发送：

```json
{
  "Name": "命令类型",
  "Info": "命令描述",
  "Time": "2026-03-26T10:00:00",
  "Value": {
    "Command": "具体命令名称",
    "参数名": "参数值"
  }
}
```

### OK响应

```json
"客户端已连接" // "数据接收成功" // "组件连接断开成功" // "组件连接成功" // "组件值设置成功" // "组件移除成功"
```

### Error响应

```json
"输入数据为空" // "未找到命令类型" // $"未知的 Component 命令: {commandType}" // $"执行 Component 命令时出错: {ex.Message}" // "输入数据为空" // ...
```

### AllComponentsInfo 数据库中的所有组件信息

```json
{
  "categorys": "所有分类",
  "count": "组件数量",
  "components": "所有注册的组件"
}
```

### ComponentLjson组件信息

```json
{
  "ComponentGuid": "组件 GUID",
  "ComponentName": "组件名称",
  "NickName": "组件昵称",
  "Description": "组件描述",
  "Category": "主分类",
  "SubCategory": "子分类",
  "Prototype": "函数签名"
}
```

### InstanceLjson组件/自由Param实例信息(简略)

```json
{
  "ComponentGuid": "组件 GUID",
  "InstanceGuid": "实例Guid",
  "ComponentName": "组件名称",
  "Position": "位置坐标",
  "Type": "Component/Param",
  "CustomValuet": "是否包含自定义值"
}
```

### InstanceLjson组件实例信息(详细)

```json
{
  "ComponentGuid": "组件 GUID",
  "InstanceGuid": "实例Guid",
  "ComponentName": "组件名称",
  "NickName": "组件昵称",
  "Category": "主分类",
  "SubCategory": "子分类",
  "Position": "位置坐标",
  "State": "状态",
  "Type": "Component/Param",
  "Inputs": "输入端",
  "Outputs": "输出端"
}
// 输入端或者输出端
"[ParamLjson.Value]"
```

### InstanceLjson自由Param实例信息(详细)

```json
{
  "ComponentGuid": "组件 GUID",
  "InstanceGuid": "实例Guid",
  "ComponentName": "组件名称",
  "NickName": "组件昵称",
  "Category": "主分类",
  "SubCategory": "子分类",
  "Position": "位置坐标",
  "State": "状态",
  "Type": "Component/Param",
  "Mapping": "mapping",
  "Reverse": "reverse",
  "Simplify": "simplify",
  "Sources": "inputs",
  "Recipients": "outputs"
}
```

### ParamToLjson端口信息

```json
{
  "ParamGuid": "paramGuid",
  "InstanceGuid": "instanceGuid",
  "Name": "name",
  "NickName": "nickName",
  "Description": "description",
  "TypeName": "typeName",
  "Optional": "optional",
  "Access": "access",
  "Mapping": "mapping",
  "Reverse": "reverse",
  "Simplify": "simplify",
  "Sources": "inputs",
  "Recipients": "outputs"
}
```

### RhinoCommand脚本执行结果

```json
{
	Result: "Script" // Result为false或True
}
```

### GetLastCreatedObjects获取的对象

```json
{
    "Object_i":"objectsData"
}
// objectsData
{
    "Id": "obj.Id",
    "Type": "objectType",
    "Layer": "layerName",
    "Name": "objectName",
    "DatabaseRecordId": "recordId"
}
```

### SelectObjects选择对象

```json
{
    "TotalRequested": "n",
    "TotalSelected": "successCount",
    "InvalidIdCount": "invalidIdCount",
    "NotFoundCount": "notFoundCount",
    "Message": "部分对象选择成功（成功: {successCount}, 无效ID: {invalidIdCount}, 未找到: {notFoundCount}
}
```

### 文档保存结果

```json
{
    "FilePath"： "savePath",
    "Message"："文档保存成功"
}
```

### 文档打开结果

```json
{
    "FilePath"： "filePath",
    "DocumentId": "Id",
    "Message"："文档保存成功"
}
```

### 数据库路径

```json
{ "DatabasePath": "path" }
```

### 文档所有对象

```c#
var data = new Dictionary<string, object>
{
    { "DocumentId", doc.DocumentID.ToString() },
    { "TotalCount", doc.ObjectCount },
    { "Graph", ComponentGraph}
};
public class ComponentGraph
{
    // 节点 GUID -> 输出端口名 -> (下游节点 GUID, 下游输入端口名)
    public Dictionary<Guid, Dictionary<string, Dictionary<Guid, string>>> Adjacency { get; };
    // 节点 GUID -> 显示名称
    public Dictionary<Guid, string> NodeNames { get; };
    // GUID 到文档对象实例的映射
    public Dictionary<Guid, IGH_DocumentObject> NodeObjects { get; };
    // 节点类型
    public Dictionary<Guid, NodeType> NodeTypes { get; };
    // 图的根节点（没有上游连接的节点，即入度为0）
    public HashSet<Guid> Heads { get; };
    public enum NodeType{Component,TopParam}
}
```

