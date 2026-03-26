using System;
using Grasshopper.Kernel;
using GrasshopperSever.Commands;
using GrasshopperSever.Params;

namespace GrasshopperSever.Components
{
    public class FindComponentsByGuid : GH_Component
    {
        /// <summary>
        /// 通过 GUID 查找组件信息
        /// </summary>
        public FindComponentsByGuid()
          : base("FindComponentsByGuid", "FCBG",
              "通过组件 GUID 查询组件信息",
              "Maths", "Sever")
        {
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
            }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Guid", "G", "组件的 GUID", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new LjsonParam(), "ComponentInfo", "C", "组件信息 (ComponentLjson)", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string guid = string.Empty;

            // 获取输入参数
            if (!DA.GetData(0, ref guid))
            {
                return;
            }

            // 调用查询方法
            var result = ComponentInfo.FindComponentsByGuid(guid);

            // 输出结果
            if (result != null)
            {
                DA.SetData(0, new LjsonGoo(result));
            }
            else
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"未找到 GUID 为 {guid} 的组件");
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.P11_FindComponentsByGuid;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("A573298C-1820-413F-A972-BCCAEFE770EC"); }
        }
    }
}