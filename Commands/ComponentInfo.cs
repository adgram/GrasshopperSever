using Grasshopper;
using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Data.SQLite;
using System.Diagnostics;


namespace GrasshopperSever.Commands
{

    // 文档信息
    public static class ComponentInfo
    {
        // 组件代理字典缓存（GUID -> Proxy），用于快速查找
        private static Dictionary<string, IGH_ObjectProxy> _componentProxyCache;
        private static readonly object _cacheLock = new object();


        /// <summary>
        /// 创建组件信息Ljson
        /// </summary>
        public static Ljson ComponentLjson(string componentGuid,
            string name, string nickName, string description,
            string category, string subCategory, string prototype)
        {
            var data = new Dictionary<string, JsonElement>
            {
                { "ComponentGuid", JsonSerializer.SerializeToElement(componentGuid) },
                { "ComponentName", JsonSerializer.SerializeToElement(name) },
                { "NickName", JsonSerializer.SerializeToElement(nickName) },
                { "Description", JsonSerializer.SerializeToElement(description) },
                { "Category", JsonSerializer.SerializeToElement(category) },
                { "SubCategory", JsonSerializer.SerializeToElement(subCategory) },
                { "Prototype", JsonSerializer.SerializeToElement(prototype) }
            };

            return new Ljson("Component", "组件信息", JsonSerializer.SerializeToElement(data));
        }

        /// <summary>
        /// 获取组件代理字典缓存
        /// </summary>
        public static Dictionary<string, IGH_ObjectProxy> GetComponentProxyCache()
        {
            if (_componentProxyCache == null)
            {
                lock (_cacheLock)
                {
                    if (_componentProxyCache == null)
                    {
                        var server = Instances.ComponentServer;
                        _componentProxyCache = new Dictionary<string, IGH_ObjectProxy>();
                        foreach (var proxy in server.ObjectProxies)
                        {
                            _componentProxyCache[proxy.Guid.ToString()] = proxy;
                        }
                    }
                }
            }
            return _componentProxyCache;
        }
       
        
        /// <summary>
        /// 获取所有组件
        /// </summary>
        /// <param name="command">命令</param>
        /// <returns>文件信息</returns>
        public static Ljson GetAllComponentsNested()
        {
            var proxies = Instances.ComponentServer.ObjectProxies;

            // 预构建组件代理字典缓存，加速后续查询
            GetComponentProxyCache();

            // 1. 使用 HashSet 记录所有分类，HashSet 的查询/添加速度接近 O(1)
            var categorySet = new HashSet<string>();

            // 2. 核心嵌套字典：Category -> SubCategory -> List of Components
            var componentsDict = new Dictionary<string, Dictionary<string, List<object>>>();

            // 组件数量
            int totalCount = 0;

            // 初始化数据库表
            ComponentsDB.InitializeAllComponentsTable();

            // 性能优化：清空原表并重建，避免查询已存在 GUID 的开销
            ComponentsDB.ClearAllComponents();

            // 收集所有组件信息（批量插入优化）
            var componentsToInsert = new List<(string componentGuid, string componentName, string nickName, string description, string category, string subCategory, string prototype)>();

            foreach (var proxy in proxies)
            {
                // 1. 跳过过期的组件
                if (proxy.Obsolete) continue;
                // 2. 获取分类，并处理 null 或 纯空格/空字符串 的情况
                string cat = proxy.Desc.Category;
                if (string.IsNullOrWhiteSpace(cat)) continue;
                // 3. 跳过那些没有正式名称的"幽灵"对象
                if (string.IsNullOrWhiteSpace(proxy.Desc.Name)) continue;

                // 4. (可选) 跳过隐藏在草稿箱(Exposability)之外的组件
                if (proxy.Exposure == GH_Exposure.hidden) continue;

                string subCat = proxy.Desc.SubCategory ?? "General";
                string guid = proxy.Guid.ToString();

                // 收集分类名称
                categorySet.Add(cat);

                // 初始化嵌套层级
                if (!componentsDict.TryGetValue(cat, out var subDict))
                {
                    subDict = new Dictionary<string, List<object>>();
                    componentsDict[cat] = subDict;
                }

                if (!subDict.TryGetValue(subCat, out var compList))
                {
                    compList = new List<object>();
                    subDict[subCat] = compList;
                }

                // 填充组件信息
                compList.Add(new
                {
                    guid = guid,
                    name = proxy.Desc.Name,
                    nickname = proxy.Desc.NickName,
                    description = proxy.Desc.Description
                });

                // 收集所有组件（性能优化：不再检查是否已存在）
                componentsToInsert.Add((
                    componentGuid: guid,
                    componentName: proxy.Desc.Name,
                    nickName: proxy.Desc.NickName,
                    description: proxy.Desc.Description,
                    category: cat,
                    subCategory: subCat,
                    prototype: string.Empty
                ));

                totalCount++;
            }

            // 批量插入所有组件（性能优化：一次性插入所有组件）
            if (componentsToInsert.Count > 0)
            {
                ComponentsDB.BulkUpsertComponents(componentsToInsert);
            }

            // 3. outdata 结构进行封装
            var data = new Dictionary<string, object>
            {
                { "AllCategorys", JsonSerializer.Serialize(categorySet.OrderBy(x => x).ToList(), LjsonHelper.JSerializerOptions) },
                { "Count", totalCount },
                { "AllComponents", JsonSerializer.Serialize(componentsDict, LjsonHelper.JSerializerOptions) }
            };

            return new Ljson("AllComponentsInfo", "所有注册的组件信息", JsonSerializer.SerializeToElement(data));
        }

