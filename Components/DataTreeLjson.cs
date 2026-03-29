using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using GrasshopperSever.Params;
using GrasshopperSever.Utils;

namespace GrasshopperSever.Components
{
    /// <summary>
    /// 将 Name, Info, String Tree 转换为 Ljson
    /// </summary>
    public class DataTreeLjson : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DataTreeLjson class.
        /// </summary>
        public DataTreeLjson()
          : base("DataTreeLjson", "ST2Q",
              "将 Name, Info 和 Data Tree 构造为 Ljson。每个 branch 只能包含 1 个或 2 个元素：1 个元素转为 list，2 个元素转为 dict。",
              "Maths", "Sever")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.last;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Ljson 的名称", GH_ParamAccess.item);
            pManager.AddTextParameter("Info", "I", "Ljson 的说明", GH_ParamAccess.item);
            pManager.AddGenericParameter("Data Tree", "DT", "Data Tree 数据。每个 branch 只能包含 1 个或 2 个元素：1 个元素转为 list，2 个元素转为 dict (key-value)。基本类型直接存储，非基本类型转为字符串", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "生成的Ljson", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "";
            string info = "";
            var dataTree = new GH_Structure<IGH_Goo>();

            if (!DA.GetData(0, ref name)) return;
            if (!DA.GetData(1, ref info)) return;
            if (!DA.GetDataTree(2, out dataTree)) return;

            if(dataTree.PathCount == 1 && dataTree[0].Count == 1)
            {
                // 直接构造 Ljson
                var j = JsonSerializer.Deserialize<JsonElement>(ConvertGooToBasicType(dataTree.First()).ToString());
                Ljson ljson = new Ljson(name, info, j);
                DA.SetData(0, ljson);
                return;
            }

            try
            {
                // 将 Data Tree 转换为 Json 数组
                var jsonArray = new List<object>();
                var jsonDict = new Dictionary<string, object>();

                foreach (GH_Path path in dataTree.Paths)
                {
                    var branchList = dataTree[path];

                    // 检查 branch 的元素数量
                    if (branchList.Count == 1)
                    {
                        // 1 个元素，直接存入
                        jsonArray.Add(ConvertGooToBasicType(branchList[0]));
                    }
                    else
                    {
                        // 2 个元素，转为 dict (key-value)
                        jsonDict.Add(ConvertGooToBasicType(branchList[0]).ToString(),
                                ConvertGooToBasicType(branchList[1]));
                    }
                }
                // 序列化为 JsonElement
                var jsonElement = new JsonElement();
                if (jsonDict.Count > 0)
                {
                    jsonElement = JsonSerializer.SerializeToElement(jsonDict);
                }
                else if (jsonArray.Count > 0)
                {
                    jsonElement = JsonSerializer.SerializeToElement(jsonArray);
                }
                // 直接构造 Ljson
                Ljson ljson = new Ljson(name, info, jsonElement);

                DA.SetData(0, ljson);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"构造 Ljson 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 将 IGH_Goo 转换为基本类型
        /// 如果是基本类型直接返回，否则转为字符串
        /// </summary>
        private static object ConvertGooToBasicType(IGH_Goo goo)
        {
            if (goo == null)
                return "";

            // 尝试提取基本类型值
            if (goo is GH_String ghString)
                return ghString.Value ?? "";
            if (goo is GH_Boolean ghBoolean)
                return ghBoolean.Value;
            if (goo is GH_Integer ghInteger)
                return ghInteger.Value;
            if (goo is GH_Number ghNumber)
                return ghNumber.Value;
            if (goo is LjsonGoo ljson)
                return ljson.Value;

            // 对于其他类型，尝试转为字符串
            try
            {
                return goo.ToString();
            }
            catch
            {
                return "";
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
                return Properties.Resources.P05_DataTreeLjson;
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