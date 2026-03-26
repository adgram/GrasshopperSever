using System;
using Grasshopper.Kernel;
using GrasshopperSever.Commands;
using GrasshopperSever.Params;
using GrasshopperSever.Utils;

namespace GrasshopperSever.Components
{
    public class CommandRhino : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CommandRhino class.
        /// </summary>
        public CommandRhino()
          : base("CommandRhino", "CmdRhino",
              "执行Rhino命令，支持运行脚本、获取最后创建的对象、选择对象等操作",
                "Maths", "Sever")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "要执行的Rhino命令Ljson数据，必须包含Command字段", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new LjsonParam(), "Result", "R", "执行后的Ljson结果", GH_ParamAccess.item);
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
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入的Ljson数据为空");
                DA.SetData(0, new LjsonGoo(Ljson.CreateErrorLjson("输入数据为空")));
                return;
            }

            Ljson inputLjson = jsonGoo.Value;

            // 执行Rhino命令并获取结果
            try
            {
                Ljson resultLjson = ActuatorHandle.DoRhinoCommand(inputLjson);
                DA.SetData(0, new LjsonGoo(resultLjson));
            }
            catch (Exception ex)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"执行Rhino命令失败: {ex.Message}");
                DA.SetData(0, new LjsonGoo(Ljson.CreateErrorLjson($"执行Rhino命令失败: {ex.Message}")));
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.P18_CommandRhino;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("25195B9A-8203-47DA-A568-5023891ED8F6"); }
        }
    }
}