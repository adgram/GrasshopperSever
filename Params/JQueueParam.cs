using Grasshopper.Kernel;
using System;

namespace GrasshopperSever.Params
{
    /// <summary>
    /// 定义电池端口，这个传输JQueue数据
    /// </summary>
    public class JQueueParam : GH_Param<JQueueGoo>{
        /// <summary>
        /// </summary>
        public JQueueParam() : base("JQueue", "JQ",
            "由`JQueue = (DateTime time, Queue<JData(string name，string description，string data)>)`组成的数据，表示tcp一次消息的多个JData。",
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.JQueueParam;
    }
}
