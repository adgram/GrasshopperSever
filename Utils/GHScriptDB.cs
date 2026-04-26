using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;

namespace GrasshopperSever.Utils
{
    /// <summary>
    /// GHScript 数据库操作类（使用文档数据库）
    /// 数据存储在 gh 文件同目录的 _ghdata.db 文件中
    /// </summary>
    public class GHScriptDB
    {
        /// <summary>
        /// 初始化 GHScript 修改记录表（在文档数据库中）
        /// </summary>
        private static void InitializeScriptModifyTable()
        {
            try
            {
                using var connection = DatabaseManager.GetDocumentConnection();
                // 检查表是否存在
                string checkTable = @"
                        SELECT name FROM sqlite_master
                        WHERE type='table' AND name='GHScriptModifyHistory'";

                using var checkCmd = new SQLiteCommand(checkTable, connection);
                using var reader = checkCmd.ExecuteReader();
                if (!reader.Read())
                {
                    // 表不存在，创建它
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

                    using var createCmd = new SQLiteCommand(createTableSql, connection);
                    createCmd.ExecuteNonQuery();
                    Debug.WriteLine("GHScript修改记录表创建成功");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化GHScript修改记录表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录组件修改历史（存储在文档数据库中）
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

                using var connection = DatabaseManager.GetDocumentConnection();
                string sql = @"
                        INSERT INTO GHScriptModifyHistory
                        (InstanceGuid, ComponentGuid, ComponentName, ModifyType, ModifyContent, Description)
                        VALUES (@instanceGuid, @componentGuid, @componentName, @modifyType, @modifyContent, @description)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@instanceGuid", instanceGuid);
                command.Parameters.AddWithValue("@componentGuid", componentGuid);
                command.Parameters.AddWithValue("@componentName", componentName ?? string.Empty);
                command.Parameters.AddWithValue("@modifyType", modifyType);
                command.Parameters.AddWithValue("@modifyContent", modifyContent ?? string.Empty);
                command.Parameters.AddWithValue("@description", description ?? string.Empty);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"记录修改历史失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取组件的修改历史（从文档数据库中）
        /// </summary>
        /// <param name="instanceGuid">实例GUID</param>
        /// <returns>修改历史列表</returns>
        public static List<Dictionary<string, object>> GetModifyHistory(string instanceGuid)
        {
            var history = new List<Dictionary<string, object>>();

            try
            {
                InitializeScriptModifyTable();

                using var connection = DatabaseManager.GetDocumentConnection();
                string sql = @"
                        SELECT Id, ComponentGuid, ComponentName, ModifyType, ModifyContent, Description, ModifyTime
                        FROM GHScriptModifyHistory
                        WHERE InstanceGuid = @instanceGuid
                        ORDER BY ModifyTime DESC
                        LIMIT 100";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@instanceGuid", instanceGuid);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    history.Add(new Dictionary<string, object>
                        {
                            { "Id", reader.IsDBNull(0) ? null : reader.GetInt64(0) },
                            { "ComponentGuid", reader.IsDBNull(1) ? null : reader.GetString(1) },
                            { "ComponentName", reader.IsDBNull(2) ? null : reader.GetString(2) },
                            { "ModifyType", reader.IsDBNull(3) ? null : reader.GetString(3) },
                            { "ModifyContent", reader.IsDBNull(4) ? null : reader.GetString(4) },
                            { "Description", reader.IsDBNull(5) ? null : reader.GetString(5) },
                            { "ModifyTime", reader.IsDBNull(6) ? null : reader.GetString(6) }
                        });
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
