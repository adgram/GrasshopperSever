using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using RhinoCodePlatform.GH;
using RhinoCodePlatform.Rhino3D.Languages.GH1;
using RhinoCodePlatform.Rhino3D.Languages.GH1.Converters;
using RhinoCodePluginGH.Components;
using RhinoCodePluginGH.Parameters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;



namespace GrasshopperSever.Commands
{
    /// <summary>
    /// 序列化数据模型
    /// </summary>
    public class ScriptVariableParamData
    {
        [JsonPropertyName("typeHintID")]
        public Guid TypeHintID { get; set; }

        [JsonPropertyName("typeHintName")]
        public string TypeHintName { get; set; }

        [JsonPropertyName("showTypeHints")]
        public bool ShowTypeHints { get; set; }

        [JsonPropertyName("allowTreeAccess")]
        public bool AllowTreeAccess { get; set; }

        [JsonPropertyName("toolTip")]
        public string ToolTip { get; set; }

        [JsonPropertyName("scriptParamAccess")]
        public int ScriptParamAccess { get; set; }

        [JsonPropertyName("variableName")]
        public string VariableName { get; set; }

        [JsonPropertyName("prettyName")]
        public string PrettyName { get; set; }

        [JsonPropertyName("optional")]
        public bool Optional { get; set; }

        [JsonPropertyName("hidden")]
        public bool Hidden { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("castTargetType")]
        public string CastTargetType { get; set; }

        public void DeserializeParam(ScriptVariableParam param)
        {
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            // 恢复类型提示
            if (TypeHintID != Guid.Empty)
            {
                var converter = Grasshopper1.GetConverter(TypeHintID);

                // 检查转换器是否匹配
                if (converter != null && converter.Id.Id == TypeHintID)
                {
                    // 如果是 CastConverter，需要特殊处理
                    if (TypeHintID == CastConverter.Identity.Id &&
                        !string.IsNullOrEmpty(CastTargetType))
                    {
                        var castConverter = new CastConverter();
                        IScriptParameter scriptParam = param;
                        scriptParam.Converter = castConverter;
                    }
                    else
                    {
                        IScriptParameter scriptParam = param;
                        scriptParam.Converter = converter;
                    }
                }
                else
                {
                    // 找不到转换器，使用 MissingConverter
                    var missingConverter = new MissingConverter(
                        TypeHintID,
                        $"Parameter failed to find Type Hint. (Missing Hint: {TypeHintID})"
                    );
                    IScriptParameter scriptParam = param;
                    scriptParam.Converter = missingConverter;
                }
            }

            // 恢复其他属性
            param.ShowHints = ShowTypeHints;
            param.AllowTreeAccess = AllowTreeAccess;
            param.VariableName = VariableName;
            param.PrettyName = PrettyName;
            param.ToolTip = ToolTip;
            param.Access = (GH_ParamAccess)ScriptParamAccess;
            param.Optional = Optional;
            param.Hidden = Hidden;

            if (!string.IsNullOrEmpty(Description))
            {
                param.Description = Description;
            }
        }
    }


    /// <summary>
    /// ScriptVariableParam 的 JSON 序列化辅助类
    /// </summary>
    public static class ScriptParamSerializer
    {

        /// <summary>
        /// 确保内部组件包含指定的参数（输入或输出）
        /// </summary>
        /// <param name="component"></param>
        /// <param name="name"></param>
        /// <param name="access"></param>
        /// <param name="isInput"></param>
        /// <param name="optional"></param>
        public static void EnsureSimplyParameter(BaseLanguageComponent component, string name, GH_ParamAccess access = GH_ParamAccess.item, bool isInput = false, bool optional = false)
        {
            // 获取对应的参数集合
            var paramsList = isInput ? ((IScriptComponent)component).Inputs : ((IScriptComponent)component).Outputs;

            // 检查是否已存在同名参数
            var existingParam = paramsList.FirstOrDefault(p => p.VariableName == name);
            if (existingParam != null)
            {
                return; // 已存在，无需创建
            }

            var newParam = new ScriptVariableParam(name);
            newParam.Access = access;
            newParam.Optional = optional;

            // 注册参数
            if (isInput)
            {
                component.Params.RegisterInputParam(newParam);
            }
            else
            {
                component.Params.RegisterOutputParam(newParam);
            }

            component.Params.OnParametersChanged();
            component.OnAttributesChanged();
        }

