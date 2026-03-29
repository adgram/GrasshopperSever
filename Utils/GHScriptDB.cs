using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;

namespace GrasshopperSever.Utils
{
    public class GHScriptDB
    {
        /// <summary>
        /// 初始化 GHScript 修改记录表
        /// </summary>
        private static void InitializeScriptModifyTable()
        {
            try
            {
                if (!DatabaseManager.TableExists("GHScriptModifyHistory"))
                {
                    string createTableSql = @"
                        CREATE TABLE GHScriptModifyHistory (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            InstanceGuid TEXT NOT NULL,
                            ComponentGuid TEXT NOT NULL,
                            ComponentName TEXT,
                            ModifyType TEXT NOT NULL,
                            ModifyContent TEXT,
                            Description TEXT,
                            ModifyTime DATETIME DEFAULT CURRENT_TIMESTAMP
                        )";

                    if (DatabaseManager.CreateTable("GHScriptModifyHistory", createTableSql, "存储GHScript组件修改历史"))
                    {
                        Debug.WriteLine("GHScript修改记录表创建成功");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化GHScript修改记录表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录组件修改历史
        /// </summary>
        /// <param name="instanceGuid">实例GUID</param>
        /// <param name="componentGuid">组件GUID</param>
        /// <param name="componentName">组件名称</param>
        /// <param name="modifyType">修改类型</param>
        /// <param name="modifyContent">修改内容（JSON格式）</param>
        /// <param name="description">描述</param>
        public static void RecordModifyHistory(string instanceGuid, string componentGuid, string componentName, string modifyType, string modifyContent, string description = null)
        {
            try
            {
                InitializeScriptModifyTable();

                using (var connection = DatabaseManager.GetConnection())
                {
                    string sql = @"
                        INSERT INTO GHScriptModifyHistory 
                        (InstanceGuid, ComponentGuid, ComponentName, ModifyType, ModifyContent, Description)
                        VALUES (@instanceGuid, @componentGuid, @componentName, @modifyType, @modifyContent, @description)";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@instanceGuid", instanceGuid);
                        command.Parameters.AddWithValue("@componentGuid", componentGuid);
                        command.Parameters.AddWithValue("@componentName", componentName ?? string.Empty);
                        command.Parameters.AddWithValue("@modifyType", modifyType);
                        command.Parameters.AddWithValue("@modifyContent", modifyContent ?? string.Empty);
                        command.Parameters.AddWithValue("@description", description ?? string.Empty);

                        command.ExecuteNonQuery();
                    }
                }

                // 更新表时间戳
                DatabaseManager.UpdateTableTimestamp("GHScriptModifyHistory");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"记录修改历史失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取组件的修改历史
        /// </summary>
        /// <param name="instanceGuid">实例GUID</param>
        /// <returns>修改历史列表</returns>
        public static List<Dictionary<string, object>> GetModifyHistory(string instanceGuid)
        {
            var history = new List<Dictionary<string, object>>();

            try
            {
                InitializeScriptModifyTable();

                using (var connection = DatabaseManager.GetConnection())
                {
                    string sql = @"
                        SELECT Id, ComponentGuid, ComponentName, ModifyType, ModifyContent, Description, ModifyTime
                        FROM GHScriptModifyHistory
                        WHERE InstanceGuid = @instanceGuid
                        ORDER BY ModifyTime DESC
                        LIMIT 100";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@instanceGuid", instanceGuid);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                history.Add(new Dictionary<string, object>
                                {
                                    { "Id", reader["Id"].ToString() },
                                    { "ComponentGuid", reader["ComponentGuid"].ToString() },
                                    { "ComponentName", reader["ComponentName"].ToString() },
                                    { "ModifyType", reader["ModifyType"].ToString() },
                                    { "ModifyContent", reader["ModifyContent"].ToString() },
                                    { "Description", reader["Description"].ToString() },
                                    { "ModifyTime", reader["ModifyTime"].ToString() }
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取修改历史失败: {ex.Message}");
            }

            return history;
        }

    }
}
