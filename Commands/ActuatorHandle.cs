using GrasshopperSever.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GrasshopperSever.Commands
{
    /// <summary>
    /// 用于执行特殊的指令
    /// </summary>
    public class ActuatorHandle
    {
        /// <summary>
        /// 执行 Component 相关命令
        /// </summary>
        /// <param name="data">输入的Ljson数据</param>
        /// <returns>执行结果Ljson</returns>
        public static Ljson DoComponentCommand(Ljson data)
        {
            if (data == null)
            {
                return Ljson.CreateErrorLjson("输入数据为空");
            }

            // 获取命令类型
            var commandElement = data.GetParameter("Command");
            if (commandElement == null || commandElement.Value.ValueKind != System.Text.Json.JsonValueKind.String)
            {
                return Ljson.CreateErrorLjson("未找到命令类型");
            }

            string commandType = commandElement.Value.GetString();

            try
            {
                switch (commandType.ToUpperInvariant())
                {
                    case "GETALLCOMPONENTS":
                        return HandleGetAllComponentsFromDB(data);

                    case "FINDCOMPONENTBYGUID":
                        return HandleFindComponentByGuid(data);

                    case "FINDCOMPONENTBYNAME":
                        return HandleFindComponentByName(data);

                    case "FINDCOMPONENTBYCATEGORY":
                        return HandleFindComponentByCategory(data);

                    case "SEARCHCOMPONENTSBYNAME":
                        return HandleSearchComponentsByName(data);

                    default:
                        return Ljson.CreateErrorLjson($"未知的 Component 命令: {commandType}");
                }
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"执行 Component 命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 Document 相关命令
        /// </summary>
        /// <param name="data">输入的Ljson数据</param>
        /// <returns>执行结果Ljson</returns>
        public static Ljson DoDocumentCommand(Ljson data)
        {
            if (data == null)
            {
                return Ljson.CreateErrorLjson("输入数据为空");
            }

            // 获取命令类型
            var commandElement = data.GetParameter("Command");
            if (commandElement == null || commandElement.Value.ValueKind != System.Text.Json.JsonValueKind.String)
            {
                return Ljson.CreateErrorLjson("未找到命令类型");
            }

            string commandType = commandElement.Value.GetString();

            try
            {
                switch (commandType.ToUpperInvariant())
                {
                    case "SAVEDOCUMENT":
                        return HandleSaveDocument(data);

                    case "LOADDOCUMENT":
                        return HandleLoadDocument(data);

                    case "DATABASEPATH":
                        return HandleDatabasePath(data);

                    default:
                        return Ljson.CreateErrorLjson($"未知的 Document 命令: {commandType}");
                }
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"执行 Document 命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 Rhino 命令
        /// </summary>
        /// <param name="data">输入的Ljson数据</param>
        /// <returns>执行结果Ljson</returns>
        public static Ljson DoRhinoCommand(Ljson data)
        {
            if (data == null)
            {
                return Ljson.CreateErrorLjson("输入数据为空");
            }

            // 获取命令类型
            var commandElement = data.GetParameter("Command");
            if (commandElement == null || commandElement.Value.ValueKind != System.Text.Json.JsonValueKind.String)
            {
                return Ljson.CreateErrorLjson("未找到命令类型");
            }

            string commandType = commandElement.Value.GetString();

            try
            {
                switch (commandType.ToUpperInvariant())
                {
                    case "RUNSCRIPT":
                        return HandleRunScript(data);

                    case "GETLASTCREATEDOBJECTS":
                        return HandleGetLastCreatedObjects(data);

                    case "SELECTOBJECTS":
                        return HandleSelectObjects(data);

                    default:
                        return Ljson.CreateErrorLjson($"未知的 Rhino 命令: {commandType}");
                }
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"执行 Rhino 命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理运行Rhino脚本命令
        /// </summary>
        private static Ljson HandleRunScript(Ljson data)
        {
            try
            {
                // 辅助方法：将 JsonElement 转换为字符串
                string ElementToString(System.Text.Json.JsonElement? element)
                {
                    if (!element.HasValue) return null;
                    var value = element.Value;
                    return value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : value.GetRawText();
                }

                string script = ElementToString(data.GetParameter("Script"));
                if (string.IsNullOrWhiteSpace(script))
                {
                    return Ljson.CreateErrorLjson("缺少参数: Script");
                }

                // 重要：RhinoApp.RunScript 必须在主文档上下文执行
                // 如果是从非命令线程调用，请确保在 Rhino 的 Idle 句柄或主线程中调度
                var doc = Rhino.RhinoDoc.ActiveDoc;

                // 执行Rhino命令 (true 表示 echo，让命令出现在命令行历史中)
                // 注意：script 前面建议加一个下划线 _ 以确保在非英文版 Rhino 中也能运行
                bool result = Rhino.RhinoApp.RunScript(doc.RuntimeSerialNumber, script, true);

                var responseData = new Dictionary<string, object>
                {
                    { "Result", result.ToString() },
                    { "Script", script }
                };

                if (result)
                {
                    return new Ljson("RunScript", "执行Rhino脚本成功", JsonSerializer.SerializeToElement(responseData));
                }
                else
                {
                    return Ljson.CreateErrorLjson("命令执行失败或被用户取消");
                }
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"运行Rhino脚本失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理获取最后创建的对象命令
        /// </summary>
        private static Ljson HandleGetLastCreatedObjects(Ljson data)
        {
            var doc = Rhino.RhinoDoc.ActiveDoc;

            // 技巧：如果你的 RunScript 刚运行完，可以使用以下逻辑获取：
            doc.Objects.UnselectAll();
            Rhino.RhinoApp.RunScript(doc.RuntimeSerialNumber, "_SelLast", false);
            var selectedObjects = doc.Objects.GetSelectedObjects(false, false);

            if (selectedObjects != null)
            {
                var objectsData = new Dictionary<string, object>();
                int count = 0;
                foreach (var obj in selectedObjects)
                {
                    objectsData[$"Object_{count}"] = obj.Id.ToString();
                    count++;
                }
                objectsData["Count"] = count.ToString();

                return new Ljson("GetLastCreatedObjects", "获取最后创建的对象", JsonSerializer.SerializeToElement(objectsData));
            }

            return new Ljson("GetLastCreatedObjects", "未找到对象", JsonSerializer.SerializeToElement(new Dictionary<string, object>()));
        }
        
        /// <summary>
        /// 处理选择对象命令
        /// 输入：Ljson包含 Command="SelectObjects", Objects="对象ID列表(逗号分隔)"
        /// 输出：选择结果
        /// </summary>
        private static Ljson HandleSelectObjects(Ljson data)
        {
            try
            {
                // 辅助方法：将 JsonElement 转换为字符串
                string ElementToString(System.Text.Json.JsonElement? element)
                {
                    if (!element.HasValue) return null;
                    var value = element.Value;
                    return value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : value.GetRawText();
                }

                string objectsParam = ElementToString(data.GetParameter("Objects"));
                if (string.IsNullOrWhiteSpace(objectsParam))
                {
                    return Ljson.CreateErrorLjson("缺少参数: Objects");
                }

                var doc = Rhino.RhinoDoc.ActiveDoc;
                if (doc == null)
                {
                    return Ljson.CreateErrorLjson("未找到活动文档");
                }

                // 1. 解析对象 ID (Rhino 使用 System.Guid)
                var idStrs = objectsParam.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int successCount = 0;

                // 在执行新选择前，通常需要清除之前的选择（视业务逻辑而定）
                doc.Objects.UnselectAll();

                foreach (var idStr in idStrs)
                {
                    // Rhino 的对象 ID 统一使用 Guid
                    if (Guid.TryParse(idStr.Trim(), out Guid id))
                    {
                        // 2. 通过 ID 查找对象并执行选择
                        // RhinoDoc.Objects.Select(Guid) 会返回被选择的对象数量 (1 或 0)
                        bool result = doc.Objects.Select(id);
                        if (result)
                        {
                            successCount++;
                        }
                    }
                }

                // 3. 必须刷新视图，否则界面上看不见选择结果
                doc.Views.Redraw();

                var responseData = new Dictionary<string, object>
                {
                    { "TotalRequested", idStrs.Length.ToString() },
                    { "TotalSelected", successCount.ToString() }
                };

                if (successCount == 0)
                {
                    // 如果一个都没选上，可能 ID 全错了，返回具体信息
                    responseData["Message"] = "未能在文档中找到匹配的 ID 或对象不可选";
                }

                return new Ljson("SelectObjects", "选择对象", JsonSerializer.SerializeToElement(responseData));
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"选择对象失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理从数据库获取所有组件命令
        /// 输入：Ljson包含 Command="GetAllComponentsFromDB"
        /// 输出：数据库中的所有组件信息
        /// </summary>
        private static Ljson HandleGetAllComponentsFromDB(Ljson data)
        {
            try
            {
                return ComponentInfo.GetAllComponentsFromDB();
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"从数据库获取组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过GUID查询组件命令
        /// 输入：Ljson包含 Command="FindComponentByGuid", Guid="组件GUID"
        /// 输出：组件详细信息
        /// </summary>
        private static Ljson HandleFindComponentByGuid(Ljson data)
        {
            try
            {
                // 辅助方法：将 JsonElement 转换为字符串
                string ElementToString(System.Text.Json.JsonElement? element)
                {
                    if (!element.HasValue) return null;
                    var value = element.Value;
                    return value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : value.GetRawText();
                }

                string guid = ElementToString(data.GetParameter("Guid"));
                if (string.IsNullOrWhiteSpace(guid))
                {
                    return Ljson.CreateErrorLjson("缺少参数: Guid");
                }

                var component = ComponentInfo.FindComponentsByGuid(guid);
                if (component == null)
                {
                    return Ljson.CreateErrorLjson($"未找到GUID为 {guid} 的组件");
                }

                return component;
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"通过GUID查询组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过名称查询组件命令
        /// 输入：Ljson包含 Command="FindComponentByName", Name="组件名称"
        /// 输出：组件详细信息
        /// </summary>
        private static Ljson HandleFindComponentByName(Ljson data)
        {
            try
            {
                // 辅助方法：将 JsonElement 转换为字符串
                string ElementToString(System.Text.Json.JsonElement? element)
                {
                    if (!element.HasValue) return null;
                    var value = element.Value;
                    return value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : value.GetRawText();
                }

                string name = ElementToString(data.GetParameter("Name"));
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Ljson.CreateErrorLjson("缺少参数: Name");
                }

                var component = ComponentInfo.FindComponentsByName(name);
                if (component == null)
                {
                    return Ljson.CreateErrorLjson($"未找到名称为 {name} 的组件");
                }

                return component;
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"通过名称查询组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过分类和名称查询组件命令
        /// 输入：Ljson包含 Command="FindComponentByCategory", Category="分类", SubCategory="子分类"(可选), Name="名称"(可选)
        /// 输出：组件详细信息
        /// </summary>
        private static Ljson HandleFindComponentByCategory(Ljson data)
        {
            try
            {
                // 辅助方法：将 JsonElement 转换为字符串
                string ElementToString(System.Text.Json.JsonElement? element)
                {
                    if (!element.HasValue) return null;
                    var value = element.Value;
                    return value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : value.GetRawText();
                }

                string category = ElementToString(data.GetParameter("Category"));
                string subCategory = ElementToString(data.GetParameter("SubCategory"));
                string name = ElementToString(data.GetParameter("Name"));

                if (string.IsNullOrWhiteSpace(category) && string.IsNullOrWhiteSpace(subCategory) && string.IsNullOrWhiteSpace(name))
                {
                    return Ljson.CreateErrorLjson("至少需要提供一个参数: Category, SubCategory 或 Name");
                }

                var component = ComponentInfo.FindComponentsByCategory(category, subCategory, name);
                if (component == null)
                {
                    return Ljson.CreateErrorLjson($"未找到符合条件的组件");
                }

                return component;
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"通过分类查询组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过名称模糊搜索组件命令
        /// 输入：Ljson包含 Command="SearchComponentsByName", Name="搜索关键词"
        /// 输出：匹配的组件列表
        /// </summary>
        private static Ljson HandleSearchComponentsByName(Ljson data)
        {
            try
            {
                // 辅助方法：将 JsonElement 转换为字符串
                string ElementToString(System.Text.Json.JsonElement? element)
                {
                    if (!element.HasValue) return null;
                    var value = element.Value;
                    return value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : value.GetRawText();
                }

                string name = ElementToString(data.GetParameter("Name"));
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Ljson.CreateErrorLjson("缺少参数: Name");
                }

                var components = ComponentInfo.SearchComponentsByName(name);
                if (components == null || components.Count == 0)
                {
                    return Ljson.CreateErrorLjson($"未找到名称包含 {name} 的组件");
                }

                // 将ComponentLjson列表合并为一个Ljson
                var resultData = new Dictionary<string, object>
                {
                    { "Count", components.Count.ToString() },
                    { "Components", components.Select(c => c.ToJson()).ToList() }
                };

                return new Ljson("SearchComponentsByName", "搜索组件", JsonSerializer.SerializeToElement(resultData));
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"搜索组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理保存文档命令
        /// 输入：Ljson包含 Command="SaveDocument", FilePath="文件路径"(可选)
        /// 输出：保存结果
        /// </summary>
        private static Ljson HandleSaveDocument(Ljson data)
        {
            try
            {
                // 辅助方法：将 JsonElement 转换为字符串
                string ElementToString(System.Text.Json.JsonElement? element)
                {
                    if (!element.HasValue) return null;
                    var value = element.Value;
                    return value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : value.GetRawText();
                }

                string filePath = ElementToString(data.GetParameter("FilePath"));
                var result = DocumentInfo.SaveDocument(filePath);
                return result;
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"保存文档失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理打开文档命令
        /// 输入：Ljson包含 Command="LoadDocument", FilePath="文件路径"
        /// 输出：打开结果
        /// </summary>
        private static Ljson HandleLoadDocument(Ljson data)
        {
            try
            {
                // 辅助方法：将 JsonElement 转换为字符串
                string ElementToString(System.Text.Json.JsonElement? element)
                {
                    if (!element.HasValue) return null;
                    var value = element.Value;
                    return value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : value.GetRawText();
                }

                string filePath = ElementToString(data.GetParameter("FilePath"));
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return Ljson.CreateErrorLjson("缺少参数: FilePath");
                }

                var result = DocumentInfo.LoadDocument(filePath);
                return result;
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"打开文档失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理获取数据库路径命令
        /// 输入：Ljson包含 Command="DatabasePath"
        /// 输出：数据库路径信息
        /// </summary>
        private static Ljson HandleDatabasePath(Ljson data)
        {
            try
            {
                var path = DatabaseManager.DatabasePath;
                var resultData = new Dictionary<string, object>
                {
                    { "DatabasePath", path }
                };
                return new Ljson("DatabasePath", "获取数据库路径", JsonSerializer.SerializeToElement(resultData));
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"获取数据库路径失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Ljson 类型枚举
    /// 用于标识 Ljson 头部 JData 的类型
    /// </summary>
    public enum LjsonType
    {
        /// <summary>
        /// 组件类型
        /// </summary>
        Component,

        /// <summary>
        /// 脚本类型
        /// </summary>
        Script,

        /// <summary>
        /// 文档类型
        /// </summary>
        Document,

        /// <summary>
        /// 设计类型
        /// </summary>
        Design,

        /// <summary>
        /// Rhino命令类型
        /// </summary>
        Rhino,

        /// <summary>
        /// 其他类型
        /// </summary>
        Other
    }

    /// <summary>
    /// Ljson 类型检测器
    /// 用于判断 Ljson 头部 JData 的类型
    /// </summary>
    public static class LjsonTypeDetector
    {
        /// <summary>
        /// 检测 Ljson 的类型
        /// 通过检查 Ljson 的 Name 属性来判断类型
        /// </summary>
        /// <param name="queue">要检测的 Ljson</param>
        /// <returns>LjsonType 枚举值</returns>
        public static LjsonType DetectType(Ljson queue)
        {
            if (queue == null || string.IsNullOrWhiteSpace(queue.Name))
            {
                return LjsonType.Other;
            }

            // 根据 Name 值判断类型（不区分大小写）
            switch (queue.Name.ToUpperInvariant())
            {
                case "COMPONENT":
                    return LjsonType.Component;

                case "SCRIPT":
                    return LjsonType.Script;

                case "DOCUMENT":
                    return LjsonType.Document;

                case "DESIGN":
                    return LjsonType.Design;

                case "RHINO":
                    return LjsonType.Rhino;

                default:
                    return LjsonType.Other;
            }
        }

        /// <summary>
        /// 获取类型的字符串表示
        /// </summary>
        /// <param name="type">LjsonType 枚举值</param>
        /// <returns>类型字符串</returns>
        public static string ToString(LjsonType type)
        {
            return type.ToString();
        }
    }
}