using Rhino;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace GrasshopperSever.Utils
{
    /// <summary>
    /// JSON序列化接口
    /// </summary>
    public interface IJsonSerializable
    {
        /// <summary>
        /// 序列化为JSON字符串
        /// </summary>
        /// <returns>JSON字符串</returns>
        string ToJson();

        /// <summary>
        /// 从JSON字符串反序列化
        /// </summary>
        /// <param name="json">JSON字符串</param>
        void FromJson(string json);
    }

    /// <summary>
    /// 基本数据，响应的数据
    /// </summary>
    public class JData
    {
        public JData() { }
        public JData(string name, string description, string value)
        {
            Name = name;
            Description = description;
            Value = value;
        }
        public JData(JData other)
        {   
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            Name = other.Name;
            Description = other.Description;
            Value = other.Value;
        }
        /// <summary>
        /// 数据名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 数据描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 数据内容
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 返回字符串表示
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"JData[Name: {Name}, Description: {Description}, Value: {Value}]";
        }

        /// <summary>
        /// 深度克隆
        /// </summary>
        /// <returns>克隆的数据</returns>
        public JData DeepClone()
        {
            return new JData(this);
        }
    }

    /// <summary>
    /// 响应队列，用于存储要发送的响应
    /// 线程安全的队列实现，支持生产者-消费者模式
    /// </summary>
    public class JQueue : IDisposable, IJsonSerializable
    {
        private readonly DateTime _time; // 创建的时间
        private readonly Queue<JData> _queue = new Queue<JData>(); // 数据
        private readonly object _lock = new object(); // 线程锁
        private bool _disposed = false; // 跟踪对象的释放状态

        /// <summary>
        /// 队列创建时间
        /// </summary>
        public DateTime Time => _time;

        /// <summary>
        /// 无参构造函数 - 创建空队列，使用当前时间
        /// </summary>
        public JQueue()
        {
            _time = DateTime.Now;
        }

        /// <summary>
        /// 从 JData 数组初始化队列，使用当前时间
        /// </summary>
        /// <param name="items">初始数据项</param>
        public JQueue(JData[] items)
        {
            _time = DateTime.Now;
            if (items != null)
            {
                foreach (var item in items)
                {
                    _queue.Enqueue(item);
                }
            }
        }

        /// <summary>
        /// 从另一个 JQueue 克隆
        /// </summary>
        /// <param name="other">要克隆的队列</param>
        public JQueue(JQueue other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            _time = other._time;
            lock (other._lock)
            {
                foreach (var item in other._queue)
                {
                    _queue.Enqueue(item.DeepClone());
                }
            }
        }

        /// <summary>
        /// 从 JSON 字符串反序列化创建队列
        /// </summary>
        /// <param name="json">JSON字符串</param>
        public JQueue(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON字符串不能为空", nameof(json));
            }

            // 先解析时间，然后调用 FromJson() 填充队列
            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.TryGetProperty("Time", out var timeElement))
                    {
                        _time = timeElement.GetDateTime();
                    }
                    else
                    {
                        _time = DateTime.Now;
                    }
                }
            }
            catch
            {
                _time = DateTime.Now;
            }

            // 调用 FromJson() 来填充队列
            FromJson(json);
        }

        /// <summary>
        /// 入队 - 添加数据到队列末尾
        /// </summary>
        /// <param name="data">要添加的数据</param>
        public void Enqueue(JData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(JQueue));
                }
                _queue.Enqueue(data);
                Monitor.Pulse(_lock); // 通知等待的线程
            }
        }

        /// <summary>
        /// 出队 - 从队列头部移除并返回数据（阻塞直到有数据）
        /// </summary>
        /// <returns>队列头部的数据</returns>
        public JData Dequeue()
        {
            lock (_lock)
            {
                while (_queue.Count == 0 && !_disposed)
                {
                    Monitor.Wait(_lock);
                }

                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(JQueue));
                }

                return _queue.Dequeue();
            }
        }

        /// <summary>
        /// 尝试出队 - 非阻塞方式从队列头部移除并返回数据
        /// </summary>
        /// <param name="item">输出的数据</param>
        /// <returns>是否成功获取数据</returns>
        public bool TryDequeue(out JData item)
        {
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    item = _queue.Dequeue();
                    return true;
                }
                item = default;
                return false;
            }
        }

        /// <summary>
        /// 尝试出队 - 带超时的出队操作
        /// </summary>
        /// <param name="item">输出的数据</param>
        /// <param name="timeoutMilliseconds">超时时间（毫秒）</param>
        /// <returns>是否成功获取数据</returns>
        public bool TryDequeue(out JData item, int timeoutMilliseconds)
        {
            if (timeoutMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "超时时间不能为负数");
            }

            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    item = _queue.Dequeue();
                    return true;
                }

                DateTime startTime = DateTime.UtcNow;
                int remainingTime = timeoutMilliseconds;

                while (_queue.Count == 0 && remainingTime > 0 && !_disposed)
                {
                    Monitor.Wait(_lock, remainingTime);
                    remainingTime = timeoutMilliseconds - (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                }

                if (_disposed)
                {
                    item = default;
                    return false;
                }

                if (_queue.Count > 0)
                {
                    item = _queue.Dequeue();
                    return true;
                }

                item = default;
                return false;
            }
        }

        /// <summary>
        /// 尝试出队 - 带取消令牌的出队操作
        /// </summary>
        /// <param name="item">输出的数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功获取数据</returns>
        public bool TryDequeue(out JData item, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    item = _queue.Dequeue();
                    return true;
                }

                while (_queue.Count == 0 && !_disposed && !cancellationToken.IsCancellationRequested)
                {
                    Monitor.Wait(_lock, TimeSpan.FromMilliseconds(100));
                }

                if (_disposed || cancellationToken.IsCancellationRequested)
                {
                    item = default;
                    return false;
                }

                if (_queue.Count > 0)
                {
                    item = _queue.Dequeue();
                    return true;
                }

                item = default;
                return false;
            }
        }

        /// <summary>
        /// 查看队列头部数据但不移除
        /// </summary>
        /// <returns>队列头部的数据</returns>
        public JData Peek()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(JQueue));
                }
                if (_queue.Count == 0)
                {
                    throw new InvalidOperationException("队列为空");
                }
                return _queue.Peek();
            }
        }

        /// <summary>
        /// 尝试查看队列头部数据但不移除
        /// </summary>
        /// <param name="item">输出的数据</param>
        /// <returns>是否成功获取数据</returns>
        public bool TryPeek(out JData item)
        {
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    item = _queue.Peek();
                    return true;
                }
                item = default;
                return false;
            }
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(JQueue));
                }
                _queue.Clear();
                Monitor.PulseAll(_lock); // 通知所有等待的线程
            }
        }

        /// <summary>
        /// 检查队列是否包含指定数据
        /// </summary>
        /// <param name="item">要检查的数据</param>
        /// <returns>是否包含</returns>
        public bool Contains(JData item)
        {
            if (item == null)
            {
                return false;
            }

            lock (_lock)
            {
                return _queue.Contains(item);
            }
        }

        /// <summary>
        /// 将队列转换为数组
        /// </summary>
        /// <returns>队列的数组副本</returns>
        public JData[] ToArray()
        {
            lock (_lock)
            {
                return _queue.ToArray();
            }
        }

        /// <summary>
        /// 深度克隆队列
        /// </summary>
        /// <returns>包含相同数据的新队列</returns>
        public JQueue DeepClone()
        {
            lock (_lock)
            {
                var clonedQueue = new JQueue();
                foreach (var item in _queue)
                {
                    clonedQueue.Enqueue(item.DeepClone());
                }
                return clonedQueue;
            }
        }

        /// <summary>
        /// 队列中的元素数量
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }

        /// <summary>
        /// 队列是否为空
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count == 0;
                }
            }
        }

        /// <summary>
        /// 返回队列的字符串表示
        /// </summary>
        /// <returns>队列信息字符串</returns>
        public override string ToString()
        {
            lock (_lock)
            {
                return $"JQueue [Time: {_time:yyyy-MM-dd HH:mm:ss}, Count: {_queue.Count}]";
            }
        }

        /// <summary>
        /// 获取用于TCP发送的原始JSON字符串
        /// 对于已经包含JSON字符串的JData.Value，不会进行二次序列化
        /// 注意：此方法仅用于TCP发送，返回的JSON格式与ToJson()不同，不能与FromJson()混用
        /// </summary>
        /// <returns>JSON字符串</returns>
        public string ToJson()
        {
            lock (_lock)
            {
                var items = _queue.Select(jdata =>
                {
                    var value = jdata.Value;
                    object parsedValue = value;
                    // 尝试解析Value是否为JSON对象或数组
                    try
                    {
                        using (var doc = JsonDocument.Parse(value))
                        {
                            if (doc.RootElement.ValueKind == JsonValueKind.Object ||
                                doc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                // 如果是对象或数组，使用 JsonElement
                                parsedValue = doc.RootElement.Clone();
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // 不是有效的JSON，使用原始字符串
                    }

                    return new RawJDataItem
                    {
                        Name = jdata.Name,
                        Description = jdata.Description,
                        Value = parsedValue
                    };
                }).ToArray();

                var wrapper = new
                {
                    Time = _time,
                    Items = items
                };

                return JsonSerializer.Serialize(wrapper, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
        }

        /// <summary>
        /// 从JSON字符串反序列化
        /// 支持两种格式：QueueWrapper（原始）和 RawJDataItem（ToJson）
        /// </summary>
        /// <param name="json">JSON字符串</param>
        public void FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON字符串不能为空", nameof(json));
            }

            lock (_lock)
            {
                _queue.Clear();

                try
                {
                    // 尝试解析为 RawJDataItem 格式（ToJson 的输出）
                    using (var doc = JsonDocument.Parse(json))
                    {
                        var itemsElement = doc.RootElement.GetProperty("Items");
                        if (itemsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var itemElement in itemsElement.EnumerateArray())
                            {
                                var jdata = new JData
                                {
                                    Name = itemElement.GetProperty("Name").GetString(),
                                    Description = itemElement.GetProperty("Description").GetString()
                                };

                                var valueElement = itemElement.GetProperty("Value");
                                // 将 Value 转换为字符串
                                // 如果是字符串类型，使用 GetString() 去除引号
                                // 如果是对象或数组，使用 GetRawText() 保留原始 JSON
                                if (valueElement.ValueKind == JsonValueKind.String)
                                {
                                    jdata.Value = valueElement.GetString();
                                }
                                else
                                {
                                    jdata.Value = valueElement.GetRawText();
                                }

                                _queue.Enqueue(jdata);
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // 如果 RawJDataItem 格式解析失败，尝试原始的 QueueWrapper 格式
                    var wrapper = JsonSerializer.Deserialize<QueueWrapper>(json);
                    if (wrapper != null && wrapper.Items != null)
                    {
                        foreach (var item in wrapper.Items)
                        {
                            _queue.Enqueue(item);
                        }
                    }
                }

                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        /// JSON序列化包装类
        /// </summary>
        private class QueueWrapper
        {
            public DateTime Time { get; set; }
            public JData[] Items { get; set; }
        }

        /// <summary>
        /// 用于TCP发送的JData项目包装类
        /// </summary>
        private class RawJDataItem
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public object Value { get; set; }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的实际实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    lock (_lock)
                    {
                        _queue.Clear();
                        Monitor.PulseAll(_lock); // 唤醒所有等待的线程
                    }
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~JQueue()
        {
            Dispose(false);
        }

    }

	public class ComponentJQueue : JQueue
    {
		public ComponentJQueue(string componentGuid, string instanceGuid,
			      string name, string nickName, string description,
                  string category, string subCategory, string position,
				  string state, string inputs, string outputs) :base()
		{
            this.Enqueue(new JData("ComponentGuid", "组件 GUID", componentGuid));
			this.Enqueue(new JData("InstanceGuid", "实例 GUID", instanceGuid));
			this.Enqueue(new JData("ComponentName", "组件名称", name));
			this.Enqueue(new JData("NickName", "组件昵称", nickName));
			this.Enqueue(new JData("Description", "组件描述", description));
			this.Enqueue(new JData("Category", "主分类", category));
			this.Enqueue(new JData("SubCategory", "子分类", subCategory));
			this.Enqueue(new JData("Position", "位置信息", position));
			this.Enqueue(new JData("State", "状态信息", state));
			this.Enqueue(new JData("Inputs", "输入端信息", inputs));
			this.Enqueue(new JData("Outputs", "输出端信息", outputs));
		}
    }

	public class ParamJQueue : JQueue
    {
		public ParamJQueue(string paramGuid, string instanceGuid,
			      string name, string nickName, string description,
                  string category, string subCategory, string position,
				  string state, string inputs, string outputs) :base()
		{
            this.Enqueue(new JData("ParamGuid", "Param GUID", paramGuid));
			this.Enqueue(new JData("InstanceGuid", "实例 GUID", instanceGuid));
			this.Enqueue(new JData("ParamName", "Param名称", name));
			this.Enqueue(new JData("NickName", "Param昵称", nickName));
			this.Enqueue(new JData("Description", "Param描述", description));
			this.Enqueue(new JData("State", "状态信息", state));
			this.Enqueue(new JData("Inputs", "输入端信息", inputs));
			this.Enqueue(new JData("Outputs", "输出端信息", outputs));
		}
    }

	/// <summary>
	/// TCP接收器，专门负责监听端口和接收JQueue数据
	/// 设计说明：
	/// 1. 一个 TcpListener 实例只能监听一个端口
	/// 2. 可以创建多个 TcpReceiver 实例，分别监听不同端口（实现多端口服务）
	/// 3. 当前实现只监听单个端口，如需扩展可修改 Server.Start 方法接受端口列表
	/// 4. 接收器将接收到的JQueue通过事件回调传递给服务器
	/// 5. 会检查JQueue的time标签，只接收比上次更新的数据
	/// 6. 每个端口只能创建一个TcpReceiver实例
	/// </summary>
	public class TcpReceiver
    {
        private static readonly Dictionary<int, TcpReceiver> _activeReceivers = new Dictionary<int, TcpReceiver>();
        private static readonly object _lock = new object();

        private TcpListener _listener;
        private bool _runing = false;
        private int _port;
        private readonly JQueue _jqueue;
        private DateTime _lastReceivedTime = DateTime.MinValue;

        /// <summary>
        /// 接收到新JQueue时触发的事件
        /// </summary>
        public event Action<JQueue> OnJQueueReceived;

        /// <summary>
        /// 有客户端连接时触发的事件
        /// </summary>
        public event Action<TcpClient> OnClientConnected;

        /// <summary>
        /// 获取监听的端口号
        /// </summary>
        public int Port => _port;

        /// <summary>
        /// 获取命令队列
        /// </summary>
        public JQueue JQueue => _jqueue;

        /// <summary>
        /// 获取最后接收到的消息时间
        /// </summary>
        public DateTime LastReceivedTime => _lastReceivedTime;

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

        public TcpReceiver(int port)
        {
            lock (_lock)
            {
                if (_activeReceivers.ContainsKey(port))
                {
                    throw new InvalidOperationException($"端口 {port} 已被另一个TcpReceiver使用。请使用 TcpReceiver.GetReceiver({port}) 获取现有实例。");
                }
                _port = port;
                _jqueue = new JQueue();
                _activeReceivers[port] = this;
            }
        }

        /// <summary>
        /// 启动TCP监听
        /// </summary>
        public void Start()
        {
            if (_runing) return;

            _runing = true;
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            RhinoApp.WriteLine($"Tcp开始从端口接收数据 {_port}.");

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
                _activeReceivers.Remove(_port);
            }

            RhinoApp.WriteLine($"Tcp接收器停止，端口 {_port} 已释放");
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
                    RhinoApp.WriteLine("Tcp客户端已经连接");

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
                    RhinoApp.WriteLine($"Tcp接收器出错: {ex.Message}");
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
                    JQueue receivedQueue = new JQueue(json);
                    if (receivedQueue.Time > _lastReceivedTime)
                    {
                        _lastReceivedTime = receivedQueue.Time;
                        // 触发事件
                        OnJQueueReceived?.Invoke(receivedQueue);
                    }
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"TcpReceiver 错误: {ex.Message}");
            }
        }
    }



    /// <summary>
    /// 响应发送器，专门负责发送响应
    /// 设计说明：
    /// 1. ResponseSender 接收外部TcpClient用于发送数据
    /// 2. 支持接收多个JQueue进行发送
    /// 3. 会过滤掉time标签早于或等于最后发送的消息
    /// 4. 接收和发送完全解耦，只通过队列通信
    /// 5. 这种设计符合单一职责原则，使代码更清晰、更易维护
    /// </summary>
    public class ResponseSender
    {
        private readonly Queue<JQueue> _sendQueue = new Queue<JQueue>();
        private readonly TcpClient _client;
        private bool _running = false;
        private DateTime _lastSentTime = DateTime.MinValue;
        private readonly object _lock = new object();

        /// <summary>
        /// 获取最后发送的消息时间
        /// </summary>
        public DateTime LastSentTime => _lastSentTime;

        /// <summary>
        /// 获取待发送队列中的消息数量
        /// </summary>
        public int PendingCount
        {
            get
            {
                lock (_lock)
                {
                    return _sendQueue.Count;
                }
            }
        }

        public ResponseSender(TcpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
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
            ClearQueue();
            RhinoApp.WriteLine("ResponseSender已停止");
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
                    JQueue queueToSend = null;

                    // 从发送队列获取JQueue
                    lock (_lock)
                    {
                        if (_sendQueue.Count > 0)
                        {
                            queueToSend = _sendQueue.Dequeue();
                        }
                    }

                    if (queueToSend != null)
                    {
                        // 检查time标签，只发送比上次更新的消息
                        if (queueToSend.Time <= _lastSentTime)
                        {
                            RhinoApp.WriteLine($"ResponseSender: 忽略过期的JQueue (时间: {queueToSend.Time}, 最后发送: {_lastSentTime})");
                            continue;
                        }

                        // 发送JQueue
                        await SendJQueue(queueToSend);
                        _lastSentTime = queueToSend.Time;
                        RhinoApp.WriteLine($"ResponseSender: 已发送JQueue (时间: {queueToSend.Time}, 数据项: {queueToSend.Count})");
                    }
                    else
                    {
                        // 队列为空，等待100毫秒
                        await Task.Delay(100);
                    }
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"ResponseSender error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 发送JQueue到客户端
        /// </summary>
        /// <param name="queue">要发送的JQueue对象</param>
        /// <returns>异步任务</returns>
        private async Task SendJQueue(JQueue queue)
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
                RhinoApp.WriteLine($"ResponseSender发送JQueue错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 添加JQueue到发送队列
        /// </summary>
        /// <param name="queue">要发送的JQueue对象</param>
        public void EnqueueJQueue(JQueue queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            lock (_lock)
            {
                _sendQueue.Enqueue(queue);
                RhinoApp.WriteLine($"ResponseSender: JQueue已加入发送队列 (时间: {queue.Time}, 待发送: {_sendQueue.Count})");
            }
        }

        /// <summary>
        /// 批量添加JQueue到发送队列
        /// </summary>
        /// <param name="queues">要发送的JQueue数组</param>
        public void EnqueueJQueueRange(JQueue[] queues)
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
                        _sendQueue.Enqueue(queue);
                    }
                }
                RhinoApp.WriteLine($"ResponseSender: {queues.Length}个JQueue已加入发送队列，待发送: {_sendQueue.Count}");
            }
        }

        /// <summary>
        /// 清空发送队列
        /// </summary>
        public void ClearQueue()
        {
            lock (_lock)
            {
                int count = _sendQueue.Count;
                _sendQueue.Clear();
                RhinoApp.WriteLine($"ResponseSender: 已清空发送队列，移除 {count} 个JQueue");
            }
        }
    }
}
