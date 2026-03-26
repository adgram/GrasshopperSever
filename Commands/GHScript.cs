using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using RhinoCodePluginGH.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Data.SQLite;

namespace GrasshopperSever.Commands
{
    internal class GHScript
    {
        #region 数据库操作

        /// <summary>
        /// 初始化 GHScript 修改记录表
        /// </summary>
        private static void InitializeScriptModifyTable()
        {
            try
            {
                if (!DatabaseManager.TableExists("GHScriptModifyHistory"))
                {
                    string createTableSql = @"
                        CREATE TABLE GHScriptModifyHistory (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            InstanceGuid TEXT NOT NULL,
                            ComponentGuid TEXT NOT NULL,
                            ComponentName TEXT,
                            ModifyType TEXT NOT NULL,
                            ModifyContent TEXT,
                            Description TEXT,
                            ModifyTime DATETIME DEFAULT CURRENT_TIMESTAMP
                        )";

                    if (DatabaseManager.CreateTable("GHScriptModifyHistory", createTableSql, "存储GHScript组件修改历史"))
                    {
                        System.Diagnostics.Debug.WriteLine("GHScript修改记录表创建成功");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化GHScript修改记录表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录组件修改历史
        /// </summary>
        /// <param name="instanceGuid">实例GUID</param>
        /// <param name="componentGuid">组件GUID</param>
        /// <param name="componentName">组件名称</param>
        /// <param name="modifyType">修改类型</param>
        /// <param name="modifyContent">修改内容（JSON格式）</param>
        /// <param name="description">描述</param>
        private static void RecordModifyHistory(string instanceGuid, string componentGuid, string componentName, string modifyType, string modifyContent, string description = null)
        {
            try
            {
                InitializeScriptModifyTable();

                using (var connection = DatabaseManager.GetConnection())
                {
                    string sql = @"
                        INSERT INTO GHScriptModifyHistory 
                        (InstanceGuid, ComponentGuid, ComponentName, ModifyType, ModifyContent, Description)
                        VALUES (@instanceGuid, @componentGuid, @componentName, @modifyType, @modifyContent, @description)";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@instanceGuid", instanceGuid);
                        command.Parameters.AddWithValue("@componentGuid", componentGuid);
                        command.Parameters.AddWithValue("@componentName", componentName ?? string.Empty);
                        command.Parameters.AddWithValue("@modifyType", modifyType);
                        command.Parameters.AddWithValue("@modifyContent", modifyContent ?? string.Empty);
                        command.Parameters.AddWithValue("@description", description ?? string.Empty);

                        command.ExecuteNonQuery();
                    }
                }

                // 更新表时间戳
                DatabaseManager.UpdateTableTimestamp("GHScriptModifyHistory");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"记录修改历史失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取组件的修改历史
        /// </summary>
        /// <param name="instanceGuid">实例GUID</param>
        /// <returns>修改历史列表</returns>
        public static List<Dictionary<string, object>> GetModifyHistory(string instanceGuid)
        {
            var history = new List<Dictionary<string, object>>();

            try
            {
                InitializeScriptModifyTable();

                using (var connection = DatabaseManager.GetConnection())
                {
                    string sql = @"
                        SELECT Id, ComponentGuid, ComponentName, ModifyType, ModifyContent, Description, ModifyTime
                        FROM GHScriptModifyHistory
                        WHERE InstanceGuid = @instanceGuid
                        ORDER BY ModifyTime DESC
                        LIMIT 100";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@instanceGuid", instanceGuid);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                history.Add(new Dictionary<string, object>
                                {
                                    { "Id", reader["Id"].ToString() },
                                    { "ComponentGuid", reader["ComponentGuid"].ToString() },
                                    { "ComponentName", reader["ComponentName"].ToString() },
                                    { "ModifyType", reader["ModifyType"].ToString() },
                                    { "ModifyContent", reader["ModifyContent"].ToString() },
                                    { "Description", reader["Description"].ToString() },
                                    { "ModifyTime", reader["ModifyTime"].ToString() }
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取修改历史失败: {ex.Message}");
            }

            return history;
        }

        #endregion

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
            component.SetParametersFromScript();// 似乎有问题
            Ljson pas = GetParametersFromScript(GetCode(component));
            if (pas.GetParameter("Status")?.GetString() == "代码为空") return;
            UpdateParameters(component, pas);
        }

        public static void SetParametersToScript(BaseLanguageComponent component)
        {
            
            component.SetParametersToScript();// 似乎有问题
            Ljson pas = GetParametersFromComponent(component);
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

        public static Ljson GetParametersFromScript(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                var emptyData = new Dictionary<string, object>
                {
                    { "Status", "代码为空" }
                };
                return new Ljson("GetParametersFromScript", "从脚本提取参数", JsonSerializer.SerializeToElement(emptyData));
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
                        { "InputParams", inputParams },
                        { "OutputParams", outputParams },
                        { "Source", $"从{commentType}代码注释中提取" }
                    };
                }
                else
                {
                    data = new Dictionary<string, object>
                    {
                        { "InputParams", "[]" },
                        { "OutputParams", "[]" },
                        { "Source", "未找到注释标记" }
                    };
                }

                return new Ljson("GetParametersFromScript", "从脚本提取参数", JsonSerializer.SerializeToElement(data));
            }
            catch (Exception ex)
            {
                var errorData = new Dictionary<string, object>
                {
                    { "Error", ex.Message },
                    { "InputParams", "[]" },
                    { "OutputParams", "[]" }
                };
                return new Ljson("GetParametersFromScript", "从脚本提取参数失败", JsonSerializer.SerializeToElement(errorData));
            }
        }

        public static Ljson GetParametersFromComponent(BaseLanguageComponent component)
        {
            if (component == null)
                return Ljson.CreateErrorLjson("组件为空");

            // 获取输入参数信息
            string inputParamsJson = ParamExchange.SerializeParamDefinitions(component.Params.Input);

            // 获取输出参数信息
            string outputParamsJson = ParamExchange.SerializeParamDefinitions(component.Params.Output);

            // 构建数据字典
            var data = new Dictionary<string, object>
            {
                { "InputParams", inputParamsJson },
                { "OutputParams", outputParamsJson },
                { "ComponentGuid", component.ComponentGuid.ToString() },
                { "InstanceGuid", component.InstanceGuid.ToString() },
                { "ComponentName", component.Name },
                { "ComponentNickname", component.NickName }
            };

            return new Ljson("GetParametersFromComponent", "从组件提取参数", JsonSerializer.SerializeToElement(data));
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

                RecordModifyHistory(
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
                System.Diagnostics.Debug.WriteLine($"记录代码修改历史失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理动态修改组件输入输出端
        /// 输入格式：Ljson中包含InputParams和OutputParams（JSON格式的参数定义数组）
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

                // 1. 从Ljson中获取参数定义JSON
                string inputParamsJson = data.GetParameterString("InputParams") ?? "[]";
                string outputParamsJson = data.GetParameterString("OutputParams") ?? "[]";

                // 2. 解析参数定义
                var targetInputParams = ParamExchange.DeserializeParamDefinitions(inputParamsJson);
                var targetOutputParams = ParamExchange.DeserializeParamDefinitions(outputParamsJson);

                // 3. 记录修改前的参数信息
                var oldInputParamsJson = ParamExchange.SerializeParamDefinitions(component.Params.Input);
                var oldOutputParamsJson = ParamExchange.SerializeParamDefinitions(component.Params.Output);

                // 4. 异步调度修改任务（非常重要：不能在计算过程中直接修改结构）
                var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                if (doc == null)
                    return Ljson.CreateErrorLjson("无法获取Grasshopper文档");

                doc.ScheduleSolution(5, (d) =>
                {
                    // 处理输入端：少加多补
                    SyncParameters(component, component.Params.Input, targetInputParams, true);

                    // 处理输出端：少加多补
                    SyncParameters(component, component.Params.Output, targetOutputParams, false);

                    // 5. 刷新组件外观和布局
                    component.Params.OnParametersChanged();
                    component.OnAttributesChanged();
                    component.ExpireSolution(false);

                    // 6. 记录修改历史
                    try
                    {
                        var modifyData = new Dictionary<string, object>
                        {
                            { "OldInputParams", oldInputParamsJson },
                            { "OldOutputParams", oldOutputParamsJson },
                            { "NewInputParams", inputParamsJson },
                            { "NewOutputParams", outputParamsJson },
                            { "InputParamCount", targetInputParams.Count },
                            { "OutputParamCount", targetOutputParams.Count },
                            { "ComponentType", GetComponentTypeName(component) }
                        };

                        RecordModifyHistory(
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
                        System.Diagnostics.Debug.WriteLine($"记录参数修改历史失败: {ex.Message}");
                    }
                });

                var responseData = new Dictionary<string, object>
                {
                    { "Status", "参数同步指令已发送至调度器" },
                    { "InputParams", targetInputParams.Count.ToString() },
                    { "OutputParams", targetOutputParams.Count.ToString() }
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
