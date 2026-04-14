using Grasshopper;
using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace GrasshopperSever.Commands
{
    public static class ParamExchange
    {
        /// <summary>
        /// 创建组件Param信息Ljson
        /// </summary>
        public static Ljson ParamLjson(string paramGuid, string instanceGuid,
            string name, string nickName, string description,
            string typeName, bool optional, GH_ParamAccess access,
            GH_DataMapping mapping, bool reverse, bool simplify,
            string inputs, string outputs)
        {
            var data = new Dictionary<string, object>
            {
                { "ParamGuid", paramGuid },
                { "InstanceGuid", instanceGuid },
                { "Name", name },
                { "NickName", nickName },
                { "Description", description },
                { "TypeName", typeName },
                { "Optional", optional },
                { "Access", access.ToString() },
                { "Mapping", mapping.ToString() },
                { "Reverse", reverse },
                { "Simplify", simplify },
                { "Inputs", inputs },
                { "Outputs", outputs }
            };

            return new Ljson("Param", "参数信息", JsonSerializer.SerializeToElement(data));
        }

        /// <summary>
        /// 核心工厂方法：根据 ParamGuid 创建一个空的 IGH_Param 实例
        /// </summary>
        public static IGH_Param CreateParamFromGuid(string paramGuidStr)
        {
            if (!Guid.TryParse(paramGuidStr, out Guid id)) return null;

            // 在 Grasshopper 对象库中查找对应的代理对象
            var proxy = Instances.ComponentServer.EmitObjectProxy(id);
            if (proxy == null) return null;

            // 实例化对象
            IGH_Param param = proxy.CreateInstance() as IGH_Param;
            return param;
        }

        /// <summary>
        /// 反序列化：将 Ljson 中的字符串数据还原到 IGH_Param 实例中
        /// </summary>
        public static void FillParamFromLjson(Ljson data, IGH_Param targetParam)
        {
            if (targetParam == null) return;
            // 1. 名称
            targetParam.Name = data.GetParameterString("Name");
            targetParam.NickName = data.GetParameterString("NickName");
            targetParam.Description = data.GetParameterString("Description");

            // 2. 布尔属性 (从字符串还原)
            if (bool.TryParse(data.GetParameterString("Optional"), out bool opt))
                targetParam.Optional = opt;

            if (bool.TryParse(data.GetParameterString("Reverse"), out bool rev))
                targetParam.Reverse = rev;

            if (bool.TryParse(data.GetParameterString("Simplify"), out bool sim))
                targetParam.Simplify = sim;

            // 3. 枚举属性 (使用 Enum.TryParse 忽略大小写还原)
            if (Enum.TryParse(data.GetParameterString("Access"), true, out GH_ParamAccess acc))
                targetParam.Access = acc;

            if (Enum.TryParse(data.GetParameterString("Mapping"), true, out GH_DataMapping map))
                targetParam.DataMapping = map;
        }

        public static Ljson ParamToLjson(IGH_Param param)
        {
            return ParamLjson(
                param.ComponentGuid.ToString(),   // 电池类型识别码
                param.InstanceGuid.ToString(),    // 画布实例识别码
                param.Name,
                param.NickName,
                param.Description,
                param.TypeName,
                param.Optional,                   // 直接传递 bool 值
                param.Access,                     // 直接传递枚举值 (item, list, tree)
                param.DataMapping,                // 直接传递枚举值 (none, flatten, graft)
                param.Reverse,                    // 直接传递 bool 值
                param.Simplify,                   // 直接传递 bool 值
                JsonSerializer.Serialize(param.Sources.Select(s => s.InstanceGuid.ToString())),
                JsonSerializer.Serialize(param.Recipients.Select(s => s.InstanceGuid.ToString()))
            );
        }

        /// <summary>
        /// 从Ljson创建IGH_Param实例
        /// </summary>
        private static IGH_Param ParamFromLjson(Ljson data)
        {
            if (data == null)
                return null;
            try
            {
                // 从Ljson中提取ParamGuid创建参数
                string paramGuid = data.GetParameterString("ParamGuid");
                IGH_Param param = null;

                if (!string.IsNullOrWhiteSpace(paramGuid))
                {
                    param = CreateParamFromGuid(paramGuid);
                }

                if (param == null)
                {
                    param = new Grasshopper.Kernel.Parameters.Param_GenericObject();
                }

                // 填充参数属性
                param.Name = data.GetParameterString("Name") ?? "";
                param.NickName = data.GetParameterString("NickName") ?? "";
                param.Description = data.GetParameterString("Description") ?? "";

                // 布尔属性
                if (bool.TryParse(data.GetParameterString("Optional"), out bool opt))
                    param.Optional = opt;
                if (bool.TryParse(data.GetParameterString("Reverse"), out bool rev))
                    param.Reverse = rev;
                if (bool.TryParse(data.GetParameterString("Simplify"), out bool sim))
                    param.Simplify = sim;

                // 枚举属性
                if (Enum.TryParse(data.GetParameterString("Access"), true, out GH_ParamAccess acc))
                    param.Access = acc;
                if (Enum.TryParse(data.GetParameterString("Mapping"), true, out GH_DataMapping map))
                    param.DataMapping = map;

                return param;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"从Ljson创建参数失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取参数定义信息（不包含连线，用于组件定义）
        /// </summary>
        /// <param name="parameters">参数列表</param>
        /// <returns>参数定义信息JSON字符串</returns>
        public static Ljson SerializeParamDefinitions(IList<IGH_Param> parameters)
        {
            var paramLjsons = new List<JsonElement>();
            for (int i = 0; i < parameters.Count; i++)
            {
                paramLjsons.Add(ParamToLjson(parameters[i]).Value);
            }
            return new Ljson("IList<IGH_Param>", "将IList<IGH_Param>转换为Ljson", JsonSerializer.SerializeToElement(paramLjsons));
        }

        /// <summary>
        /// 解析参数定义JSON字符串为IGH_Param列表
        /// 与SerializeParamDefinitions成对使用，解析LjsonHelper.SerializeLjsonArray的输出
        /// </summary>
        public static List<IGH_Param> DeserializeParamDefinitions(Ljson json)
        {
            var result = new List<IGH_Param>();

            if (json == null || json.Name != "IList<IGH_Param>")
                return result;
            try
            {
                var jlists = json.Value.EnumerateArray().ToList();

                // 将每个Ljson转换为IGH_Param
                foreach (var jlist in jlists)
                {
                    var param = ParamFromLjson(new Ljson("", "", jlist));
                    if (param != null)
                        result.Add(param);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"解析参数定义失败: {ex.Message}");
            }

            return result;
        }


        /// <summary>
        /// 获取组件的输入输出信息（通过创建组件实例）
        /// </summary>
        /// <param name="componentGuid">组件 GUID</param>
        /// <returns>包含 inputs 和 outputs JSON 字符串的元组</returns>
        public static (Ljson inputs, Ljson outputs) GetComponentIOInfo(string componentGuid)
        {
            try
            {
                // 方法1：尝试使用 EmitObjectProxy 直接获取代理（更快）
                var proxy = Instances.ComponentServer.EmitObjectProxy(new Guid(componentGuid));
                if (proxy != null)
                {
                    var type = proxy.Type;
                    if (type != null)
                    {
                        IGH_Component component = null;
                        try
                        {
                            // 使用反射创建实例，可能比 CreateInstance 更轻量
                            component = Activator.CreateInstance(type) as IGH_Component;
                            if (component != null)
                            {
                                var inputsJson = SerializeParamDefinitions(component.Params.Input);
                                var outputsJson = SerializeParamDefinitions(component.Params.Output);

                                return (inputsJson, outputsJson);
                            }
                        }
                        finally
                        {
                            // 手动释放资源
                            if (component is IDisposable d)
                                d.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EmitObjectProxy 方法失败 ({componentGuid}): {ex.Message}，回退到字典缓存方案");
            }

            // 方法2：回退到字典缓存方案（兼容性更好）
            try
            {
                var cache = ComponentInfo.GetComponentProxyCache();
                if (cache.TryGetValue(componentGuid, out var proxy))
                {
                    var component = proxy.CreateInstance() as IGH_Component;
                    if (component != null)
                    {
                        var inputsJson = SerializeParamDefinitions(component.Params.Input);
                        var outputsJson = SerializeParamDefinitions(component.Params.Output);

                        return (inputsJson, outputsJson);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取组件 IO 信息失败 ({componentGuid}): {ex.Message}");
            }

            return (null, null);
        }


        public static string ParamsToPrototype(IList<IGH_Param> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return "";

            var signatures = new List<string>();

            for (int i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];
                string access = param.Access.ToString().ToLower();

                // 格式：Name: TypeName [Access] (optional?)
                string signature = $"{param.Name}: {param.TypeName} [{access}]";

                // 如果是可选参数，添加标记
                if (param.Optional)
                {
                    signature += " (optional)";
                }

                signatures.Add(signature);
            }

            // 用逗号和空格拼接多个参数签名，适合函数签名
            return string.Join(", ", signatures);
        }
    }
}
