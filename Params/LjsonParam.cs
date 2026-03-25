using Grasshopper.Kernel;
using System;

namespace GrasshopperSever.Params
{
    /// <summary>
    /// 定义电池端口，这个传输Ljson数据
    /// </summary>
    public class LjsonParam : GH_Param<LjsonGoo>{
        /// <summary>
        /// </summary>
        public LjsonParam() : base("Ljson", "LJ",
            "由`(DateTime time, string name，string description，JsonElement value)`组成的数据，表示tcp一次消息。",
            "Maths", "Sever", GH_ParamAccess.item)
        {
        }
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.last;
            }
        }

        public override Guid ComponentGuid
        {
            get
            {
                return new Guid("74F00FF0-9A60-4516-910C-5466A609D874");
            }
        }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.P01_LjsonParam;
    }
}