        /// <summary>
        /// 从数据库获取所有组件
        /// </summary>
        /// <returns>数据库中的所有组件信息</returns>
        public static Ljson GetAllComponentsFromDB()
        {
            var categorySet = new HashSet<string>();
            var componentsDict = new Dictionary<string, Dictionary<string, List<object>>>();
            int totalCount = 0;

            // 从数据库查询所有组件
            using (var connection = DatabaseManager.GetConnection())
            {
                string sql = @"
                    SELECT ComponentGuid, ComponentName, NickName, Description, Category, SubCategory
                    FROM ALLCOMPS
                    ORDER BY Category, SubCategory, ComponentName";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string category = reader["Category"].ToString();
                            string subCategory = reader["SubCategory"].ToString();

                            // 收集分类名称
                            categorySet.Add(category);

                            // 初始化嵌套层级
                            if (!componentsDict.TryGetValue(category, out var subDict))
                            {
                                subDict = new Dictionary<string, List<object>>();
                                componentsDict[category] = subDict;
                            }

                            if (!subDict.TryGetValue(subCategory, out var compList))
                            {
                                compList = new List<object>();
                                subDict[subCategory] = compList;
                            }

                            // 填充组件信息
                            compList.Add(new
                            {
                                guid = reader["ComponentGuid"].ToString(),
                                name = reader["ComponentName"].ToString(),
                                nickname = reader["NickName"].ToString(),
                                description = reader["Description"].ToString()
                            });

                            totalCount++;
                        }
                    }
                }
            }

            // 封装结果
            var data = new Dictionary<string, object>
            {
                { "AllCategorys", JsonSerializer.Serialize(categorySet.OrderBy(x => x).ToList(), LjsonHelper.JSerializerOptions) },
                { "Count", totalCount },
                { "AllComponents", JsonSerializer.Serialize(componentsDict, LjsonHelper.JSerializerOptions) },
                { "UpdateTime", ComponentsDB.GetLastUpdateTime()?.ToString("yyyy-MM-dd HH:mm:ss") ?? "未知" }
            };

            return new Ljson("AllComponentsInfo", "数据库中的所有组件信息", JsonSerializer.SerializeToElement(data));
        }

        // 通过Guid查询组件信息
        public static Ljson FindComponentsByGuid(string guid)
        {
            // 从数据库查询组件信息
            using (var connection = DatabaseManager.GetConnection())
            {
                string sql = @"
                    SELECT ComponentGuid, ComponentName, NickName, Description, Category, SubCategory, Prototype
                    FROM ALLCOMPS
                    WHERE ComponentGuid = @guid";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@guid", guid);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string prototype = reader["Prototype"].ToString();

                            // 检查并更新输入输出信息（如果为空）
                            CheckAndUpdateComponentPrototype(guid, ref prototype);

                            return ComponentLjson(
                                componentGuid: guid,
                                name: reader["ComponentName"].ToString(),
                                nickName: reader["NickName"].ToString(),
                                description: reader["Description"].ToString(),
                                category: reader["Category"].ToString(),
                                subCategory: reader["SubCategory"].ToString(),
                                prototype: prototype
                            );
                        }
                    }
                }
            }

            return null;
        }
        
        // 通过名称查询组件信息
        public static Ljson FindComponentsByName(string name)
        {
            // 从数据库查询第一个匹配的组件信息
            using (var connection = DatabaseManager.GetConnection())
            {
                /*ORDER BY ComponentName
                按组件名称排序
                如果有多个匹配结果，按名称排序
                LIMIT 1
                只返回第一条结果
                即使有多个匹配，也只返回一个
                */
                string sql = @"
                    SELECT ComponentGuid, ComponentName, NickName, Description, Category, SubCategory, Prototype
                    FROM ALLCOMPS
                    WHERE ComponentName = @name COLLATE NOCASE
                    ORDER BY ComponentName
                    LIMIT 1";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@name", name);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string guid = reader["ComponentGuid"].ToString();
                            string prototype = reader["Prototype"].ToString();

                            // 检查并更新输入输出信息（如果为空）
                            CheckAndUpdateComponentPrototype(guid, ref prototype);

                            return ComponentLjson(
                                componentGuid: guid,
                                name: reader["ComponentName"].ToString(),
                                nickName: reader["NickName"].ToString(),
                                description: reader["Description"].ToString(),
                                category: reader["Category"].ToString(),
                                subCategory: reader["SubCategory"].ToString(),
                                prototype: prototype
                            );
                        }
                    }
                }
            }

            return null;
        }

        public static string FindComponentsGuidByName(string name)
        {
            // 从数据库查询第一个匹配的组件信息
            using (var connection = DatabaseManager.GetConnection())
            {
                /*ORDER BY ComponentName
                按组件名称排序
                如果有多个匹配结果，按名称排序
                LIMIT 1
                只返回第一条结果
                即使有多个匹配，也只返回一个
                */
                string sql = @"
                    SELECT ComponentGuid
                    FROM ALLCOMPS
                    WHERE ComponentName = @name COLLATE NOCASE
                    ORDER BY ComponentName
                    LIMIT 1";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@name", name);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader["ComponentGuid"].ToString();
                        }
                    }
                }
            }

            return null;
        }

        // 通过分类和名称搜索组件（只返回第一个匹配的）
        public static Ljson FindComponentsByCategory(string category, string subCategory, string name)
        {
            // 构建 SQL 查询
            string sql = @"SELECT ComponentGuid, ComponentName, NickName, Description, Category, SubCategory, Prototype
                FROM ALLCOMPS
                WHERE 1=1";

            var conditions = new List<string>();
            var parameters = new Dictionary<string, object>();

            // 添加分类过滤条件
            if (!string.IsNullOrWhiteSpace(category))
            {
                conditions.Add("Category = @category COLLATE NOCASE");
                parameters["@category"] = category;
            }

            // 添加子分类过滤条件
            if (!string.IsNullOrWhiteSpace(subCategory))
            {
                conditions.Add("SubCategory = @subCategory COLLATE NOCASE");
                parameters["@subCategory"] = subCategory;
            }

            // 添加名称过滤条件（精确匹配，不区分大小写）
            if (!string.IsNullOrWhiteSpace(name))
            {
                conditions.Add("(ComponentName = @name COLLATE NOCASE OR NickName = @name COLLATE NOCASE)");
                parameters["@name"] = name;
            }

            // 组合条件
            if (conditions.Count > 0)
            {
                sql += " AND " + string.Join(" AND ", conditions);
            }

            sql += " ORDER BY Category, SubCategory, ComponentName LIMIT 1";

            // 执行查询
            using (var connection = DatabaseManager.GetConnection())
            {
                using (var command = new SQLiteCommand(sql, connection))
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string guid = reader["ComponentGuid"].ToString();
                            string prototype = reader["Prototype"].ToString();

                            // 检查并更新输入输出信息（如果为空）
                            CheckAndUpdateComponentPrototype(guid, ref prototype);

                            return ComponentLjson(
                                componentGuid: guid,
                                name: reader["ComponentName"].ToString(),
                                nickName: reader["NickName"].ToString(),
                                description: reader["Description"].ToString(),
                                category: reader["Category"].ToString(),
                                subCategory: reader["SubCategory"].ToString(),
                                prototype: prototype
                            );
                        }
                    }
                }
            }

            return null;
        }


        // 通过名称搜索组件，可以模糊匹配
        public static List<Ljson> SearchComponentsByName(string name)
        {
            var result = new List<Ljson>();

            // 从数据库模糊查询组件信息
            using (var connection = DatabaseManager.GetConnection())
            {
                string sql = @"                    SELECT ComponentGuid, ComponentName, NickName, Description, Category, SubCategory, Prototype
                    FROM ALLCOMPS
                    WHERE ComponentName LIKE @name COLLATE NOCASE OR NickName LIKE @name COLLATE NOCASE OR Description LIKE @name COLLATE NOCASE
                    ORDER BY ComponentName";

                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@name", $"%{name}%");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string guid = reader["ComponentGuid"].ToString();
                            string prototype = reader["Prototype"].ToString();

                            // 检查并更新输入输出信息（如果为空）
                            CheckAndUpdateComponentPrototype(guid, ref prototype);

                            result.Add(ComponentLjson(
                                componentGuid: guid,
                                name: reader["ComponentName"].ToString(),
                                nickName: reader["NickName"].ToString(),
                                description: reader["Description"].ToString(),
                                category: reader["Category"].ToString(),
                                subCategory: reader["SubCategory"].ToString(),
                                prototype: prototype
                            ));
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 检查并更新组件的输入输出信息
        /// </summary>
        /// <param name="componentGuid">组件 GUID</param>
        /// <param name="prototype">函数签名（引用传递）</param>
        private static void CheckAndUpdateComponentPrototype(string componentGuid, ref string prototype)
        {
            // 检查是否为空
            bool needsUpdate = string.IsNullOrWhiteSpace(prototype);
            if (needsUpdate)
            {
                // 获取组件的 prototype 信息
                string newPrototype = GetComponentPrototype(componentGuid);

                // 更新引用
                if (!string.IsNullOrWhiteSpace(newPrototype))
                {
                    prototype = newPrototype;

                    // 如果获取到了新信息，更新数据库
                    ComponentsDB.UpdateComponentPrototype(componentGuid, prototype);

                }
            }
        }
        
        /// <summary>
        /// 获取组件的函数签名（通过创建组件实例）
        /// </summary>
        /// <param name="componentGuid">组件 GUID</param>
        /// <returns>Python 风格的函数签名字符串</returns>
        private static string GetComponentPrototype(string componentGuid)
        {
            try
            {
                // 方法1：尝试使用 EmitObjectProxy 直接获取代理（更快）
                var proxy = Instances.ComponentServer.EmitObjectProxy(new Guid(componentGuid));
                if (proxy != null)
                {
                    var type = proxy.Type;
                    if (type != null)
                    {
                        IGH_Component component = null;
                        try
                        {
                            // 使用反射创建实例
                            component = Activator.CreateInstance(type) as IGH_Component;
                            if (component != null)
                            {
                                // 生成函数签名
                                string functionName = FormatFunctionName(proxy.Desc.Category, proxy.Desc.Name);
                                string inputs = ParamExchange.ParamsToPrototype(component.Params.Input);
                                string outputs = ParamExchange.ParamsToPrototype(component.Params.Output);

                                // 格式：Category_ComponentName(input1, input2) -> (output1, output2)
                                string signature = $"{functionName}({inputs})";

                                // 如果有输出，添加返回值部分
                                if (!string.IsNullOrWhiteSpace(outputs))
                                {
                                    signature += $" -> ({outputs})";
                                }

                                return signature;
                            }
                        }
                        finally
                        {
                            // 手动释放资源
                            if (component is IDisposable d)
                                d.Dispose();
                        }
                        IGH_Param param = null;
                        try
                        {
                            // 使用反射创建实例
                            param = Activator.CreateInstance(type) as IGH_Param;
                            if (param != null)
                            {
                                return "param item";
                            }
                        }
                        finally
                        {
                            // 手动释放资源
                            if (param is IDisposable d)
                                d.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EmitObjectProxy 方法失败 ({componentGuid}): {ex.Message}，回退到字典缓存方案");
            }

            // 方法2：回退到字典缓存方案（兼容性更好）
            try
            {
                var cache = GetComponentProxyCache();
                if (cache.TryGetValue(componentGuid, out var proxy))
                {
                    var component = proxy.CreateInstance() as IGH_Component;
                    if (component != null)
                    {
                        // 生成函数签名
                        string functionName = FormatFunctionName(proxy.Desc.Category, proxy.Desc.Name);
                        string inputs = ParamExchange.ParamsToPrototype(component.Params.Input);
                        string outputs = ParamExchange.ParamsToPrototype(component.Params.Output);

                        // 格式：Category_ComponentName(input1, input2) -> (output1, output2)
                        string signature = $"{functionName}({inputs})";

                        // 如果有输出，添加返回值部分
                        if (!string.IsNullOrWhiteSpace(outputs))
                        {
                            signature += $" -> ({outputs})";
                        }

                        return signature;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取组件函数签名失败 ({componentGuid}): {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 格式化函数名称：Category_ComponentName
        /// </summary>
        /// <param name="category">组件分类</param>
        /// <param name="componentName">组件名称</param>
        /// <returns>格式化后的函数名</returns>
        private static string FormatFunctionName(string category, string componentName)
        {
            // 移除空格和特殊字符，使用下划线连接
            string cleanCategory = string.Join("_", category.Split(new[] { ' ', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries));
            string cleanName = string.Join("_", componentName.Split(new[] { ' ', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries));

            return $"{cleanCategory}_{cleanName}";
        }

    }

}