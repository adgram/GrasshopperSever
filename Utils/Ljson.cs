using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Grasshopper.Kernel;


namespace GrasshopperSever.Utils
{
    /// <summary>
    /// 统一的JSON数据结构，合并了原有的 JData 和 Ljson
    /// 可以表示单个数据项或数据集合
    /// </summary>
    public class Ljson : IDisposable
    {
        /// <summary>
        /// 数据名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 数据说明
        /// </summary>
        public string Info { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// 数据值（JsonElement，可以是对象、数组或原始值）
        /// </summary>
        public JsonElement Value { get; set; }
        
        // 跟踪对象的释放状态
        private bool _disposed = false;

        /// <summary>
        /// 无参构造函数 - 使用当前时间
        /// </summary>
        public Ljson()
        {
            Time = DateTime.Now;
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        /// <param name="name">数据名称</param>
        /// <param name="info">数据说明</param>
        /// <param name="value">数据值</param>
        public Ljson(string name, string info, JsonElement value)
        {
            Name = name;
            Info = info;
            Value = value;
            Time = DateTime.Now;
        }

        /// <summary>
        /// 从另一个Ljson克隆
        /// </summary>
        /// <param name="other">要克隆的对象</param>
        public Ljson(Ljson other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            Name = other.Name;
            Info = other.Info;
            Time = other.Time;
            Value = other.Value.Clone();
        }

        /// <summary>
        /// 从JSON字符串反序列化
        /// </summary>
        /// <param name="json">JSON字符串</param>
        public Ljson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON字符串不能为空", nameof(json));
            }

            FromJson(json);
        }

