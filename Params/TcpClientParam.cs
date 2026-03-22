using Grasshopper.Kernel;
using System;

namespace GrasshopperSever.Params
{
    /// <summary>
    /// 定义电池端口，这个传输Client
    /// </summary>
    public class TcpClientParam : GH_Param<TcpClientGoo>{
        /// <summary>
        /// </summary>
        public TcpClientParam() : base("Client", "CL",
            "一个`System.Net.Sockets.TcpClient`连接，用于接收和传输数据。该对象由GHServer根据端口唯一创建。",
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
                return new Guid("37F9476B-5FCD-42E2-8927-F9883B1B688D");
            }
        }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.TcpClientParam;
    }
}
