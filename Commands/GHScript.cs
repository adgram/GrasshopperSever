using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using Rhino.NodeInCode;
using RhinoCodePluginGH.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GrasshopperSever.Commands
{
    internal class GHScript
    {
        /// <summary>
        /// 获取组件类型的友好名称
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

        public static void SetParametersFromScript(BaseLanguageComponent component)
        {
            // 似乎有问题
            component.SetParametersFromScript();
            JList pas = GetParametersFromScript(GetCode(component));
            UpdateParameters(component, pas);
        }

        public static void SetParametersToScript(BaseLanguageComponent component)
        {
            // 似乎有问题
            component.SetParametersToScript();
            JList pas = GetParametersFromComponent(component);
            string code = GetCode(component);
            
            // 获取组件的输入输出参数信息
            string inputParamsJson = ParamExchange.SerializeParamDefinitions(component.Params.Input);
            string outputParamsJson = ParamExchange.SerializeParamDefinitions(component.Params.Output);
            
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
        private static string GenerateIOCommentBlock(string inputParamsJson, string outputParamsJson, bool isPython)
        {
            // 将JSON中的换行符替换为空格，避免注释中断
            inputParamsJson = inputParamsJson?.Replace("\n", " ").Replace("\r", " ") ?? "[]";
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

        public static JList GetParametersFromScript(string code)
        {
            var result = new JList();
            
            if (string.IsNullOrEmpty(code))
            {
                result.Add(new JData("Status", "状态", "代码为空"));
                return result;
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
                
                if (match.Success)
                {
                    string inputParams = match.Groups[1].Value.Trim();
                    string outputParams = match.Groups[2].Value.Trim();
                    
                    result.Add(new JData("InputParams", "提取的输入参数", inputParams));
                    result.Add(new JData("OutputParams", "提取的输出参数", outputParams));
                    result.Add(new JData("Source", "数据来源", $"从{commentType}代码注释中提取"));
                }
                else
                {
                    result.Add(new JData("InputParams", "提取的输入参数", "[]"));
                    result.Add(new JData("OutputParams", "提取的输出参数", "[]"));
                    result.Add(new JData("Source", "数据来源", "未找到注释标记"));
                }
            }
            catch (Exception ex)
            {
                result.Add(new JData("Error", "错误信息", ex.Message));
                result.Add(new JData("InputParams", "提取的输入参数", "[]"));
                result.Add(new JData("OutputParams", "提取的输出参数", "[]"));
            }
            
            result.AddSuccessStatus();
            return result;
        }

        public static JList GetParametersFromComponent(BaseLanguageComponent component)
        {
            if (component == null)
                return JList.CreateErrorJList("组件为空");
                
            var result = new JList();
            
            // 获取输入参数信息
            string inputParamsJson = ParamExchange.SerializeParamDefinitions(component.Params.Input);
            result.Add(new JData("InputParams", "输入参数信息", inputParamsJson));
            
            // 获取输出参数信息
            string outputParamsJson = ParamExchange.SerializeParamDefinitions(component.Params.Output);
            result.Add(new JData("OutputParams", "输出参数信息", outputParamsJson));
            
            // 添加组件基本信息
            result.Add(new JData("ComponentGuid", "组件GUID", component.ComponentGuid.ToString()));
            result.Add(new JData("InstanceGuid", "实例GUID", component.InstanceGuid.ToString()));
            result.Add(new JData("ComponentName", "组件名称", component.Name));
            result.Add(new JData("ComponentNickname", "组件昵称", component.NickName));
            
            result.AddSuccessStatus();
            return result;
        }

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
        public static void SetCode(BaseLanguageComponent component, string code)
        {
            component.SetSource(code);
        }

        /// <summary>
        /// 处理动态修改组件输入输出端
        /// 输入格式：JList中包含InputParams和OutputParams（JSON格式的参数定义数组）
        /// 功能：按照参数定义匹配参数，缺少的添加，多余的删除（少加多补）
        /// </summary>
        public static JList UpdateParameters(BaseLanguageComponent component, JList pas)
        {
            try
            {
                if (component == null)
                    return JList.CreateErrorJList("目标组件无效");

                if (pas == null || pas.IsEmpty)
                    return JList.CreateErrorJList("参数数据为空");

                // 1. 从JList中获取参数定义JSON
                string inputParamsJson = pas.GetParameter("InputParams") ?? "[]";
                string outputParamsJson = pas.GetParameter("OutputParams") ?? "[]";

                // 2. 解析参数定义
                var targetInputParams = ParamExchange.DeserializeParamDefinitions(inputParamsJson);
                var targetOutputParams = ParamExchange.DeserializeParamDefinitions(outputParamsJson);

                // 3. 异步调度修改任务（非常重要：不能在计算过程中直接修改结构）
                var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                if (doc == null)
                    return JList.CreateErrorJList("无法获取Grasshopper文档");

                doc.ScheduleSolution(5, (d) =>
                {
                    // 处理输入端：少加多补
                    SyncParameters(component, component.Params.Input, targetInputParams, true);

                    // 处理输出端：少加多补
                    SyncParameters(component, component.Params.Output, targetOutputParams, false);

                    // 4. 刷新组件外观和布局
                    component.Params.OnParametersChanged();
                    component.OnAttributesChanged();
                    component.ExpireSolution(false);
                });

                var response = new JList();
                response.Add(new JData("Status", "消息", "参数同步指令已发送至调度器"));
                response.Add(new JData("InputParams", "目标输入参数数量", targetInputParams.Count.ToString()));
                response.Add(new JData("OutputParams", "目标输出参数数量", targetOutputParams.Count.ToString()));
                response.AddSuccessStatus();
                return response;
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"修改组件参数失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 同步参数列表：少加多补
        /// </summary>
        public static void SyncParameters(IGH_Component component, IList<IGH_Param> currentParams, List<IGH_Param> targetParams, bool isInput)
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
