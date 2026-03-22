using System;
using Grasshopper.Kernel;
using GrasshopperSever.Params;
using GrasshopperSever.Utils;
using GrasshopperSever.Commands;

namespace GrasshopperSever.Components
{
    public class GHActuator : GH_Component
    {
        private Actuator _actuator = new Actuator(); // 执行器

        /// <summary>
        /// 专门用于执行一些特殊的JQueue
        /// </summary>
        public GHActuator()
          : base("GHActuator", "Actuator",
              "专门用于执行一些特殊的JQueue",
                "Maths", "Sever")
        {
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new JQueueParam(), "Json", "JS", "要发送的JQueue数据", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "ST", "执行结果", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            JQueueGoo jsonGoo = null;

            // 获取输入参数
            if (!DA.GetData(0, ref jsonGoo)) return;

            // 检查输入是否有效
            if (jsonGoo == null || jsonGoo.Value == null)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入的Json队列为空");
                DA.SetData(0, "无输入数据");
                return;
            }

            JQueue queue = jsonGoo.Value;

            // 创建或更新发送器
            try
            {
                var res_queue = _actuator.DoCommand(queue);
                DA.SetData(0, res_queue.ToString());
            }
            catch (Exception ex)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"执行失败: {ex.Message}");
                DA.SetData(0, $"执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.GHActuator;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("6FDF874C-D2AC-43C7-A4DB-196A227189F2"); 
    }
}