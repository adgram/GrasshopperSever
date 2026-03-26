# Grasshopper文件打开和保存API测试报告

## 测试概述
测试GrasshopperSever插件的文档操作API，包括打开和保存GH文件功能。

## 测试时间
2026-03-26

## 测试环境
- GrasshopperSever TCP服务端口: 6879
- 测试目录: `C:\Users\SZ\AppData\Roaming\Grasshopper\Libraries\GHserver\test`

## 测试方法
通过TCP连接发送LJSON格式的命令到GrasshopperSever服务器。

## 测试结果

### 测试1: 获取数据库路径
**命令**: DOCUMENT.DATABASEPATH

**结果**: ✓ 成功

**响应**:

```json
{
  "Name": "DatabasePath",
  "Info": "获取数据库路径",
  "Value": {
    "DatabasePath": "C:\\Users\\SZ\\AppData\\Roaming\\Grasshopper\\Libraries\\GHserver\\GrasshopperSever.db"
  }
}
```

---

### 测试2: 保存当前文档（不指定路径）
**命令**: DOCUMENT.SAVEDOCUMENT

**结果**: ✗ 失败（预期行为）

**错误信息**:
```json
{
  "Name": "Error",
  "Info": "错误响应",
  "Value": "文档未保存过，请指定保存路径"
}
```

**说明**: 这是预期的行为，未保存过的文档必须指定保存路径。

---

### 测试3: 保存文档到指定路径
**命令**: DOCUMENT.SAVEDOCUMENT
**参数**: `{"FilePath": "C:\\Users\\SZ\\AppData\\Roaming\\Grasshopper\\Libraries\\GHserver\\test\\test_save.gh"}`

**结果**: ✓ 成功

**响应**:
```json
{
  "Name": "SaveDocument",
  "Info": "保存文档成功",
  "Value": {
    "FilePath": "C:\\Users\\SZ\\AppData\\Roaming\\Grasshopper\\Libraries\\GHserver\\test\\test_save.gh",
    "Message": "文档保存成功"
  }
}
```

**验证**: 文件已成功创建在指定路径。

---

### 测试4: 加载文档（测试手动创建的假文件）
**命令**: DOCUMENT.LOADDOCUMENT
**参数**: `{"FilePath": "C:\\Users\\SZ\\AppData\\Roaming\\Grasshopper\\Libraries\\GHserver\\test\\test.gh"}`

**结果**: ✗ 失败（预期行为）

**错误信息**:
```json
{
  "Name": "Error",
  "Info": "错误响应",
  "Value": "打开文档失败"
}
```

**说明**: 测试文件是手动创建的假Grasshopper文件，不是正确的格式，因此无法加载。

---

### 测试5: 加载刚才保存的文档
**命令**: DOCUMENT.LOADDOCUMENT
**参数**: `{"FilePath": "C:\\Users\\SZ\\AppData\\Roaming\\Grasshopper\\Libraries\\GHserver\\test\\test_save.gh"}`

**结果**: ✓ 成功

**响应**:
```json
{
  "Name": "LoadDocument",
  "Info": "加载文档成功",
  "Value": {
    "FilePath": "C:\\Users\\SZ\\AppData\\Roaming\\Grasshopper\\Libraries\\GHserver\\test\\test_save.gh",
    "DocumentId": "1952f951-4cd4-4c9c-94af-47cc7f0c71f6",
    "Message": "文档打开成功"
  }
}
```

**说明**: 成功加载之前保存的文档，系统分配了文档ID。

---

### 测试6: 再次保存文档
**命令**: DOCUMENT.SAVEDOCUMENT

**结果**: ✓ 成功

**响应**:
```json
{
  "Name": "SaveDocument",
  "Info": "保存文档成功",
  "Value": {
    "FilePath": "C:\\Users\\SZ\\AppData\\Roaming\\Grasshopper\\Libraries\\GHserver\\test\\test_save.gh",
    "Message": "文档保存成功"
  }
}
```

**说明**: 文档已经保存过，可以直接保存而不需要指定路径。

---

## 总结

### 成功的测试
1. ✓ 获取数据库路径
2. ✓ 保存文档到指定路径
3. ✓ 加载已保存的文档
4. ✓ 再次保存已保存过的文档

### 预期失败的测试
1. ✗ 保存未保存过的文档（未指定路径）
2. ✗ 加载格式错误的文件

### API功能验证

| 功能 | 命令 | 状态 | 说明 |
|------|------|------|------|
| 保存文档（指定路径） | DOCUMENT.SAVEDOCUMENT | ✓ 正常工作 | 需要提供FilePath参数 |
| 保存文档（默认路径） | DOCUMENT.SAVEDOCUMENT | ✓ 正常工作 | 文档已保存过时可省略路径 |
| 加载文档 | DOCUMENT.LOADDOCUMENT | ✓ 正常工作 | 需要提供FilePath参数 |
| 获取数据库路径 | DOCUMENT.DATABASEPATH | ✓ 正常工作 | 无需参数 |

### 测试文件
- `test_document_apis.py` - 完整的API测试脚本
- `test_load_saved.py` - 加载已保存文档的测试脚本
- `test_save.gh` - 通过API保存的Grasshopper文档

### 结论
GrasshopperSever的文档操作API（打开和保存GH文件）功能正常，能够：
- 正确保存Grasshopper文档到指定路径
- 正确加载已保存的Grasshopper文档
- 正确处理边界情况（未保存过的文档、格式错误的文件）
- 返回详细的操作结果和错误信息

所有核心功能测试通过。