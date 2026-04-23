using GrasshopperSever.Utils;
using RhinoCodePlatform.GH;
using RhinoCodePluginGH.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

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
        public static string SetParametersFromScript(BaseLanguageComponent component, string code)
        {
            //component.SetParametersFromScript();// 这个在c#起作用，但是这个不可控
            var (ipas, opas) = GetParametersFromScript(code);
            // 假设这里[]表示无参数，"null"表示未设置。只要获取成功，说明已经设置了。
            if (ipas == null && opas == null) return null;
            return ScriptParamSerializer.UpdateParameters(component, ipas, opas);
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
            var inputParamsJson = ScriptParamSerializer.SerializeParamDefinitions(((IScriptComponent)component).Inputs);
            var outputParamsJson = ScriptParamSerializer.SerializeParamDefinitions(((IScriptComponent)component).Outputs);

            // 根据组件类型确定注释格式
            bool isPython = IsPythonComponent(component);
            string ioCommentBlock = GenerateIOCommentBlock(inputParamsJson, outputParamsJson, isPython);
            
            // 将注释块添加到代码开头（如果还没有的话）
            string startMarker = isPython ? "# GH_COMPONENT_IO_START" : "// GH_COMPONENT_IO_START";
            if (!code.Contains(startMarker))
            {
                code = ioCommentBlock + Environment.NewLine + code;
            }
            else
            {
                // 如果已存在注释块，则更新它
                code = UpdateExistingIOComment(code, inputParamsJson, outputParamsJson, isPython);
            }
            
            SetCode(component, code);
        }

        /// <summary>
        /// 获取端口信息，并填入脚本
        /// </summary>
        /// <param name="component"></param>
        public static void UpdateScript(BaseLanguageComponent component, ref string code, string inputParamsJson, string outputParamsJson)
        {
            // 根据组件类型确定注释格式
            bool isPython = IsPythonComponent(component);
            string startMarker = isPython ? "# GH_COMPONENT_IO_START" : "// GH_COMPONENT_IO_START";

            // 如果代码中没有 IO 注释标记，且需要添加参数，则创建注释块
            if (!code.Contains(startMarker))
            {
                // 只有当至少有一个参数不是 null 或 "" 时才创建注释块
                bool hasInputParam = !string.IsNullOrEmpty(inputParamsJson);
                bool hasOutputParam = !string.IsNullOrEmpty(outputParamsJson);

                if (hasInputParam || hasOutputParam)
                {
                    string ioCommentBlock = GenerateIOCommentBlock(inputParamsJson, outputParamsJson, isPython);
                    code = ioCommentBlock + Environment.NewLine + code;
                }
                // 否则保持代码不变
            }
            else
            {
                // 如果已存在注释块，则根据非 null/"" 值分别更新
                code = UpdateExistingIOComment(code, inputParamsJson, outputParamsJson, isPython);
            }
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
        private static string GenerateIOCommentBlock(string inputParamsJson, string outputParamsJson, bool isPython)
        {
            // 处理输入参数：如果为 null 或 ""否则处理换行符
            string processedInput = string.IsNullOrEmpty(inputParamsJson) ? "" : inputParamsJson.Replace("\n", " ").Replace("\r", " ").Trim();

            // 处理输出参数：如果为 null 或 ""否则处理换行符
            string processedOutput = string.IsNullOrEmpty(outputParamsJson) ? "" : outputParamsJson.Replace("\n", " ").Replace("\r", " ").Trim();

            if (isPython)
            {
                // Python使用#注释
                return $"# GH_COMPONENT_IO_START\n# INPUT_PARAMS: {processedInput}\n# OUTPUT_PARAMS: {processedOutput}\n# GH_COMPONENT_IO_END";
            }
            else
            {
                // C#使用//注释
                return $"// GH_COMPONENT_IO_START\n// INPUT_PARAMS: {processedInput}\n// OUTPUT_PARAMS: {processedOutput}\n// GH_COMPONENT_IO_END";
            }
        }

        /// <summary>
        /// 更新已存在的IO注释块
        /// </summary>
        private static string UpdateExistingIOComment(string code, string inputParamsJson, string outputParamsJson, bool isPython)
        {
            string pattern;
            if (isPython)
            {
                // Python注释的正则表达式
                pattern = @"# GH_COMPONENT_IO_START\s*# INPUT_PARAMS: (.*?)\s*# OUTPUT_PARAMS: (.*?)\s*# GH_COMPONENT_IO_END";
            }
            else
            {
                // C#注释的正则表达式
                pattern = @"// GH_COMPONENT_IO_START\s*// INPUT_PARAMS: (.*?)\s*// OUTPUT_PARAMS: (.*?)\s*// GH_COMPONENT_IO_END";
            }

            var match = Regex.Match(code, pattern, RegexOptions.Singleline);

            if (!match.Success)
            {
                // 如果没有匹配到，保持原样
                return code;
            }

            // 获取现有的输入和输出参数
            string existingInput = match.Groups[1].Value.Trim();
            string existingOutput = match.Groups[2].Value.Trim();

            // 确定新的输入参数：
            // - null 或 "" → 保留原有值
            // - "[]" → 清空端口（设置为空数组）
            // - 其他值 → 使用新值
            string newInput;
            if (string.IsNullOrEmpty(inputParamsJson))
            {
                // null 或 ""，保留原有值
                newInput = existingInput;
            }
            else if (inputParamsJson.Trim() == "[]")
            {
                // "[]"，清空端口
                newInput = "[]";
            }
            else
            {
                // 其他值，使用新值并处理换行符
                newInput = inputParamsJson.Replace("\n", " ").Replace("\r", " ").Trim();
            }

            // 确定新的输出参数：
            // - null 或 "" → 保留原有值
            // - "[]" → 清空端口（设置为空数组）
            // - 其他值 → 使用新值
            string newOutput;
            if (string.IsNullOrEmpty(outputParamsJson))
            {
                // null 或 ""，保留原有值
                newOutput = existingOutput;
            }
            else if (outputParamsJson.Trim() == "[]")
            {
                // "[]"，清空端口
                newOutput = "[]";
            }
            else
            {
                // 其他值，使用新值并处理换行符
                newOutput = outputParamsJson.Replace("\n", " ").Replace("\r", " ").Trim();
            }

            // 生成新的注释块
            string newCommentBlock;
            if (isPython)
            {
                newCommentBlock = $"# GH_COMPONENT_IO_START\n# INPUT_PARAMS: {newInput}\n# OUTPUT_PARAMS: {newOutput}\n# GH_COMPONENT_IO_END";
            }
            else
            {
                newCommentBlock = $"// GH_COMPONENT_IO_START\n// INPUT_PARAMS: {newInput}\n// OUTPUT_PARAMS: {newOutput}\n// GH_COMPONENT_IO_END";
            }

            return Regex.Replace(code, pattern, newCommentBlock, RegexOptions.Singleline);
        }

        /// <summary>
        /// 提取出代码标记字段
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static (string, string) GetParametersFromScript(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return (null, null);
            }

            // 尝试从代码中的特殊注释标记提取输入输出信息
            // 支持C# (//) 和 Python (#) 两种注释格式
            try
            {
                // 首先尝试匹配C#格式的注释
                string csharpPattern = @"// GH_COMPONENT_IO_START\s*// INPUT_PARAMS: (.*?)\s*// OUTPUT_PARAMS: (.*?)\s*// GH_COMPONENT_IO_END";
                var match = Regex.Match(code, csharpPattern, RegexOptions.Singleline);

                // 如果没有匹配到C#格式，尝试Python格式
                if (!match.Success)
                {
                    string pythonPattern = @"# GH_COMPONENT_IO_START\s*# INPUT_PARAMS: (.*?)\s*# OUTPUT_PARAMS: (.*?)\s*# GH_COMPONENT_IO_END";
                    match = Regex.Match(code, pythonPattern, RegexOptions.Singleline);
                }

                if (match.Success)
                {
                    return (match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());
                }
                return (null, null);
            }
            catch (Exception)
            {
                return (null, null);
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
            string inputParamsJson = ScriptParamSerializer.SerializeParamDefinitions(((IScriptComponent)component).Inputs);

            // 获取输出参数信息
            string outputParamsJson = ScriptParamSerializer.SerializeParamDefinitions(((IScriptComponent)component).Outputs);

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
                if (component == null)
                {
                    Debug.WriteLine("GetCode: component is null");
                    return "(component is null)";
                }

                if (component.TryGetSource(out var src))
                {
                    if (string.IsNullOrEmpty(src))
                    {
                        Debug.WriteLine("GetCode: source code is empty");
                        return "(empty code)";
                    }
                    return src;
                }
                else
                {
                    Debug.WriteLine("GetCode: TryGetSource failed");
                    return "(no code)";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetCode error: {ex.Message}");
                return $"(error: {ex.Message})";
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

}
