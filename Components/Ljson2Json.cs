using System;
using Grasshopper.Kernel;
using GrasshopperSever.Params;

namespace GrasshopperSever.Components
{
    public class Ljson2Json : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Ljson2Json()
          : base("Ljson2Json", "L2J",
              "将Ljson转换为Json",
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
            pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "需要转换的Ljson", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("String", "S", "Json格式", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            LjsonGoo jlistGoo = null;
            if (!DA.GetData(0, ref jlistGoo))
            {
                return;
            }

            if (jlistGoo == null || !jlistGoo.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Ljson 输入无效");
                return;
            }

            string jsonString = jlistGoo.Value.ToJson();
            DA.SetData(0, jsonString);
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
                return Properties.Resources.P04_Ljson2Json;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0F40D0B0-06E4-4505-8ED3-F02186FE084B"); }
        }
    }
}