using System;
using Grasshopper.Kernel;
using GrasshopperSever.Commands;
using GrasshopperSever.Params;
using GrasshopperSever.Utils;

namespace GrasshopperSever.Components
{
    public class AllComponents : GH_Component
    {
        private bool _lastRefreshValue = false;

        /// <summary>
        /// 获取所有注册的组件
        /// </summary>
        public AllComponents()
          : base("AllComponents", "AllComps",
              "获取所有已注册的组件信息",
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
            pManager.AddBooleanParameter("Refresh", "R", "刷新，值改变就刷新一次", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JListParam(), "JList", "JQ", "所有组件的信息", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool refresh = false;
            DA.GetData(0, ref refresh);

            // 只有当 refresh 值改变时才执行刷新
            if (refresh != _lastRefreshValue)
            {
                _lastRefreshValue = refresh;

                // 调用 Infos 中的方法获取所有组件信息
                JList jlist = ComponentInfo.GetAllComponentsNested();

                // 设置输出
                DA.SetData(0, jlist);
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.P10_AllComponents;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("1C08E2CE-AA86-4EBA-8A63-D3567C5169D7");
    }
}