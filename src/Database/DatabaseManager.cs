using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using MySql.Data.MySqlClient;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Newtonsoft.Json;
using System.Data.SQLite;
using Relfost.Database.Query;

namespace Relfost.Database
{
    public class DatabaseManager
    {
        internal Configuration config;
        internal Configuration.DatabaseInfo currentDatabase;
        internal string currentSchema;
        internal Settings settings;
        private readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "logs", "extensions", "Relfost.Database.log");

        public DatabaseManager()
        {
            try
            {
                var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "extensions", "data", $"{nameof(DatabaseManager)}", "config.yaml");
                if (!File.Exists(configFilePath))
                {
                    CreateDefaultConfigFile(configFilePath);
                }
                config = LoadConfig(configFilePath);
                currentDatabase = config.connections[config.@default];

                var settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "extensions", "data", $"{nameof(DatabaseManager)}", "settings.yaml");
                if (!File.Exists(settingsFilePath))
                {
                    CreateDefaultSettingsFile(settingsFilePath);
                }
                settings = LoadSettings(settingsFilePath);

                EnsureAllSettingsPresent(settingsFilePath);

                if (currentDatabase.driver == "sqlite")
                {
                    var sqliteDatabasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "extensions", "data", $"{nameof(DatabaseManager)}", "database.sqlite");
                    Directory.CreateDirectory(Path.GetDirectoryName(sqliteDatabasePath)); // Убедиться, что директория существует
                    currentDatabase.database = sqliteDatabasePath;
                }

                // Test connection to validate configuration
                using var connection = getConnection();
                connection.Open();
                connection.Close();
            }
            catch (Exception ex)
            {
                LogError("Error initializing DatabaseManager: " + ex.Message);
            }
        }

        private void CreateDefaultConfigFile(string path)
        {
            var defaultConfig = new Configuration();
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(defaultConfig);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, yaml);
        }

        private void CreateDefaultSettingsFile(string path)
        {
            var defaultSettings = new Settings { managing_tables = false, logging = "consoleandfile" };
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(defaultSettings);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, yaml);
        }

        private Configuration LoadConfig(string path)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = File.ReadAllText(path);
            return deserializer.Deserialize<Configuration>(yaml);
        }

        private Settings LoadSettings(string path)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = File.ReadAllText(path);
            return deserializer.Deserialize<Settings>(yaml);
        }

        private void EnsureAllSettingsPresent(string settingsFilePath)
        {
            var currentSettings = LoadSettings(settingsFilePath);
            var updatedSettings = new Settings
            {
                managing_tables = currentSettings.managing_tables,
                logging = currentSettings.logging ?? "consoleandfile"
            };

            if (updatedSettings.logging != currentSettings.logging)
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                var yaml = serializer.Serialize(updatedSettings);
                File.WriteAllText(settingsFilePath, yaml);
                settings = updatedSettings;
            }
            else
            {
                settings = currentSettings;
            }
        }

        internal DatabaseManager schema(string schema)
        {
            if (currentDatabase.driver == "pgsql")
            {
                currentSchema = schema;
            }
            return this;
        }

        internal DbConnection getConnection(string dbType = null)
        {
            try
            {
                var databaseInfo = dbType == null ? currentDatabase : config.connections[dbType];

                return databaseInfo.driver switch
                {
                    "pgsql" => new NpgsqlConnection(buildConnectionString(databaseInfo)),
                    "mysql" => new MySqlConnection(buildConnectionString(databaseInfo)),
                    "sqlite" => new SQLiteConnection(buildConnectionString(databaseInfo)),
                    "mariadb" => new MySqlConnection(buildConnectionString(databaseInfo)),
                    _ => throw new Exception("Unsupported database driver.")
                };
            }
            catch (Exception ex)
            {
                LogError("Error getting database connection: " + ex.Message);
                throw;
            }
        }

        private string buildConnectionString(Configuration.DatabaseInfo databaseInfo)
        {
            return databaseInfo.driver switch
            {
                "pgsql" => $"Host={databaseInfo.host};Port={databaseInfo.port};Database={databaseInfo.database};Username={databaseInfo.username};Password={databaseInfo.password}",
                "mysql" or "mariadb" => $"Server={databaseInfo.host};Port={databaseInfo.port};Database={databaseInfo.database};User={databaseInfo.username};Password={databaseInfo.password};Charset={databaseInfo.charset};SslMode={databaseInfo.sslmode}",
                "sqlite" => $"Data Source={databaseInfo.database};Version=3;",
                _ => throw new Exception("Unsupported database driver.")
            };
        }

        public async Task<T> scalar<T>(string query, string dbType = null)
        {
            try
            {
                using var connection = getConnection(dbType);
                await connection.OpenAsync();

                if (!string.IsNullOrEmpty(currentSchema) && currentDatabase.driver == "pgsql")
                {
                    using var schemaCommand = connection.CreateCommand();
                    schemaCommand.CommandText = $"SET search_path TO {currentSchema}";
                    await schemaCommand.ExecuteNonQueryAsync();
                }

                using var command = connection.CreateCommand();
                command.CommandText = query;

                var result = await command.ExecuteScalarAsync();
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                LogError("Error executing scalar query: " + ex.Message);
                throw;
            }
        }

        public Builder table(string tableName)
        {
            return new Builder(this, tableName);
        }

        public async Task createTable(string tableName, Dictionary<string, string> columns)
        {
            if (!settings.managing_tables)
            {
                throw new InvalidOperationException("Managing tables is prohibited");
            }

            try
            {
                using var connection = getConnection();
                await connection.OpenAsync();

                var columnDefinitions = columns.Select(column => $"{column.Key} {column.Value}");
                var query = $"CREATE TABLE {tableName} ({string.Join(", ", columnDefinitions)})";

                using var command = connection.CreateCommand();
                command.CommandText = query;
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                LogError("Error creating table: " + ex.Message);
                throw;
            }
        }

        public async Task<bool> tableExists(string tableName)
        {
            try
            {
                using var connection = getConnection();
                await connection.OpenAsync();

                string query = currentDatabase.driver switch
                {
                    "pgsql" => $"SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = '{currentSchema}' AND table_name = '{tableName}');",
                    "mysql" or "mariadb" => $"SHOW TABLES LIKE '{tableName}';",
                    "sqlite" => $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';",
                    _ => throw new Exception("Unsupported database driver.")
                };

                using var command = connection.CreateCommand();
                command.CommandText = query;

                var result = await command.ExecuteScalarAsync();
                return result != null && result != DBNull.Value;
            }
            catch (Exception ex)
            {
                LogError("Error checking if table exists: " + ex.Message);
                throw;
            }
        }

        public async Task addIndex(string tableName, string indexName, params string[] columns)
        {
            if (!settings.managing_tables)
            {
                throw new InvalidOperationException("Managing tables is prohibited");
            }

            try
            {
                using var connection = getConnection();
                await connection.OpenAsync();

                var columnsList = string.Join(", ", columns);
                var query = $"CREATE INDEX {indexName} ON {tableName} ({columnsList})";

                using var command = connection.CreateCommand();
                command.CommandText = query;
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                LogError("Error adding index: " + ex.Message);
                throw;
            }
        }

        public async Task dropIndex(string tableName, string indexName)
        {
            if (!settings.managing_tables)
            {
                throw new InvalidOperationException("Managing tables is prohibited");
            }

            try
            {
                using var connection = getConnection();
                await connection.OpenAsync();

                string query = currentDatabase.driver switch
                {
                    "pgsql" => $"DROP INDEX IF EXISTS {indexName};",
                    "mysql" or "mariadb" => $"DROP INDEX {indexName} ON {tableName};",
                    "sqlite" => $"DROP INDEX IF EXISTS {indexName};",
                    _ => throw new Exception("Unsupported database driver.")
                };

                using var command = connection.CreateCommand();
                command.CommandText = query;
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                LogError("Error dropping index: " + ex.Message);
                throw;
            }
        }

        public void UpdateConfig(Configuration newConfig)
        {
            try
            {
                var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "extensions", "data", $"{nameof(DatabaseManager)}", "config.yaml");
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                var yaml = serializer.Serialize(newConfig);
                File.WriteAllText(configFilePath, yaml);
                config = newConfig;
                currentDatabase = config.connections[config.@default];
                LogInfo("Configuration updated successfully.");
            }
            catch (Exception ex)
            {
                LogError("Error updating configuration: " + ex.Message);
                throw;
            }
        }

        public void UpdateSettings(Settings newSettings)
        {
            try
            {
                var settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "extensions", "data", $"{nameof(DatabaseManager)}", "settings.yaml");
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                var yaml = serializer.Serialize(newSettings);
                File.WriteAllText(settingsFilePath, yaml);
                settings = newSettings;
                LogInfo("Settings updated successfully.");
            }
            catch (Exception ex)
            {
                LogError("Error updating settings: " + ex.Message);
                throw;
            }
        }

        public void ClearLogs()
        {
            try
            {
                if (File.Exists(logFilePath))
                {
                    File.Delete(logFilePath);
                }
                LogInfo("Logs cleared successfully.");
            }
            catch (Exception ex)
            {
                LogError("Error clearing logs: " + ex.Message);
                throw;
            }
        }

        internal void LogError(string message)
        {
            if (settings.logging == "file" || settings.logging == "consoleandfile" || settings.logging == "fileandconsole")
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                    File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
                }
                catch
                {
                    // Ignore any errors while logging to avoid recursive issues
                }
            }
            if (settings.logging == "console" || settings.logging == "consoleandfile" || settings.logging == "fileandconsole")
            {
                Console.WriteLine(message);
            }
        }

        internal void LogInfo(string message)
        {
            if (settings.logging == "file" || settings.logging == "consoleandfile" || settings.logging == "fileandconsole")
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                    File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
                }
                catch
                {
                    // Ignore any errors while logging to avoid recursive issues
                }
            }
            if (settings.logging == "console" || settings.logging == "consoleandfile" || settings.logging == "fileandconsole")
            {
                Console.WriteLine(message);
            }
        }

        internal DbDataAdapter getAdapter(IDbCommand command, string dbType = null)
        {
            if (dbType == null)
            {
                dbType = currentDatabase.driver;
            }

            return dbType switch
            {
                "pgsql" => new NpgsqlDataAdapter((NpgsqlCommand)command),
                "mysql" or "mariadb" => new MySqlDataAdapter((MySqlCommand)command),
                "sqlite" => new SQLiteDataAdapter((SQLiteCommand)command),
                _ => throw new Exception("Unsupported database driver.")
            };
        }

        public class Configuration
        {
            public string @default { get; set; } = "pgsql";
            public Dictionary<string, DatabaseInfo> connections { get; set; } = new()
            {
                {
                    "pgsql", new DatabaseInfo
                    {
                        driver = "pgsql",
                        host = "localhost",
                        port = 5432,
                        database = "postgres",
                        username = "postgres",
                        password = "postgres"
                    }
                },
                {
                    "mysql", new DatabaseInfo
                    {
                        driver = "mysql",
                        host = "localhost",
                        port = 3306,
                        database = "database",
                        username = "root",
                        password = "",
                        charset = "utf8mb4",
                        collation = "utf8mb4_unicode_ci",
                        prefix = "",
                        prefix_indexes = true,
                        strict = true,
                        engine = null,
                        sslmode = "preferred",
                        options = new Dictionary<string, string>()
                    }
                },
                {
                    "sqlite", new DatabaseInfo
                    {
                        driver = "sqlite",
                        database = "database.sqlite",
                        prefix = "",
                        foreign_key_constraints = true
                    }
                },
                {
                    "mariadb", new DatabaseInfo
                    {
                        driver = "mariadb",
                        host = "localhost",
                        port = 3306,
                        database = "database",
                        username = "root",
                        password = "",
                        charset = "utf8mb4",
                        collation = "utf8mb4_unicode_ci",
                        prefix = "",
                        prefix_indexes = true,
                        strict = true,
                        engine = null,
                        sslmode = "preferred",
                        options = new Dictionary<string, string>()
                    }
                }
            };

            public class DatabaseInfo
            {
                public string driver { get; set; }
                public string host { get; set; }
                public uint port { get; set; }
                public string database { get; set; }
                public string username { get; set; }
                public string password { get; set; }
                public string charset { get; set; }
                public string collation { get; set; }
                public string prefix { get; set; }
                public bool prefix_indexes { get; set; }
                public bool strict { get; set; }
                public string engine { get; set; }
                public string sslmode { get; set; }
                public bool foreign_key_constraints { get; set; }
                public Dictionary<string, string> options { get; set; }
            }
        }

        public class Settings
        {
            public bool managing_tables { get; set; }
            public string logging { get; set; }
        }
    }
}
