using System;
using System.Data.SQLite;
using System.Diagnostics;

namespace GrasshopperSever.Utils
{
    /// <summary>
    /// Rhino 对象数据库操作类（使用文档数据库）
    /// 数据存储在 gh 文件同目录的 _ghdata.db 文件中
    /// </summary>
    internal class RhinoObjectDB
    {
        /// <summary>
        /// 初始化对象信息表（在文档数据库中）
        /// </summary>
        public static void InitializeObjectTable()
        {
            try
            {
                using (var connection = DatabaseManager.GetDocumentConnection())
                {
                    // 检查表是否存在
                    string checkTable = @"
                        SELECT name FROM sqlite_master
                        WHERE type='table' AND name='RhinoObjects'";

                    using (var checkCmd = new SQLiteCommand(checkTable, connection))
                    {
                        using (var reader = checkCmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                // 表不存在，创建它
                                string createTableSql = @"
                                    CREATE TABLE RhinoObjects (
                                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                        ObjectId TEXT NOT NULL,
                                        ObjectType TEXT,
                                        LayerName TEXT,
                                        ObjectName TEXT,
                                        CreateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                                        DocumentSerialNumber TEXT,
                                        Description TEXT
                                    )";

                                using (var createCmd = new SQLiteCommand(createTableSql, connection))
                                {
                                    createCmd.ExecuteNonQuery();
                                    Debug.WriteLine("对象表创建成功");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化对象表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 插入对象记录（存储在文档数据库中）
        /// </summary>
        /// <param name="objectId">对象ID</param>
        /// <param name="objectType">对象类型</param>
        /// <param name="layerName">图层名称</param>
        /// <param name="objectName">对象名称</param>
        /// <param name="documentSerialNumber">文档序列号</param>
        /// <param name="description">描述</param>
        /// <returns>插入的记录ID，失败返回-1</returns>
        public static long InsertObjectRecord(string objectId, string objectType = null, string layerName = null, string objectName = null, string documentSerialNumber = null, string description = null)
        {
            try
            {
                using (var connection = DatabaseManager.GetDocumentConnection())
                {
                    string sql = @"
                        INSERT INTO RhinoObjects (ObjectId, ObjectType, LayerName, ObjectName, DocumentSerialNumber, Description)
                        VALUES (@objectId, @objectType, @layerName, @objectName, @documentSerialNumber, @description)";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@objectId", objectId);
                        command.Parameters.AddWithValue("@objectType", objectType ?? string.Empty);
                        command.Parameters.AddWithValue("@layerName", layerName ?? string.Empty);
                        command.Parameters.AddWithValue("@objectName", objectName ?? string.Empty);
                        command.Parameters.AddWithValue("@documentSerialNumber", documentSerialNumber ?? string.Empty);
                        command.Parameters.AddWithValue("@description", description ?? string.Empty);

                        command.ExecuteNonQuery();

                        // 获取插入的记录ID
                        command.CommandText = "SELECT last_insert_rowid()";
                        long insertedId = (long)command.ExecuteScalar();

                        return insertedId;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"插入对象记录失败: {ex.Message}");
                return -1;
            }
        }

    }
}
