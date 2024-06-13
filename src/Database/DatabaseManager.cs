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
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
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
        private readonly string logFilePath;

        public DatabaseManager()
        {
            var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "extensions", "Relfost.Database", "config.yaml");
            if (!File.Exists(configFilePath))
            {
                CreateDefaultConfigFile(configFilePath);
            }
            config = LoadConfig(configFilePath);
            currentDatabase = config.connections[config.@default];

            var settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "extensions", "Relfost.Database", "settings.yaml");
            if (!File.Exists(settingsFilePath))
            {
                CreateDefaultSettingsFile(settingsFilePath);
            }
            settings = LoadSettings(settingsFilePath);

            if (currentDatabase.driver == "sqlite")
            {
                var sqliteDatabasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "extensions", "Relfost.Database", "database.sqlite");
                Directory.CreateDirectory(Path.GetDirectoryName(sqliteDatabasePath)); // Убедиться, что директория существует
                currentDatabase.database = sqliteDatabasePath;
            }

            logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "logs", "extensions", "Relfost.Database.log");
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
            var settings = deserializer.Deserialize<Settings>(yaml);

            // Проверить наличие всех обязательных параметров
            if (settings.logging == null)
            {
                settings.logging = "consoleandfile";
            }

            var updatedYaml = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build()
                .Serialize(settings);
            File.WriteAllText(path, updatedYaml);

            return settings;
        }

        internal DbConnection getConnection(string dbType = null)
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

        public Builder table(string tableName)
        {
            return new Builder(this, tableName);
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

        public void LogError(string message)
        {
            if (settings.logging == "file" || settings.logging == "consoleandfile" || settings.logging == "fileandconsole")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}\n");
            }
            if (settings.logging == "console" || settings.logging == "consoleandfile" || settings.logging == "fileandconsole")
            {
                Console.WriteLine($"{DateTime.Now}: {message}");
            }
        }

        public void UpdateConfig(Configuration newConfig)
        {
            config = newConfig;
            var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "extensions", "Relfost.Database", "config.yaml");
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(config);
            File.WriteAllText(configFilePath, yaml);
        }

        public void UpdateSettings(Settings newSettings)
        {
            settings = newSettings;
            var settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carbon", "extensions", "Relfost.Database", "settings.yaml");
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(settings);
            File.WriteAllText(settingsFilePath, yaml);
        }

        public void ClearLogs()
        {
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
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
