# GrasshopperSever 命令列表

本文档列出了GrasshopperSever插件支持的所有可用命令。

警告：不要轻易获取所有组件信息，优先使用分组或名称查询、检索，或者调用数据库。

## 命令格式

所有命令通过LJSON格式发送，必须包含以下结构：

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

**Name字段（命令类型）**：
- `COMPONENT` - 组件相关命令
- `DOCUMENT` - 文档相关命令
- `RHINO` - Rhino相关命令
- `SCRIPT` - 脚本相关命令
- `DESIGN` - 设计相关命令

---

## 数据库表结构

GrasshopperSever 使用 SQLite 数据库存储组件信息和对象信息。数据库文件位于：
- **路径**：`C:\Users\[用户名]\AppData\Roaming\Grasshopper\Libraries\GHserver\ComponentsInfo.db`
- **查询命令**：使用 DATABASEPATH 命令获取具体路径

### 1. GHScriptModifyHistory 表（GHScript组件修改历史表）

存储 GHScript 组件（C# Script、Python Script 等）的修改历史记录。

| 字段名 | 数据类型 | 约束 | 说明 |
|--------|----------|------|------|
| Id | INTEGER | PRIMARY KEY AUTOINCREMENT | 主键，自增 |
| InstanceGuid | TEXT | NOT NULL | 组件实例 GUID（用于标识具体的组件实例） |
| ComponentGuid | TEXT | NOT NULL | 组件类型 GUID（用于标识组件类型） |
| ComponentName | TEXT | - | 组件名称 |
| ModifyType | TEXT | NOT NULL | 修改类型（CODE_CHANGE：代码修改，PARAM_CHANGE：参数修改） |
| ModifyContent | TEXT | - | 修改内容（JSON 格式） |
| Description | TEXT | - | 描述信息 |
| ModifyTime | DATETIME | DEFAULT CURRENT_TIMESTAMP | 修改时间 |

**SQL 创建语句**：
```sql
CREATE TABLE IF NOT EXISTS GHScriptModifyHistory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    InstanceGuid TEXT NOT NULL,
    ComponentGuid TEXT NOT NULL,
    ComponentName TEXT,
    ModifyType TEXT NOT NULL,
    ModifyContent TEXT,
    Description TEXT,
    ModifyTime DATETIME DEFAULT CURRENT_TIMESTAMP
)
```

**示例查询**：
```sql
-- 查询所有修改历史
SELECT * FROM GHScriptModifyHistory ORDER BY ModifyTime DESC;

-- 查询特定实例的修改历史
SELECT * FROM GHScriptModifyHistory WHERE InstanceGuid = '{instance_guid}' ORDER BY ModifyTime DESC;

-- 查询代码修改历史
SELECT * FROM GHScriptModifyHistory WHERE ModifyType = 'CODE_CHANGE' ORDER BY ModifyTime DESC;

-- 查询参数修改历史
SELECT * FROM GHScriptModifyHistory WHERE ModifyType = 'PARAM_CHANGE' ORDER BY ModifyTime DESC;

-- 按组件类型统计修改次数
SELECT ComponentName, COUNT(*) as ModifyCount FROM GHScriptModifyHistory GROUP BY ComponentName;

-- 查询最近的修改
SELECT ComponentName, ModifyType, Description, ModifyTime FROM GHScriptModifyHistory ORDER BY ModifyTime DESC LIMIT 20;
```

**ModifyContent 字段说明**：

代码修改（CODE_CHANGE）：
```json
{
  "OldCodeLength": 1234,
  "NewCodeLength": 1500,
  "CodeChanged": true,
  "ComponentType": "C# Script"
}
```

参数修改（PARAM_CHANGE）：
```json
{
  "OldInputParams": "[...]",
  "OldOutputParams": "[...]",
  "NewInputParams": "[...]",
  "NewOutputParams": "[...]",
  "InputParamCount": 3,
  "OutputParamCount": 2,
  "ComponentType": "Python 3 Script"
}
```

**注意事项**：
- 该表在第一次修改 GHScript 组件时自动创建
- 每次修改代码或参数时，会自动记录修改历史
- 该表是暂存文件，不会和 Grasshopper 文件同步
- InstanceGuid 用于区分不同的组件实例
- ComponentGuid 用于标识组件类型（如：C# Script、Python 3 Script 等）

---

## 错误处理

所有命令在执行失败时会返回错误格式的LJSON：

```json
{
  "Name": "Error",
  "Info": "错误描述",
  "Time": "2026-03-26T10:00:00",
  "Value": "错误详情信息"
}
```

常见错误类型：
- 输入数据为空
- 未找到命令类型
- 未知的命令
- 缺少必需参数
- 执行命令时出错
