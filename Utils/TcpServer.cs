using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace GrasshopperSever.Utils
{
	/// <summary>
	/// TCP接收器，专门负责监听端口和接收Ljson数据
	/// 设计说明：
	/// 1. 一个 TcpListener 实例只能监听一个端口
	/// 2. 可以创建多个 TcpReceiver 实例，分别监听不同端口（实现多端口服务）
	/// 3. 当前实现只监听单个端口，如需扩展可修改 Server.Start 方法接受端口列表
	/// 4. 接收器将接收到的Ljson通过事件回调传递给服务器
	/// 5. 会检查Ljson的time标签，只接收比上次更新的数据
	/// 6. 每个端口只能创建一个TcpReceiver实例
	/// </summary>
	public class TcpReceiver
    {
        private static readonly Dictionary<int, TcpReceiver> _activeReceivers = new Dictionary<int, TcpReceiver>();
        private static readonly object _lock = new object();

        private TcpListener _listener;
        private bool _runing = false;

        /// <summary>
        /// 接收到新Ljson时触发的事件
        /// </summary>
        public event Action<Ljson> OnLjsonReceived;

        /// <summary>
        /// 有客户端连接时触发的事件
        /// </summary>
        public event Action<TcpClient> OnClientConnected;

        /// <summary>
        /// 日志消息事件
        /// </summary>
        public event Action<string> OnLog;

        /// <summary>
        /// 获取监听的端口号
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// 获取命令队列
        /// </summary>
        public Ljson Jlist { get; private set; }

        /// <summary>
        /// 获取最后接收到的消息时间
        /// </summary>
        public DateTime LastReceivedTime { get; private set; }

        public TcpReceiver(int port)
        {
            lock (_lock)
            {
                if (_activeReceivers.ContainsKey(port))
                {
                    throw new InvalidOperationException($"端口 {port} 已被另一个TcpReceiver使用。请使用 TcpReceiver.GetReceiver({port}) 获取现有实例。");
                }
                Port = port;
                Jlist = new Ljson();
                LastReceivedTime = DateTime.MinValue;
                _activeReceivers[port] = this;
            }
        }


        /// <summary>
        /// 获取指定端口的TcpReceiver实例（如果存在）
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>TcpReceiver实例，如果不存在则返回null</returns>
        public static TcpReceiver GetReceiver(int port)
        {
            lock (_lock)
            {
                _activeReceivers.TryGetValue(port, out var receiver);
                return receiver;
            }
        }

        /// <summary>
        /// 检查指定端口是否已被使用
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>是否已被使用</returns>
        public static bool IsPortInUse(int port)
        {
            lock (_lock)
            {
                return _activeReceivers.ContainsKey(port);
            }
        }

        /// <summary>
        /// 触发日志事件
        /// </summary>
        private void Log(string message)
        {
            OnLog?.Invoke(message);
        }

        /// <summary>
        /// 启动TCP监听
        /// </summary>
        public void Start()
        {
            if (_runing) return;

            _runing = true;
            _listener = new TcpListener(IPAddress.Loopback, Port);
            _listener.Start();
            Log($"Tcp开始从端口接收数据 {Port}.");

            Task.Run(ListenerLoop);
        }

        /// <summary>
        /// 停止TCP监听
        /// </summary>
        public void Stop()
        {
            if (!_runing) return;

            _runing = false;
            _listener.Stop();

            // 从活动接收器字典中移除自己
            lock (_lock)
            {
                _activeReceivers.Remove(Port);
            }

            Log($"Tcp接收器停止，端口 {Port} 已释放");
        }

        /// <summary>
        /// 监听循环，持续接收客户端连接
        /// </summary>
        private async Task ListenerLoop()
        {
            try
            {
                while (_runing)
                {
                    // 等待客户端连接
                    var client = await _listener.AcceptTcpClientAsync();
                    Log("Tcp客户端已经连接");

                    // 触发客户端连接事件
                    OnClientConnected?.Invoke(client);

                    // 处理客户端连接
                    _ = Task.Run(() => HandleClient(client));
                }
            }
            catch (Exception ex)
            {
                if (_runing)
                {
                    Log($"Tcp接收器出错: {ex.Message}");
                    _runing = false;
                }
            }
        }

        /// <summary>
        /// 处理客户端连接
        /// </summary>
        /// <param name="client">TCP客户端</param>
        private async Task HandleClient(TcpClient client)
        {
            try
            {
                var stream = client.GetStream();
                var reader = new StreamReader(stream, Encoding.UTF8);

                // 读取数据
                string json = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(json))
                {
                    Ljson receivedList = new Ljson(json);
                    if (receivedList.Time > LastReceivedTime)
                    {
                        LastReceivedTime = receivedList.Time;
                        // 触发事件
                        OnLjsonReceived?.Invoke(receivedList);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"TcpReceiver 错误: {ex.Message}");
            }
        }
    }



    /// <summary>
    /// 响应发送器，专门负责发送响应
    /// 设计说明：
    /// 1. ResponseSender 接收外部TcpClient用于发送数据
    /// 2. 支持接收多个Ljson进行发送
    /// 3. 会过滤掉time标签早于或等于最后发送的消息
    /// 4. 接收和发送完全解耦，只通过队列通信
    /// 5. 这种设计符合单一职责原则，使代码更清晰、更易维护
    /// </summary>
    public class ResponseSender
    {
        private readonly Queue<Ljson> _sendList = new Queue<Ljson>();
        private readonly TcpClient _client;
        private bool _running = false;
        private readonly object _lock = new object();

        /// <summary>
        /// 日志消息事件
        /// </summary>
        public event Action<string> OnLog;

        /// <summary>
        /// 获取最后发送的消息时间
        /// </summary>
        public DateTime LastSentTime { get; private set; }

        /// <summary>
        /// 获取待发送队列中的消息数量
        /// </summary>
        public int PendingCount
        {
            get
            {
                lock (_lock)
                {
                    return _sendList.Count;
                }
            }
        }

        public ResponseSender(TcpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            LastSentTime = DateTime.MinValue;
        }

        /// <summary>
        /// 触发日志事件
        /// </summary>
        private void Log(string message)
        {
            OnLog?.Invoke(message);
        }

        /// <summary>
        /// 启动响应发送器
        /// </summary>
        public void Start()
        {
            if (_running) return;
            _running = true;
            Task.Run(SendResponses);
        }

        /// <summary>
        /// 停止响应发送器
        /// </summary>
        public void Stop()
        {
            if (!_running) return;

            _running = false;
            ClearList();
            Log("ResponseSender已停止");
        }

        /// <summary>
        /// 发送响应循环
        /// </summary>
        private async Task SendResponses()
        {
            while (_running)
            {
                try
                {
                    Ljson queueToSend = null;

                    // 从发送队列获取Ljson
                    lock (_lock)
                    {
                        if (_sendList.Count > 0)
                        {
                            queueToSend = _sendList.Dequeue();
                        }
                    }

                    if (queueToSend != null)
                    {
                        // 检查time标签，只发送比上次更新的消息
                        if (queueToSend.Time <= LastSentTime)
                        {
                            Log($"ResponseSender: 忽略过期的Ljson (时间: {queueToSend.Time}, 最后发送: {LastSentTime})");
                            continue;
                        }

                        // 发送Ljson
                        await SendLjson(queueToSend);
                        LastSentTime = queueToSend.Time;
                        Log($"ResponseSender: 已发送Ljson (时间: {queueToSend.Time}, 数据项: {queueToSend.Name})");
                    }
                    else
                    {
                        // 队列为空，等待100毫秒
                        await Task.Delay(100);
                    }
                }
                catch (Exception ex)
                {
                    Log($"ResponseSender error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 发送Ljson到客户端
        /// </summary>
        /// <param name="queue">要发送的Ljson对象</param>
        /// <returns>异步任务</returns>
        private async Task SendLjson(Ljson queue)
        {
            try
            {
                if (_client == null || !_client.Connected)
                {
                    throw new InvalidOperationException("TcpClient未连接");
                }
                var stream = _client.GetStream();
                var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                string queueJson = queue.ToJson();
                await writer.WriteLineAsync(queueJson);
            }
            catch (Exception ex)
            {
                Log($"ResponseSender发送Ljson错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 添加Ljson到发送队列
        /// </summary>
        /// <param name="queue">要发送的Ljson对象</param>
        public void EnqueueLjson(Ljson queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            lock (_lock)
            {
                _sendList.Enqueue(queue);
                Log($"ResponseSender: Ljson已加入发送队列 (时间: {queue.Time}, 待发送: {_sendList.Count})");
            }
        }

        /// <summary>
        /// 批量添加Ljson到发送队列
        /// </summary>
        /// <param name="queues">要发送的Ljson数组</param>
        public void EnqueueLjsonRange(Ljson[] queues)
        {
            if (queues == null)
            {
                throw new ArgumentNullException(nameof(queues));
            }

            lock (_lock)
            {
                foreach (var queue in queues)
                {
                    if (queue != null)
                    {
                        _sendList.Enqueue(queue);
                    }
                }
                Log($"ResponseSender: {queues.Length}个Ljson已加入发送队列，待发送: {_sendList.Count}");
            }
        }

        /// <summary>
        /// 清空发送队列
        /// </summary>
        public void ClearList()
        {
            lock (_lock)
            {
                int count = _sendList.Count;
                _sendList.Clear();
                Log($"ResponseSender: 已清空发送队列，移除 {count} 个Ljson");
            }
        }
    }
}