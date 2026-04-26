using GrasshopperSever.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace GrasshopperSever.Commands
{
    /// <summary>
    /// 用于执行特殊的指令
    /// </summary>
    public class ActuatorHandle
    {
        /// <summary>
        /// 这里处理数据
        /// </summary>
        public static Ljson DoCommand(Ljson json, ref string out_data, string output_link)
        {
            var out_d = json.GetParameterString("OUTPUT");
            JsonElement out_data_info = default;
            if (!string.IsNullOrEmpty(out_d)) out_data = out_d;
            if (!string.IsNullOrEmpty(out_data) && !string.IsNullOrEmpty(output_link))
            {
                out_data_info = JsonSerializer.SerializeToElement(output_link);
            }
            Ljson result = null;
            if (json != null && !string.IsNullOrWhiteSpace(json.Name))
            {
                // 根据 Name 值判断类型（不区分大小写）
                result = json.Name.ToUpperInvariant() switch
                {
                    "COMPONENT" => DoComponentCommand(json),
                    "DOCUMENT" => DoDocumentCommand(json),
                    "DESIGN" => DoDesignCommand(json),
                    "DESIGNLIST" => DoDesignListCommand(json),
                    "RHINO" => DoRhinoCommand(json),
                    _ => null,
                };
            }
            if (out_data_info.ValueKind != JsonValueKind.Undefined)
            {
                result ??= new Ljson("OUTPUTDATA", "output端连接信息", default);
                result.SetParameter("OUTPUTDATA", out_data_info);
            }
            return result;
        }

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
            if (commandElement == null || commandElement?.ValueKind != JsonValueKind.String)
            {
                return Ljson.CreateErrorLjson("未找到命令类型");
            }

            string commandType = commandElement?.GetString();

            try
            {
                return commandType.ToUpperInvariant() switch
                {
                    "GETALLCOMPONENTS" => HandleGetAllComponentsFromDB(data),
                    "FINDCOMPONENTBYGUID" => HandleFindComponentByGuid(data),
                    "FINDCOMPONENTBYNAME" => HandleFindComponentByName(data),
                    "FINDCOMPONENTBYCATEGORY" => HandleFindComponentByCategory(data),
                    "SEARCHCOMPONENTSBYNAME" => HandleSearchComponentsByName(data),
                    _ => Ljson.CreateErrorLjson($"未知的 Component 命令: {commandType}"),
                };
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
            if (commandElement == null || commandElement?.ValueKind != JsonValueKind.String)
            {
                return Ljson.CreateErrorLjson("未找到命令类型");
            }

            string commandType = commandElement?.GetString();

            try
            {
                return commandType.ToUpperInvariant() switch
                {
                    "SAVEDOCUMENT" => HandleSaveDocument(data),
                    "LOADDOCUMENT" => HandleLoadDocument(data),
                    "GETALLOBJECTS" => HandleGetAllObjects(data),
                    "GETOBJECT" => HandleGetObject(data),
                    "DATABASEPATH" => HandleDatabasePath(data),
                    _ => Ljson.CreateErrorLjson($"未知的 Document 命令: {commandType}"),
                };
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
            if (commandElement == null || commandElement?.ValueKind != JsonValueKind.String)
            {
                return Ljson.CreateErrorLjson("未找到命令类型");
            }

            string commandType = commandElement?.GetString();

            try
            {
                return commandType.ToUpperInvariant() switch
                {
                    "RHINOSCRIPT" => HandleRunRhinoScript(data),
                    "GETLASTCREATEDOBJECTS" => HandleGetLastCreatedObjects(data),
                    "SELECTOBJECTS" => HandleSelectObjects(data),
                    "GETANDSELECTLASTOBJECTS" => HandleGetAndSelectLastObjects(data),
                    _ => Ljson.CreateErrorLjson($"未知的 Rhino 命令: {commandType}"),
                };
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
            if (commandElement == null || commandElement?.ValueKind != JsonValueKind.String)
            {
                return Ljson.CreateErrorLjson("未找到命令类型");
            }

            string commandType = commandElement?.GetString();

            try
            {
                return commandType.ToUpperInvariant() switch
                {
                    "ADDCOMPONENTBYGUID" => HandleAddComponentByGuid(data),
                    "ADDCOMPONENTBYNAME" => HandleAddComponentByName(data),
                    "ADDPARAMWITHVALUE" => HandleAddParamWithValue(data),
                    "REMOVECOMPONENT" => HandleRemoveComponent(data),
                    "SETPARAMVALUE" => HandleSetParamValue(data),
                    "CONNECTCOMPONENTS" => HandleConnectComponents(data),
                    "DISCONNECTCOMPONENTS" => HandleDisconnectComponents(data),
                    _ => Ljson.CreateErrorLjson($"未知的 Design 命令: {commandType}"),
                };
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"执行 Design 命令时出错: {ex.Message}");
            }
        }

        private static readonly Dictionary<string, int> ListCommandParamCount = new(StringComparer.OrdinalIgnoreCase)
            {
                { "ac", 4 },
                { "ap", 6 },
                { "dp", 1 },
                { "sv", 3 },
                { "cc", 4 },
                { "dc", 4 }
            };

        public static Ljson DoDesignListCommand(Ljson data)
        {
            if (data == null)
            {
                return Ljson.CreateErrorLjson("输入数据为空");
            }
            // 按顺序执行value
            string design = data.Value.GetString();
            if (string.IsNullOrWhiteSpace(design))
            {
                return Ljson.CreateErrorLjson("设计字符串为空");
            }
            // 按空格分割，得到命令与参数的原始片段（忽略空项）
            var designsegs = new List<string>(Tokenize(design));
            // 定义命令及其所需的参数个数
            // 存储解析出的命令和参数
            Ljson lastResult = null;
            int idx = 0;

            while (idx < designsegs.Count)
            {
                string cmd = designsegs[idx];
                // 检查命令是否合法
                if (!ListCommandParamCount.TryGetValue(cmd, out int paramCount))
                {
                    return Ljson.CreateErrorLjson($"未知命令: {cmd}");
                }
                // 检查剩余参数是否足够
                if (idx + paramCount >= designsegs.Count)
                {
                    return Ljson.CreateErrorLjson($"命令 {cmd} 参数不足，需要 {paramCount} 个参数，但实际剩余 token 不足");
                }
                // 提取参数列表
                List<string> args = designsegs.GetRange(idx + 1, paramCount);
                bool tag = cmd.ToLowerInvariant() switch
                {
                    "ac" => HandleListAddComponentByName(args[0], args[1], args[2], args[3], ref lastResult),
                    "ap" => HandleListAddParamWithValue(args[0], args[1], args[2], args[3], args[4], args[5], ref lastResult),
                    "dp" => HandleListRemoveComponent(args[0], ref lastResult),
                    "sv" => HandleListSetParamValue(args[0], args[1], args[2], ref lastResult),
                    "cc" => HandleListConnectComponents(args[0], args[1], args[2], args[3], ref lastResult),
                    "dc" => HandleListDisconnectComponents(args[0], args[1], args[2], args[3], ref lastResult),
                    _ => false,
                };
                if (tag)
                {
                    // 移动索引到下一个命令
                    idx += 1 + paramCount;
                }
                else break;
            }
            return lastResult;
        }

        /// <summary>
        /// 处理字符串中用引号包裹的对象
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static List<string> Tokenize(string input)
        {
            List<string> tokens = [];
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    // 不将引号本身加入 token
                    continue;
                }

                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    // 空格分隔，提交当前 token
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            // 最后一个 token
            if (current.Length > 0)
                tokens.Add(current.ToString());

            return tokens;
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
                var nickName = data.GetParameter("UserNick")?.GetString();

                if (string.IsNullOrWhiteSpace(componentGuid))
                {
                    return Ljson.CreateErrorLjson("缺少 ComponentGuid 参数");
                }

                if (!xElement.HasValue || !yElement.HasValue)
                {
                    return Ljson.CreateErrorLjson("缺少坐标参数（X, Y）");
                }

                var x = xElement?.GetDouble();
                var y = yElement?.GetDouble();

                var point = new PointF((float)x, (float)y);
                var result = ComponentExchange.AddComponentByGuid(componentGuid, point, nickName);

                return result;
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
                var nickName = data.GetParameter("UserNick")?.GetString();

                if (string.IsNullOrWhiteSpace(componentName))
                {
                    return Ljson.CreateErrorLjson("缺少 ComponentName 参数");
                }

                if (!xElement.HasValue || !yElement.HasValue)
                {
                    return Ljson.CreateErrorLjson("缺少坐标参数（X, Y）");
                }

                var x = xElement?.GetDouble();
                var y = yElement?.GetDouble();

                var point = new PointF((float)x, (float)y);
                var result = ComponentExchange.AddComponentByName(componentName, point, nickName);
                if (result != null) return result;
                return Ljson.CreateErrorLjson("添加组件失败");
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"添加组件失败: {ex.Message}");
            }
        }

        private static Ljson HandleAddParamWithValue(Ljson data)
        {
            try
            {
                var componentName = data.GetParameterString("ParamName");
                var xElement = data.GetParameter("X");
                var yElement = data.GetParameter("Y");
                var path = data.GetParameterString("Path");
                var value = data.GetParameterString("Value");
                var nickName = data.GetParameter("UserNick")?.GetString();

                if (string.IsNullOrWhiteSpace(componentName))
                {
                    return Ljson.CreateErrorLjson("缺少 ParamName 参数");
                }

                if (!xElement.HasValue || !yElement.HasValue)
                {
                    return Ljson.CreateErrorLjson("缺少坐标参数（X, Y）");
                }

                var x = xElement?.GetDouble();
                var y = yElement?.GetDouble();

                var point = new PointF((float)x, (float)y);
                var result = CommonlyParam.AddParamWithValue(componentName, point, path, value, nickName);

                return result;
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"添加组件失败: {ex.Message}");
            }
        }

        private static string GetGuidOrNick(Ljson data)
        {

            var tag = data.GetParameterString("InstanceGuid");

            if (string.IsNullOrWhiteSpace(tag))
            {
                tag = data.GetParameterString("UserNick");
                if (string.IsNullOrWhiteSpace(tag))
                {
                    return null;
                }
            }
            return tag;
        }

        /// <summary>
        /// 处理移除组件命令
        /// </summary>
        private static Ljson HandleRemoveComponent(Ljson data)
        {
            try
            {
                var instanceTag = GetGuidOrNick(data);

                if (string.IsNullOrWhiteSpace(instanceTag))
                {
                    return Ljson.CreateErrorLjson("缺少 InstanceGuid 或 NickName 参数");
                }

                var result = ComponentExchange.RemoveComponent(instanceTag);

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
        private static Ljson HandleSetParamValue(Ljson data)
        {
            try
            {
                var instanceTag = GetGuidOrNick(data);
                var path = data.GetParameterString("Path");
                var value = data.GetParameterString("Value");

                if (string.IsNullOrWhiteSpace(instanceTag))
                {
                    return Ljson.CreateErrorLjson("缺少 InstanceGuid 或 NickName 参数");
                }


                if (string.IsNullOrWhiteSpace(value))
                {
                    return Ljson.CreateErrorLjson("缺少 Value 参数");
                }

                var result = CommonlyParam.SetParamValue(instanceTag, path, value);

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
                var fromTag = data.GetParameterString("FromGuid");
                if (string.IsNullOrWhiteSpace(fromTag))
                {
                    fromTag = data.GetParameterString("FromNick");
                }
                var fromParameter = data.GetParameterString("FromParameter");
                var ToTag = data.GetParameterString("ToGuid");
                if (string.IsNullOrWhiteSpace(ToTag))
                {
                    ToTag = data.GetParameterString("ToNick");
                }
                var toParameter = data.GetParameterString("ToParameter");

                if (string.IsNullOrWhiteSpace(fromTag) || string.IsNullOrWhiteSpace(ToTag))
                {
                    return Ljson.CreateErrorLjson("缺少必要参数（fromTag, ToTag）");
                }

                var result = ComponentExchange.ConnectComponents(fromTag, fromParameter, ToTag, toParameter);

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
                var fromTag = data.GetParameterString("FromGuid");
                if (string.IsNullOrWhiteSpace(fromTag))
                {
                    fromTag = data.GetParameterString("FromNick");
                }
                var fromParameter = data.GetParameterString("FromParameter");
                var ToTag = data.GetParameterString("ToGuid");
                if (string.IsNullOrWhiteSpace(ToTag))
                {
                    ToTag = data.GetParameterString("ToNick");
                }
                var toParameter = data.GetParameterString("ToParameter");

                if (string.IsNullOrWhiteSpace(fromTag) || string.IsNullOrWhiteSpace(ToTag))
                {
                    return Ljson.CreateErrorLjson("缺少必要参数（fromTag, ToTag）");
                }

                var result = ComponentExchange.DisconnectComponents(fromTag, fromParameter, ToTag, toParameter);

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

        public static bool HandleListAddComponentByName(string name, string x, string y, string nick, ref Ljson result)
        {
            if ((float.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out float x1))
                && (float.TryParse(y, NumberStyles.Float, CultureInfo.InvariantCulture, out float y1)))
            {
                var point = new PointF(x1, y1);
                result = ComponentExchange.AddComponentByName(name, point, nick);
                return result != null;
            }

            return false;
        }
        public static bool HandleListAddParamWithValue(string name, string x, string y, string path, string value, string nick, ref Ljson result)
        {
            if ((float.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out float x1))
                && (float.TryParse(y, NumberStyles.Float, CultureInfo.InvariantCulture, out float y1)))
            {
                var point = new PointF(x1, y1);
                result = CommonlyParam.AddParamWithValue(name, point, path, value, nick);
                return result != null;
            }
            return false;
        }
        public static bool HandleListRemoveComponent(string nick, ref Ljson result)
        {
            var b = ComponentExchange.RemoveComponent(nick);
            result = b ? Ljson.CreateOKLjson("组件移除成功") : Ljson.CreateErrorLjson("组件移除失败");
            return b;
        }
        public static bool HandleListSetParamValue(string nick, string path, string value, ref Ljson result)
        {
            var b = CommonlyParam.SetParamValue(nick, path, value);
            result = b ? Ljson.CreateOKLjson("Param值设置成功") : Ljson.CreateErrorLjson("Param值设置失败");
            return b;
        }
        public static bool HandleListConnectComponents(string fromNick, string fromParam, string toNick, string toParam, ref Ljson result)
        {
            var b = ComponentExchange.ConnectComponents(fromNick, fromParam, toNick, toParam);
            result = b ? Ljson.CreateOKLjson("组件连接成功") : Ljson.CreateErrorLjson("组件连接失败");
            return b;
        }
        public static bool HandleListDisconnectComponents(string fromNick, string fromParam, string toNick, string toParam, ref Ljson result)
        {
            var b = ComponentExchange.DisconnectComponents(fromNick, fromParam, toNick, toParam);
            result = b ? Ljson.CreateOKLjson("组件断开成功") : Ljson.CreateErrorLjson("组件断开失败");
            return b;
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
            string objectsParam = data.GetParameterString("Objects");
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
            return ComponentInfo.GetAllComponentsFromDB();
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
                string guid = data.GetParameterString("Guid");
                if (string.IsNullOrWhiteSpace(guid))
                {
                    return Ljson.CreateErrorLjson("缺少参数: Guid");
                }

                var component = ComponentInfo.FindComponentsByGuid(guid)
                    ?? Ljson.CreateErrorLjson($"未找到GUID为 {guid} 的组件");
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
                string name = data.GetParameterString("Name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Ljson.CreateErrorLjson("缺少参数: Name");
                }

                var component = ComponentInfo.FindComponentsByName(name)
                    ?? Ljson.CreateErrorLjson($"未找到名称为 {name} 的组件");
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
                string category = data.GetParameterString("Category");
                string subCategory = data.GetParameterString("SubCategory");
                string name = data.GetParameterString("Name");

                if (string.IsNullOrWhiteSpace(category) && string.IsNullOrWhiteSpace(subCategory) && string.IsNullOrWhiteSpace(name))
                {
                    return Ljson.CreateErrorLjson("至少需要提供一个参数: Category, SubCategory 或 Name");
                }

                var component = ComponentInfo.FindComponentsByCategory(category, subCategory, name)
                        ?? Ljson.CreateErrorLjson($"未找到符合条件的组件");
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
                    { "Components", components.Select(c => c.Value).ToList() }
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
            var path = DatabaseManager.DatabasePath;
            var resultData = new Dictionary<string, object>
            {
                { "DatabasePath", path }
            };
            return new Ljson("DatabasePath", "获取数据库路径", JsonSerializer.SerializeToElement(resultData));
        }

        /// <summary>
        /// 处理获取所有对象命令
        /// 输入：Ljson包含 Command="GetAllObjects"
        /// 输出：文档中所有对象的信息
        /// </summary>
        private static Ljson HandleGetAllObjects(Ljson data)
        {
            return DocumentInfo.GetAllObjects();
        }

        private static Ljson HandleGetObject(Ljson data)
        {
            string tag = GetGuidOrNick(data);
            if (string.IsNullOrWhiteSpace(tag))
            {
                return Ljson.CreateErrorLjson("缺少参数: tag");
            }
            return DocumentInfo.GetObject(tag);
        }
    }
}