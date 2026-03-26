using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using System;
using System.Net.Sockets;

namespace GrasshopperSever.Components
{
    public class GHServer : GH_Component
    {
        private TcpReceiver _receiver;
        private volatile TcpClient _client;
        private int _currentPort = -1;
        private ResponseSender _sender;
        private string _log = "";
        private string _output_data = null;
        private Ljson _pendingLjson = null;  // 保存待处理的 Ljson 数据

        /// <summary>
        /// 从端口接收Json数据并进行处理，默认接收到会立即响应
        /// 功能：监听端口，创建TcpClient连接，接收Ljson数据，通过Actuator执行命令并自动发送响应
        /// （GHServer 是 GHReceiver 和 GHSender 的合并组件）
        /// </summary>
        public GHServer()
          : base("GHServer", "Server",
                "监听端口，接收Ljson数据，通过Actuator执行命令并自动发送响应。",
                "Maths", "Sever")
        {
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

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
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
            pManager.AddTextParameter("Status", "ST", "回复状态", GH_ParamAccess.item);
            pManager.AddGenericParameter("OutPut", "O", "显示输出数据", GH_ParamAccess.item);
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
                DA.SetData(0, $"端口 {port} 无效，请使用 1024-49151 之间的端口");
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
                if (_client != null)
                {
                    _client.Close();
                    _client = null;
                }

                // 停止响应发送器
                if (_sender != null)
                {
                    _sender.Stop();
                    _sender.OnLog -= OnLogHandler;
                    _sender = null;
                }
            }

            // 设置状态输出
            if (!enabled)
            {
                DA.SetData(0, "服务器已停止");
            }
            else if (_client == null)
            {
                DA.SetData(0, "等待客户端连接...");
            }
            else
            {
                DA.SetData(0, _log);
            }
            DA.SetData(1, _output_data);
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
            _sender.EnqueueLjson(Ljson.CreateOKLjson("客户端已连接"));
            this.OnPingDocument()?.ScheduleSolution(5, doc => {
                    this.ExpireSolution(false); // 仅标记过期，由 Schedule 触发重算
                });
        }

        /// <summary>
        /// 处理接收到的Ljson数据（在后台线程中调用）
        /// </summary>
        private void OnLjsonReceivedHandler(Ljson lst)
        {
            if (lst == null) return;

            // 保存接收到的 Ljson
            _pendingLjson = lst;

            // 更新最新数据
            AddLog($"GHServer: 接收到新数据 (时间: {lst.Time}, 数据项: {lst.Name})");
            _sender.EnqueueLjson(Ljson.CreateOKLjson("数据接收成功"));

            // 使用 Actuator 执行 Ljson 中的命令，获取响应
            if (_sender != null)
            {
                try
                {
                    // 在后台线程中暂存数据，然后在主线程中执行命令
                    // 这对于 Rhino 命令非常重要，因为 RhinoApp.RunScript 必须在主线程中执行
                    Rhino.RhinoApp.Idle += ExecuteCommandOnMainThread;
                    System.Windows.Forms.Application.DoEvents(); // 触发 Idle 事件
                }
                catch (Exception ex)
                {
                    AddLog($"GHServer: 处理响应失败: {ex.Message}");
                }
            }

            // 触发重新计算以更新状态
            this.OnPingDocument()?.ScheduleSolution(5, (doc) => {
                    this.ExpireSolution(false); // 仅标记过期，由 Schedule 触发重算
                });
        }

        /// <summary>
        /// 在主线程中执行命令
        /// </summary>
        private void ExecuteCommandOnMainThread(object sender, EventArgs e)
        {
            Rhino.RhinoApp.Idle -= ExecuteCommandOnMainThread;

            if (_sender == null || _pendingLjson == null) return;

            try
            {
                // 获取待处理的 Ljson 数据
                Ljson lst = _pendingLjson;
                string outputData = null;

                // 执行命令并获取响应
                Ljson responseList = GHActuator.DoCommand(lst, ref outputData);
                _output_data = outputData;

                // 将响应加入发送队列
                _sender.EnqueueLjson(responseList);
                AddLog($"GHServer: 已添加响应到发送队列 (时间: {responseList.Time}, 数据项: {responseList.Name})");

                // 清空待处理数据
                _pendingLjson = null;
            }
            catch (Exception ex)
            {
                AddLog($"GHServer: 主线程执行命令失败: {ex.Message}");
                _pendingLjson = null;
            }
        }
        
        public override void RemovedFromDocument(GH_Document document)
        {
            if (_receiver != null)
            {
                _receiver.OnLog -= OnLogHandler;
                _receiver.Stop();
            }
            if (_client != null)
            {
                _client.Close();
            }
            if (_sender != null)
            {
                _sender.OnLog -= OnLogHandler;
                _sender.Stop();
                _sender = null;
            }
            AddLog("GHServer: 组件已从文档中移除");
            base.RemovedFromDocument(document);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.P08_GHServer;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("26008bd8-557d-4af7-a4a4-f21b1d426179");

    }
}