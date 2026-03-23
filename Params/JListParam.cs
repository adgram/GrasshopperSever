using Grasshopper.Kernel;
using System;

namespace GrasshopperSever.Params
{
    /// <summary>
    /// 定义电池端口，这个传输JList数据
    /// </summary>
    public class JListParam : GH_Param<JListGoo>{
        /// <summary>
        /// </summary>
        public JListParam() : base("JList", "JQ",
            "由`JList = (DateTime time, List<JData(string name，string description，string data)>)`组成的数据，表示tcp一次消息的多个JData。",
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.P01_JListParam;
    }
}
