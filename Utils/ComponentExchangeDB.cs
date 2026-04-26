using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;

namespace GrasshopperSever.Utils
{
    /// <summary>
    /// 组件交换操作数据库类（使用文档数据库）
    /// 记录组件的添加、删除、连接、断开等操作历史
    /// 数据存储在 gh 文件同目录的 _ghdata.db 文件中
    /// </summary>
    internal class ComponentExchangeDB
    {
        /// <summary>
        /// 操作类型枚举
        /// </summary>
        public enum OperationType
        {
            AddComponent,        // 添加组件
            RemoveComponent,     // 删除组件
            SetComponentValue,   // 设置组件值
            ConnectComponents,   // 连接组件
            DisconnectComponents // 断开连接
        }

        /// <summary>
        /// 初始化组件交换操作表（在文档数据库中）
        /// </summary>
        private static void InitializeComponentExchangeTable()
        {
            try
            {
                using var connection = DatabaseManager.GetDocumentConnection();
                // 检查表是否存在
                string checkTable = @"
                        SELECT name FROM sqlite_master
                        WHERE type='table' AND name='ComponentExchangeHistory'";

                using var checkCmd = new SQLiteCommand(checkTable, connection);
                using var reader = checkCmd.ExecuteReader();
                if (!reader.Read())
                {
                    // 表不存在，创建它
                    string createTableSql = @"
                                    CREATE TABLE ComponentExchangeHistory (
                                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                        OperationType TEXT NOT NULL,
                                        ComponentGuid TEXT,
                                        InstanceGuid TEXT,
                                        ComponentName TEXT,
                                        PositionX REAL,
                                        PositionY REAL,
                                        Value TEXT,
                                        FromInstanceGuid TEXT,
                                        FromParameter TEXT,
                                        ToInstanceGuid TEXT,
                                        ToParameter TEXT,
                                        OperationTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                                        Description TEXT
                                    )";

                    using (var createCmd = new SQLiteCommand(createTableSql, connection))
                    {
                        createCmd.ExecuteNonQuery();
                        Debug.WriteLine("组件交换操作表创建成功");
                    }

                    // 创建索引以提高查询性能
                    string createIndexSql = @"
                                    CREATE INDEX IF NOT EXISTS idx_component_exchange_time 
                                    ON ComponentExchangeHistory(OperationTime DESC);

                                    CREATE INDEX IF NOT EXISTS idx_component_exchange_instance 
                                    ON ComponentExchangeHistory(InstanceGuid);";

                    using var indexCmd = new SQLiteCommand(createIndexSql, connection);
                    indexCmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化组件交换操作表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录添加组件操作
        /// </summary>
        public static void RecordAddComponent(string componentGuid, string instanceGuid, string componentName, float x, float y, string description = null)
        {
            try
            {
                InitializeComponentExchangeTable();

                using var connection = DatabaseManager.GetDocumentConnection();
                string sql = @"
                        INSERT INTO ComponentExchangeHistory 
                        (OperationType, ComponentGuid, InstanceGuid, ComponentName, PositionX, PositionY, Description)
                        VALUES (@operationType, @componentGuid, @instanceGuid, @componentName, @positionX, @positionY, @description)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@operationType", OperationType.AddComponent.ToString());
                command.Parameters.AddWithValue("@componentGuid", componentGuid ?? string.Empty);
                command.Parameters.AddWithValue("@instanceGuid", instanceGuid ?? string.Empty);
                command.Parameters.AddWithValue("@componentName", componentName ?? string.Empty);
                command.Parameters.AddWithValue("@positionX", x);
                command.Parameters.AddWithValue("@positionY", y);
                command.Parameters.AddWithValue("@description", description ?? string.Empty);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"记录添加组件操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录删除组件操作
        /// </summary>
        public static void RecordRemoveComponent(string instanceGuid, string componentName, string description = null)
        {
            try
            {
                InitializeComponentExchangeTable();

                using var connection = DatabaseManager.GetDocumentConnection();
                string sql = @"
                        INSERT INTO ComponentExchangeHistory 
                        (OperationType, InstanceGuid, ComponentName, Description)
                        VALUES (@operationType, @instanceGuid, @componentName, @description)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@operationType", OperationType.RemoveComponent.ToString());
                command.Parameters.AddWithValue("@instanceGuid", instanceGuid ?? string.Empty);
                command.Parameters.AddWithValue("@componentName", componentName ?? string.Empty);
                command.Parameters.AddWithValue("@description", description ?? string.Empty);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"记录删除组件操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录设置组件值操作
        /// </summary>
        public static void RecordSetComponentValue(string instanceGuid, string componentName, string value, string description = null)
        {
            try
            {
                InitializeComponentExchangeTable();

                using var connection = DatabaseManager.GetDocumentConnection();
                string sql = @"
                        INSERT INTO ComponentExchangeHistory 
                        (OperationType, InstanceGuid, ComponentName, Value, Description)
                        VALUES (@operationType, @instanceGuid, @componentName, @value, @description)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@operationType", OperationType.SetComponentValue.ToString());
                command.Parameters.AddWithValue("@instanceGuid", instanceGuid ?? string.Empty);
                command.Parameters.AddWithValue("@componentName", componentName ?? string.Empty);
                command.Parameters.AddWithValue("@value", value ?? string.Empty);
                command.Parameters.AddWithValue("@description", description ?? string.Empty);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"记录设置组件值操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录连接组件操作
        /// </summary>
        public static void RecordConnectComponents(string fromInstanceGuid, string fromParameter, string toInstanceGuid, string toParameter, string description = null)
        {
            try
            {
                InitializeComponentExchangeTable();

                using var connection = DatabaseManager.GetDocumentConnection();
                string sql = @"
                        INSERT INTO ComponentExchangeHistory 
                        (OperationType, FromInstanceGuid, FromParameter, ToInstanceGuid, ToParameter, Description)
                        VALUES (@operationType, @fromInstanceGuid, @fromParameter, @toInstanceGuid, @toParameter, @description)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@operationType", OperationType.ConnectComponents.ToString());
                command.Parameters.AddWithValue("@fromInstanceGuid", fromInstanceGuid ?? string.Empty);
                command.Parameters.AddWithValue("@fromParameter", fromParameter ?? string.Empty);
                command.Parameters.AddWithValue("@toInstanceGuid", toInstanceGuid ?? string.Empty);
                command.Parameters.AddWithValue("@toParameter", toParameter ?? string.Empty);
                command.Parameters.AddWithValue("@description", description ?? string.Empty);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"记录连接组件操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录断开连接操作
        /// </summary>
        public static void RecordDisconnectComponents(string fromInstanceGuid, string fromParameter, string toInstanceGuid, string toParameter, string description = null)
        {
            try
            {
                InitializeComponentExchangeTable();

                using var connection = DatabaseManager.GetDocumentConnection();
                string sql = @"
                        INSERT INTO ComponentExchangeHistory 
                        (OperationType, FromInstanceGuid, FromParameter, ToInstanceGuid, ToParameter, Description)
                        VALUES (@operationType, @fromInstanceGuid, @fromParameter, @toInstanceGuid, @toParameter, @description)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@operationType", OperationType.DisconnectComponents.ToString());
                command.Parameters.AddWithValue("@fromInstanceGuid", fromInstanceGuid ?? string.Empty);
                command.Parameters.AddWithValue("@fromParameter", fromParameter ?? string.Empty);
                command.Parameters.AddWithValue("@toInstanceGuid", toInstanceGuid ?? string.Empty);
                command.Parameters.AddWithValue("@toParameter", toParameter ?? string.Empty);
                command.Parameters.AddWithValue("@description", description ?? string.Empty);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"记录断开连接操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取组件交换操作历史
        /// </summary>
        /// <param name="limit">返回的最大记录数，默认100</param>
        /// <returns>操作历史列表</returns>
        public static List<Dictionary<string, object>> GetExchangeHistory(int limit = 100)
        {
            var history = new List<Dictionary<string, object>>();

            try
            {
                InitializeComponentExchangeTable();

                using var connection = DatabaseManager.GetDocumentConnection();
                string sql = @"
                        SELECT Id, OperationType, ComponentGuid, InstanceGuid, ComponentName, 
                               PositionX, PositionY, Value, FromInstanceGuid, FromParameter, 
                               ToInstanceGuid, ToParameter, OperationTime, Description
                        FROM ComponentExchangeHistory
                        ORDER BY OperationTime DESC
                        LIMIT @limit";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@limit", limit);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    history.Add(new Dictionary<string, object>
                        {
                            { "Id", reader.IsDBNull(0) ? null : reader.GetInt64(0) },
                            { "OperationType", reader.IsDBNull(1) ? null : reader.GetString(1) },
                            { "ComponentGuid", reader.IsDBNull(2) ? null : reader.GetString(2) },
                            { "InstanceGuid", reader.IsDBNull(3) ? null : reader.GetString(3) },
                            { "ComponentName", reader.IsDBNull(4) ? null : reader.GetString(4) },
                            { "PositionX", reader.IsDBNull(5) ? null : (object)reader.GetDouble(5) },
                            { "PositionY", reader.IsDBNull(6) ? null : (object)reader.GetDouble(6) },
                            { "Value", reader.IsDBNull(7) ? null : reader.GetString(7) },
                            { "FromInstanceGuid", reader.IsDBNull(8) ? null : reader.GetString(8) },
                            { "FromParameter", reader.IsDBNull(9) ? null : reader.GetString(9) },
                            { "ToInstanceGuid", reader.IsDBNull(10) ? null : reader.GetString(10) },
                            { "ToParameter", reader.IsDBNull(11) ? null : reader.GetString(11) },
                            { "OperationTime", reader.IsDBNull(12) ? null : reader.GetString(12) },
                            { "Description", reader.IsDBNull(13) ? null : reader.GetString(13) }
                        });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取组件交换操作历史失败: {ex.Message}");
            }

            return history;
        }

        /// <summary>
        /// 清空组件交换操作历史
        /// </summary>
        public static bool ClearExchangeHistory()
        {
            try
            {
                InitializeComponentExchangeTable();

                using var connection = DatabaseManager.GetDocumentConnection();
                string sql = "DELETE FROM ComponentExchangeHistory";

                using var command = new SQLiteCommand(sql, connection);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清空组件交换操作历史失败: {ex.Message}");
                return false;
            }
        }
    }
}