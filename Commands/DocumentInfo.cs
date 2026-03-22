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
        /// <returns>包含保存结果的JQueue</returns>
        public static JQueue SaveDocument(string filePath = null)
        {
            var result = new JQueue();

            try
            {
                // 1. 获取当前活跃的文档
                GH_Document doc = Instances.ActiveCanvas?.Document;
                if (doc == null)
                {
                    result.Enqueue(new JData("Status", "状态", "Error"));
                    result.Enqueue(new JData("ErrorMessage", "错误信息", "当前没有活动的Grasshopper文档"));
                    return result;
                }

                // 2. 确定保存路径
                string savePath = filePath;
                if (string.IsNullOrWhiteSpace(savePath))
                {
                    savePath = doc.FilePath;
                    if (string.IsNullOrWhiteSpace(savePath))
                    {
                        result.Enqueue(new JData("Status", "状态", "Error"));
                        result.Enqueue(new JData("ErrorMessage", "错误信息", "文档未保存过，请指定保存路径"));
                        return result;
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
                    result.Enqueue(new JData("Status", "状态", "Success"));
                    result.Enqueue(new JData("FilePath", "文件路径", savePath));
                    result.Enqueue(new JData("Message", "消息", "文档保存成功"));
                }
                else
                {
                    result.Enqueue(new JData("Status", "状态", "Error"));
                    result.Enqueue(new JData("ErrorMessage", "错误信息", "文档保存失败"));
                }
            }
            catch (Exception ex)
            {
                result.Enqueue(new JData("Status", "状态", "Error"));
                result.Enqueue(new JData("ErrorMessage", "错误信息", $"保存文档时出错: {ex.Message}"));
            }

            return result;
        }

        /// <summary>
        /// 打开Grasshopper文档
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>包含打开结果的JQueue</returns>
        public static JQueue LoadDocument(string filePath)
        {
            var result = new JQueue();

            try
            {
                // 检查文件路径
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    result.Enqueue(new JData("Status", "状态", "Error"));
                    result.Enqueue(new JData("ErrorMessage", "错误信息", "文件路径不能为空"));
                    return result;
                }

                // 检查文件是否存在
                if (!System.IO.File.Exists(filePath))
                {
                    result.Enqueue(new JData("Status", "状态", "Error"));
                    result.Enqueue(new JData("ErrorMessage", "错误信息", $"文件不存在: {filePath}"));
                    return result;
                }

                // 检查文件扩展名
                string extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                if (extension != ".gh" && extension != ".ghx")
                {
                    result.Enqueue(new JData("Status", "状态", "Error"));
                    result.Enqueue(new JData("ErrorMessage", "错误信息", "不支持的文件格式，只支持 .gh 和 .ghx 文件"));
                    return result;
                }

                // 1. 创建一个新的IO对象并加载文件内容到内存
                GH_DocumentIO docIO = new GH_DocumentIO();
                if (!docIO.Open(filePath))
                {
                    result.Enqueue(new JData("Status", "状态", "Error"));
                    result.Enqueue(new JData("ErrorMessage", "错误信息", "打开文档失败"));
                    return result;
                }

                // 2. 获取加载好的文档对象
                GH_Document newDoc = docIO.Document;
                if (newDoc == null)
                {
                    result.Enqueue(new JData("Status", "状态", "Error"));
                    result.Enqueue(new JData("ErrorMessage", "错误信息", "文档对象为空"));
                    return result;
                }

                // 3. 将文档实例化到当前的Grasshopper画布中
                // 这步非常关键，否则文件只在内存里，不会显示在UI上
                Instances.ActiveCanvas.Document = newDoc;

                result.Enqueue(new JData("Status", "状态", "Success"));
                result.Enqueue(new JData("FilePath", "文件路径", filePath));
                result.Enqueue(new JData("DocumentId", "文档ID", newDoc.DocumentID.ToString()));
                result.Enqueue(new JData("Message", "消息", "文档打开成功"));
            }
            catch (Exception ex)
            {
                result.Enqueue(new JData("Status", "状态", "Error"));
                result.Enqueue(new JData("ErrorMessage", "错误信息", $"打开文档时出错: {ex.Message}"));
            }

            return result;
        }
    }
}
