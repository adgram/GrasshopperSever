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
            Name = name;                // 数据名称
            Description = description;  // 数据描述
            Value = value;              // 数据值
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
    /// 响应数据集合，用于存储要发送的响应数据
    /// 线程安全的 List 实现
    /// </summary>
    public class JList : IDisposable, IJsonSerializable
    {
        private readonly DateTime _time; // 创建的时间
        private readonly List<JData> _items; // 数据列表
        private readonly object _lock = new object(); // 线程锁
        private bool _disposed = false; // 跟踪对象的释放状态

        /// <summary>
        /// 队列创建时间
        /// </summary>
        public DateTime Time => _time;

        /// <summary>
        /// 无参构造函数 - 创建空列表，使用当前时间
        /// </summary>
        public JList()
        {
            _time = DateTime.Now;
            _items = new List<JData>();
        }

        /// <summary>
        /// 从 JData 数组初始化列表，使用当前时间
        /// </summary>
        /// <param name="items">初始数据项</param>
        public JList(JData[] items)
        {
            _time = DateTime.Now;           // 每个数据创建时自动生成，用于检查数据是否被使用过
            _items = new List<JData>();
            if (items != null)
            {
                _items.AddRange(items);
            }
        }

        /// <summary>
        /// 从另一个 JList 克隆
        /// </summary>
        /// <param name="other">要克隆的列表</param>
        public JList(JList other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            _time = other._time;
            lock (other._lock)
            {
                _items = new List<JData>();
                foreach (var item in other._items)
                {
                    _items.Add(item.DeepClone());
                }
            }
        }

        /// <summary>
        /// 从 JSON 字符串反序列化创建队列
        /// </summary>
        /// <param name="json">JSON字符串</param>
        public JList(string json)
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
        /// 添加数据到列表末尾
        /// </summary>
        /// <param name="data">要添加的数据</param>
        public void Add(JData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(JList));
                }
                _items.Add(data);
            }
        }

        /// <summary>
        /// 清空列表
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(JList));
                }
                _items.Clear();
            }
        }

        /// <summary>
        /// 检查列表是否包含指定数据
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
                return _items.Contains(item);
            }
        }

        /// <summary>
        /// 将列表转换为数组
        /// </summary>
        /// <returns>列表的数组副本</returns>
        public JData[] ToArray()
        {
            lock (_lock)
            {
                return _items.ToArray();
            }
        }

        /// <summary>
        /// 深度克隆列表
        /// </summary>
        /// <returns>包含相同数据的新列表</returns>
        public JList DeepClone()
        {
            lock (_lock)
            {
                var clonedList = new JList();
                foreach (var item in _items)
                {
                    clonedList.Add(item.DeepClone());
                }
                return clonedList;
            }
        }

        /// <summary>
        /// 列表中的元素数量
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _items.Count;
                }
            }
        }

        /// <summary>
        /// 列表是否为空
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                lock (_lock)
                {
                    return _items.Count == 0;
                }
            }
        }

        /// <summary>
        /// 返回列表的字符串表示
        /// </summary>
        /// <returns>列表信息字符串</returns>
        public override string ToString()
        {
            lock (_lock)
            {
                return $"JList [Time: {_time:yyyy-MM-dd HH:mm:ss}, Count: {_items.Count}]";
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
                var items = _items.Select(jdata =>
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
        /// 支持两种格式：ListWrapper（原始）和 RawJDataItem（ToJson）
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
                _items.Clear();

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

                                _items.Add(jdata);
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // 如果 RawJDataItem 格式解析失败，尝试原始的 ListWrapper 格式
                    var wrapper = JsonSerializer.Deserialize<ListWrapper>(json);
                    if (wrapper != null && wrapper.Items != null)
                    {
                        foreach (var item in wrapper.Items)
                        {
                            _items.Add(item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// JSON序列化包装类
        /// </summary>
        private class ListWrapper
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
                        _items.Clear();
                    }
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~JList()
        {
            Dispose(false);
        }

        /// <summary>
        /// 从JList中获取指定参数的值
        /// </summary>
        /// <param name="data">输入JList</param>
        /// <param name="paramName">参数名称</param>
        /// <returns>参数值，如果不存在则返回null</returns>
        public string GetParameter(string paramName)
        {
            lock (_lock)
            {
                return _items.FirstOrDefault(item =>
                    item.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase))?.Value;
            }
        }
        public string[] SearchParameter(string paramName)
        {
            lock (_lock)
            {
                return _items
                    .Where(item => item.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase))
                    .Select(item => item.Value)
                    .ToArray();
            }
        }

        /// <summary>
        /// 创建错误响应JList
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>错误JList</returns>
        public static JList CreateErrorJList(string errorMessage)
        {
            var result = new JList();
            result.Add(new JData("Status", "状态", "Error"));
            result.Add(new JData("ErrorMessage", "错误信息", errorMessage));
            return result;
        }

        /// <summary>
        /// 创建成功响应JList
        /// </summary>
        /// <param name="message">成功信息</param>
        /// <returns>成功JList</returns>
        public static JList CreateOKJList(string message)
        {
            var result = new JList();
            result.Add(new JData("Status", "状态", "OK"));
            return result;
        }

        /// <summary>
        /// 向JList添加成功状态
        /// </summary>
        /// <param name="queue">要添加状态的JList</param>
        public void AddSuccessStatus()
        {
            lock (_lock)
            {
                if (_items.Count > 0 && _items[0].Name == "Status")
                {
                    return;
                }
                // 将状态插入到列表头部
                _items.Insert(0, new JData("Status", "状态", "Success"));
            }
        }

        public static JList ComponentJList(string componentGuid, string instanceGuid,
			      string name, string nickName, string description,
                  string category, string subCategory, string position,
				  string state, string inputs, string outputs)
		{
            var compjq = new JList();
            compjq.Add(new JData("ComponentGuid", "组件 GUID", componentGuid));
			compjq.Add(new JData("InstanceGuid", "实例 GUID", instanceGuid));
			compjq.Add(new JData("ComponentName", "组件名称", name));
			compjq.Add(new JData("NickName", "组件昵称", nickName));
			compjq.Add(new JData("Description", "组件描述", description));
			compjq.Add(new JData("Category", "主分类", category));
			compjq.Add(new JData("SubCategory", "子分类", subCategory));
			compjq.Add(new JData("Position", "位置信息", position));
			compjq.Add(new JData("State", "状态信息", state));
			compjq.Add(new JData("Inputs", "输入端信息", inputs));
			compjq.Add(new JData("Outputs", "输出端信息", outputs));
            return compjq;
		}

        public static JList ParamJList(string paramGuid, string instanceGuid,
			      string name, string nickName, string description,
                  string category, string subCategory, string position,
				  string state, string inputs, string outputs)
		{
            var paramjq = new JList();
            paramjq.Add(new JData("ParamGuid", "Param GUID", paramGuid));
			paramjq.Add(new JData("InstanceGuid", "实例 GUID", instanceGuid));
			paramjq.Add(new JData("ParamName", "Param名称", name));
			paramjq.Add(new JData("NickName", "Param昵称", nickName));
			paramjq.Add(new JData("Description", "Param描述", description));
			paramjq.Add(new JData("State", "状态信息", state));
			paramjq.Add(new JData("Inputs", "输入端信息", inputs));
			paramjq.Add(new JData("Outputs", "输出端信息", outputs));
            return  paramjq;
		}


    }

	/// <summary>
	/// TCP接收器，专门负责监听端口和接收JList数据
	/// 设计说明：
	/// 1. 一个 TcpListener 实例只能监听一个端口
	/// 2. 可以创建多个 TcpReceiver 实例，分别监听不同端口（实现多端口服务）
	/// 3. 当前实现只监听单个端口，如需扩展可修改 Server.Start 方法接受端口列表
	/// 4. 接收器将接收到的JList通过事件回调传递给服务器
	/// 5. 会检查JList的time标签，只接收比上次更新的数据
	/// 6. 每个端口只能创建一个TcpReceiver实例
	/// </summary>
	public class TcpReceiver
    {
        private static readonly Dictionary<int, TcpReceiver> _activeReceivers = new Dictionary<int, TcpReceiver>();
        private static readonly object _lock = new object();

        private TcpListener _listener;
        private bool _runing = false;
        private int _port;
        private readonly JList _jlist;
        private DateTime _lastReceivedTime = DateTime.MinValue;

        /// <summary>
        /// 接收到新JList时触发的事件
        /// </summary>
        public event Action<JList> OnJListReceived;

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
        public int Port => _port;

        /// <summary>
        /// 获取命令队列
        /// </summary>
        public JList JList => _jlist;

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
                _jlist = new JList();
                _activeReceivers[port] = this;
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
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            Log($"Tcp开始从端口接收数据 {_port}.");

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

            Log($"Tcp接收器停止，端口 {_port} 已释放");
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
                    JList receivedList = new JList(json);
                    if (receivedList.Time > _lastReceivedTime)
                    {
                        _lastReceivedTime = receivedList.Time;
                        // 触发事件
                        OnJListReceived?.Invoke(receivedList);
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
    /// 2. 支持接收多个JList进行发送
    /// 3. 会过滤掉time标签早于或等于最后发送的消息
    /// 4. 接收和发送完全解耦，只通过队列通信
    /// 5. 这种设计符合单一职责原则，使代码更清晰、更易维护
    /// </summary>
    public class ResponseSender
    {
        private readonly Queue<JList> _sendList = new Queue<JList>();
        private readonly TcpClient _client;
        private bool _running = false;
        private DateTime _lastSentTime = DateTime.MinValue;
        private readonly object _lock = new object();

        /// <summary>
        /// 日志消息事件
        /// </summary>
        public event Action<string> OnLog;

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
                    return _sendList.Count;
                }
            }
        }

        public ResponseSender(TcpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
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
                    JList queueToSend = null;

                    // 从发送队列获取JList
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
                        if (queueToSend.Time <= _lastSentTime)
                        {
                            Log($"ResponseSender: 忽略过期的JList (时间: {queueToSend.Time}, 最后发送: {_lastSentTime})");
                            continue;
                        }

                        // 发送JList
                        await SendJList(queueToSend);
                        _lastSentTime = queueToSend.Time;
                        Log($"ResponseSender: 已发送JList (时间: {queueToSend.Time}, 数据项: {queueToSend.Count})");
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
        /// 发送JList到客户端
        /// </summary>
        /// <param name="queue">要发送的JList对象</param>
        /// <returns>异步任务</returns>
        private async Task SendJList(JList queue)
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
                Log($"ResponseSender发送JList错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 添加JList到发送队列
        /// </summary>
        /// <param name="queue">要发送的JList对象</param>
        public void EnqueueJList(JList queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            lock (_lock)
            {
                _sendList.Enqueue(queue);
                Log($"ResponseSender: JList已加入发送队列 (时间: {queue.Time}, 待发送: {_sendList.Count})");
            }
        }

        /// <summary>
        /// 批量添加JList到发送队列
        /// </summary>
        /// <param name="queues">要发送的JList数组</param>
        public void EnqueueJListRange(JList[] queues)
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
                Log($"ResponseSender: {queues.Length}个JList已加入发送队列，待发送: {_sendList.Count}");
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
                Log($"ResponseSender: 已清空发送队列，移除 {count} 个JList");
            }
        }
    }
}
