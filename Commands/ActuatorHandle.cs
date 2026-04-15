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
            if (commandElement == null || commandElement.Value.ValueKind != JsonValueKind.String)
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
            if (commandElement == null || commandElement.Value.ValueKind != JsonValueKind.String)
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

                    case "GETALLOBJECTS":
                        return HandleGetAllObjects(data);

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
            if (commandElement == null || commandElement.Value.ValueKind != JsonValueKind.String)
            {
                return Ljson.CreateErrorLjson("未找到命令类型");
            }

            string commandType = commandElement.Value.GetString();

            try
            {
                switch (commandType.ToUpperInvariant())
                {
                    case "RHINOSCRIPT":
                        return HandleRunRhinoScript(data);

                    case "GETLASTCREATEDOBJECTS":
                        return HandleGetLastCreatedObjects(data);

                    case "SELECTOBJECTS":
                        return HandleSelectObjects(data);

                    case "GETANDSELECTLASTOBJECTS":
                        return HandleGetAndSelectLastObjects(data);

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
        /// 执行 Design 相关命令（组件布局和连接）
        /// </summary>
        /// <param name="data">输入的Ljson数据</param>
        /// <returns>执行结果Ljson</returns>
        public static Ljson DoDesignCommand(Ljson data)
        {
            if (data == null)
            {
                return Ljson.CreateErrorLjson("输入数据为空");
            }

            // 获取命令类型
            var commandElement = data.GetParameter("Command");
            if (commandElement == null || commandElement.Value.ValueKind != JsonValueKind.String)
            {
                return Ljson.CreateErrorLjson("未找到命令类型");
            }

            string commandType = commandElement.Value.GetString();

            try
            {
                switch (commandType.ToUpperInvariant())
                {
                    case "ADDCOMPONENTBYGUID":
                        return HandleAddComponentByGuid(data);

                    case "ADDCOMPONENTBYNAME":
                        return HandleAddComponentByName(data);

                    case "REMOVECOMPONENT":
                        return HandleRemoveComponent(data);

                    case "SETCOMPONENTVALUE":
                        return HandleSetComponentValue(data);

                    case "CONNECTCOMPONENTS":
                        return HandleConnectComponents(data);

                    case "DISCONNECTCOMPONENTS":
                        return HandleDisconnectComponents(data);

                    default:
                        return Ljson.CreateErrorLjson($"未知的 Design 命令: {commandType}");
                }
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"执行 Design 命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过 GUID 添加组件命令
        /// </summary>
        private static Ljson HandleAddComponentByGuid(Ljson data)
        {
            try
            {
                var componentGuid = data.GetParameterString("ComponentGuid");
                var xElement = data.GetParameter("X");
                var yElement = data.GetParameter("Y");

                if (string.IsNullOrWhiteSpace(componentGuid))
                {
                    return Ljson.CreateErrorLjson("缺少 ComponentGuid 参数");
                }

                if (!xElement.HasValue || !yElement.HasValue)
                {
                    return Ljson.CreateErrorLjson("缺少坐标参数（X, Y）");
                }

                var x = xElement.Value.GetDouble();
                var y = yElement.Value.GetDouble();

                var point = new System.Drawing.PointF((float)x, (float)y);
                var result = ComponentExchange.AddComponentByGuid(componentGuid, point);

                return Ljson.CreateOKLjson($"组件添加成功{result.Value}");
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"添加组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过名称添加组件命令
        /// </summary>
        private static Ljson HandleAddComponentByName(Ljson data)
        {
            try
            {
                var componentName = data.GetParameterString("ComponentName");
                var xElement = data.GetParameter("X");
                var yElement = data.GetParameter("Y");

                if (string.IsNullOrWhiteSpace(componentName))
                {
                    return Ljson.CreateErrorLjson("缺少 ComponentName 参数");
                }

                if (!xElement.HasValue || !yElement.HasValue)
                {
                    return Ljson.CreateErrorLjson("缺少坐标参数（X, Y）");
                }

                var x = xElement.Value.GetDouble();
                var y = yElement.Value.GetDouble();

                var point = new System.Drawing.PointF((float)x, (float)y);
                var result = ComponentExchange.AddComponentByName(componentName, point);

                return Ljson.CreateOKLjson($"组件添加成功{result.Value}");
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"添加组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理移除组件命令
        /// </summary>
        private static Ljson HandleRemoveComponent(Ljson data)
        {
            try
            {
                var instanceGuid = data.GetParameterString("InstanceGuid");

                if (string.IsNullOrWhiteSpace(instanceGuid))
                {
                    return Ljson.CreateErrorLjson("缺少 InstanceGuid 参数");
                }

                var result = ComponentExchange.RemoveComponent(instanceGuid);

                if (result)
                {
                    return Ljson.CreateOKLjson("组件移除成功");
                }
                else
                {
                    return Ljson.CreateErrorLjson("组件移除失败");
                }
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"移除组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理设置组件值命令
        /// </summary>
        private static Ljson HandleSetComponentValue(Ljson data)
        {
            try
            {
                var instanceGuid = data.GetParameterString("InstanceGuid");
                var value = data.GetParameterString("Value");

                if (string.IsNullOrWhiteSpace(instanceGuid))
                {
                    return Ljson.CreateErrorLjson("缺少 InstanceGuid 参数");
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    return Ljson.CreateErrorLjson("缺少 Value 参数");
                }

                var result = ComponentExchange.SetComponentValue(instanceGuid, value);

                if (result)
                {
                    return Ljson.CreateOKLjson("组件值设置成功");
                }
                else
                {
                    return Ljson.CreateErrorLjson("组件值设置失败");
                }
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"设置组件值失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理连接组件命令
        /// </summary>
        private static Ljson HandleConnectComponents(Ljson data)
        {
            try
            {
                var fromGuid = data.GetParameterString("FromGuid");
                var fromParameter = data.GetParameterString("FromParameter");
                var toGuid = data.GetParameterString("ToGuid");
                var toParameter = data.GetParameterString("ToParameter");

                if (string.IsNullOrWhiteSpace(fromGuid) || string.IsNullOrWhiteSpace(fromParameter) ||
                    string.IsNullOrWhiteSpace(toGuid) || string.IsNullOrWhiteSpace(toParameter))
                {
                    return Ljson.CreateErrorLjson("缺少必要参数（FromGuid, FromParameter, ToGuid, ToParameter）");
                }

                var result = ComponentExchange.ConnectComponents(fromGuid, fromParameter, toGuid, toParameter);

                if (result)
                {
                    return Ljson.CreateOKLjson("组件连接成功");
                }
                else
                {
                    return Ljson.CreateErrorLjson("组件连接失败");
                }
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"连接组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理断开组件连接命令
        /// </summary>
        private static Ljson HandleDisconnectComponents(Ljson data)
        {
            try
            {
                var fromGuid = data.GetParameterString("FromGuid");
                var fromParameter = data.GetParameterString("FromParameter");
                var toGuid = data.GetParameterString("ToGuid");
                var toParameter = data.GetParameterString("ToParameter");

                if (string.IsNullOrWhiteSpace(fromGuid) || string.IsNullOrWhiteSpace(fromParameter) ||
                    string.IsNullOrWhiteSpace(toGuid) || string.IsNullOrWhiteSpace(toParameter))
                {
                    return Ljson.CreateErrorLjson("缺少必要参数（FromGuid, FromParameter, ToGuid, ToParameter）");
                }

                var result = ComponentExchange.DisconnectComponents(fromGuid, fromParameter, toGuid, toParameter);

                if (result)
                {
                    return Ljson.CreateOKLjson("组件连接断开成功");
                }
                else
                {
                    return Ljson.CreateErrorLjson("组件连接断开失败");
                }
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"断开组件连接失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理运行Rhino脚本命令
        /// </summary>
        private static Ljson HandleRunRhinoScript(Ljson data)
        {
            try
            {
                string script = data.GetParameterString("Script");
                if (string.IsNullOrWhiteSpace(script))
                {
                    return Ljson.CreateErrorLjson("缺少参数: Script");
                }
                return RhinoCommand.RinoRunScript(script);
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
            return RhinoCommand.GetLastCreatedObjects();
        }
        
        /// <summary>
        /// 处理选择对象命令
        /// 输入：Ljson包含 Command="SelectObjects", Objects="对象ID列表(逗号分隔)"
        /// 输出：选择结果
        /// </summary>
        private static Ljson HandleSelectObjects(Ljson data)
        {
            string objectsParam =data.GetParameterString("Objects");
            if (string.IsNullOrWhiteSpace(objectsParam))
            {
                return Ljson.CreateErrorLjson("缺少参数: Objects");
            }

            return RhinoCommand.SelectObjects(objectsParam);
        }

        /// <summary>
        /// 处理获取并选择最后创建的对象命令
        /// 输入：Ljson包含 Command="GetAndSelectLastObjects"
        /// 输出：对象信息和选择结果
        /// </summary>
        private static Ljson HandleGetAndSelectLastObjects(Ljson data)
        {
            try
            {
                // 1. 获取最后创建的对象
                var getObjectsResult = RhinoCommand.GetLastCreatedObjects();

                // 检查是否成功获取对象
                if (getObjectsResult.Name == "Error" || getObjectsResult.Value.ValueKind != JsonValueKind.Object)
                {
                    return getObjectsResult; // 返回错误信息
                }

                // 2. 将结果转换为 SelectObjects 需要的格式
                string objectsParam = RhinoCommand.ConvertToSelectObjectsFormat(getObjectsResult);

                if (string.IsNullOrWhiteSpace(objectsParam))
                {
                    return Ljson.CreateErrorLjson("未找到任何对象");
                }

                // 3. 选择这些对象
                var selectResult = RhinoCommand.SelectObjects(objectsParam);

                // 4. 合并返回结果
                var combinedResult = new Dictionary<string, object>
                {
                    { "Objects", getObjectsResult.Value },
                    { "Selection", selectResult.Value }
                };

                return new Ljson("GetAndSelectLastObjects", "获取并选择最后创建的对象", JsonSerializer.SerializeToElement(combinedResult));
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"获取并选择对象失败: {ex.Message}");
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
                string guid =data.GetParameterString("Guid");
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
                string name =data.GetParameterString("Name");
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
                string category =data.GetParameterString("Category");
                string subCategory =data.GetParameterString("SubCategory");
                string name =data.GetParameterString("Name");

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
                string name = data.GetParameterString("Name");
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
                string filePath = data.GetParameterString("FilePath");
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
                string filePath = data.GetParameterString("FilePath");
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

        /// <summary>
        /// 处理获取所有对象命令
        /// 输入：Ljson包含 Command="GetAllObjects"
        /// 输出：文档中所有对象的信息
        /// </summary>
        private static Ljson HandleGetAllObjects(Ljson data)
        {
            try
            {
                return DocumentInfo.GetAllObjects();
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"获取文档对象失败: {ex.Message}");
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