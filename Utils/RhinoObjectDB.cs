using System;
using System.Data.SQLite;
using System.Diagnostics;

namespace GrasshopperSever.Utils
{
    internal class RhinoObjectDB
    {
        /// <summary>
        /// 初始化对象信息表
        /// </summary>
        public static void InitializeObjectTable()
        {
            try
            {
                if (!DatabaseManager.TableExists("RhinoObjects"))
                {
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

                    if (DatabaseManager.CreateTable("RhinoObjects", createTableSql, "存储Rhino对象信息"))
                    {
                        Debug.WriteLine("对象表创建成功");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化对象表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 插入对象记录
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
                using (var connection = DatabaseManager.GetConnection())
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

                        // 更新表时间戳
                        DatabaseManager.UpdateTableTimestamp("RhinoObjects");

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
