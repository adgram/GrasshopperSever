using System;
using Grasshopper.Kernel;
using GrasshopperSever.Params;
using GrasshopperSever.Utils;

namespace GrasshopperSever.Components
{
    /// <summary>
    /// 将 JSON 字符串转换为 JList
    /// </summary>
    public class Json2JList : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the String2JList class.
        /// </summary>
        public Json2JList()
          : base("Json2JList", "J2Q",
              "将JSON格式的字符串转换为JList对象",
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
            pManager.AddTextParameter("String", "S", "JSON格式的字符串", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JListParam(), "JList", "JQ", "转换后的JList对象", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string jsonString = null;
            if (!DA.GetData(0, ref jsonString))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "JSON字符串不能为空");
                return;
            }

            try
            {
                JList jlst = new JList(jsonString);
                DA.SetData(0, jlst);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"JSON解析失败: {ex.Message}");
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
                return Properties.Resources.P03_Json2JList;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("D18AD8E0-1A39-42A7-9F4B-9FC5EEC2523C"); }
        }
    }
}