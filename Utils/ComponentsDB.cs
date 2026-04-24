using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;

namespace GrasshopperSever.Utils
{
    internal class ComponentsDB
    {
        /// <summary>
        /// 初始化 AllComponents 表（使用主数据库 ComponentsInfo.db）
        /// </summary>
        public static void InitializeAllComponentsTable()
        {
            string createTableSql = @"
                CREATE TABLE IF NOT EXISTS ALLCOMPS (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ComponentGuid TEXT NOT NULL UNIQUE,
                    ComponentName TEXT NOT NULL,
                    NickName TEXT,
                    Description TEXT,
                    Category TEXT NOT NULL,
                    SubCategory TEXT NOT NULL,
                    Prototype TEXT DEFAULT ''
                )";

            DatabaseManager.CreateTable(
                tableName: "ALLCOMPS",
                createTableSql: createTableSql,
                description: "存储组件详细信息"
            );
        }

        /// <summary>
        /// 插入或更新组件信息
        /// </summary>
        public static bool UpsertComponent(
            string componentGuid,
            string componentName,
            string nickName = null,
            string description = null,
            string category = null,
            string subCategory = null,
            string prototype = null)
        {
            string sql = @"
                INSERT OR REPLACE INTO ALLCOMPS 
                (ComponentGuid, ComponentName, NickName, Description, Category, SubCategory, Prototype)
                VALUES (@guid, @name, @nickName, @description, @category, @subCategory, @prototype)";

            var parameters = new Dictionary<string, object>
            {
                { "@guid", componentGuid },
                { "@name", componentName },
                { "@nickName", nickName ?? string.Empty },
                { "@description", description ?? string.Empty },
                { "@category", category ?? string.Empty },
                { "@subCategory", subCategory ?? string.Empty },
                { "@prototype", prototype ?? string.Empty }
            };

            return DatabaseManager.ExecuteCommandWithTimestamp("ALLCOMPS", sql, parameters) >= 0;
        }

