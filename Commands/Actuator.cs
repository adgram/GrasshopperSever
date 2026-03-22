using System;
using GrasshopperSever.Utils;


namespace GrasshopperSever.Commands
{
    /// <summary>
    /// 用于执行特殊的指令
    /// </summary>
    public class Actuator
    {
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="data">输入的JQueue数据</param>
        /// <returns>执行结果JQueue</returns>
        public JQueue DoCommand(JQueue data)
        {
            if (data == null || data.Count == 0)
            {
                return CreateErrorJQueue("输入数据为空");
            }

            // 获取命令类型（第一个JData的Value）
            var commandData = data.Peek();
            if (commandData == null || string.IsNullOrWhiteSpace(commandData.Value))
            {
                return CreateErrorJQueue("未找到命令类型");
            }

            string commandType = commandData.Value;

            // 根据命令类型路由到对应的处理方法
            try
            {
                switch (commandType.ToUpperInvariant())
                {
                    case "GETALLCOMPONENTS":
                        return HandleGetAllComponentsFromDB(data);

                    case "FINDCOMPONENTBYGUID":
                        return HandleFindComponentByGuid(data);

                    case "FINDCOMPONENTBYNAME":
                        return HandleFindComponentByName(data);

                    case "FINDCOMPONENTBYCATEGORY":
                        return HandleFindComponentByCategory(data);

                    case "SEARCHCOMPONENTSBYNAME":
                        return HandleSearchComponentsByName(data);

                    case "SAVEDOCUMENT":
                        return HandleSaveDocument(data);

                    case "LOADDOCUMENT":
                        return HandleLoadDocument(data);

                    default:
                        return CreateErrorJQueue($"未知命令类型: {commandType}");
                }
            }
            catch (Exception ex)
            {
                return CreateErrorJQueue($"执行命令时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理从数据库获取所有组件命令
        /// 输入：JQueue包含 Command="GetAllComponentsFromDB"
        /// 输出：数据库中的所有组件信息
        /// </summary>
        private JQueue HandleGetAllComponentsFromDB(JQueue data)
        {
            try
            {
                var result = ComponentInfo.GetAllComponentsFromDB();
                AddSuccessStatus(result);
                return result;
            }
            catch (Exception ex)
            {
                return CreateErrorJQueue($"从数据库获取组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过GUID查询组件命令
        /// 输入：JQueue包含 Command="FindComponentByGuid", Guid="组件GUID"
        /// 输出：组件详细信息
        /// </summary>
        private JQueue HandleFindComponentByGuid(JQueue data)
        {
            try
            {
                string guid = GetParameter(data, "Guid");
                if (string.IsNullOrWhiteSpace(guid))
                {
                    return CreateErrorJQueue("缺少参数: Guid");
                }

                var component = ComponentInfo.FindComponentsByGuid(guid);
                if (component == null)
                {
                    return CreateErrorJQueue($"未找到GUID为 {guid} 的组件");
                }

                AddSuccessStatus(component);
                return component;
            }
            catch (Exception ex)
            {
                return CreateErrorJQueue($"通过GUID查询组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过名称查询组件命令
        /// 输入：JQueue包含 Command="FindComponentByName", Name="组件名称"
        /// 输出：组件详细信息
        /// </summary>
        private JQueue HandleFindComponentByName(JQueue data)
        {
            try
            {
                string name = GetParameter(data, "Name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    return CreateErrorJQueue("缺少参数: Name");
                }

                var component = ComponentInfo.FindComponentsByName(name);
                if (component == null)
                {
                    return CreateErrorJQueue($"未找到名称为 {name} 的组件");
                }

                AddSuccessStatus(component);
                return component;
            }
            catch (Exception ex)
            {
                return CreateErrorJQueue($"通过名称查询组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过分类和名称查询组件命令
        /// 输入：JQueue包含 Command="FindComponentByCategory", Category="分类", SubCategory="子分类"(可选), Name="名称"(可选)
        /// 输出：组件详细信息
        /// </summary>
        private JQueue HandleFindComponentByCategory(JQueue data)
        {
            try
            {
                string category = GetParameter(data, "Category");
                string subCategory = GetParameter(data, "SubCategory");
                string name = GetParameter(data, "Name");

                if (string.IsNullOrWhiteSpace(category) && string.IsNullOrWhiteSpace(subCategory) && string.IsNullOrWhiteSpace(name))
                {
                    return CreateErrorJQueue("至少需要提供一个参数: Category, SubCategory 或 Name");
                }

                var component = ComponentInfo.FindComponentsByCategory(category, subCategory, name);
                if (component == null)
                {
                    return CreateErrorJQueue($"未找到符合条件的组件");
                }

                AddSuccessStatus(component);
                return component;
            }
            catch (Exception ex)
            {
                return CreateErrorJQueue($"通过分类查询组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理通过名称模糊搜索组件命令
        /// 输入：JQueue包含 Command="SearchComponentsByName", Name="搜索关键词"
        /// 输出：匹配的组件列表
        /// </summary>
        private JQueue HandleSearchComponentsByName(JQueue data)
        {
            try
            {
                string name = GetParameter(data, "Name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    return CreateErrorJQueue("缺少参数: Name");
                }

                var components = ComponentInfo.SearchComponentsByName(name);
                if (components == null || components.Count == 0)
                {
                    return CreateErrorJQueue($"未找到名称包含 {name} 的组件");
                }

                // 将ComponentJQueue列表合并为一个JQueue
                var result = new JQueue();
                result.Enqueue(new JData("Count", "匹配的组件数量", components.Count.ToString()));

                for (int i = 0; i < components.Count; i++)
                {
                    var comp = components[i];
                    var items = comp.ToArray();
                    foreach (var item in items)
                    {
                        result.Enqueue(new JData(
                            $"Component_{i}_{item.Name}",
                            $"组件{i}的{item.Description}",
                            item.Value
                        ));
                    }
                }

                AddSuccessStatus(result);
                return result;
            }
            catch (Exception ex)
            {
                return CreateErrorJQueue($"搜索组件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从JQueue中获取指定参数的值
        /// </summary>
        /// <param name="data">输入JQueue</param>
        /// <param name="paramName">参数名称</param>
        /// <returns>参数值，如果不存在则返回null</returns>
        private string GetParameter(JQueue data, string paramName)
        {
            var items = data.ToArray();
            foreach (var item in items)
            {
                if (item.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase))
                {
                    return item.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// 创建错误响应JQueue
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>错误JQueue</returns>
        private JQueue CreateErrorJQueue(string errorMessage)
        {
            var result = new JQueue();
            result.Enqueue(new JData("Status", "状态", "Error"));
            result.Enqueue(new JData("ErrorMessage", "错误信息", errorMessage));
            return result;
        }

        /// <summary>
        /// 向JQueue添加成功状态
        /// </summary>
        /// <param name="queue">要添加状态的JQueue</param>
        private void AddSuccessStatus(JQueue queue)
        {
            // 将状态插入到队列头部
            var status = new JData("Status", "状态", "Success");
            var tempQueue = new JQueue();
            tempQueue.Enqueue(status);

            var items = queue.ToArray();
            foreach (var item in items)
            {
                tempQueue.Enqueue(item);
            }

            // 清空原队列并添加新元素
            queue.Clear();
            var newItems = tempQueue.ToArray();
            foreach (var item in newItems)
            {
                queue.Enqueue(item);
            }
        }

        /// <summary>
        /// 处理保存文档命令
        /// 输入：JQueue包含 Command="SaveDocument", FilePath="文件路径"(可选)
        /// 输出：保存结果
        /// </summary>
        private JQueue HandleSaveDocument(JQueue data)
        {
            try
            {
                string filePath = GetParameter(data, "FilePath");
                var result = DocumentInfo.SaveDocument(filePath);
                return result;
            }
            catch (Exception ex)
            {
                return CreateErrorJQueue($"保存文档失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理打开文档命令
        /// 输入：JQueue包含 Command="LoadDocument", FilePath="文件路径"
        /// 输出：打开结果
        /// </summary>
        private JQueue HandleLoadDocument(JQueue data)
        {
            try
            {
                string filePath = GetParameter(data, "FilePath");
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return CreateErrorJQueue("缺少参数: FilePath");
                }

                var result = DocumentInfo.LoadDocument(filePath);
                return result;
            }
            catch (Exception ex)
            {
                return CreateErrorJQueue($"打开文档失败: {ex.Message}");
            }
        }
    }
}