using GrasshopperSever.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Data.SQLite;

namespace GrasshopperSever.Commands
{
    internal class RhinoCommand
    {
        /// <summary>
        /// 初始化对象信息表
        /// </summary>
        private static void InitializeObjectTable()
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
                        System.Diagnostics.Debug.WriteLine("对象表创建成功");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化对象表失败: {ex.Message}");
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
        private static long InsertObjectRecord(string objectId, string objectType = null, string layerName = null, string objectName = null, string documentSerialNumber = null, string description = null)
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
                System.Diagnostics.Debug.WriteLine($"插入对象记录失败: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 处理运行Rhino脚本命令
        /// </summary>
        public static Ljson RinoRunScript(string script)
        {
            // 重要：RhinoApp.RunScript 必须在主文档上下文执行
            // 如果是从非命令线程调用，请确保在 Rhino 的 Idle 句柄或主线程中调度
            var doc = Rhino.RhinoDoc.ActiveDoc;

            // 执行Rhino命令 (true 表示 echo，让命令出现在命令行历史中)
            // 注意：script 前面建议加一个下划线 _ 以确保在非英文版 Rhino 中也能运行
            bool result = Rhino.RhinoApp.RunScript(doc.RuntimeSerialNumber, script, true);

            var responseData = new Dictionary<string, object>
            {
                { "Result", result.ToString() },
                { "Script", script }
            };

            if (result)
            {
                return new Ljson("RunScript", "执行Rhino脚本成功", JsonSerializer.SerializeToElement(responseData));
            }
            else
            {
                return Ljson.CreateErrorLjson("命令执行失败或被用户取消");
            }
        }

        /// <summary>
        /// 处理获取最后创建的对象命令
        /// </summary>
        public static Ljson GetLastCreatedObjects()
        {
            // 确保对象表已初始化
            InitializeObjectTable();

            var doc = Rhino.RhinoDoc.ActiveDoc;

            // 技巧：如果你的 RunScript 刚运行完，可以使用以下逻辑获取：
            doc.Objects.UnselectAll();
            Rhino.RhinoApp.RunScript(doc.RuntimeSerialNumber, "_SelLast", false);
            var selectedObjects = doc.Objects.GetSelectedObjects(false, false);

            if (selectedObjects != null)
            {
                var objectsData = new Dictionary<string, object>();
                int count = 0;
                foreach (var obj in selectedObjects)
                {
                    // 获取对象详细信息
                    string objectType = obj.ObjectType.ToString();
                    string layerName = obj.Attributes.LayerIndex >= 0 && obj.Attributes.LayerIndex < doc.Layers.Count
                        ? doc.Layers[obj.Attributes.LayerIndex].Name
                        : "Unknown";
                    string objectName = obj.Attributes.Name;

                    // 将对象信息存入数据库
                    long recordId = InsertObjectRecord(
                        objectId: obj.Id.ToString(),
                        objectType: objectType,
                        layerName: layerName,
                        objectName: objectName,
                        documentSerialNumber: doc.RuntimeSerialNumber.ToString(),
                        description: "通过 GetLastCreatedObjects 获取的对象"
                    );

                    objectsData[$"Object_{count}"] = new Dictionary<string, object>
                    {
                        { "Id", obj.Id.ToString() },
                        { "Type", objectType },
                        { "Layer", layerName },
                        { "Name", objectName },
                        { "DatabaseRecordId", recordId.ToString() }
                    };
                    count++;
                }
                objectsData["Count"] = count.ToString();

                return new Ljson("GetLastCreatedObjects", "获取最后创建的对象", JsonSerializer.SerializeToElement(objectsData));
            }

            return new Ljson("GetLastCreatedObjects", "未找到对象", JsonSerializer.SerializeToElement(new Dictionary<string, object>()));
        }

        /// <summary>
        /// 处理选择对象命令
        /// 输入：Ljson包含 Command="SelectObjects", Objects="对象ID列表(逗号分隔)"
        /// 输出：选择结果
        /// </summary>
        public static Ljson SelectObjects(string objectsParam)
        {
            try
            {
                var doc = Rhino.RhinoDoc.ActiveDoc;
                if (doc == null)
                {
                    return Ljson.CreateErrorLjson("未找到活动文档");
                }

                // 1. 解析对象 ID (Rhino 使用 System.Guid)
                var idStrs = objectsParam.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int successCount = 0;
                int invalidIdCount = 0;
                int notFoundCount = 0;

                // 在执行新选择前，通常需要清除之前的选择（视业务逻辑而定）
                doc.Objects.UnselectAll();

                foreach (var idStr in idStrs)
                {
                    // Rhino 的对象 ID 统一使用 Guid
                    if (Guid.TryParse(idStr.Trim(), out Guid id))
                    {
                        // 2. 通过 ID 查找对象并执行选择
                        // RhinoDoc.Objects.Select(Guid) 会返回被选择的对象数量 (1 或 0)
                        bool result = doc.Objects.Select(id);
                        if (result)
                        {
                            successCount++;
                        }
                        else
                        {
                            notFoundCount++;
                        }
                    }
                    else
                    {
                        invalidIdCount++;
                    }
                }

                // 3. 必须刷新视图，否则界面上看不见选择结果
                doc.Views.Redraw();

                var responseData = new Dictionary<string, object>
                {
                    { "TotalRequested", idStrs.Length.ToString() },
                    { "TotalSelected", successCount.ToString() },
                    { "InvalidIdCount", invalidIdCount.ToString() },
                    { "NotFoundCount", notFoundCount.ToString() }
                };

                // 根据不同情况返回不同的提示信息
                if (successCount == 0)
                {
                    if (invalidIdCount > 0)
                    {
                        responseData["Message"] = $"所有ID均无效或未找到对象（无效ID: {invalidIdCount}, 未找到: {notFoundCount}）";
                    }
                    else
                    {
                        responseData["Message"] = "未能在文档中找到匹配的 ID 或对象不可选";
                    }
                }
                else if (invalidIdCount > 0 || notFoundCount > 0)
                {
                    responseData["Message"] = $"部分对象选择成功（成功: {successCount}, 无效ID: {invalidIdCount}, 未找到: {notFoundCount}）";
                }

                return new Ljson("SelectObjects", "选择对象", JsonSerializer.SerializeToElement(responseData));
            }
            catch (Exception ex)
            {
                return Ljson.CreateErrorLjson($"选择对象失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 将 GetLastCreatedObjects 的返回结果转换为 SelectObjects 需要的格式
        /// </summary>
        /// <param name="getObjectsResult">GetLastCreatedObjects 的返回结果</param>
        /// <returns>包含逗号分隔的 Guid 字符串，如果转换失败返回 null</returns>
        public static string ConvertToSelectObjectsFormat(Ljson getObjectsResult)
        {
            try
            {
                if (getObjectsResult == null || getObjectsResult.Value.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                var guids = new List<string>();
                var count = 0;

                // 遍历返回的 Value，提取所有以 "Object_" 开头的项
                foreach (var prop in getObjectsResult.Value.EnumerateObject())
                {
                    if (prop.Name.StartsWith("Object_") && prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        // 尝试获取 Guid 字段
                        if (prop.Value.TryGetProperty("Id", out var guidElement))
                        {
                            guids.Add(guidElement.GetString());
                            count++;
                        }
                    }
                }

                return guids.Count > 0 ? string.Join(",", guids) : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"转换 SelectObjects 格式失败: {ex.Message}");
                return null;
            }
        }

    }
}
