using Grasshopper;
using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using System;

namespace GrasshopperSever.Commands
{
    internal class DocumentInfo
    {
        /// <summary>
        /// 保存当前Grasshopper文档
        /// </summary>
        /// <param name="filePath">文件保存路径，如果为空则保存到当前文档位置</param>
        /// <returns>包含保存结果的JList</returns>
        public static JList SaveDocument(string filePath = null)
        {
            try
            {
                // 1. 获取当前活跃的文档
                GH_Document doc = Instances.ActiveCanvas?.Document;
                if (doc == null)
                {
                    return JList.CreateErrorJList("当前没有活动的Grasshopper文档");
                }

                // 2. 确定保存路径
                string savePath = filePath;
                if (string.IsNullOrWhiteSpace(savePath))
                {
                    savePath = doc.FilePath;
                    if (string.IsNullOrWhiteSpace(savePath))
                    {
                        return JList.CreateErrorJList("文档未保存过，请指定保存路径");
                    }
                }

                // 3. 确保目录存在
                var directory = System.IO.Path.GetDirectoryName(savePath);
                if (!string.IsNullOrWhiteSpace(directory) && !System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                // 4. 设置文档的文件路径
                doc.FilePath = savePath;

                // 5. 创建一个IO控制对象并保存
                GH_DocumentIO docIO = new GH_DocumentIO(doc);
                bool success = docIO.Save();

                if (success)
                {
                    doc.IsModified = false;
                    var result = new JList();
                    result.Add(new JData("FilePath", "文件路径", savePath));
                    result.Add(new JData("Message", "消息", "文档保存成功"));
                    result.AddSuccessStatus();
                    return result;
                }
                else
                {
                    return JList.CreateErrorJList("文档保存失败");
                }
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"保存文档时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 打开Grasshopper文档
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>包含打开结果的JList</returns>
        public static JList LoadDocument(string filePath)
        {
            try
            {
                // 检查文件路径
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return JList.CreateErrorJList("文件路径不能为空");
                }

                // 检查文件是否存在
                if (!System.IO.File.Exists(filePath))
                {
                    return JList.CreateErrorJList($"文件不存在: {filePath}");
                }

                // 检查文件扩展名
                string extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                if (extension != ".gh" && extension != ".ghx")
                {
                    return JList.CreateErrorJList("不支持的文件格式，只支持 .gh 和 .ghx 文件");
                }

                // 1. 创建一个新的IO对象并加载文件内容到内存
                GH_DocumentIO docIO = new GH_DocumentIO();
                if (!docIO.Open(filePath))
                {
                    return JList.CreateErrorJList("打开文档失败");
                }

                // 2. 获取加载好的文档对象
                GH_Document newDoc = docIO.Document;
                if (newDoc == null)
                {
                    return JList.CreateErrorJList("文档对象为空");
                }

                // 3. 将文档实例化到当前的Grasshopper画布中
                // 这步非常关键，否则文件只在内存里，不会显示在UI上
                Instances.ActiveCanvas.Document = newDoc;

                var result = new JList();
                result.Add(new JData("FilePath", "文件路径", filePath));
                result.Add(new JData("DocumentId", "文档ID", newDoc.DocumentID.ToString()));
                result.Add(new JData("Message", "消息", "文档打开成功"));
                result.AddSuccessStatus();
                return result;
            }
            catch (Exception ex)
            {
                return JList.CreateErrorJList($"打开文档时出错: {ex.Message}");
            }
        }
    }
}
