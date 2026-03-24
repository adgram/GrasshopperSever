using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
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
        /// 序列化JList数组为JSON字符串（避免多重转义）
        /// 与ParseJListArray成对使用
        /// </summary>
        public static string SerializeJListArray(List<JList> jlists)
        {
            // 将每个JList转换为可序列化的对象，避免先ToJson再序列化导致的双重转义
            var items = jlists.Select(jlist =>
            {
                // 复用JList.ToJson()中的RawJDataItem逻辑
                return jlist.ToArray().Select(jdata =>
                {
                    var value = jdata.Value;
                    object parsedValue = value;
                    try
                    {
                        using (var doc = JsonDocument.Parse(value))
                        {
                            if (doc.RootElement.ValueKind == JsonValueKind.Object ||
                                doc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                parsedValue = doc.RootElement.Clone();
                            }
                        }
                    }
                    catch (JsonException) { }

                    return new RawJDataItem
                    {
                        Name = jdata.Name,
                        Description = jdata.Description,
                        Value = parsedValue
                    };
                }).ToArray();
            }).ToArray();

            var wrapper = new
            {
                Time = DateTime.Now,
                Items = items
            };

            return JsonSerializer.Serialize(wrapper, new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        /// <summary>
        /// 从JSON字符串反序列化为JList数组
        /// 与SerializeJListArray成对使用
        /// </summary>
        public static List<JList> ParseJListArray(string json)
        {
            var result = new List<JList>();

            if (string.IsNullOrWhiteSpace(json))
                return result;

            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    var itemsElement = doc.RootElement.GetProperty("Items");
                    if (itemsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var paramItems in itemsElement.EnumerateArray())
                        {
                            var jlist = new JList();
                            if (paramItems.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in paramItems.EnumerateArray())
                                {
                                    var jdata = new JData(
                                        item.GetProperty("Name").GetString(),
                                        item.GetProperty("Description").GetString(),
                                        GetValueAsString(item.GetProperty("Value"))
                                    );
                                    jlist.Add(jdata);
                                }
                            }
                            result.Add(jlist);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ParseJListArray失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 将JsonElement Value转换为字符串
        /// </summary>
        private static string GetValueAsString(System.Text.Json.JsonElement valueElement)
        {
            if (valueElement.ValueKind == JsonValueKind.String)
            {
                return valueElement.GetString();
            }
            else
            {
                return valueElement.GetRawText();
            }
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
                  string typeName, string optional, string access,
                  string mapping, string reverse, string simplify, 
                  string inputs, string outputs)
		{
            var paramjq = new JList();
            paramjq.Add(new JData("ParamGuid", "Param GUID", paramGuid));
			paramjq.Add(new JData("InstanceGuid", "实例 GUID", instanceGuid));
			paramjq.Add(new JData("Name", "Param名称", name));
			paramjq.Add(new JData("NickName", "Param昵称", nickName));
			paramjq.Add(new JData("Description", "Param描述", description));
            paramjq.Add(new JData("TypeName", "数据类型", typeName));
            paramjq.Add(new JData("Optional", "是否可选", optional));
            paramjq.Add(new JData("Access", "数据结构", access));
            paramjq.Add(new JData("Mapping", "数据结构", mapping));
            paramjq.Add(new JData("Reverse", "数据结构", reverse));
            paramjq.Add(new JData("Simplify", "数据结构", simplify));
            paramjq.Add(new JData("Inputs", "输入端信息", inputs));
			paramjq.Add(new JData("Outputs", "输出端信息", outputs));
            return  paramjq;
		}
    }
}