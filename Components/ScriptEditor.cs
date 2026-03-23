using Grasshopper.Kernel;
using RhinoCodePluginGH.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using GrasshopperSever.Utils;

namespace GrasshopperSever.Components
{
    public class ScriptEditor : GH_Component
    {
        /// <summary>
        /// 标记是否正在更新代码，用于防止循环调用
        /// </summary>
        private bool _isUpdatingCode = false;

        /// <summary>
        /// 上次设置的代码，用于检测是否需要更新
        /// </summary>
        private string _lastAppliedCode = "";

        /// <summary>
        /// 上次更新的目标组件GUID
        /// </summary>
        private Guid _lastTargetGuid = Guid.Empty;

        /// <summary>
        /// Initializes a new instance of the ScriptEditor class.
        /// </summary>
        public ScriptEditor()
          : base("ScriptEditor", "Script",
              "操作Script组件。如果输入代码，会修改脚本的代码",
                "Maths", "Sever")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("ScriptComponent", "SC", "Rhino8 Grasshopper 的脚本组件，仅支持操作一个组件", GH_ParamAccess.tree);
            pManager.AddTextParameter("Code", "C", "脚本代码", GH_ParamAccess.item, "");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Result", "R", "显示运行信息", GH_ParamAccess.item);
            pManager.AddTextParameter("ComponentType", "T", "显示组件信息", GH_ParamAccess.item);
            pManager.AddBooleanParameter("IsSDKMode", "SDK", "代码是否是SDK模式", GH_ParamAccess.item);
            pManager.AddTextParameter("SourceCode", "SC", "代码code", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
            DA.SetData(0, "");
            DA.SetData(1, "");
            DA.SetData(2, false);
            DA.SetData(3, "");
            // 如果正在更新代码，跳过执行以防止循环调用
            if (_isUpdatingCode)
            {
                DA.SetData(0, "程序正在运行...");
                return;
            }
            // 通过 Sources 获取连接的脚本组件
            var targetComponent = GetLanguageComponent();
            if (targetComponent == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "The provided component is not a valid LanguageComponent (C# or Python script component)");
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "目标组件不是支持的脚本组件");
                DA.SetData(0, "输入的组件无效");
                return;
            }
            // 获取代码输入
            string newCode = "";
            DA.GetData(1, ref newCode);
            // 检查是否需要更新（代码或目标组件是否改变）
            if (string.IsNullOrEmpty(newCode) || _lastAppliedCode.Equals(newCode, StringComparison.Ordinal))
            {
                GetComponentInfo(targetComponent, DA);
                //targetComponent.Read
                DA.SetData(0, "输入的代码未更新");
                return;
            }