        /// <summary>
        /// JSON 序列化选项
        /// </summary>
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// 序列化为 JSON 字符串
        /// </summary>
        public static string SerializeToJson(ScriptVariableParam param)
        {
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            IScriptParameter scriptParam = param;
            var converter = scriptParam.Converter;

            var data = new ScriptVariableParamData
            {
                TypeHintID = converter?.Id.Id ?? Guid.Empty,
                TypeHintName = converter?.Id.Name ?? string.Empty,
                ShowTypeHints = param.ShowHints,
                AllowTreeAccess = param.AllowTreeAccess,
                ToolTip = param.ToolTip,
                ScriptParamAccess = (int)param.Access,
                VariableName = param.VariableName,
                PrettyName = param.PrettyName,
                Optional = param.Optional,
                Hidden = param.Hidden,
                Description = param.Description
            };

            // 如果是 CastConverter，尝试获取目标类型信息
            if (converter is CastConverter castConverter)
            {
                data.CastTargetType = castConverter.TargetType?.FullName ?? string.Empty;
            }

            return JsonSerializer.Serialize(data, Options);
        }



        /// <summary>
        /// 从 JSON 字符串创建新的 ScriptVariableParam 实例
        /// </summary>
        public static ScriptVariableParam DeserializeFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON 字符串不能为空", nameof(json));

            var data = JsonSerializer.Deserialize<ScriptVariableParamData>(json, Options);
            if (data == null)
                throw new InvalidOperationException("JSON 反序列化失败");

            var param = new ScriptVariableParam(data.VariableName);
            data.DeserializeParam(param);
            return param;
        }

        /// <summary>
        /// 获取参数定义信息（不包含连线，用于组件定义）
        /// </summary>
        /// <param name="parameters">参数列表</param>
        /// <returns>参数定义信息JSON数组字符串</returns>
        public static string SerializeParamDefinitions(IEnumerable<IScriptParameter> parameters)
        {
            var paramLjsons = new List<string>();
            foreach (var param in parameters)
            {
                if (param is ScriptVariableParam scriptVarParam)
                {
                    paramLjsons.Add(SerializeToJson(scriptVarParam));
                }
            }
            return "[" + string.Join(",", paramLjsons) + "]";
        }

