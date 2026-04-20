using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using GrasshopperSever.Params;
using ScriptComponents;
using System;
using System.IO;
using System.Reflection;

namespace GrasshopperSever.Components
{
    public class RunScript2 : Component_CSNET_Script, IGH_VariableParameterComponent
    {
        private string _cachedCode = "";

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

            if (code != _cachedCode)
            {
                ClearScriptAssemblyCache();
                _cachedCode = code;
                this.ScriptSource.ScriptCode = code;
                var thisFile = Assembly.GetExecutingAssembly().Location;
                string sqlFile = Path.Combine(Path.GetDirectoryName(thisFile), "System.Data.SQLite.dll");
                this.ScriptSource.References.Add(thisFile);
                this.ScriptSource.References.Add(sqlFile);
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

        public override void CreateAttributes()
        {
            m_attributes = new GH_ComponentAttributes(this);
        }
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.P20_RunScript;

        public override Guid ComponentGuid => new Guid("8E02C89D-B876-4197-862E-7D668858304B");
    }
}