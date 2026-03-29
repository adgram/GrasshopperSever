using Grasshopper.Kernel;
using GrasshopperSever.Commands;
using GrasshopperSever.Utils;
using RhinoCodePluginGH.Components;
using System;
using System.Collections.Generic;
using System.Text.Json;

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
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("ScriptComponent", "SC", "Rhino8 Grasshopper 的脚本组件，仅支持操作一个组件", GH_ParamAccess.tree);
            pManager.AddTextParameter("Code", "C", "脚本代码", GH_ParamAccess.item, "");
            pManager.AddTextParameter("IntputParams", "IP", "输入端参数定义", GH_ParamAccess.item);
            pManager.AddTextParameter("OutputParams", "OP", "输出端参数定义", GH_ParamAccess.item);
            Params.Input[2].Optional = true;
            Params.Input[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Result", "R", "显示运行信息", GH_ParamAccess.item);
            pManager.AddTextParameter("ComponentType", "T", "显示组件信息", GH_ParamAccess.item);
            pManager.AddBooleanParameter("IsSDKMode", "SDK", "代码是否是SDK模式", GH_ParamAccess.item);
            pManager.AddTextParameter("SourceCode", "SC", "代码code", GH_ParamAccess.item);
            pManager.AddTextParameter("InputParams", "IP", "当前输入端参数信息", GH_ParamAccess.item);
            pManager.AddTextParameter("OutputParams", "OP", "当前输出端参数信息", GH_ParamAccess.item);
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
                    "目标组件不是支持的脚本组件");
                DA.SetData(0, "输入的组件无效");
                return;
            }

            // 检测目标组件是否切换了，如果切换了需要重置状态
            if (_lastTargetGuid != targetComponent.InstanceGuid)
            {
                _lastAppliedCode = "";
                _lastTargetGuid = targetComponent.InstanceGuid;
            }

            // 获取代码输入
            string newCode = "";
            DA.GetData(1, ref newCode);

            // 获取可选的输入/输出参数定义
            string inputParamsGoo = null;
            string outputParamsGoo = null;
            DA.GetData(2, ref inputParamsGoo);
            DA.GetData(3, ref outputParamsGoo);

            bool hasParamUpdate = (inputParamsGoo != null) || (outputParamsGoo != null);
            bool hasCodeUpdate = !string.IsNullOrEmpty(newCode) && !_lastAppliedCode.Equals(newCode, StringComparison.Ordinal);

            // 检查代码中是否包含IO注释标记
            bool hasIOComment = !string.IsNullOrEmpty(newCode) &&
                (newCode.Contains("// GH_COMPONENT_IO_START") || newCode.Contains("# GH_COMPONENT_IO_START"));

            // 没有任何需要更新的内容，只同步注释并输出信息
            if (!hasCodeUpdate && !hasParamUpdate)
            {
                GetComponentInfo(targetComponent, DA);
                DA.SetData(0, "无更新");
                return;
            }

            try
            {
                _isUpdatingCode = true;

                // 处理参数更新
                if (hasParamUpdate)
                {
                    var paramData = new Dictionary<string, object>();

                    if (!string.IsNullOrEmpty(inputParamsGoo))
                        paramData["InputParams"] = inputParamsGoo;

                    if (!string.IsNullOrEmpty(outputParamsGoo))
                        paramData["OutputParams"] = outputParamsGoo;

                    if (paramData.Count > 0)
                    {
                        var paramLjson = new Ljson("UpdateParams", "更新参数", JsonSerializer.SerializeToElement(paramData));
                        ScriptParamConfig.UpdateParameters(targetComponent, paramLjson);
                    }
                }

                // 处理代码更新
                if (hasCodeUpdate)
                {
                    GHScript.SetCode(targetComponent, newCode);
                    _lastAppliedCode = newCode;

                    // 只有当代码中有IO注释标记时才根据代码更新参数
                    // 否则保留现有参数，避免意外删除所有输入输出端
                    if (hasIOComment)
                    {
                        GHScript.SetParametersFromScript(targetComponent);
                    }
                }

                // 同步参数注释到代码
                GHScript.SetParametersToScript(targetComponent);

                // 在当前 solution 结束后，仅让目标组件重算
                var doc = OnPingDocument();
                doc?.ScheduleSolution(5, d =>
                {
                    targetComponent.ExpireSolution(false);
                });

                DA.SetData(0, hasCodeUpdate ? "代码更新成功" : "参数更新成功");
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"更新失败: {ex.Message}");
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

        private static void GetComponentInfo(BaseLanguageComponent component, IGH_DataAccess DA)
        {
            if (component == null) return;

            // 获取组件类型信息
            string componentType = GHScript.GetComponentTypeName(component);
            DA.SetData(1, componentType);

            // Is SDK Mode
            bool is_sdk = component.IsSDKMode;
            DA.SetData(2, is_sdk);

            // Source Code
            string source = GHScript.GetCode(component);
            DA.SetData(3, source);

            // Input Params - 使用 ParamExchange 获取详细参数信息
            string inputsJson = JsonSerializer.Serialize(ParamExchange.SerializeParamDefinitions(component.Params.Input).Value);
            DA.SetData(4, inputsJson);

            // Output Params - 使用 ParamExchange 获取详细参数信息
            string outputsJson = JsonSerializer.Serialize(ParamExchange.SerializeParamDefinitions(component.Params.Output));
            DA.SetData(5, outputsJson);
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
