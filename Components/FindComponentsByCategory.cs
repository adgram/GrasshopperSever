using System;
using Grasshopper.Kernel;
using GrasshopperSever.Commands;
using GrasshopperSever.Params;

namespace GrasshopperSever.Components
{
    public class FindComponentsByCategory : GH_Component
    {
        /// <summary>
        /// 通过分类和名称查找组件信息
        /// </summary>
        public FindComponentsByCategory()
          : base("FindComponentsByCategory", "FCBC",
              "通过分类和名称查询组件信息",
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
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Category", "C", "主分类", GH_ParamAccess.item);
            pManager.AddTextParameter("SubCategory", "SC", "子分类", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "N", "组件名称或昵称", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JQueueParam(), "ComponentInfo", "CI", "组件信息 (ComponentJQueue)", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string category = string.Empty;
            string subCategory = string.Empty;
            string name = string.Empty;

            // 获取输入参数
            DA.GetData(0, ref category);
            DA.GetData(1, ref subCategory);
            DA.GetData(2, ref name);

            // 调用查询方法
            var result = ComponentInfo.FindComponentsByCategory(category, subCategory, name);

            // 输出结果
            if (result != null)
            {
                DA.SetData(0, new JQueueGoo(result));
            }
            else
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "未找到匹配的组件");
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.FindComponentsByCategory;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("CFF58902-2619-4CA0-B524-6AD8F52BF17C"); }
        }
    }
}