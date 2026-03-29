using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using RhinoCodePluginGH.Components;
using RhinoCodePluginGH.Parameters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace GrasshopperSever.Commands
{
    internal class GHScript
    {
        /// <summary>
        /// 获取组件类型的名称
        /// </summary>
        public static string GetComponentTypeName(BaseLanguageComponent component)
        {
            if (component == null) return "Unknown";

            if (component is CSharpComponent)
                return "C# Script";
            if (component is Python3Component)
                return "Python 3 Script";
            if (component is IronPython2Component)
                return "Python 2 Script";

            // 通过类型名称判断
            var typeName = component.GetType().Name;
            if (typeName.Contains("CSharp"))
                return "C# Script";
            if (typeName.Contains("Python3") || typeName.Contains("CPython"))
                return "Python 3 Script";
            if (typeName.Contains("Python") || typeName.Contains("IronPython"))
                return "Python Script";

            return $"Script ({typeName})";
        }

        /// <summary>
        /// 从脚本里获取对端口的标注，同步到组件
        /// </summary>
        /// <param name="component"></param>
        public static void SetParametersFromScript(BaseLanguageComponent component)
        {
            //component.SetParametersFromScript();// 这个在c#起作用，但是这个不可控
            var pas = GetParametersFromScript(GetCode(component));
            // 假设这里[]表示无参数，"null"表示未设置。只要获取成功，说明已经设置了。
            if (pas == null) return;
            ScriptParamConfig.UpdateParameters(component, pas);
        }

        /// <summary>
        /// 获取端口信息，并填入脚本
        /// </summary>
        /// <param name="component"></param>
        public static void SetParametersToScript(BaseLanguageComponent component)
        {
            //component.SetParametersToScript();// 这个在c#起作用，但是这个不可控
            string code = GetCode(component);
            
            // 获取组件的输入输出参数信息
            var inputParams = ParamExchange.SerializeParamDefinitions(component.Params.Input);
            var outputParams = ParamExchange.SerializeParamDefinitions(component.Params.Output);
            
            // 根据组件类型确定注释格式
            bool isPython = IsPythonComponent(component);
            string ioCommentBlock = GenerateIOCommentBlock(inputParams.Value, outputParams.Value, isPython);
            
            // 将注释块添加到代码开头（如果还没有的话）
            string startMarker = isPython ? "# GH_COMPONENT_IO_START" : "// GH_COMPONENT_IO_START";
            if (!code.Contains(startMarker))
            {
                code = ioCommentBlock + Environment.NewLine + code;
            }
            else
            {
                // 如果已存在注释块，则更新它
                code = UpdateExistingIOComment(code, ioCommentBlock, isPython);
            }
            
            SetCode(component, code);
        }

        /// <summary>
        /// 判断组件是否为Python组件
        /// </summary>
        private static bool IsPythonComponent(BaseLanguageComponent component)
        {
            if (component is Python3Component || component is IronPython2Component)
                return true;
            
            var typeName = component.GetType().Name;
            return typeName.Contains("Python") || typeName.Contains("IronPython");
        }

        /// <summary>
        /// 生成IO注释块
        /// </summary>
        private static string GenerateIOCommentBlock(JsonElement inputParams, JsonElement outputParams, bool isPython)
        {
            // 将JSON中的换行符替换为空格，避免注释中断
            string inputParamsJson = JsonSerializer.Serialize(inputParams);
            inputParamsJson = inputParamsJson?.Replace("\n", " ").Replace("\r", " ") ?? "[]";
            string outputParamsJson = JsonSerializer.Serialize(outputParams);
            outputParamsJson = outputParamsJson?.Replace("\n", " ").Replace("\r", " ") ?? "[]";
            
            if (isPython)
            {
                // Python使用#注释
                return $"# GH_COMPONENT_IO_START\n# INPUT_PARAMS: {inputParamsJson}\n# OUTPUT_PARAMS: {outputParamsJson}\n# GH_COMPONENT_IO_END";
            }
            else
            {
                // C#使用//注释
                return $"// GH_COMPONENT_IO_START\n// INPUT_PARAMS: {inputParamsJson}\n// OUTPUT_PARAMS: {outputParamsJson}\n// GH_COMPONENT_IO_END";
            }
        }

        /// <summary>
        /// 更新已存在的IO注释块
        /// </summary>
        private static string UpdateExistingIOComment(string code, string newCommentBlock, bool isPython)
        {
            string pattern;
            if (isPython)
            {
                // Python注释的正则表达式
                pattern = @"# GH_COMPONENT_IO_START\s*# INPUT_PARAMS: .*?\s*# OUTPUT_PARAMS: .*?\s*# GH_COMPONENT_IO_END";
            }
            else
            {
                // C#注释的正则表达式
                pattern = @"// GH_COMPONENT_IO_START\s*// INPUT_PARAMS: .*?\s*// OUTPUT_PARAMS: .*?\s*// GH_COMPONENT_IO_END";
            }
            
            return System.Text.RegularExpressions.Regex.Replace(code, pattern, newCommentBlock, 
                System.Text.RegularExpressions.RegexOptions.Singleline);
        }

        public static Ljson GetParametersFromScript(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return null;
            }

            // 尝试从代码中的特殊注释标记提取输入输出信息
            // 支持C# (//) 和 Python (#) 两种注释格式
            try
            {
                // 首先尝试匹配C#格式的注释
                string csharpPattern = @"// GH_COMPONENT_IO_START\s*// INPUT_PARAMS: (.*?)\s*// OUTPUT_PARAMS: (.*?)\s*// GH_COMPONENT_IO_END";
                var match = System.Text.RegularExpressions.Regex.Match(code, csharpPattern, System.Text.RegularExpressions.RegexOptions.Singleline);
                string commentType = "C#";

                // 如果没有匹配到C#格式，尝试Python格式
                if (!match.Success)
                {
                    string pythonPattern = @"# GH_COMPONENT_IO_START\s*# INPUT_PARAMS: (.*?)\s*# OUTPUT_PARAMS: (.*?)\s*# GH_COMPONENT_IO_END";
                    match = System.Text.RegularExpressions.Regex.Match(code, pythonPattern, System.Text.RegularExpressions.RegexOptions.Singleline);
                    commentType = "Python";
                }

                Dictionary<string, object> data;
                if (match.Success)
                {
                    string inputParams = match.Groups[1].Value.Trim();
                    string outputParams = match.Groups[2].Value.Trim();

                    data = new Dictionary<string, object>
                    {
                        { "InputParams", inputParams},
                        { "OutputParams", outputParams},
                        { "Source", $"从{commentType}代码注释中提取" }
                    };
                    return new Ljson("GetParametersFromScript", "从脚本提取参数", JsonSerializer.SerializeToElement(data));
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        /// 从组件获取端口信息
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public static Ljson GetParametersFromComponent(BaseLanguageComponent component)
        {
            if (component == null)
                return Ljson.CreateErrorLjson("组件为空");

            // 获取输入参数信息
            JsonElement inputParamsJson = ParamExchange.SerializeParamDefinitions(component.Params.Input).Value;

            // 获取输出参数信息
            JsonElement outputParamsJson = ParamExchange.SerializeParamDefinitions(component.Params.Output).Value;

            // 构建数据字典
            var data = new Dictionary<string, object>
            {
                { "InputParams", inputParamsJson},
                { "OutputParams", outputParamsJson},
                { "ComponentGuid", component.ComponentGuid.ToString() },
                { "InstanceGuid", component.InstanceGuid.ToString() },
                { "ComponentName", component.Name },
                { "ComponentNickname", component.NickName }
            };

            return new Ljson("GetParametersFromComponent", "从组件提取参数", JsonSerializer.SerializeToElement(data));
        }

        /// <summary>
        /// 获取脚本
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public static string GetCode(BaseLanguageComponent component)
        {
            try
            {
                return component.TryGetSource(out var src) ? src : "(no code)";
            }
            catch
            {
                return "(error)";
            }
        }

        /// <summary>
        /// 设置脚本
        /// </summary>
        /// <param name="component"></param>
        /// <param name="code"></param>
        public static void SetCode(BaseLanguageComponent component, string code)
        {
            // 记录修改前的代码
            string oldCode = GetCode(component);

            // 设置新代码
            component.SetSource(code);

            // 记录修改历史
            try
            {
                var modifyData = new Dictionary<string, object>
                {
                    { "OldCodeLength", oldCode.Length },
                    { "NewCodeLength", code.Length },
                    { "CodeChanged", oldCode != code },
                    { "ComponentType", GetComponentTypeName(component) }
                };

                GHScriptDB.RecordModifyHistory(
                    instanceGuid: component.InstanceGuid.ToString(),
                    componentGuid: component.ComponentGuid.ToString(),
                    componentName: component.Name,
                    modifyType: "CODE_CHANGE",
                    modifyContent: JsonSerializer.Serialize(modifyData),
                    description: "修改脚本代码"
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"记录代码修改历史失败: {ex.Message}");
            }
        }

    }

    /// <summary>
    /// 脚本参数配置
    /// </summary>
    public class ScriptParamConfig
    {
        public string Name { get; set; }
        public GH_ParamAccess Access { get; set; } = GH_ParamAccess.item;
        public bool IsInput { get; set; } = true;
        public bool Optional { get; set; } = false;
        public string Description { get; set; } = "";
        public bool Reverse { get; set; } = false;
        public bool Simplify { get; set; } = false;
        public GH_DataMapping DataMapping { get; set; } = GH_DataMapping.None;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="access"></param>
        /// <param name="isInput"></param>
        /// <param name="optional"></param>
        public ScriptParamConfig(string name, GH_ParamAccess access = GH_ParamAccess.item, bool isInput = false, bool optional = false)
        {
            Name = name;
            Access = access;
            IsInput = isInput;
            Optional = optional;
        }
        public ScriptParamConfig(JsonElement data, bool isInput = false)
        {
            Name = GetParameterString(data, "Name");
            IsInput = isInput;
            Description = GetParameterString(data, "Description");
            // 布尔属性
            if (bool.TryParse(GetParameterString(data, "Optional"), out bool opt))
                Optional = opt;
            if (bool.TryParse(GetParameterString(data, "Reverse"), out bool rev))
                Reverse = rev;
            if (bool.TryParse(GetParameterString(data, "Simplify"), out bool sim))
                Simplify = sim;
            // 枚举属性
            if (Enum.TryParse(GetParameterString(data, "Access"), true, out GH_ParamAccess acc))
                Access = acc;
            if (Enum.TryParse(GetParameterString(data, "Mapping"), true, out GH_DataMapping map))
                DataMapping = map;
        }
        private static string GetParameterString(JsonElement data, string paramName)
        {
            if (data.TryGetProperty(paramName, out var valueElement))
            {
                return JsonSerializer.Serialize(valueElement);
            }
            return "";
        }
        public ScriptVariableParam CreateParam()
        {
            // 创建新的 ScriptVariableParam
            var newParam = new ScriptVariableParam(Name);
            newParam.Access = Access;
            newParam.Optional = Optional;
            newParam.Description = Description;
            newParam.Reverse = Reverse;
            newParam.Simplify = Simplify;
            newParam.DataMapping = DataMapping;
            //string source = "";
            //component.TryGetSource(out source);
            //newParam.UpdateConverter(new Grasshopper1Script(source).LanguageSpec, ParamType paramType);
            return newParam;
        }

        /// <summary>
        /// 确保内部组件包含指定的参数（输入或输出）
        /// </summary>
        public void EnsureComponentParameter(BaseLanguageComponent component)
        {
            // 获取对应的参数集合
            var paramsList = IsInput ? component.Params.Input : component.Params.Output;

            // 检查是否已存在同名参数
            var existingParam = paramsList.FirstOrDefault(p => p.Name == Name || p.NickName == Name);
            if (existingParam != null)
            {
                return; // 已存在，无需创建
            }
            var newParam = CreateParam();
            // 注册参数
            if (IsInput)
            {
                component.Params.RegisterInputParam(newParam);
            }
            else
            {
                component.Params.RegisterOutputParam(newParam);
            }

            component.Params.OnParametersChanged();
        }

        /// <summary>
        /// 确保内部组件包含指定的参数（输入或输出）
        /// </summary>
        /// <param name="component"></param>
        /// <param name="name"></param>
        /// <param name="access"></param>
        /// <param name="isInput"></param>
        /// <param name="optional"></param>
        public static void EnsureParameter(BaseLanguageComponent component, string name, GH_ParamAccess access = GH_ParamAccess.item, bool isInput = false, bool optional = false)
        {
            var config = new ScriptParamConfig(name, access, isInput, optional);
            config.EnsureComponentParameter(component);
        }

        public static List<ScriptVariableParam> CreatParams(string json, bool isInput)
        {
            var jlists = JsonSerializer.Deserialize<List<JsonElement>>(json);
            var pas = new List<ScriptVariableParam>();
            // 将每个Ljson转换为IGH_Param
            foreach (var jlist in jlists)
            {
                pas.Add(new ScriptParamConfig(jlist, isInput).CreateParam());
            }
            return pas;
        }


        /// <summary>
        /// 处理动态修改组件输入输出端
        /// 输入格式：Ljson中包含InputParams和OutputParams（JSON格式的参数定义数组）
        /// 假设这里[]表示无参数，"null"表示未设置。只要获取成功，说明已经设置了。
        /// 功能：按照参数定义匹配参数，缺少的添加，多余的删除（少加多补）
        /// </summary>
        public static Ljson UpdateParameters(BaseLanguageComponent component, Ljson data)
        {
            try
            {
                if (component == null)
                    return Ljson.CreateErrorLjson("目标组件无效");

                if (data == null)
                    return Ljson.CreateErrorLjson("参数数据为空");

                // 假设这里[]表示无参数，"null"表示未设置。
                // 1. 从Ljson中获取参数定义JSON
                string inputParamsJson = data.GetParameterString("InputParams");
                string outputParamsJson = data.GetParameterString("OutputParams");
                if (string.IsNullOrEmpty(inputParamsJson) || string.IsNullOrEmpty(outputParamsJson))
                {
                    return Ljson.CreateErrorLjson("代码不存在有效端口信息");
                }
                // 2. 解析参数定义
                //var targetInputParams = ParamExchange.DeserializeParamDefinitions(new Ljson("IList<IGH_Param>", "", JsonSerializer.Deserialize<JsonElement>(inputParamsJson)));
                //var targetOutputParams = ParamExchange.DeserializeParamDefinitions(new Ljson("IList<IGH_Param>", "", JsonSerializer.Deserialize<JsonElement>(outputParamsJson)));
                // 3. 记录修改前的参数信息
                var oldInputParamsJson = ParamExchange.SerializeParamDefinitions(component.Params.Input).Value;
                var oldOutputParamsJson = ParamExchange.SerializeParamDefinitions(component.Params.Output).Value;

                // 4. 异步调度修改任务（非常重要：不能在计算过程中直接修改结构）
                var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                if (doc == null)
                    return Ljson.CreateErrorLjson("无法获取Grasshopper文档");

                doc.ScheduleSolution(5, (d) =>
                {
                    //// 处理输入端：少加多补
                    SyncParameters(component, component.Params.Input, CreatParams(inputParamsJson, true), true);
                    //// 处理输出端：少加多补
                    SyncParameters(component, component.Params.Output, CreatParams(outputParamsJson, false), false);
                    // 5. 刷新组件外观和布局
                    component.Params.OnParametersChanged();
                    component.OnAttributesChanged();
                    component.ExpireSolution(false);

                    // 6. 记录修改历史
                    try
                    {
                        var modifyData = new Dictionary<string, object>
                        {
                            { "OldInputParams", JsonSerializer.Serialize(oldInputParamsJson) },
                            { "OldOutputParams", JsonSerializer.Serialize(oldOutputParamsJson) },
                            { "NewInputParams", inputParamsJson },
                            { "NewOutputParams", outputParamsJson },
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
                        Debug.WriteLine($"记录参数修改历史失败: {ex.Message}");
                    }
                });

                var responseData = new Dictionary<string, object>
                {
                    { "Status", "参数同步指令已发送至调度器" }
                };

                return new Ljson("UpdateParameters", "更新组件参数", JsonSerializer.SerializeToElement(responseData));
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"修改组件参数失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 同步参数列表：少加多补
        /// </summary>
        public static void SyncParameters(BaseLanguageComponent component, IList<IGH_Param> currentParams, List<ScriptVariableParam> targetParams, bool isInput)
        {
            var currentNames = currentParams.Select(p => p.NickName).ToList();
            var targetNames = targetParams.Select(p => p.NickName).ToList();

            // 1. 删除多余的参数（在目标列表中不存在的）
            // 从后往前删除，避免索引变化问题
            for (int i = currentParams.Count - 1; i >= 0; i--)
            {
                if (!targetNames.Contains(currentNames[i]))
                {
                    if (isInput)
                        component.Params.UnregisterInputParameter(currentParams[i]);
                    else
                        component.Params.UnregisterOutputParameter(currentParams[i]);
                }
            }

            // 2. 添加缺失的参数（在目标列表中存在但当前没有的）
            foreach (var targetParam in targetParams)
            {
                if (!currentNames.Contains(targetParam.NickName))
                {
                    // 直接注册从JSON创建的参数
                    if (isInput)
                        component.Params.RegisterInputParam(targetParam);
                    else
                        component.Params.RegisterOutputParam(targetParam);
                }
            }
        }
    }

}