            // 应用代码更改
            try
            {
                _isUpdatingCode = true;

                // 设置代码源
                targetComponent.SetSource(newCode);

                // 记录上次应用的代码和目标
                _lastAppliedCode = newCode;
                _lastTargetGuid = targetComponent.InstanceGuid;

                // 根据新代码更新参数
                targetComponent.SetParametersFromScript();
                targetComponent.SetParametersToScript();
                //UpdateParameters(targetComponent, _parameters)
                // 在当前 solution 结束后，仅让目标组件重算
                // 使用 ScheduleSolution + ExpireComponent 而非 ExpireSolution，避免触发本组件循环
                var doc = OnPingDocument();
                doc?.ScheduleSolution(5, d =>
                {
                    targetComponent.ExpireSolution(false);
                });

                DA.SetData(0, $"代码更新成功");
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"代码更新失败: {ex.Message}");
                DA.SetData(0, $"Error: {ex.Message}");
            }
            finally
            {
                _isUpdatingCode = false;
                GetComponentInfo(targetComponent, DA);
            }
        }

        /// <summary>
        /// 从输入参数 Sources 中获取连接的 LanguageComponent
        /// </summary>
        private BaseLanguageComponent GetLanguageComponent()
        {
            BaseLanguageComponent sourceComponent = null;

            // 遍历第一个输入参数的所有连接源
            foreach (var source in Params.Input[0].Sources)
            {
                // 获取源参数的顶级文档对象
                if (source is IGH_Param sourceParam && sourceParam.Attributes != null)
                {
                    var docObject = sourceParam.Attributes.GetTopLevel?.DocObject;

                    // 检查是否是 LanguageComponent
                    if (docObject is BaseLanguageComponent baseLangComp)
                    {
                        sourceComponent = baseLangComp;
                        break;
                    }

                    // 尝试通过类型检查
                    if (docObject is IGH_Component ghComp)
                    {
                        // 直接尝试转换
                        if (ghComp is BaseLanguageComponent baseComp)
                        {
                            sourceComponent = baseComp;
                            break;
                        }

                        // 通过类型名称检查
                        var typeName = ghComp.GetType().Name;
                        if (typeName.Contains("CSharp") ||
                            typeName.Contains("Python") ||
                            typeName.Contains("Script"))
                        {
                            // 再次尝试转换
                            if (ghComp is BaseLanguageComponent scriptComp)
                            {
                                sourceComponent = scriptComp;
                                break;
                            }
                        }
                    }
                }
            }

            return sourceComponent;
        }


        /// <summary>
        /// 获取组件类型的友好名称
        /// </summary>
        private static string GetComponentTypeName(BaseLanguageComponent component)
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

        private static void GetComponentInfo(BaseLanguageComponent component, IGH_DataAccess DA)
        {
            JList cdata = new JList();
            if (component == null) return;

            // 获取组件类型信息
            string componentType = GetComponentTypeName(component);
            DA.SetData(1, componentType);
            cdata.Add(new JData("Type", "目标脚本组件类型", componentType));

            // Is SDK Mode
            bool is_sdk = component.IsSDKMode;
            DA.SetData(2, is_sdk);
            cdata.Add(new JData("IsSDK", "脚本结构类型", is_sdk.ToString()));

            // Source Code
            string source = "";
            try
            {
                source = component.TryGetSource(out var src) ? src : "(no source)";
                DA.SetData(3, source);
            }
            catch
            {
                source = "null";
                DA.SetData(3, "获取失败");
            }
            cdata.Add(new JData("Code", "脚本代码", source));

            // Input Params
            List<string> inputNames = null;
            try
            {
                inputNames = component.Params.Input
                    .Select(p => $"{p.NickName} ({p.GetType().Name})")
                    .ToList();
            }
            catch { }
            cdata.Add(new JData("Code", "脚本代码", inputNames.ToString()));

            // Output Params
            List<string> outputNames = null;
            try
            {
                outputNames = component.Params.Output
                    .Select(p => $"{p.NickName} ({p.GetType().Name})")
                    .ToList();
            }
            catch { }
            cdata.Add(new JData("Code", "脚本代码", outputNames.ToString()));

            // Instance GUID
            string guid = component.InstanceGuid.ToString();
            cdata.Add(new JData("GUID", "目标组件的GUID", guid));
        }

        /// <summary>
        /// 处理动态修改组件输入输出端
        /// 输入格式：JSON字符串 {"InputParams":"x,y","OutputParams":"result"}
        /// 参数列表格式：逗号或分号分隔的参数名，例如 "x,y,z"
        /// 功能：按照列表匹配参数，缺少的添加，多余的删除（少加多补）
        /// </summary>
        private static JList UpdateParameters(BaseLanguageComponent component, string jsonData)
        {
            try
            {
                if (component == null)
                    return JList.CreateErrorJList("目标组件无效");

                if (string.IsNullOrWhiteSpace(jsonData))
                    return JList.CreateErrorJList("JSON数据为空");

                // 1. 解析JSON字符串（简单解析，不依赖Newtonsoft.Json）
                var parameters = new Dictionary<string, string>();
                var pairs = jsonData.Trim().TrimStart('{').TrimEnd('}').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var pair in pairs)
                {
                    var keyValue = pair.Split(new[] { ':' }, 2);
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].Trim().Trim('"');
                        var value = keyValue[1].Trim().Trim('"');
                        parameters[key] = value;
                    }
                }

                // 2. 获取参数列表
                if (!parameters.TryGetValue("InputParams", out string inputParamsList))
                    inputParamsList = "";

                if (!parameters.TryGetValue("OutputParams", out string outputParamsList))
                    outputParamsList = "";

                // 3. 解析参数列表
                var targetInputParams = string.IsNullOrWhiteSpace(inputParamsList)
                    ? new List<string>()
                    : inputParamsList.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .ToList();

                var targetOutputParams = string.IsNullOrWhiteSpace(outputParamsList)
                    ? new List<string>()
                    : outputParamsList.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .ToList();

                // 4. 异步调度修改任务（非常重要：不能在计算过程中直接修改结构）
                var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                doc.ScheduleSolution(5, (d) =>
                {
                    // 处理输入端：少加多补
                    SyncParameters(component.Params.Input, targetInputParams, true);

                    // 处理输出端：少加多补
                    SyncParameters(component.Params.Output, targetOutputParams, false);

                    // 5. 刷新组件外观和布局
                    component.Params.OnParametersChanged();
                    component.OnAttributesChanged();
                    component.ExpireSolution(false);
                });

                var response = new JList();
                response.Add(new JData("Status", "消息", "参数同步指令已发送至调度器"));
                response.Add(new JData("InputParams", "目标输入参数", string.Join(",", targetInputParams)));
                response.Add(new JData("OutputParams", "目标输出参数", string.Join(",", targetOutputParams)));
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
        /// <param name="currentParams">当前参数列表</param>
        /// <param name="targetParams">目标参数名称列表</param>
        /// <param name="isInput">是否为输入参数</param>
        private static void SyncParameters(IList<IGH_Param> currentParams, List<string> targetParams, bool isInput)
        {
            var currentNames = currentParams.Select(p => p.NickName).ToList();

            // 1. 删除多余的参数（在目标列表中不存在的）
            // 从后往前删除，避免索引变化问题
            for (int i = currentParams.Count - 1; i >= 0; i--)
            {
                if (!targetParams.Contains(currentNames[i]))
                {
                    if (isInput)
                    {
                        var component = currentParams[i].Attributes.GetTopLevel.DocObject as IGH_Component;
                        component?.Params.UnregisterInputParameter(currentParams[i]);
                    }
                    else
                    {
                        var component = currentParams[i].Attributes.GetTopLevel.DocObject as IGH_Component;
                        component?.Params.UnregisterOutputParameter(currentParams[i]);
                    }
                }
            }

            // 2. 添加缺失的参数（在目标列表中存在但当前没有的）
            foreach (var targetName in targetParams)
            {
                if (!currentNames.Contains(targetName))
                {
                    if (isInput)
                    {
                        var p = new Grasshopper.Kernel.Parameters.Param_GenericObject();
                        p.Name = targetName;
                        p.NickName = targetName;
                        p.MutableNickName = true;
                        p.Access = GH_ParamAccess.item;
                        var component = currentParams.FirstOrDefault()?.Attributes?.GetTopLevel?.DocObject as IGH_Component;
                        component?.Params.RegisterInputParam(p);
                    }
                    else
                    {
                        var p = new Grasshopper.Kernel.Parameters.Param_GenericObject();
                        p.Name = targetName;
                        p.NickName = targetName;
                        var component = currentParams.FirstOrDefault()?.Attributes?.GetTopLevel?.DocObject as IGH_Component;
                        component?.Params.RegisterOutputParam(p);
                    }
                }
            }
        }


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.P16_ScriptEditor;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("FC1C200D-A3C4-42C5-9BF0-42E56EE4020F");
    }
}
