using System;
using Grasshopper.Kernel;
using GrasshopperSever.Params;
using GrasshopperSever.Utils;
using System.Net.Sockets;

namespace GrasshopperSever.Components
{
    public class GHSender : GH_Component
    {
        private ResponseSender _sender;
        // 用于记录当前绑定的客户端，判断是否换了人
        private TcpClient _currentClient;

        /// <summary>
        /// 用于使用Ljson发送数据到客户端
        /// </summary>
        public GHSender()
          : base("GHSender", "Sender",
              "通过TcpClient发送Ljson数据到客户端。ResponseSender会自动过滤time标签未更新的数据。",
                "Maths", "Sever")
        {
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new TcpClientParam(), "Client", "CL", "从GHServer接收的TcpClient连接", GH_ParamAccess.item);
            pManager.AddParameter(new LjsonParam(), "LJson", "LJ", "要发送的Ljson数据", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "ST", "发送状态", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            TcpClientGoo clientGoo = null;
            LjsonGoo jsonGoo = null;

            // 获取输入参数
            if (!DA.GetData(0, ref clientGoo)) return;
            if (!DA.GetData(1, ref jsonGoo)) return;

            // 检查输入是否有效
            if (clientGoo == null || clientGoo.Value == null)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入的Client为空");
                DA.SetData(0, "等待客户端连接...");
                return;
            }

            if (jsonGoo == null || jsonGoo.Value == null)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入的Json队列为空");
                DA.SetData(0, "无待发送数据");
                return;
            }

            Ljson queue = jsonGoo.Value;
            TcpClient client = clientGoo.Value;

            // 创建或更新发送器
            try
            {
                // 如果发送器不存在，或者传入了全新的客户端连接，则重建发送器
                if (_sender == null || _currentClient != client)
                {
                    // 如果旧的发送器还在，先让它安全停下
                    if (_sender != null)
                    {
                        _sender.Stop();
                    }

                    _sender = new ResponseSender(client);
                    _sender.Start();
                    _currentClient = client; // 记录当前客户端
                }

                // 将Ljson传递给ResponseSender，由它自己判断time标签
                _sender.EnqueueLjson(queue);

                DA.SetData(0, $"已发送数据 (时间: {queue.Time}, 数据项: {queue.Name})");
            }
            catch (Exception ex)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"发送失败: {ex.Message}");
                DA.SetData(0, $"发送失败: {ex.Message}");
            }
        }

        // 只有当用户在画布上把这个组件删掉时，才清理后台线程
        public override void RemovedFromDocument(GH_Document document)
        {
            if (_sender != null)
            {
                _sender.Stop();
                _sender = null;
            }
            _currentClient = null;
            base.RemovedFromDocument(document);
        }


        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.P07_GHSender;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("00C7E124-8BDE-437A-9387-4AC4ABBC793A");
    }
}