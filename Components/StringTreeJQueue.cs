using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperSever.Params;
using GrasshopperSever.Utils;

namespace GrasshopperSever.Components
{
    /// <summary>
    /// 将 String Tree 转换为 JQueue
    /// </summary>
    public class StringTreeJQueue : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the StringTreeJQueue class.
        /// </summary>
        public StringTreeJQueue()
          : base("StringTreeJQueue", "ST2Q",
              "将string tree转换为JQueue。每个branch只取前三项，非string格式转为string，项目不足则使用空值补齐。",
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
            pManager.AddTextParameter("String Tree", "ST", "将string tree转换为JQueue", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new JQueueParam(), "JQueue", "JQ", "生成的JQueue", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_String> stringTree = new GH_Structure<GH_String>();
            if (!DA.GetDataTree(0, out stringTree))
            {
                return;
            }

            try
            {
                // 创建 JQueue
                JQueue jqueue = new JQueue();

                // 遍历每个 branch
                foreach (GH_Path path in stringTree.Paths)
                {
                    var branchList = stringTree[path];

                    // 将 branch 中的所有项转为字符串
                    List<string> stringValues = branchList.Select(item => item.Value ?? "").ToList();

                    // 取前三项作为 JData 的三个属性，不足则用空字符串补齐
                    string name = stringValues.Count > 0 ? stringValues[0] : "";
                    string description = stringValues.Count > 1 ? stringValues[1] : "";
                    string value = stringValues.Count > 2 ? stringValues[2] : "";

                    // 创建 JData
                    JData jdata = new JData(name, description, value);

                    // 添加到 JQueue
                    jqueue.Enqueue(jdata);
                }

                // 如果没有数据，创建一个空数据的 JQueue
                if (jqueue.Count == 0)
                {
                    JData emptyJData = new JData("", "", "");
                    jqueue.Enqueue(emptyJData);
                }

                DA.SetData(0, jqueue);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"转换失败: {ex.Message}");
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
                return Properties.Resources.StringTreeJQueue;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("76BBFA34-2573-4A96-A20A-2004A2530C79"); }
        }
    }
}