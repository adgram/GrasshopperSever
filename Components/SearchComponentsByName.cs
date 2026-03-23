using System;
using Grasshopper.Kernel;
using GrasshopperSever.Commands;
using GrasshopperSever.Params;

namespace GrasshopperSever.Components
{
    public class SearchComponentsByName : GH_Component
    {
        /// <summary>
        /// 通过名称模糊搜索组件信息
        /// </summary>
        public SearchComponentsByName()
          : base("SearchComponentsByName", "SCBN",
              "通过名称模糊搜索组件信息",
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
            pManager.AddTextParameter("Name", "N", "搜索关键词（模糊匹配）", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JListParam(), "Components", "C", "匹配的组件信息列表 (ComponentJList)", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = string.Empty;

            // 获取输入参数
            if (!DA.GetData(0, ref name))
            {
                return;
            }

            // 调用搜索方法
            var results = ComponentInfo.SearchComponentsByName(name);

            // 输出结果
            if (results != null && results.Count > 0)
            {
                foreach (var result in results)
                {
                    DA.SetData(0, new JListGoo(result));
                }
            }
            else
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"未找到包含 '{name}' 的组件");
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.P14_SearchComponentsByName;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("494A2586-868E-4BE1-B560-99B65BF52D72"); }
        }
    }
}