using Grasshopper.Kernel.Types;
using System.Net.Sockets;

namespace GrasshopperSever.Params
{
    /// <summary>
    /// 定义电池端口，这个传输Port数据
    /// </summary>
    public class TcpClientGoo : GH_Goo<TcpClient>
    {
        /// <summary>
        /// </summary>
        public override bool IsValid
        {
            get
            {
                return Value != null;
            }
        }

        public override string TypeName
        {
            get
            {
                return "TcpClient";
            }
        }

        public override string TypeDescription
        {
            get
            {
                return "一个`System.Net.Sockets.TcpClient`连接，用于接收和传输数据。";
            }
        }

        public TcpClientGoo()
        {
            Value = null;
        }

        public TcpClientGoo(TcpClient client)
        {
            Value = client;
        }

        public override IGH_Goo Duplicate()
        {
            return new TcpClientGoo(Value);
        }

        public override string ToString()
        {
            return Value != null ? "TcpClient (Connected)" : "TcpClient (Null)";
        }
    }
}