        /// <summary>
        /// 根据GUID获取组件信息
        /// </summary>
        public static (string name, string nickName, string description, string category, string subCategory, string prototype) GetComponentByGuid(string componentGuid)
        {
            string sql = @"
                SELECT ComponentName, NickName, Description, Category, SubCategory, Prototype 
                FROM ALLCOMPS 
                WHERE ComponentGuid = @guid";

            using (var connection = DatabaseManager.GetConnection())
            {
                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@guid", componentGuid);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return (
                                reader["ComponentName"].ToString(),
                                reader["NickName"].ToString(),
                                reader["Description"].ToString(),
                                reader["Category"].ToString(),
                                reader["SubCategory"].ToString(),
                                reader["Prototype"].ToString()
                            );
                        }
                    }
                }
            }

            return (null, null, null, null, null, null);
        }

        /// <summary>
        /// 根据名称搜索组件
        /// </summary>
        public static List<(string guid, string name, string nickName)> SearchAllComponentsByName(string searchTerm)
        {
            var components = new List<(string, string, string)>();
            string sql = @"
                SELECT ComponentGuid, ComponentName, NickName 
                FROM ALLCOMPS 
                WHERE ComponentName LIKE @searchTerm
                ORDER BY ComponentName";

            using (var connection = DatabaseManager.GetConnection())
            {
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    components.Add((
                        reader["ComponentGuid"].ToString(),
                        reader["ComponentName"].ToString(),
                        reader["NickName"].ToString()
                    ));
                }
            }

            return components;
        }

        /// <summary>
        /// 根据分类和子分类获取组件列表
        /// </summary>
        public static List<(string guid, string name, string nickName)> GetAllComponentsByCategory(string category, string subCategory = null)
        {
            var components = new List<(string, string, string)>();
            string sql;

            if (string.IsNullOrEmpty(subCategory))
            {
                sql = @"
                    SELECT ComponentGuid, ComponentName, NickName 
                    FROM ALLCOMPS 
                    WHERE Category = @category
                    ORDER BY ComponentName";
            }
            else
            {
                sql = @"
                    SELECT ComponentGuid, ComponentName, NickName 
                    FROM ALLCOMPS 
                    WHERE Category = @category AND SubCategory = @subCategory
                    ORDER BY ComponentName";
            }

            using (var connection = DatabaseManager.GetConnection())
            {
                using (var command = new SQLiteCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@category", category);

                    if (!string.IsNullOrEmpty(subCategory))
                    {
                        command.Parameters.AddWithValue("@subCategory", subCategory);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            components.Add((
                                reader["ComponentGuid"].ToString(),
                                reader["ComponentName"].ToString(),
                                reader["NickName"].ToString()
                            ));
                        }
                    }
                }
            }

            return components;
        }

        /// <summary>
        /// 更新组件的输入输出信息
        /// </summary>
        public static bool UpdateComponentPrototype(string componentGuid, string prototype = null)
        {
            if (prototype == null)
                return false;

            string sql = "UPDATE ALLCOMPS SET Prototype = @prototype WHERE ComponentGuid = @guid";
            var parameters = new Dictionary<string, object>
            {
                { "@prototype", prototype },
                { "@guid", componentGuid }
            };

            return DatabaseManager.ExecuteCommandWithTimestamp("ALLCOMPS", sql, parameters) >= 0;
        }

        /// <summary>
        /// 清空 AllComponents 表
        /// </summary>
        public static bool ClearAllComponents()
        {
            string sql = "DELETE FROM ALLCOMPS";
            return DatabaseManager.ExecuteCommandWithTimestamp("ALLCOMPS", sql) >= 0;
        }

        /// <summary>
        /// 批量插入或更新组件信息（使用事务优化性能）
        /// </summary>
        /// <param name="components">组件信息列表</param>
        /// <returns>成功插入/更新的组件数量</returns>
        public static int BulkUpsertComponents(List<(string componentGuid, string componentName, string nickName,
            string description, string category, string subCategory, string prototype)> components)
        {
            if (components == null || components.Count == 0)
                return 0;

            int successCount = 0;

            // 使用 WAL 模式和连接池优化，避免数据库锁定
            using (var connection = new SQLiteConnection($"Data Source={DatabaseManager.DatabasePath};Version=3;Default Timeout=30;Journal Mode=WAL;Pooling=True;"))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string sql = @"
                            INSERT OR REPLACE INTO ALLCOMPS
                            (ComponentGuid, ComponentName, NickName, Description, Category, SubCategory, Prototype)
                            VALUES (@guid, @name, @nickName, @description, @category, @subCategory, @prototype)";

                        using (var command = new SQLiteCommand(sql, connection, transaction))
                        {
                            // 预编译参数
                            command.Parameters.Add(new SQLiteParameter("@guid"));
                            command.Parameters.Add(new SQLiteParameter("@name"));
                            command.Parameters.Add(new SQLiteParameter("@nickName"));
                            command.Parameters.Add(new SQLiteParameter("@description"));
                            command.Parameters.Add(new SQLiteParameter("@category"));
                            command.Parameters.Add(new SQLiteParameter("@subCategory"));
                            command.Parameters.Add(new SQLiteParameter("@prototype"));

                            foreach (var comp in components)
                            {
                                command.Parameters["@guid"].Value = comp.componentGuid;
                                command.Parameters["@name"].Value = comp.componentName;
                                command.Parameters["@nickName"].Value = comp.nickName ?? string.Empty;
                                command.Parameters["@description"].Value = comp.description ?? string.Empty;
                                command.Parameters["@category"].Value = comp.category ?? string.Empty;
                                command.Parameters["@subCategory"].Value = comp.subCategory ?? string.Empty;
                                command.Parameters["@prototype"].Value = comp.prototype ?? string.Empty;

                                successCount += command.ExecuteNonQuery();
                            }
                        }

                        // 提交事务
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // 回滚事务
                        transaction.Rollback();
                        Debug.WriteLine($"批量插入组件失败: {ex.Message}");
                        return -1;
                    }
                }
            }

            // 在连接关闭后，再更新表时间戳（避免锁冲突）
            DatabaseManager.UpdateTableTimestamp("ALLCOMPS");

            return successCount;
        }

        /// <summary>
        /// 获取表最后更新时间
        /// </summary>
        public static DateTime? GetLastUpdateTime()
        {
            return DatabaseManager.GetTableTimestamp("ALLCOMPS");
        }

        /// <summary>
        /// 获取组件总数
        /// </summary>
        public static int GetComponentCount()
        {
            string sql = "SELECT COUNT(*) FROM ALLCOMPS";

            using (var connection = DatabaseManager.GetConnection())
            {
                using (var command = new SQLiteCommand(sql, connection))
                {
                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }
    }
}
