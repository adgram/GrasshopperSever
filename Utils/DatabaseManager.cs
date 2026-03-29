using System;
using System.IO;
using System.Data.SQLite;
using System.Reflection;
using System.Diagnostics;

namespace GrasshopperSever.Utils
{
    /// <summary>
    /// 数据库管理器 - 用于管理插件数据库和跟踪表更新时间
    /// </summary>
    public static class DatabaseManager
    {
        private static readonly string DatabaseFileName = "GrasshopperSever.db";
        private static readonly string AssemblyLocation = Assembly.GetExecutingAssembly().Location;
        private static readonly string AssemblyDirectory = Path.GetDirectoryName(AssemblyLocation);
        private static string _databasePath;

        /// <summary>
        /// 获取数据库完整路径
        /// </summary>
        public static string DatabasePath
        {
            get
            {
                if (_databasePath == null)
                {
                    _databasePath = Path.Combine(AssemblyDirectory, DatabaseFileName);
                }
                return _databasePath;
            }
        }

        /// <summary>
        /// 获取插件所在目录
        /// </summary>
        public static string AssemblyDirectoryPath
        {
            get { return AssemblyDirectory; }
        }

        /// <summary>
        /// 初始化数据库
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // 如果数据库不存在，则创建
                if (!File.Exists(DatabasePath))
                {
                    CreateDatabase();
                }
                else
                {
                    // 检查 MetaInfo 表是否存在
                    EnsureMetaInfoTableExists();
                }
                Debug.WriteLine($"数据库已初始化: {DatabasePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"数据库初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建数据库和 MetaInfo 表
        /// </summary>
        private static void CreateDatabase()
        {
            SQLiteConnection.CreateFile(DatabasePath);

            using (var connection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;"))
            {
                connection.Open();

                // 创建元信息表，用于跟踪表的更新时间
                string createMetaInfoTable = @"
                    CREATE TABLE IF NOT EXISTS MetaInfo (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TableName TEXT NOT NULL UNIQUE,
                        LastUpdateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                        Description TEXT
                    )";

                using (var command = new SQLiteCommand(createMetaInfoTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                // 插入数据库创建信息
                string insertMetaInfo = @"
                    INSERT INTO MetaInfo (TableName, Description)
                    VALUES ('DatabaseCreated', '数据库初始化创建')";

                using (var command = new SQLiteCommand(insertMetaInfo, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 确保 MetaInfo 表存在
        /// </summary>
        private static void EnsureMetaInfoTableExists()
        {
            using (var connection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;"))
            {
                connection.Open();

                // 检查 MetaInfo 表是否存在
                string checkTable = @"
                    SELECT name FROM sqlite_master 
                    WHERE type='table' AND name='MetaInfo'";

                using (var command = new SQLiteCommand(checkTable, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            // 表不存在，创建它
                            string createMetaInfoTable = @"
                                CREATE TABLE IF NOT EXISTS MetaInfo (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    TableName TEXT NOT NULL UNIQUE,
                                    LastUpdateTime DATETIME DEFAULT CURRENT_TIMESTAMP,
                                    Description TEXT
                                )";

                            using (var createCmd = new SQLiteCommand(createMetaInfoTable, connection))
                            {
                                createCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 创建新表并记录元信息
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="createTableSql">创建表的SQL语句</param>
        /// <param name="description">表描述</param>
        public static bool CreateTable(string tableName, string createTableSql, string description = null)
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;"))
                {
                    connection.Open();

                    // 创建表
                    using (var command = new SQLiteCommand(createTableSql, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    // 记录表元信息
                    string insertMetaInfo = @"
                        INSERT OR REPLACE INTO MetaInfo (TableName, LastUpdateTime, Description)
                        VALUES (@tableName, CURRENT_TIMESTAMP, @description)";

                    using (var command = new SQLiteCommand(insertMetaInfo, connection))
                    {
                        command.Parameters.AddWithValue("@tableName", tableName);
                        command.Parameters.AddWithValue("@description", description ?? string.Empty);
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建表失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 更新表的时间戳
        /// </summary>
        /// <param name="tableName">表名</param>
        public static bool UpdateTableTimestamp(string tableName)
        {
            try
            {
                // 使用 WAL 模式和连接池优化
                using (var connection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;Default Timeout=30;Journal Mode=WAL;Pooling=True;"))
                {
                    connection.Open();

                    string sql = @"
                        INSERT OR REPLACE INTO MetaInfo (TableName, LastUpdateTime)
                        VALUES (@tableName, CURRENT_TIMESTAMP)";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@tableName", tableName);
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新表时间戳失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取表的最后更新时间
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>最后更新时间，如果不存在则返回null</returns>
        public static DateTime? GetTableTimestamp(string tableName)
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;"))
                {
                    connection.Open();

                    string sql = "SELECT LastUpdateTime FROM MetaInfo WHERE TableName = @tableName";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@tableName", tableName);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read() && !reader.IsDBNull(0))
                            {
                                return DateTime.Parse(reader["LastUpdateTime"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取表时间戳失败: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 检查表是否存在
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>表是否存在</returns>
        public static bool TableExists(string tableName)
        {
            try
            {
                using (var connection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;"))
                {
                    connection.Open();

                    string sql = @"
                        SELECT name FROM sqlite_master 
                        WHERE type='table' AND name=@tableName";

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@tableName", tableName);

                        using (var reader = command.ExecuteReader())
                        {
                            return reader.Read();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查表存在失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取数据库连接对象（用于执行自定义SQL）
        /// </summary>
        /// <returns>SQLite连接对象</returns>
        public static SQLiteConnection GetConnection()
        {
            // 使用 WAL 模式和连接池优化，避免数据库锁定
            var connection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;Default Timeout=30;Journal Mode=WAL;Pooling=True;");
            connection.Open();
            return connection;
        }

        /// <summary>
        /// 执行SQL命令并自动更新表时间戳
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数字典</param>
        /// <returns>影响的行数</returns>
        public static int ExecuteCommandWithTimestamp(string tableName, string sql, System.Collections.Generic.Dictionary<string, object> parameters = null)
        {
            try
            {
                // 使用 WAL 模式和连接池优化
                using (var connection = new SQLiteConnection($"Data Source={DatabasePath};Version=3;Default Timeout=30;Journal Mode=WAL;Pooling=True;"))
                {
                    connection.Open();

                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                command.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }

                        int rowsAffected = command.ExecuteNonQuery();

                        // 更新表时间戳
                        if (rowsAffected > 0)
                        {
                            UpdateTableTimestamp(tableName);
                        }

                        return rowsAffected;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"执行命令失败: {ex.Message}");
                return -1;
            }
        }
    }
}