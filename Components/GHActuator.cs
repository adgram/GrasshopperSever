using System;
using System.Text.Json;
using Grasshopper.Kernel;
using GrasshopperSever.Commands;
using GrasshopperSever.Params;
using GrasshopperSever.Utils;

namespace GrasshopperSever.Components
{
    public class GHActuator : GH_Component
    {
        private string _output_data = null;

        /// <summary>
        /// 专门用于执行一些特殊的Ljson
        /// </summary>
        public GHActuator()
          : base("GHActuator", "Actuator",
              "专门用于执行一些特殊的Ljson",
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
            pManager.AddParameter(new LjsonParam(), "LJson", "LJ", "要发送的Ljson数据", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "ST", "执行结果", GH_ParamAccess.item);
            pManager.AddParameter(new LjsonParam(), "Result", "R", "处理后的Ljson结果", GH_ParamAccess.item);
            pManager.AddGenericParameter("OutPut", "O", "显示输出数据", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            LjsonGoo jsonGoo = null;

            // 获取输入参数
            if (!DA.GetData(0, ref jsonGoo)) return;

            // 检查输入是否有效
            if (jsonGoo == null || jsonGoo.Value == null)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入的Json队列为空");
                DA.SetData(0, "无输入数据");
                return;
            }

            Ljson lst = jsonGoo.Value;

            // 处理命令并获取结果
            try
            {
                var res_lst = DoCommand(lst, ref _output_data);
                DA.SetData(0, res_lst.ToString());
                DA.SetData(1, new LjsonGoo(res_lst));
                DA.SetData(2, _output_data);
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.P09_GHActuator;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("6FDF874C-D2AC-43C7-A4DB-196A227189F2"); 
        
        /// <summary>
        /// 这里处理数据
        /// </summary>
        public static Ljson DoCommand(Ljson lst, ref string out_data)
        {
            var h_type = LjsonTypeDetector.DetectType(lst);

            // 根据 Value 值判断类型（不区分大小写）
            out_data = lst.GetParameterString("OUTPUT");
            switch (h_type)
            {
                case LjsonType.Component:
                    return ActuatorHandle.DoComponentCommand(lst);

                case LjsonType.Document:
                    return ActuatorHandle.DoDocumentCommand(lst);

                case LjsonType.Rhino:
                    return ActuatorHandle.DoRhinoCommand(lst);

                case LjsonType.Design:
                    break;

                default:
                    break;
            }
            return Ljson.CreateOKLjson("ok");
        }
    }
}