using Grasshopper.Kernel;
using GrasshopperSever.Commands;
using RhinoCodePlatform.GH;
using RhinoCodePluginGH.Components;
using System;

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
        private string _lastAppliedParams = "";

        /// <summary>
        /// 上次更新的目标组件GUID
        /// </summary>
        private Guid _lastTargetGuid = Guid.Empty;

        private string _log = "";

        /// <summary>
        /// Initializes a new instance of the ScriptEditor class.
        /// </summary>
        public ScriptEditor()
          : base("ScriptEditor", "Script",
              "操作Script组件。如果输入代码，会修改脚本的代码",
                "Maths", "Sever")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

        private void AddLog(string message)
        {
            _log += message + Environment.NewLine;
        }

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
            var component = GetLanguageComponent();
            if (component == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "目标组件不是支持的脚本组件");
                DA.SetData(0, "输入的组件无效");
                return;
            }

            // 检测目标组件是否切换了，如果切换了需要重置状态
            if (_lastTargetGuid != component.InstanceGuid)
            {
                _lastAppliedCode = "";
                _lastAppliedParams = "";
                _log = "";
                _lastTargetGuid = component.InstanceGuid;
            }

            // 获取代码输入
            string newCode = "";
            DA.GetData(1, ref newCode);

            // 获取可选的输入/输出参数定义
            string inputParams = null;
            string outputParams = null;
            DA.GetData(2, ref inputParams);
            DA.GetData(3, ref outputParams);

            bool hasParamUpdate = !_lastAppliedParams.Equals(inputParams + outputParams, StringComparison.Ordinal);
            bool hasCodeUpdate = !_lastAppliedCode.Equals(newCode, StringComparison.Ordinal);
            // 没有任何需要更新的内容，只同步注释并输出信息
            if (!hasCodeUpdate && !hasParamUpdate)
            {
                GetComponentInfo(component, DA);
                AddLog("无更新");
                DA.SetData(0, _log);
                return;
            }
            if (!hasCodeUpdate)
            {
                newCode = GHScript.GetCode(component);
                AddLog($"获取组件代码，长度: {newCode?.Length ?? 0}");
            }

            _lastAppliedCode = newCode;
            _lastAppliedParams = inputParams + outputParams;

            GHScript.UpdateScript(component, ref newCode, inputParams, outputParams);

            try
            {
                _isUpdatingCode = true;

                GHScript.SetCode(component, newCode);
                AddLog("修改了代码");

                // 只有当代码中有IO注释标记时才根据代码更新参数
                var _l = GHScript.SetParametersFromScript(component, newCode);
                if (_l == null)
                {
                    // 在当前 solution 结束后，仅让目标组件重算
                    var doc = OnPingDocument();
                    doc?.ScheduleSolution(5, d =>
                    {
                        component.ExpireSolution(false);
                    });
                }
                else
                {
                    AddLog(_l);
                }
                AddLog(hasCodeUpdate ? "代码更新成功" : "参数更新成功");
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"更新失败: {ex.Message}");
                AddLog($"Error: {ex.Message}");
            }
            finally
            {
                _isUpdatingCode = false;
                GetComponentInfo(component, DA);
                DA.SetData(0, _log);
            }
        }

        /// <summary>
        /// 从输入参数 Sources 中获取连接的 LanguageComponent
        /// </summary>
        public BaseLanguageComponent GetLanguageComponent()
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

            // Input Params
            string inputsJson = ScriptParamSerializer.SerializeParamDefinitions(((IScriptComponent)component).Inputs);
            DA.SetData(4, inputsJson);

            // Output Params
            string outputsJson = ScriptParamSerializer.SerializeParamDefinitions(((IScriptComponent)component).Outputs);
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
