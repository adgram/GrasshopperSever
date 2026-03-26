using System;
using System.Linq;
using Grasshopper.Kernel;
using GrasshopperSever.Params;
using System.Text.Json;

namespace GrasshopperSever.Components
{
    public class FindJdata : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ReadLjson class.
        /// </summary>
        public FindJdata()
          : base("FindJdata", "FJ",
              "通过名称查找Jdata",
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
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new LjsonParam(), "Ljson", "LJ", "需要转换的Ljson", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "N", "需要查找的键值", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Data", "D", "找到的值（基本类型或字符串）", GH_ParamAccess.item);
            pManager.AddGenericParameter("DataList", "DL", "找到的值列表（基本类型或字符串）", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            LjsonGoo jlistGoo = null;
            string name = "";
            // 获取输入参数
            if (!DA.GetData(0, ref jlistGoo)) return;
            if (!DA.GetData(1, ref name)) return;
            // 检查输入是否有效
            if (jlistGoo == null || jlistGoo.Value == null)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入的Ljson为空");
                return;
            }
            if (string.IsNullOrEmpty(name))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入的Name为空");
                return;
            }
            // 查找参数值
            var jlist = jlistGoo.Value;
            // 辅助方法：将 JsonElement 转换为基本类型或字符串
            object ElementToBasicType(JsonElement? element)
            {
                if (!element.HasValue) return null;
                var _value = element.Value;
                switch (_value.ValueKind)
                {
                    case JsonValueKind.String:
                        return _value.GetString();
                    case JsonValueKind.Number:
                        if (_value.TryGetInt32(out int intVal))
                            return intVal;
                        if (_value.TryGetInt64(out long longVal))
                            return longVal;
                        if (_value.TryGetDouble(out double doubleVal))
                            return doubleVal;
                        return _value.GetRawText();
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        return null;
                    default:
                        // 对于对象、数组等复杂类型，转为字符串
                        return _value.GetRawText();
                }
            }
            object value = ElementToBasicType(jlist.GetParameter(name));
            var elements = jlist.SearchParameter(name);
            var values = elements.Select(e => ElementToBasicType(e)).ToList();
            // 设置输出
            DA.SetData(0, value);
            DA.SetDataList(1, values);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.P17_FindJData;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("ECCCF6E5-C2BC-415D-9FFA-9DAFD7D6F9E2"); 
    }
}