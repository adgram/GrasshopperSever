using System;
using Grasshopper.Kernel;
using GrasshopperSever.Params;

namespace GrasshopperSever.Components
{
    public class FindJData : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ReadJList class.
        /// </summary>
        public FindJData()
          : base("FindJData", "SJL",
              "通过名称查找JData",
              "Maths", "Sever")
        {
        }
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.last;
            }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new JListParam(), "JList", "JQ", "需要转换的JList", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "N", "需要查找的键值", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Data", "D", "找到的值", GH_ParamAccess.item);
            pManager.AddTextParameter("DataList", "DL", "找到的值", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            JListGoo jlistGoo = null;
            string name = "";

            // 获取输入参数
            if (!DA.GetData(0, ref jlistGoo)) return;
            if (!DA.GetData(1, ref name)) return;

            // 检查输入是否有效
            if (jlistGoo == null || jlistGoo.Value == null)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入的JList为空");
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入的Name为空");
                return;
            }

            // 查找参数值
            var jlist = jlistGoo.Value;
            string value = jlist.GetParameter(name);
            string[] values = jlist.SearchParameter(name);

            // 设置输出
            DA.SetData(0, value);
            DA.SetDataList(1, values);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.P17_FindJData;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("ECCCF6E5-C2BC-415D-9FFA-9DAFD7D6F9E2"); 
    }
}