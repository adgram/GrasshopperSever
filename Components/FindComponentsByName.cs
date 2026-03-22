using System;
using Grasshopper.Kernel;
using GrasshopperSever.Commands;
using GrasshopperSever.Params;

namespace GrasshopperSever.Components
{
    public class FindComponentsByName : GH_Component
    {
        /// <summary>
        /// 通过名称查找组件信息
        /// </summary>
        public FindComponentsByName()
          : base("FindComponentsByName", "FCBN",
              "通过组件名称查询组件信息",
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
            pManager.AddTextParameter("Name", "N", "组件名称或昵称", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JQueueParam(), "ComponentInfo", "C", "组件信息 (ComponentJQueue)", GH_ParamAccess.item);
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

            // 调用查询方法
            var result = ComponentInfo.FindComponentsByName(name);

            // 输出结果
            if (result != null)
            {
                DA.SetData(0, new JQueueGoo(result));
            }
            else
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"未找到名称为 {name} 的组件");
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.FindComponentsByName;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9FFB4997-4F1D-4434-BEB3-9F8A544743D6"); }
        }
    }
}