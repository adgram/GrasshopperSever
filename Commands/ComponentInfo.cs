using Grasshopper;
using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GrasshopperSever.Commands
{
    // 文档信息
    public static class ComponentInfo
    {
        // 组件代理字典缓存（GUID -> Proxy），用于快速查找
        private static Dictionary<string, IGH_ObjectProxy> _componentProxyCache;
        private static readonly object _cacheLock = new object();

        /// <summary>
        /// 获取组件代理字典缓存
        /// </summary>
        private static Dictionary<string, IGH_ObjectProxy> GetComponentProxyCache()
        {
            if (_componentProxyCache == null)
            {
                lock (_cacheLock)
                {
                    if (_componentProxyCache == null)
                    {
                        var server = Grasshopper.Instances.ComponentServer;
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
        public static JList GetAllComponentsNested()
        {
            var server = Grasshopper.Instances.ComponentServer;
            var proxies = server.ObjectProxies;

            // 预构建组件代理字典缓存，加速后续查询
            GetComponentProxyCache();

            // 1. 使用 HashSet 记录所有分类，HashSet 的查询/添加速度接近 O(1)
            var categorySet = new HashSet<string>();

            // 2. 核心嵌套字典：Category -> SubCategory -> List of Components
            var componentsDict = new Dictionary<string, Dictionary<string, List<object>>>();

            // 组件数量
            int totalCount = 0;

            // 初始化数据库表
            AllComponentsDB.InitializeAllComponentsTable();

            // 性能优化：清空原表并重建，避免查询已存在 GUID 的开销
            AllComponentsDB.ClearAllComponents();

            // 收集所有组件信息（批量插入优化）
            var componentsToInsert = new List<(string componentGuid, string componentName, string nickName,
                string description, string category, string subCategory, string inputs, string outputs)>();

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
                    inputs: string.Empty,
                    outputs: string.Empty
                ));

                totalCount++;
            }

            // 批量插入所有组件（性能优化：一次性插入所有组件）
            if (componentsToInsert.Count > 0)
            {
                AllComponentsDB.BulkUpsertComponents(componentsToInsert);
            }

            // 3. outdata 结构进行封装
            var outData = new JList();
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            outData.Add(new JData("AllCategorys", "所有分类", JsonSerializer.Serialize(categorySet.OrderBy(x => x).ToList(), options)));
            outData.Add(new JData("Count", "组件数量", totalCount.ToString()));
            outData.Add(new JData("AllComponents",
                "所有注册的组件，string({category : {subCategory : dict{guid, name, nickname, description}}}})",
                JsonSerializer.Serialize(componentsDict, options)));
            return outData;
        }

        /// <summary>
        /// 从数据库获取所有组件
        /// </summary>
        /// <returns>数据库中的所有组件信息</returns>
        public static JList GetAllComponentsFromDB()
        {
            var categorySet = new HashSet<string>();
            var componentsDict = new Dictionary<string, Dictionary<string, List<object>>>();
            int totalCount = 0;

            // 从数据库查询所有组件
            using (var connection = DatabaseManager.GetConnection())
            {
                string sql = @"
                    SELECT ComponentGuid, ComponentName, NickName, Description, Category, SubCategory
                    FROM AllComponents
                    ORDER BY Category, SubCategory, ComponentName";

                using (var command = new System.Data.SQLite.SQLiteCommand(sql, connection))
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
            var outData = new JList();
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            outData.Add(new JData("AllCategorys", "所有分类", JsonSerializer.Serialize(categorySet.OrderBy(x => x).ToList(), options)));
            outData.Add(new JData("Count", "组件数量", totalCount.ToString()));
            outData.Add(new JData("AllComponents",
                "数据库中的所有组件，string({category : {subCategory : dict{guid, name, nickname, description}}}})",
                JsonSerializer.Serialize(componentsDict, options)));
            outData.Add(new JData("UpdateTime", "数据库最后更新时间", AllComponentsDB.GetLastUpdateTime()?.ToString("yyyy-MM-dd HH:mm:ss") ?? "未知"));

            return outData;
        }

        // 通过Guid查询组件信息
        public static JList FindComponentsByGuid(string guid)
        {
            // 从数据库查询组件信息
            using (var connection = DatabaseManager.GetConnection())
            {
                string sql = @"
                    SELECT ComponentGuid, ComponentName, NickName, Description, Category, SubCategory, Inputs, Outputs
                    FROM AllComponents
                    WHERE ComponentGuid = @guid";

                using (var command = new System.Data.SQLite.SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@guid", guid);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string inputs = reader["Inputs"].ToString();
                            string outputs = reader["Outputs"].ToString();

                            // 检查并更新输入输出信息（如果为空）
                            CheckAndUpdateComponentIO(guid, ref inputs, ref outputs);

                            return JList.ComponentJList(
                                componentGuid: reader["ComponentGuid"].ToString(),
                                instanceGuid: "",
                                name: reader["ComponentName"].ToString(),
                                nickName: reader["NickName"].ToString(),
                                description: reader["Description"].ToString(),
                                category: reader["Category"].ToString(),
                                subCategory: reader["SubCategory"].ToString(),
                                position: "",
                                state: "",
                                inputs: inputs,
                                outputs: outputs
                            );
                        }
                    }
                }
            }

            return null;
        }
        
        // 通过名称查询组件信息
        public static JList FindComponentsByName(string name)
        {
            // 从数据库查询第一个匹配的组件信息
            using (var connection = DatabaseManager.GetConnection())
            {
                string sql = @"
                    SELECT ComponentGuid, ComponentName, NickName, Description, Category, SubCategory, Inputs, Outputs
                    FROM AllComponents
                    WHERE ComponentName = @name COLLATE NOCASE OR NickName = @name COLLATE NOCASE
                    ORDER BY ComponentName
                    LIMIT 1";

                using (var command = new System.Data.SQLite.SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@name", name);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string guid = reader["ComponentGuid"].ToString();
                            string inputs = reader["Inputs"].ToString();
                            string outputs = reader["Outputs"].ToString();

                            // 检查并更新输入输出信息（如果为空）
                            CheckAndUpdateComponentIO(guid, ref inputs, ref outputs);

                            return JList.ComponentJList(
                                componentGuid: guid,
                                instanceGuid: "",
                                name: reader["ComponentName"].ToString(),
                                nickName: reader["NickName"].ToString(),
                                description: reader["Description"].ToString(),
                                category: reader["Category"].ToString(),
                                subCategory: reader["SubCategory"].ToString(),
                                position: "",
                                state: "",
                                inputs: inputs,
                                outputs: outputs
                            );
                        }
                    }
                }
            }

            return null;
        }
        
        // 通过分类和名称搜索组件（只返回第一个匹配的）
        public static JList FindComponentsByCategory(string category, string subCategory, string name)
        {
            // 构建 SQL 查询
            string sql = @"
                SELECT ComponentGuid, ComponentName, NickName, Description, Category, SubCategory, Inputs, Outputs
                FROM AllComponents
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
                using (var command = new System.Data.SQLite.SQLiteCommand(sql, connection))
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
                            string inputs = reader["Inputs"].ToString();
                            string outputs = reader["Outputs"].ToString();

                            // 检查并更新输入输出信息（如果为空）
                            CheckAndUpdateComponentIO(guid, ref inputs, ref outputs);

                            return JList.ComponentJList(
                                componentGuid: guid,
                                instanceGuid: "",
                                name: reader["ComponentName"].ToString(),
                                nickName: reader["NickName"].ToString(),
                                description: reader["Description"].ToString(),
                                category: reader["Category"].ToString(),
                                subCategory: reader["SubCategory"].ToString(),
                                position: "",
                                state: "",
                                inputs: inputs,
                                outputs: outputs
                            );
                        }
                    }
                }
            }

            return null;
        }


        // 通过名称搜索组件，可以模糊匹配
        public static List<JList> SearchComponentsByName(string name)
        {
            var result = new List<JList>();

            // 从数据库模糊查询组件信息
            using (var connection = DatabaseManager.GetConnection())
            {
                string sql = @"
                    SELECT ComponentGuid, ComponentName, NickName, Description, Category, SubCategory, Inputs, Outputs
                    FROM AllComponents
                    WHERE ComponentName LIKE @name COLLATE NOCASE OR NickName LIKE @name COLLATE NOCASE OR Description LIKE @name COLLATE NOCASE
                    ORDER BY ComponentName";

                using (var command = new System.Data.SQLite.SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@name", $"%{name}%");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string guid = reader["ComponentGuid"].ToString();
                            string inputs = reader["Inputs"].ToString();
                            string outputs = reader["Outputs"].ToString();

                            // 检查并更新输入输出信息（如果为空）
                            CheckAndUpdateComponentIO(guid, ref inputs, ref outputs);

                            result.Add(JList.ComponentJList(
                                componentGuid: guid,
                                instanceGuid: "",
                                name: reader["ComponentName"].ToString(),
                                nickName: reader["NickName"].ToString(),
                                description: reader["Description"].ToString(),
                                category: reader["Category"].ToString(),
                                subCategory: reader["SubCategory"].ToString(),
                                position: "",
                                state: "",
                                inputs: inputs,
                                outputs: outputs
                            ));
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 获取组件的输入输出信息（通过创建组件实例）
        /// </summary>
        /// <param name="componentGuid">组件 GUID</param>
        /// <returns>包含 inputs 和 outputs JSON 字符串的元组</returns>
        private static (string inputs, string outputs) GetComponentIOInfo(string componentGuid)
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
                            // 使用反射创建实例，可能比 CreateInstance 更轻量
                            component = Activator.CreateInstance(type) as IGH_Component;
                            if (component != null)
                            {
                                var inputsJson = ParamExchange.SerializeParamDefinitions(component.Params.Input);
                                var outputsJson = ParamExchange.SerializeParamDefinitions(component.Params.Output);

                                return (inputsJson, outputsJson);
                            }
                        }
                        finally
                        {
                            // 手动释放资源
                            if (component is IDisposable d)
                                d.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmitObjectProxy 方法失败 ({componentGuid}): {ex.Message}，回退到字典缓存方案");
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
                        var inputsJson = ParamExchange.SerializeParamDefinitions(component.Params.Input);
                        var outputsJson = ParamExchange.SerializeParamDefinitions(component.Params.Output);

                        return (inputsJson, outputsJson);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取组件 IO 信息失败 ({componentGuid}): {ex.Message}");
            }

            return ("", "");
        }

        /// <summary>
        /// 检查并更新组件的输入输出信息
        /// </summary>
        /// <param name="componentGuid">组件 GUID</param>
        /// <param name="inputs">输入信息（引用传递）</param>
        /// <param name="outputs">输出信息（引用传递）</param>
        private static void CheckAndUpdateComponentIO(string componentGuid, ref string inputs, ref string outputs)
        {
            // 检查是否为空
            bool needsUpdate = string.IsNullOrWhiteSpace(inputs) || string.IsNullOrWhiteSpace(outputs);

            if (needsUpdate)
            {
                var (newInputs, newOutputs) = GetComponentIOInfo(componentGuid);

                // 更新引用
                if (!string.IsNullOrWhiteSpace(newInputs))
                    inputs = newInputs;
                if (!string.IsNullOrWhiteSpace(newOutputs))
                    outputs = newOutputs;

                // 如果获取到了新信息，更新数据库
                if (!string.IsNullOrWhiteSpace(newInputs) || !string.IsNullOrWhiteSpace(newOutputs))
                {
                    AllComponentsDB.UpdateComponentIO(componentGuid, inputs, outputs);
                }
            }
        }


    }

    public static class ParamExchange
    {
        /// <summary>
        /// 核心工厂方法：根据 ParamGuid 创建一个空的 IGH_Param 实例
        /// </summary>
        public static IGH_Param CreateParamFromGuid(string paramGuidStr)
        {
            if (!Guid.TryParse(paramGuidStr, out Guid id)) return null;

            // 在 Grasshopper 对象库中查找对应的代理对象
            var proxy = Instances.ComponentServer.EmitObjectProxy(id);
            if (proxy == null) return null;

            // 实例化对象
            IGH_Param param = proxy.CreateInstance() as IGH_Param;
            return param;
        }

        /// <summary>
        /// 反序列化：将 JList 中的字符串数据还原到 IGH_Param 实例中
        /// </summary>
        public static void FillParamFromJList(JList data, IGH_Param targetParam)
        {
            if (targetParam == null) return;

            // 1. 名称
            targetParam.Name = data.GetParameter("Name");
            targetParam.NickName = data.GetParameter("NickName");
            targetParam.Description = data.GetParameter("Description");

            // 2. 布尔属性 (从字符串还原)
            if (bool.TryParse(data.GetParameter("Optional"), out bool opt))
                targetParam.Optional = opt;

            if (bool.TryParse(data.GetParameter("Reverse"), out bool rev))
                targetParam.Reverse = rev;

            if (bool.TryParse(data.GetParameter("Simplify"), out bool sim))
                targetParam.Simplify = sim;

            // 3. 枚举属性 (使用 Enum.TryParse 忽略大小写还原)
            if (Enum.TryParse(data.GetParameter("Access"), true, out GH_ParamAccess acc))
                targetParam.Access = acc;

            if (Enum.TryParse(data.GetParameter("Mapping"), true, out GH_DataMapping map))
                targetParam.DataMapping = map;
        }

        public static JList ParamToJList(IGH_Param param)
        {
            return JList.ParamJList(
                param.ComponentGuid.ToString(),   // 电池类型识别码
                param.InstanceGuid.ToString(),    // 画布实例识别码
                param.Name,
                param.NickName,
                param.Description,
                param.TypeName,
                param.Optional.ToString(),
                param.Access.ToString(),          // 枚举转字符串 (item, list, tree)
                param.DataMapping.ToString(),     // 枚举转字符串 (none, flatten, graft)
                param.Reverse.ToString(),
                param.Simplify.ToString(),
                JsonSerializer.Serialize(param.Sources.Select(s => s.InstanceGuid.ToString())),
                JsonSerializer.Serialize(param.Recipients.Select(s => s.InstanceGuid.ToString()))
            );
        }

        /// <summary>
        /// 从JList创建IGH_Param实例
        /// </summary>
        private static IGH_Param ParamFromJList(JList jlist)
        {
            if (jlist == null || jlist.IsEmpty)
                return null;

            try
            {
                // 从JList中提取ParamGuid创建参数
                string paramGuid = jlist.GetParameter("ParamGuid");
                IGH_Param param = null;

                if (!string.IsNullOrWhiteSpace(paramGuid))
                {
                    param = CreateParamFromGuid(paramGuid);
                }

                if (param == null)
                {
                    param = new Grasshopper.Kernel.Parameters.Param_GenericObject();
                }

                // 填充参数属性
                param.Name = jlist.GetParameter("Name") ?? "";
                param.NickName = jlist.GetParameter("NickName") ?? "";
                param.Description = jlist.GetParameter("Description") ?? "";

                // 布尔属性
                if (bool.TryParse(jlist.GetParameter("Optional"), out bool opt))
                    param.Optional = opt;
                if (bool.TryParse(jlist.GetParameter("Reverse"), out bool rev))
                    param.Reverse = rev;
                if (bool.TryParse(jlist.GetParameter("Simplify"), out bool sim))
                    param.Simplify = sim;

                // 枚举属性
                if (Enum.TryParse(jlist.GetParameter("Access"), true, out GH_ParamAccess acc))
                    param.Access = acc;
                if (Enum.TryParse(jlist.GetParameter("Mapping"), true, out GH_DataMapping map))
                    param.DataMapping = map;

                return param;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从JList创建参数失败: {ex.Message}");
                return null;
            }
        }



        /// <summary>
        /// 获取参数定义信息（不包含连线，用于组件定义）
        /// </summary>
        /// <param name="parameters">参数列表</param>
        /// <returns>参数定义信息JSON字符串</returns>
        public static string SerializeParamDefinitions(IList<IGH_Param> parameters)
        {
            var paramJLists = new List<JList>();
            for (int i = 0; i < parameters.Count; i++)
            {
                var p = parameters[i];
                paramJLists.Add(ParamToJList(p));
            }
            
            return JList.SerializeJListArray(paramJLists);
        }

        /// <summary>
        /// 解析参数定义JSON字符串为IGH_Param列表
        /// 与SerializeParamDefinitions成对使用，解析JList.SerializeJListArray的输出
        /// </summary>
        public static List<IGH_Param> DeserializeParamDefinitions(string json)
        {
            var result = new List<IGH_Param>();

            if (string.IsNullOrWhiteSpace(json))
                return result;

            try
            {
                // 使用JList.ParseJListArray解析SerializeJListArray的输出
                var jlists = JList.ParseJListArray(json);

                // 将每个JList转换为IGH_Param
                foreach (var jlist in jlists)
                {
                    var param = ParamFromJList(jlist);
                    if (param != null)
                        result.Add(param);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析参数定义失败: {ex.Message}");
            }

            return result;
        }
    }
    
}