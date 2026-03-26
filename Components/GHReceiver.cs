using Grasshopper.Kernel;
using GrasshopperSever.Params;
using GrasshopperSever.Utils;
using System;
using System.Net.Sockets;

namespace GrasshopperSever.Components
{
    public class GHReceiver : GH_Component
    {
        private ResponseSender _sender;
        private TcpReceiver _receiver;
        private volatile Ljson _latestData;
        private volatile TcpClient _client;
        private int _currentPort = -1;
        private string _log = "";

        /// <summary>
        /// 从端口接收Json数据并进行处理，接收到不会立即响应
        /// 功能：监听端口，创建TcpClient连接，接收Ljson数据
        /// </summary>
        public GHReceiver()
          : base("GHReceiver", "Receiver",
                "监听端口，创建TcpClient连接并接收Ljson数据。TcpClient可传递给 GHSender用于发送响应。",
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
        /// 添加日志信息，集中管理输出
        /// </summary>
        private void AddLog(string message)
        {
            _log += message + Environment.NewLine;
        }

        /// <summary>
        /// 处理来自 TcpReceiver 和 ResponseSender 的日志消息
        /// </summary>
        private void OnLogHandler(string message)
        {
            AddLog(message);
            this.OnPingDocument()?.ScheduleSolution(5, doc => {
                this.ExpireSolution(false);
            });
        }

        /// <summary>
        /// 从端口接收Json数据，进行处理
        /// Enabled: 是否启用服务器
        /// Port: 监听的端口
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Enabled", "E", "是否启用服务器监听", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Port", "P", "监听的端口号（1024-49151）", GH_ParamAccess.item, 6879);
        }

        /// <summary>
        /// 输出TcpClient连接和接收到的Ljson数据
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new TcpClientParam(), "Client", "CL", "客户端连接，可传递给GHActuator用于发送响应", GH_ParamAccess.item);
            pManager.AddParameter(new LjsonParam(), "Json", "JS", "接收到的Ljson数据", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "ST", "状态", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool enabled = false;
            int port = 0;

            // 获取输入参数
            if (!DA.GetData(0, ref enabled)) return;
            if (!DA.GetData(1, ref port)) return;

            // 验证端口范围
            if (port < 1024 || port > 49151)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"端口 {port} 无效，请使用 1024-49151 之间的端口");
                if (_receiver != null)
                {
                    _receiver.Stop();
                    _receiver = null;
                }
                DA.SetData(0, new TcpClientGoo());
                DA.SetData(1, new LjsonGoo());
                return;
            }

            // 启动或停止服务器
            if (enabled)
            {
                // 如果端口改变或接收器不存在，则创建新的接收器
                if (_receiver == null || _currentPort != port)
                {
                    // 停止旧的接收器
                    if (_receiver != null)
                    {
                        _receiver.Stop();
                        _receiver.OnLjsonReceived -= OnLjsonReceivedHandler;
                        _receiver.OnClientConnected -= OnClientConnectedHandler;
                        _receiver.OnLog -= OnLogHandler;
                    }
                    if(_sender != null)
                    {
                        _sender.Stop();
                        _sender.OnLog -= OnLogHandler;
                        _sender = null;
                    }

                    // 创建新的接收器（TcpReceiver内部会构造Ljson）
                    _receiver = new TcpReceiver(port);
                    _receiver.OnLjsonReceived += OnLjsonReceivedHandler;
                    _receiver.OnClientConnected += OnClientConnectedHandler;
                    _receiver.OnLog += OnLogHandler;
                    _receiver.Start();
                    _currentPort = port;
                    AddLog($"启动Tcp任务，端口 {port}.");
                }
            }
            else
            {
                // 停止服务器
                if (_receiver != null)
                {
                    _receiver.Stop();
                    _receiver.OnLjsonReceived -= OnLjsonReceivedHandler;
                    _receiver.OnClientConnected -= OnClientConnectedHandler;
                    _receiver.OnLog -= OnLogHandler;
                    _receiver = null;
                    AddLog("Tcp服务器已停止");
                }
                if (_sender != null)
                {
                    _sender.Stop();
                    _sender.OnLog -= OnLogHandler;
                    _sender = null;
                }
                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }
                _latestData = null;
            }
            // 设置输出
            DA.SetData(0, new TcpClientGoo(_client));
            DA.SetData(1, new LjsonGoo(_latestData));
            DA.SetData(2, _log);
        }

        /// <summary>
        /// 处理客户端连接事件（在后台线程中调用）
        /// </summary>
        private void OnClientConnectedHandler(TcpClient client)
        {
            if (client == null) return;

            // 保存客户端连接
            _client = client;

            // 初始化响应发送器
            if (_sender != null)
            {
                _sender.Stop();
                _sender = null;
            }
            _sender = new ResponseSender(_client);
            _sender.OnLog += OnLogHandler;
            _sender.Start();

            AddLog($"GHServer: 客户端已连接");
            // 将响应加入发送队列
            _sender.EnqueueLjson(Ljson.CreateOKLjson("客户端已连接"));
            this.OnPingDocument()?.ScheduleSolution(5, doc => {
                this.ExpireSolution(false); // 仅标记过期，由 Schedule 触发重算
            });
        }

        /// <summary>
        /// 处理接收到的Ljson数据（在后台线程中调用）
        /// </summary>
        private void OnLjsonReceivedHandler(Ljson queue)
        {
            if (queue == null) return;

            // 更新最新数据
            _latestData = queue;
            AddLog($"GHServer: 接收到新数据 (时间: {queue.Time}, 数据项: {queue.Name})");
            // 将响应加入发送队列
            _sender.EnqueueLjson(Ljson.CreateOKLjson("数据接收成功"));
            this.OnPingDocument()?.ScheduleSolution(5, (doc) => {
                this.ExpireSolution(false); // 仅标记过期，由 Schedule 触发重算
            });
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if (_receiver != null)
            {
                _receiver.OnLog -= OnLogHandler;
                _receiver.Stop();
            }
            if (_sender != null)
            {
                _sender.OnLog -= OnLogHandler;
                _sender.Stop();
                _sender = null;
            }
            if (_client != null)
            {
                _client.Close();
            }
            AddLog("GHReceiver: 组件已从文档中移除");
            base.RemovedFromDocument(document);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.P06_GHReceiver;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("7F67F8C9-C557-47F5-AA9F-4D851C2A7899");
    }
}