        /// <summary>
        /// 序列化为JSON字符串（避免多重转义）
        /// </summary>
        /// <returns>JSON字符串</returns>
        public string ToJson()
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions
                {
                    Indented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }))
                {
                    writer.WriteStartObject();

                    // 写入 Name
                    writer.WriteString("Name", Name);

                    // 写入 Info
                    writer.WriteString("Info", Info);

                    // 写入 Time
                    writer.WriteString("Time", Time.ToString("O"));

                    // 写入 Value - 直接写入 JsonElement，避免双重序列化
                    writer.WritePropertyName("Value");
                    Value.WriteTo(writer);

                    writer.WriteEndObject();
                    writer.Flush();
                }

                return System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// 从JSON字符串反序列化
        /// </summary>
        /// <param name="json">JSON字符串</param>
        public void FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON字符串不能为空", nameof(json));
            }

            using (var doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;

                // 读取 Name
                Name = root.TryGetProperty("Name", out var nameElement) ? nameElement.GetString() : null;

                // 读取 Info
                Info = root.TryGetProperty("Info", out var infoElement) ? infoElement.GetString() : null;

                // 读取 Time
                if (root.TryGetProperty("Time", out var timeElement))
                {
                    Time = timeElement.GetDateTime();
                }
                else
                {
                    Time = DateTime.Now;
                }

                // 读取 Value - 直接克隆 JsonElement
                if (root.TryGetProperty("Value", out var valueElement))
                {
                    Value = valueElement.Clone();
                }
            }
        }

        /// <summary>
        /// 深度克隆
        /// </summary>
        /// <returns>克隆的对象</returns>
        public Ljson DeepClone()
        {
            return new Ljson(this);
        }

        /// <summary>
        /// 返回字符串表示
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"Ljson[Time: {Time:yyyy-MM-dd HH:mm:ss}, Name: {Name}, Info: {Info}]";
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
                    Name = null;
                    Info = null;
                    Value = default;
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~Ljson()
        {
            Dispose(false);
        }

        /// <summary>
        /// 从Value中获取指定参数的值
        /// 支持两种格式：
        /// 1. 数组格式：[{"Name": "x", "Value": 123}, ...]
        /// 2. 对象格式：{"x": 123, "y": 456}
        /// </summary>
        /// <param name="paramName">参数名称</param>
        /// <returns>参数值（JsonElement），如果不存在则返回null</returns>
        public JsonElement? GetParameter(string paramName)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                return null;
            }

            // 支持对象格式（字典）
            if (Value.ValueKind == JsonValueKind.Object)
            {
                if (Value.TryGetProperty(paramName, out var valueElement))
                {
                    return valueElement.Clone();
                }
                return null;
            }

            // 支持数组格式（带 Name 属性的项）
            if (Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in Value.EnumerateArray())
                {
                    if (item.TryGetProperty("Name", out var nameElement) &&
                        nameElement.GetString().Equals(paramName, StringComparison.OrdinalIgnoreCase))
                    {
                        return item.GetProperty("Value").Clone();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 从Value中搜索所有匹配参数名的值
        /// 支持两种格式：
        /// 1. 数组格式：[{"Name": "x", "Value": 123}, ...]
        /// 2. 对象格式：{"x": 123, "y": 456}
        /// </summary>
        /// <param name="paramName">参数名称</param>
        /// <returns>参数值数组（JsonElement）</returns>
        public JsonElement[] SearchParameter(string paramName)
        {
            var result = new List<JsonElement>();

            if (string.IsNullOrEmpty(paramName))
            {
                return result.ToArray();
            }

            // 支持对象格式（字典）- 精确匹配键名
            if (Value.ValueKind == JsonValueKind.Object)
            {
                if (Value.TryGetProperty(paramName, out var valueElement))
                {
                    result.Add(valueElement.Clone());
                }
                return result.ToArray();
            }

            // 支持数组格式（带 Name 属性的项）- 支持多个同名项
            if (Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in Value.EnumerateArray())
                {
                    if (item.TryGetProperty("Name", out var nameElement) &&
                        nameElement.GetString().Equals(paramName, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(item.GetProperty("Value").Clone());
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 设置参数值
        /// 支持两种格式：
        /// 1. 对象格式：直接设置键值对
        /// 2. 数组格式：添加或更新带 Name 属性的项
        /// </summary>
        /// <param name="paramName">参数名称</param>
        /// <param name="paramValue">参数值（JsonElement）</param>
        public void SetParameter(string paramName, JsonElement paramValue)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                return;
            }

            // 对象格式：直接更新键值对
            if (Value.ValueKind == JsonValueKind.Object)
            {
                var obj = new Dictionary<string, JsonElement>();
                foreach (var prop in Value.EnumerateObject())
                {
                    obj[prop.Name] = prop.Value.Clone();
                }
                obj[paramName] = paramValue.Clone();
                Value = JsonSerializer.SerializeToElement(obj);
                return;
            }

            // 数组格式：更新或添加项
            if (Value.ValueKind == JsonValueKind.Array)
            {
                var items = Value.EnumerateArray().ToList();
                bool found = false;

                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].TryGetProperty("Name", out var nameElement) &&
                        nameElement.GetString().Equals(paramName, StringComparison.OrdinalIgnoreCase))
                    {
                        // 更新现有项
                        var newItem = new Dictionary<string, JsonElement>();
                        foreach (var prop in items[i].EnumerateObject())
                        {
                            newItem[prop.Name] = prop.Value.Clone();
                        }
                        newItem["Value"] = paramValue.Clone();
                        items[i] = JsonSerializer.SerializeToElement(newItem);
                        found = true;
                        break;
                    }
                }

                // 如果没找到，添加新项
                if (!found)
                {
                    var newItem = new Dictionary<string, JsonElement>
                    {
                        { "Name", JsonSerializer.SerializeToElement(paramName) },
                        { "Info", JsonSerializer.SerializeToElement("") },
                        { "Value", paramValue.Clone() }
                    };
                    items.Add(JsonSerializer.SerializeToElement(newItem));
                }

                Value = JsonSerializer.SerializeToElement(items);
            }
            else
            {
                // Value 不是数组也不是对象，创建一个新对象
                var obj = new Dictionary<string, JsonElement>
                {
                    { paramName, paramValue.Clone() }
                };
                Value = JsonSerializer.SerializeToElement(obj);
            }
        }

        /// <summary>
        /// 批量设置参数值
        /// 支持两种格式：
        /// 1. 对象格式：直接设置多个键值对
        /// 2. 数组格式：添加或更新多个带 Name 属性的项
        /// </summary>
        /// <param name="parameters">参数字典，键为参数名，值为参数值（JsonElement）</param>
        public void SetParameters(Dictionary<string, JsonElement> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return;
            }

            foreach (var param in parameters)
            {
                SetParameter(param.Key, param.Value);
            }
        }


        /// <summary>
        /// 创建错误响应Ljson
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>错误Ljson</returns>
        public static Ljson CreateErrorLjson(string errorMessage)
        {
            return new Ljson("Error", "错误响应",
                JsonSerializer.SerializeToElement(errorMessage));
        }

        /// <summary>
        /// 创建成功响应Ljson
        /// </summary>
        /// <param name="message">成功信息</param>
        /// <returns>成功Ljson</returns>
        public static Ljson CreateOKLjson(string message)
        {
            return new Ljson("OK", "成功响应",
                JsonSerializer.SerializeToElement(message));
        }

        /// <summary>
        /// 创建成功响应Ljson
        /// </summary>
        /// <param name="message">成功信息</param>
        /// <returns>成功Ljson</returns>
        public static Ljson CreateSuccessLjson()
        {
            return new Ljson("Success", "操作成功", 
                JsonSerializer.SerializeToElement(new Dictionary<string, string>()));
        }

        /// <summary>
        /// 创建组件信息Ljson
        /// </summary>
        public static Ljson ComponentLjson(string componentGuid, string instanceGuid,
            string name, string nickName, string description,
            string category, string subCategory, string position,
            string state, string inputs, string outputs)
        {
            var data = new Dictionary<string, JsonElement>
            {
                { "ComponentGuid", JsonSerializer.SerializeToElement(componentGuid) },
                { "InstanceGuid", JsonSerializer.SerializeToElement(instanceGuid) },
                { "ComponentName", JsonSerializer.SerializeToElement(name) },
                { "NickName", JsonSerializer.SerializeToElement(nickName) },
                { "Description", JsonSerializer.SerializeToElement(description) },
                { "Category", JsonSerializer.SerializeToElement(category) },
                { "SubCategory", JsonSerializer.SerializeToElement(subCategory) },
                { "Position", JsonSerializer.SerializeToElement(position) },
                { "State", JsonSerializer.SerializeToElement(state) },
                { "Inputs", JsonSerializer.SerializeToElement(inputs) },
                { "Outputs", JsonSerializer.SerializeToElement(outputs) }
            };

            return new Ljson("Component", "组件信息",
                JsonSerializer.SerializeToElement(data));
        }

        /// <summary>
        /// 创建参数信息Ljson
        /// </summary>
        public static Ljson ParamLjson(string paramGuid, string instanceGuid,
            string name, string nickName, string description,
            string typeName, bool optional, GH_ParamAccess access,
            GH_DataMapping mapping, bool reverse, bool simplify,
            string inputs, string outputs)
        {
            var data = new Dictionary<string, object>
            {
                { "ParamGuid", paramGuid },
                { "InstanceGuid", instanceGuid },
                { "Name", name },
                { "NickName", nickName },
                { "Description", description },
                { "TypeName", typeName },
                { "Optional", optional },
                { "Access", access.ToString() },
                { "Mapping", mapping.ToString() },
                { "Reverse", reverse },
                { "Simplify", simplify },
                { "Inputs", inputs },
                { "Outputs", outputs }
            };

            return new Ljson("Param", "参数信息",
                JsonSerializer.SerializeToElement(data));
        }
    }

    /// <summary>
    /// 静态工具方法，用于Ljson的批量操作
    /// </summary>
    public static class LjsonHelper
    {
        /// <summary>
        /// 序列化Ljson数组为JSON字符串（避免多重转义）
        /// </summary>
        public static string SerializeLjsonArray(List<Ljson> sjsons)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(ms, new JsonWriterOptions
                {
                    Indented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }))
                {
                    writer.WriteStartObject();

                    // 写入 Time
                    writer.WriteString("Time", DateTime.Now.ToString("O"));

                    // 写入 Items
                    writer.WritePropertyName("Items");
                    writer.WriteStartArray();

                    foreach (var sjson in sjsons)
                    {
                        // 直接写入每个 Ljson 对象
                        writer.WriteStartObject();
                        writer.WriteString("Name", sjson.Name);
                        writer.WriteString("Info", sjson.Info);
                        writer.WriteString("Time", sjson.Time.ToString("O"));

                        // 直接写入 Value JsonElement，避免双重序列化
                        writer.WritePropertyName("Value");
                        sjson.Value.WriteTo(writer);

                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    writer.Flush();
                }

                return System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// 从JSON字符串反序列化为Ljson数组
        /// </summary>
        public static List<Ljson> ParseLjsonArray(string json)
        {
            var result = new List<Ljson>();

            if (string.IsNullOrWhiteSpace(json))
                return result;

            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    var itemsElement = doc.RootElement.GetProperty("Items");
                    if (itemsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var jsonStr in itemsElement.EnumerateArray())
                        {
                            result.Add(new Ljson(jsonStr.GetString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ParseLjsonArray失败: {ex.Message}");
            }

            return result;
        }
    }
}