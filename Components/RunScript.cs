using Grasshopper.Kernel;
using GrasshopperSever.Params;
using RhinoCodePluginGH.Components;
using System;
using System.Linq;
using GrasshopperSever.Commands;

namespace GrasshopperSever.Components
{
    public class RunScript : GH_Component
    {
        // 编译缓存
        private readonly CSharpComponent _cachedComponent;
        // 当前代码
        private string _cachedCode = @"
        // Grasshopper Script Instance
        #region Usings
        using System;
        using System.Linq;
        using System.Collections;
        using System.Collections.Generic;
        using System.Drawing;
        using GrasshopperSever.Utils;

        using Rhino;
        using Rhino.Geometry;

        using Grasshopper;
        using Grasshopper.Kernel;
        using Grasshopper.Kernel.Data;
        using Grasshopper.Kernel.Types;
        using GrasshopperSever.Params;
        #endregion

        public class Script_Instance : GH_ScriptInstance
        {
            #region Notes
            /*
              Members:
                RhinoDoc RhinoDocument
                GH_Document GrasshopperDocument
                IGH_Component Component
                int Iteration
        
              Methods (Virtual & overridable):
                Print(string text)
                Print(string format, params object[] args)
                Reflect(object obj)
                Reflect(object obj, string method_name)
            */
            #endregion
        
            private void RunScript(ref object ljson)
            {
                ljson = Ljson.CreateOKLjson(@""初始化成功"");
            }
        }";

        /// <summary>
        /// Initializes a new instance of the RunScript class.
        /// </summary>
        public RunScript()
          : base("RunScript", "RunC#",
              "一个对C#的包装器",
                "Maths", "Sever")
        {
            // 初始化组件
            _cachedComponent ??= RunScript.CreateCSharpComponent();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "C", "脚本", GH_ParamAccess.item, "");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "数据输出", GH_ParamAccess.item);
            pManager.AddTextParameter("Out", "O", "调试输出", GH_ParamAccess.item);
        }


        /// <summary>
        /// 求解逻辑 - 使用缓存的 CSharpComponent 执行代码
        /// </summary>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            try
            {
                // 收集调试信息
                var debugInfo = new System.Text.StringBuilder();
                string _currentCode = "";

                // 读取输入参数
                if (!DA.GetData(0, ref _currentCode))
                    _currentCode = _cachedCode;

                // 1. 如果代码有改变，更新代码并重新配置参数
                if (_cachedCode != _currentCode && !string.IsNullOrEmpty(_currentCode))
                {
                    _cachedCode = _currentCode;
                }
                else
                {
                    debugInfo.AppendLine("代码未改变，使用缓存代码");
                }
                string _code = "#r \"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"\r\n" + _cachedCode;
                _cachedComponent.SetSource(_code);

                // 使用 SetParametersFromScript 自动解析脚本参数
                //_cachedComponent.SetParametersFromScript();
                ScriptParamConfig.EnsureParameter(_cachedComponent, "ljson", GH_ParamAccess.item, false, false);

                // 2. 强制内部组件进行计算！
                _cachedComponent.ClearData();
                _cachedComponent.CollectData();
                _cachedComponent.ComputeData(); // 真正执行内部C#脚本的 SolveInstance

                // 3. 读取内部组件的输出数据
                var outputData = _cachedComponent.Params.Output;

                // 查找 out 端口（默认的调试输出）
                var outParam = outputData.FirstOrDefault(p => p.Name == "out" || p.NickName == "out");
                if (outParam != null)
                {
                    var paths = outParam.VolatileData.Paths;
                    foreach (var path in paths)
                    {
                        var items = outParam.VolatileData.get_Branch(path);
                        debugInfo.AppendLine($"out端口有 {items.Count} 项数据");
                        foreach (var item in items)
                        {
                            debugInfo.AppendLine($"数据项类型: {item.GetType().Name}, 值: {item}");
                        }
                    }
                }
                else
                {
                    debugInfo.AppendLine("未找到 out 端口！");
                }

                // 查找 ljson 端口（优先 ScriptVariableParam 类型）
                var ljsonParam = outputData.FirstOrDefault(p => (p.Name == "ljson" || p.NickName == "ljson"));

                if (ljsonParam != null)
                {
                    if (!ljsonParam.VolatileData.IsEmpty)
                    {
                        DA.SetDataTree(0, ljsonParam.VolatileData);
                    }
                    else
                    {
                        debugInfo.AppendLine("ljson 端口数据为空！");
                    }
                }
                else
                {
                    debugInfo.AppendLine("未找到 ljson 端口！");
                }

                // 将调试信息作为最终输出（覆盖原有的 out 输出）
                DA.SetData(1, debugInfo.ToString());
            }
            catch (Exception ex)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"执行失败: {ex.Message}");
                DA.SetData(1, $"错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建 CSharpComponent 实例（只创建一次）
        /// </summary>
        private static CSharpComponent CreateCSharpComponent()
        {
            var component = CSharpComponent.Create("TempScript", "", null, true);
            // 启用标准输出参数
            component.UsingStandardOutputParam = true;
            component.UsingScriptOutputParam = true; // 启用脚本输出参数
            return component;
        }

        /// <summary>
        /// 组件从文档中移除时清理
        /// </summary>
        public override void RemovedFromDocument(GH_Document document)
        {
            if (_cachedComponent != null && _cachedComponent.OnPingDocument() != null)
            {
                _cachedComponent.OnPingDocument().RemoveObject(_cachedComponent, false);
            }
            base.RemovedFromDocument(document);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.P20_RunScript;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("0329F832-5487-438F-97A8-CC05DE2AC415");

    }
}