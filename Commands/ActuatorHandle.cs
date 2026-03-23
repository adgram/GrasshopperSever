using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using Rhino;
using System;


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
        /// <param name="data">输入的JList数据</param>
        /// <returns>执行结果JList</returns>
        public static JList DoComponentCommand(JList data)
        {
            if (data == null || data.Count == 0)
            {
                return JList.CreateErrorJList("输入数据为空");
            }

            // 获取命令类型
            var items = data.ToArray();
            var commandData = items[0];
            if (commandData == null || string.IsNullOrWhiteSpace(commandData.Value))
            {
                return JList.CreateErrorJList("未找到命令类型");
            }

            string commandType = commandData.Value;

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
                        return JList.CreateErrorJList($"未知的 Component 命令: {commandType}");
                }
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"执行 Component 命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 Document 相关命令
        /// </summary>
        /// <param name="data">输入的JList数据</param>
        /// <returns>执行结果JList</returns>
        public static JList DoDocumentCommand(JList data)
        {
            if (data == null || data.Count == 0)
            {
                return JList.CreateErrorJList("输入数据为空");
            }

            // 获取命令类型
            var items = data.ToArray();
            var commandData = items[0];
            if (commandData == null || string.IsNullOrWhiteSpace(commandData.Value))
            {
                return JList.CreateErrorJList("未找到命令类型");
            }

            string commandType = commandData.Value;

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
                        return JList.CreateErrorJList($"未知的 Document 命令: {commandType}");
                }
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"执行 Document 命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行 Rhino 命令
        /// </summary>
        /// <param name="data">输入的JList数据</param>
        /// <returns>执行结果JList</returns>
        public static JList DoRhinoCommand(JList data)
        {
            if (data == null || data.Count == 0)
            {
                return JList.CreateErrorJList("输入数据为空");
            }

            // 获取命令类型
            var items = data.ToArray();
            var commandData = items[0];
            if (commandData == null || string.IsNullOrWhiteSpace(commandData.Value))
            {
                return JList.CreateErrorJList("未找到命令类型");
            }

            string commandType = commandData.Value;

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
                        return JList.CreateErrorJList($"未知的 Rhino 命令: {commandType}");
                }
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"执行 Rhino 命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理运行Rhino脚本命令
        /// </summary>
        private static JList HandleRunScript(JList data)
        {
            try
            {
                string script = data.GetParameter("Script");
                if (string.IsNullOrWhiteSpace(script))
                {
                    return JList.CreateErrorJList("缺少参数: Script");
                }

                // 重要：RhinoApp.RunScript 必须在主文档上下文执行
                // 如果是从非命令线程调用，请确保在 Rhino 的 Idle 句柄或主线程中调度
                var doc = Rhino.RhinoDoc.ActiveDoc;

                // 执行Rhino命令 (true 表示 echo，让命令出现在命令行历史中)
                // 注意：script 前面建议加一个下划线 _ 以确保在非英文版 Rhino 中也能运行
                bool result = Rhino.RhinoApp.RunScript(doc.RuntimeSerialNumber, script, true);

                var response = new JList();
                response.Add(new JData("Result", "执行结果", result.ToString()));
                response.Add(new JData("Script", "执行的命令", script));

                if (result)
                {
                    response.AddSuccessStatus();
                    return response;
                }
                else
                {
                    return JList.CreateErrorJList("命令执行失败或被用户取消");
                }
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"运行Rhino脚本失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理获取最后创建的对象命令
        /// </summary>
        private static JList HandleGetLastCreatedObjects(JList data)
        {
            var doc = Rhino.RhinoDoc.ActiveDoc;
            var response = new JList();

            // 技巧：如果你的 RunScript 刚运行完，可以使用以下逻辑获取：
            doc.Objects.UnselectAll();
            Rhino.RhinoApp.RunScript(doc.RuntimeSerialNumber, "_SelLast", false);
            var selectedObjects = doc.Objects.GetSelectedObjects(false, false);

            if (selectedObjects != null)
            {
                int count = 0;
                foreach (var obj in selectedObjects)
                {
                    response.Add(new JData($"Object_{count}", "对象ID", obj.Id.ToString()));
                    count++;
                }
                response.Add(new JData("Count", "数量", count.ToString()));
                response.AddSuccessStatus();
            }

            return response;
        }
        
        /// <summary>
        /// 处理选择对象命令
        /// 输入：JList包含 Command="SelectObjects", Objects="对象ID列表(逗号分隔)"
        /// 输出：选择结果
        /// </summary>
        private static JList HandleSelectObjects(JList data)
        {
            try
            {
                string objectsParam = data.GetParameter("Objects");
                if (string.IsNullOrWhiteSpace(objectsParam))
                {
                    return JList.CreateErrorJList("缺少参数: Objects");
                }

                var doc = Rhino.RhinoDoc.ActiveDoc;
                if (doc == null)
                {
                    return JList.CreateErrorJList("未找到活动文档");
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

                var response = new JList();
                response.Add(new JData("TotalRequested", "请求选择的对象数量", idStrs.Length.ToString()));
                response.Add(new JData("TotalSelected", "实际选择的对象数量", successCount.ToString()));

                if (successCount > 0)
                {
                    response.AddSuccessStatus();
                }
                else
                {
                    // 如果一个都没选上，可能 ID 全错了，返回具体信息
                    response.Add(new JData("Message", "消息", "未能在文档中找到匹配的 ID 或对象不可选"));
                }

                return response;
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"选择对象失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理从数据库获取所有组件命令
        /// 输入：JList包含 Command="GetAllComponentsFromDB"
        /// 输出：数据库中的所有组件信息
        /// </summary>
        private static JList HandleGetAllComponentsFromDB(JList data)
        {
            try
            {
                var result = ComponentInfo.GetAllComponentsFromDB();
                result.AddSuccessStatus();
                return result;
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"从数据库获取组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过GUID查询组件命令
        /// 输入：JList包含 Command="FindComponentByGuid", Guid="组件GUID"
        /// 输出：组件详细信息
        /// </summary>
        private static JList HandleFindComponentByGuid(JList data)
        {
            try
            {
                string guid = data.GetParameter("Guid");
                if (string.IsNullOrWhiteSpace(guid))
                {
                    return JList.CreateErrorJList("缺少参数: Guid");
                }

                var component = ComponentInfo.FindComponentsByGuid(guid);
                if (component == null)
                {
                    return JList.CreateErrorJList($"未找到GUID为 {guid} 的组件");
                }

                component.AddSuccessStatus();
                return component;
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"通过GUID查询组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过名称查询组件命令
        /// 输入：JList包含 Command="FindComponentByName", Name="组件名称"
        /// 输出：组件详细信息
        /// </summary>
        private static JList HandleFindComponentByName(JList data)
        {
            try
            {
                string name = data.GetParameter("Name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    return JList.CreateErrorJList("缺少参数: Name");
                }

                var component = ComponentInfo.FindComponentsByName(name);
                if (component == null)
                {
                    return JList.CreateErrorJList($"未找到名称为 {name} 的组件");
                }

                component.AddSuccessStatus();
                return component;
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"通过名称查询组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过分类和名称查询组件命令
        /// 输入：JList包含 Command="FindComponentByCategory", Category="分类", SubCategory="子分类"(可选), Name="名称"(可选)
        /// 输出：组件详细信息
        /// </summary>
        private static JList HandleFindComponentByCategory(JList data)
        {
            try
            {
                string category = data.GetParameter("Category");
                string subCategory = data.GetParameter("SubCategory");
                string name = data.GetParameter("Name");

                if (string.IsNullOrWhiteSpace(category) && string.IsNullOrWhiteSpace(subCategory) && string.IsNullOrWhiteSpace(name))
                {
                    return JList.CreateErrorJList("至少需要提供一个参数: Category, SubCategory 或 Name");
                }

                var component = ComponentInfo.FindComponentsByCategory(category, subCategory, name);
                if (component == null)
                {
                    return JList.CreateErrorJList($"未找到符合条件的组件");
                }

                component.AddSuccessStatus();
                return component;
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"通过分类查询组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过名称模糊搜索组件命令
        /// 输入：JList包含 Command="SearchComponentsByName", Name="搜索关键词"
        /// 输出：匹配的组件列表
        /// </summary>
        private static JList HandleSearchComponentsByName(JList data)
        {
            try
            {
                string name = data.GetParameter("Name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    return JList.CreateErrorJList("缺少参数: Name");
                }

                var components = ComponentInfo.SearchComponentsByName(name);
                if (components == null || components.Count == 0)
                {
                    return JList.CreateErrorJList($"未找到名称包含 {name} 的组件");
                }

                // 将ComponentJList列表合并为一个JList
                var result = new JList();
                result.Add(new JData("Count", "匹配的组件数量", components.Count.ToString()));

                for (int i = 0; i < components.Count; i++)
                {
                    var comp = components[i];
                    var items = comp.ToArray();
                    foreach (var item in items)
                    {
                        result.Add(new JData(
                            $"Component_{i}_{item.Name}",
                            $"组件{i}的{item.Description}",
                            item.Value
                        ));
                    }
                }

                result.AddSuccessStatus();
                return result;
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"搜索组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理保存文档命令
        /// 输入：JList包含 Command="SaveDocument", FilePath="文件路径"(可选)
        /// 输出：保存结果
        /// </summary>
        private static JList HandleSaveDocument(JList data)
        {
            try
            {
                string filePath = data.GetParameter("FilePath");
                var result = DocumentInfo.SaveDocument(filePath);
                return result;
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"保存文档失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理打开文档命令
        /// 输入：JList包含 Command="LoadDocument", FilePath="文件路径"
        /// 输出：打开结果
        /// </summary>
        private static JList HandleLoadDocument(JList data)
        {
            try
            {
                string filePath = data.GetParameter("FilePath");
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return JList.CreateErrorJList("缺少参数: FilePath");
                }

                var result = DocumentInfo.LoadDocument(filePath);
                return result;
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"打开文档失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理获取数据库路径命令
        /// 输入：JList包含 Command="DatabasePath"
        /// 输出：数据库路径信息
        /// </summary>
        private static JList HandleDatabasePath(JList data)
        {
            try
            {
                var path = DatabaseManager.DatabasePath;
                var result = new JList();
                result.Add(new JData("DatabasePath", "数据库路径", path));
                result.AddSuccessStatus();
                return result;
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"获取数据库路径失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// JList 类型枚举
    /// 用于标识 JList 头部 JData 的类型
    /// </summary>
    public enum JListType
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
    /// JList 类型检测器
    /// 用于判断 JList 头部 JData 的类型
    /// </summary>
    public static class JListTypeDetector
    {
        /// <summary>
        /// 检测 JList 的类型
        /// 通过检查队列中第一个 JData 的 Name 值来判断类型
        /// </summary>
        /// <param name="queue">要检测的 JList</param>
        /// <returns>JListType 枚举值</returns>
        public static JListType DetectType(JList queue)
        {
            if (queue == null || queue.Count == 0)
            {
                return JListType.Other;
            }

            // 获取列表中的第一个 JData
            var items = queue.ToArray();
            var firstData = items[0];
            if (firstData == null || string.IsNullOrWhiteSpace(firstData.Value))
            {
                return JListType.Other;
            }

            // 根据 Value 值判断类型（不区分大小写）
            switch (firstData.Name.ToUpperInvariant())
            {
                case "COMPONENT":
                    return JListType.Component;

                case "SCRIPT":
                    return JListType.Script;

                case "DOCUMENT":
                    return JListType.Document;

                case "DESIGN":
                    return JListType.Design;

                case "RHINO":
                    return JListType.Rhino;

                default:
                    return JListType.Other;
            }
        }

        /// <summary>
        /// 获取类型的字符串表示
        /// </summary>
        /// <param name="type">JListType 枚举值</param>
        /// <returns>类型字符串</returns>
        public static string ToString(JListType type)
        {
            return type.ToString();
        }
    }
}