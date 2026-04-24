using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using GrasshopperSever.Params;
using ScriptComponents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace GrasshopperSever.Components
{
    public class RunScript2 : Component_CSNET_Script, IGH_VariableParameterComponent
    {
        private string _cachedCode = "";
        // 定义你要处理的两个固定文件名称
        private const string SqliteDllName = "System.Data.SQLite.dll";
        private static string PluginDllName => Path.GetFileName(Assembly.GetExecutingAssembly().Location);

        public RunScript2() : base()
        {
            this.Name = "RunScript2";
            this.NickName = "RunC#2";
            this.Description = "继承自旧版C#Script，输出端口为LJ";
            this.Category = "Maths";
            this.SubCategory = "Sever";
        }

        public override bool Obsolete => false;

        public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddScriptVariableParameter("Code", "C", "添加C#脚本到C#的RunScript函数内", GH_ParamAccess.item);
            pManager.AddScriptVariableParameter("Using", "U", "为脚本添加Using语句", GH_ParamAccess.item);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Out", "O", "Output", GH_ParamAccess.item);
            pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "数据输出", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string code = "";
            string uusing = "";
            DA.GetData(0, ref code);
            DA.GetData(1, ref uusing);

            if (string.IsNullOrEmpty(code))
                code = _cachedCode;
            if (string.IsNullOrEmpty(code))
                code = "Print(\"Hello\");";

            EnsureLocalReferences();

            if (code != _cachedCode)
            {
                ClearScriptAssemblyCache();
                _cachedCode = code;
                this.ScriptSource.ScriptCode = code;
                this.ScriptSource.UsingCode = "using GrasshopperSever.Utils; using System.Data.SQLite;" + uusing;
            }
            base.SolveInstance(DA);
        }
        private void ClearScriptAssemblyCache()
        {
            var prop = typeof(Component_AbstractScript_Roslyn).GetProperty(
                "ScriptAssembly", BindingFlags.NonPublic | BindingFlags.Instance);
            prop?.SetValue(this, null);
        }

        // 检查并动态注入当前设备的路径，防止重复添加
        private void EnsureLocalReferences()
        {
            if (this.ScriptSource == null || this.ScriptSource.References == null) return;

            var thisFile = Assembly.GetExecutingAssembly().Location;
            string sqlFile = Path.Combine(Path.GetDirectoryName(thisFile), SqliteDllName);

            bool hasPlugin = false;
            bool hasSql = false;

            // 遍历当前引用，看是否已经加过了
            foreach (var refPath in this.ScriptSource.References)
            {
                string fileName = Path.GetFileName(refPath);
                if (fileName.Equals(RunScript2.PluginDllName, StringComparison.OrdinalIgnoreCase)) hasPlugin = true;
                if (fileName.Equals(SqliteDllName, StringComparison.OrdinalIgnoreCase)) hasSql = true;
            }

            // 如果没加过，再把当前电脑的绝对路径填进去
            if (!hasPlugin && File.Exists(thisFile))
                this.ScriptSource.References.Add(thisFile);

            if (!hasSql && File.Exists(sqlFile))
                this.ScriptSource.References.Add(sqlFile);
        }

        public override void CreateAttributes()
        {
            m_attributes = new GH_ComponentAttributes(this);
        }
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        // 核心 1：保存文件时（序列化）拦截，剔除绝对路径
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            List<string> temporarilyRemovedPaths = new List<string>();

            // 1. 在保存前，找出我们的两个固定路径，并把它们从列表中删掉
            if (this.ScriptSource != null && this.ScriptSource.References != null)
            {
                for (int i = this.ScriptSource.References.Count - 1; i >= 0; i--)
                {
                    string refPath = this.ScriptSource.References[i];
                    string fileName = Path.GetFileName(refPath);

                    if (fileName.Equals(SqliteDllName, StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals(PluginDllName, StringComparison.OrdinalIgnoreCase))
                    {
                        temporarilyRemovedPaths.Add(refPath);
                        this.ScriptSource.References.RemoveAt(i);
                    }
                }
            }

            // 2. 执行原生的序列化操作（此时写入文件的列表是干净的）
            bool result = base.Write(writer);

            // 3. 保存完之后，立刻把路径加回来，以免影响当前电脑的继续使用
            if (this.ScriptSource != null)
            {
                foreach (var path in temporarilyRemovedPaths)
                {
                    this.ScriptSource.References.Add(path);
                }
            }

            return result;
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.P20_RunScript;

        public override Guid ComponentGuid => new Guid("8E02C89D-B876-4197-862E-7D668858304B");
    }
}