        /// <summary>
        /// 解析参数定义JSON字符串为ScriptVariableParam列表
        /// 与SerializeParamDefinitions成对使用
        /// </summary>
        public static List<ScriptVariableParam> DeserializeParamDefinitions(string json)
        {
            var result = new List<ScriptVariableParam>();

            if (string.IsNullOrWhiteSpace(json))
                return result;

            try
            {
                // 使用 JsonDocument 解析 JSON 数组
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                    return result;

                // 遍历数组中的每个对象
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    // 将每个对象转换为 JSON 字符串
                    string jsonItem = element.GetRawText();
                    var param = DeserializeFromJson(jsonItem);
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
        /// 处理动态修改组件输入输出端
        /// 输入格式：InputParams和OutputParams为JSON数组字符串（参数定义）
        /// 假设这里[]表示无参数，"null"表示未设置。只要获取成功，说明已经设置了。
        /// 功能：按照参数定义匹配参数，缺少的添加，多余的删除（少加多补）
        /// </summary>
        public static string UpdateParameters(BaseLanguageComponent component, string inputParams, string outputParams)
        {
            string log = "UpdateParameters" + Environment.NewLine;
            try
            {
                if (component == null)
                {
                    log += "目标组件无效" + Environment.NewLine;
                    return log;
                }

                if (string.IsNullOrWhiteSpace(inputParams) && string.IsNullOrWhiteSpace(outputParams))
                {
                    log += "参数数据为空" + Environment.NewLine;
                    return log;
                }

                // 记录修改前的参数信息
                var oldInputParams = SerializeParamDefinitions(((IScriptComponent)component).Inputs);
                var oldOutputParams = SerializeParamDefinitions(((IScriptComponent)component).Outputs);

                //log += "oldInputParams==" + oldInputParams + Environment.NewLine;
                //log += "oldOutputParams==" + oldOutputParams + Environment.NewLine;
                //log += "inputParams==" + inputParams + Environment.NewLine;
                //log += "outputParams==" + outputParams + Environment.NewLine;

                // 异步调度修改任务（非常重要：不能在计算过程中直接修改结构）
                var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                if (doc == null)
                {
                    log += "无法获取Grasshopper文档" + Environment.NewLine;
                    return log;
                }

                doc.ScheduleSolution(5, (d) =>
                {
                    // 处理输入端：少加多补
                    if (! string.IsNullOrEmpty(inputParams))
                        SyncParameters(component, ((IScriptComponent)component).Inputs, DeserializeParamDefinitions(inputParams), true);
                    // 处理输出端：少加多补
                    if (!string.IsNullOrEmpty(outputParams))
                        SyncParameters(component, ((IScriptComponent)component).Outputs, DeserializeParamDefinitions(outputParams), false);
                    // 5. 刷新组件外观和布局
                    component.Params.OnParametersChanged();
                    ((IGH_VariableParameterComponent)component).VariableParameterMaintenance();
                    component.Attributes.ExpireLayout();
                    component.Attributes.PerformLayout();
                    component.OnDisplayExpired(true);
                    component.SetParametersToScript();
                    component.ExpireSolution(false);

                    // 6. 记录修改历史
                    try
                    {
                        var modifyData = new Dictionary<string, object>
                        {
                            { "OldInputParams", JsonSerializer.Serialize(oldInputParams) },
                            { "OldOutputParams", JsonSerializer.Serialize(oldOutputParams) },
                            { "NewInputParams", JsonSerializer.Serialize(inputParams) },
                            { "NewOutputParams", JsonSerializer.Serialize(outputParams) },
                            { "ComponentType", GHScript.GetComponentTypeName(component) }
                        };

                        GHScriptDB.RecordModifyHistory(
                            instanceGuid: component.InstanceGuid.ToString(),
                            componentGuid: component.ComponentGuid.ToString(),
                            componentName: component.Name,
                            modifyType: "PARAM_CHANGE",
                            modifyContent: JsonSerializer.Serialize(modifyData),
                            description: "修改组件参数"
                        );
                    }
                    catch (Exception ex)
                    {
                        log += "记录参数修改历史失败" + ex.Message + Environment.NewLine;
                    }
                });
                log += "参数同步指令已发送至调度器" + Environment.NewLine;
                return log;
            }
            catch (Exception ex)
            {
                log += "修改组件参数失败" + ex.Message + Environment.NewLine;
                return log;
            }
        }

        /// <summary>
        /// 同步参数列表：少加多补
        /// </summary>
        public static void SyncParameters(BaseLanguageComponent component, IEnumerable<IScriptParameter> currentParams, IEnumerable<IScriptParameter> targetParams, bool isInput)
        {
            var currentNames = currentParams.Select(p => p.VariableName).ToList();
            var targetNames = targetParams.Select(p => p.VariableName).ToList();

            // 1. 删除多余的参数（在目标列表中不存在的）
            // 从后往前删除，避免索引变化问题
            foreach (var param in currentParams)
            {
                if (!targetNames.Contains(param.VariableName))
                {
                    if(param is IGH_Param p)
                    {
                        if (isInput)
                            component.Params.UnregisterInputParameter(p);
                        else
                            component.Params.UnregisterOutputParameter(p);
                    }
                }
            }

            // 2. 添加缺失的参数（在目标列表中存在但当前没有的）
            foreach (var targetParam in targetParams)
            {
                if (!currentNames.Contains(targetParam.VariableName))
                {
                    if (targetParam is IGH_Param p)
                    {
                        // 直接注册从JSON创建的参数
                        if (isInput)
                            component.Params.RegisterInputParam(p);
                        else
                            component.Params.RegisterOutputParam(p);
                    }
                }
            }
        }


    }
}
