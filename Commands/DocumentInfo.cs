using Grasshopper;
using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using Rhino;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GrasshopperSever.Commands
{
    internal class DocumentInfo
    {
        /// <summary>
        /// 保存当前Grasshopper文档
        /// </summary>
        /// <param name="filePath">文件保存路径，如果为空则保存到当前文档位置</param>
        /// <returns>包含保存结果的Ljson</returns>
        public static Ljson SaveDocument(string filePath = null)
        {
            try
            {
                // 1. 获取当前活跃的文档
                GH_Document doc = Instances.ActiveCanvas?.Document;
                if (doc == null)
                {
                    return Ljson.CreateErrorLjson("当前没有活动的Grasshopper文档");
                }

                // 2. 确定保存路径
                string savePath = filePath;
                if (string.IsNullOrWhiteSpace(savePath))
                {
                    savePath = doc.FilePath;
                    if (string.IsNullOrWhiteSpace(savePath))
                    {
                        return Ljson.CreateErrorLjson("文档未保存过，请指定保存路径");
                    }
                }

                // 3. 确保目录存在
                var directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 4. 设置文档的文件路径
                doc.FilePath = savePath;

                // 5. 创建一个IO控制对象并保存
                GH_DocumentIO docIO = new GH_DocumentIO(doc);
                bool success = docIO.Save();

                if (success)
                {
                    doc.IsModified = false;
                    var data = new Dictionary<string, object>
                    {
                        { "FilePath", savePath },
                        { "Message", "文档保存成功" }
                    };
                    return new Ljson("SaveDocument", "保存文档成功", JsonSerializer.SerializeToElement(data));
                }
                else
                {
                    return Ljson.CreateErrorLjson("文档保存失败");
                }
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"保存文档时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 打开Grasshopper文档
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>包含打开结果的Ljson</returns>
        public static Ljson LoadDocument(string filePath)
        {
            try
            {
                // 检查文件路径
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return Ljson.CreateErrorLjson("文件路径不能为空");
                }

                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    return Ljson.CreateErrorLjson($"文件不存在: {filePath}");
                }

                // 检查文件扩展名
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (extension != ".gh" && extension != ".ghx")
                {
                    return Ljson.CreateErrorLjson("不支持的文件格式，只支持 .gh 和 .ghx 文件");
                }

                // 1. 创建一个新的IO对象并加载文件内容到内存
                GH_DocumentIO docIO = new();
                if (!docIO.Open(filePath))
                {
                    return Ljson.CreateErrorLjson("打开文档失败");
                }

                // 2. 获取加载好的文档对象
                GH_Document newDoc = docIO.Document;
                if (newDoc == null)
                {
                    return Ljson.CreateErrorLjson("文档对象为空");
                }

                // 3. 将文档实例化到当前的Grasshopper画布中
                // 这步非常关键，否则文件只在内存里，不会显示在UI上
                Instances.ActiveCanvas.Document = newDoc;

                var data = new Dictionary<string, object>
                {
                    { "FilePath", filePath },
                    { "DocumentId", newDoc.DocumentID.ToString() },
                    { "Message", "文档打开成功" }
                };
                return new Ljson("LoadDocument", "加载文档成功", JsonSerializer.SerializeToElement(data));
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"打开文档时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前文档的所有对象
        /// </summary>
        /// <returns>包含所有对象信息的Ljson</returns>
        public static Ljson GetAllObjects()
        {
            try
            {
                // 1. 获取当前活跃的文档
                GH_Document doc = Instances.ActiveCanvas?.Document;
                if (doc == null)
                {
                    return Ljson.CreateErrorLjson("当前没有活动的Grasshopper文档");
                }
                var data = new Dictionary<string, object>
                {
                    { "DocumentId", doc.DocumentID.ToString() },
                    { "TotalCount", doc.ObjectCount },
                    { "Graph", ComponentGraph.BuildComponentGraph(doc).SerializeToElement() }
                };
                return new Ljson("DocumentGraph", "当前文档所有对象", JsonSerializer.SerializeToElement(data));
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"获取文档对象时出错: {ex.Message}");
            }
        }

        public static Ljson GetObject(string guid)
        {
            Exception caughtException = null;
            IGH_DocumentObject obj = null;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    var doc = (Instances.ActiveCanvas?.Document) ?? throw new InvalidOperationException("No active Grasshopper document");
                    // 查找组件
                    obj = doc.FindObject(new Guid(guid), false);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            }));

            if (caughtException != null)
            {
                return Ljson.CreateErrorLjson($"获取对象失败{caughtException}");
            }
            return ComponentInfo.InstanceLjson(obj);
        }

    }


    /// <summary>
    /// 混合组件/参数节点的有向图数据结构
    /// </summary>
    public class ComponentGraph
    {
        // 节点 GUID -> 输出端口名 -> (下游节点 GUID, 下游输入端口名)
        public Dictionary<Guid, Dictionary<string, Dictionary<Guid, string>>> Adjacency { get; } = new();

        // 节点 GUID -> 显示名称
        public Dictionary<Guid, string> NodeNames { get; } = new();

        // GUID 到文档对象实例的映射
        public Dictionary<Guid, IGH_DocumentObject> NodeObjects { get; } = new();

        // 节点类型
        public Dictionary<Guid, NodeType> NodeTypes { get; } = new();

        // 图的根节点（没有上游连接的节点，即入度为0）
        public HashSet<Guid> Heads { get; } = new();

        public enum NodeType
        {
            Component,
            TopParam
        }

        /// <summary>
        /// 构建包含 IGH_Component 和独立 IGH_Param 的混合连接图
        /// </summary>
        public static ComponentGraph BuildComponentGraph(GH_Document doc)
        {
            ComponentGraph graph = new();
            if (doc == null) return graph;

            // 1. 收集所有顶层对象，并分离组件与独立参数
            var allObjects = doc.Objects.ToList();

            foreach (var obj in allObjects)
            {
                var guid = obj.InstanceGuid;

                // 1.1 如果是 IGH_Component（绝大多数情况）
                if (obj is IGH_Component component)
                {
                    graph.Adjacency[guid] = new();
                    graph.NodeNames[guid] = string.IsNullOrEmpty(component.Name) ? component.NickName : component.Name;
                    graph.NodeObjects[guid] = component;
                    graph.NodeTypes[guid] = NodeType.Component;
                }
                // 1.2 如果不是组件，但实现了 IGH_Param（游离参数）
                else if (obj is IGH_Param param && param.Kind == GH_ParamKind.floating)
                {
                    graph.Adjacency[guid] = new(); // 只有一个输出端
                    graph.NodeNames[guid] = param.Name;
                    graph.NodeObjects[guid] = param;
                    graph.NodeTypes[guid] = NodeType.TopParam;
                }
            }

            var hasInput = new HashSet<Guid>();

            // 2. 为每个节点找出其下游节点
            foreach (var nodeId in graph.NodeObjects.Keys.ToList())
            {
                var node = graph.NodeObjects[nodeId];
                IEnumerable<IGH_Param> outputParams;

                // 根据节点类型获取“输出端口”列表
                if (node is IGH_Component comp)
                {
                    outputParams = comp.Params.Output;
                }
                else if (node is IGH_Param paramNode)
                {
                    outputParams = new[] { paramNode };
                }
                else
                {
                    continue;
                }

                // 遍历每一个输出参数，找到连接到的下游参数
                foreach (var outParam in outputParams)
                {
                    Dictionary<Guid, string> recipientsParams = new();
                    foreach (var targetParam in outParam.Recipients)
                    {
                        // 通过参数回溯到其所属的顶层文档对象（可能是组件或独立参数）
                        var targetDocObj = targetParam.Attributes?.GetTopLevel?.DocObject;
                        if (targetDocObj == null) continue;

                        var targetId = targetDocObj.InstanceGuid;
                        if (graph.Adjacency.ContainsKey(targetId) && targetId != nodeId)
                        {
                            recipientsParams[targetId] = targetParam.Name;
                            hasInput.Add(targetId);
                        }
                    }
                    graph.Adjacency[nodeId][outParam.Name] = recipientsParams;
                }
            }

            // 3. 计算根节点（Heads）：没有任何上游连接的节点
            foreach (var id in graph.NodeObjects.Keys)
            {
                if (!hasInput.Contains(id))
                    graph.Heads.Add(id);
            }

            return graph;
        }
        /// <summary>
        /// 序列化为 JSON 字符串（仅含拓扑信息，不包含运行时对象和单独的边列表）
        /// </summary>
        public JsonElement SerializeToElement()
        {
            // 构建适合序列化的匿名对象，将 Guid 转换为可读字符串
            var dto = new
            {
                Heads = Heads.Select(g => g.ToString("D")).ToList(),

                Nodes = NodeNames.Select(kvp => new
                {
                    Id = kvp.Key.ToString("D"),
                    Name = kvp.Value,
                    Type = NodeTypes.TryGetValue(kvp.Key, out var t) ? t.ToString() : "Unknown"
                }).ToList(),

                Adjacency = Adjacency.ToDictionary(
                    src => src.Key.ToString("D"),
                    src => src.Value.ToDictionary(
                        outPortName => outPortName.Key,
                        outPortName => outPortName.Value.ToDictionary(
                            tgt => tgt.Key.ToString("D"),
                            tgt => tgt.Value
                        )
                    )
                )
            };

            return JsonSerializer.SerializeToElement(dto);
        }
    }

}
