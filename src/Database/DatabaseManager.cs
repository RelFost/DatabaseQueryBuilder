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

namespace Relfost.Database
{
    public class DatabaseManager
    {
        private Configuration config;
        private Configuration.DatabaseInfo currentDatabase;
        private string currentSchema;
        private Settings settings;

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
                Directory.CreateDirectory(Path.GetDirectoryName(sqliteDatabasePath));
                currentDatabase.database = sqliteDatabasePath;
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
            var settings = deserializer.Deserialize<Settings>(yaml);
            
            var requiredProperties = new List<string> { "managing_tables", "logging" };
            var existingProperties = settings.GetType().GetProperties().Select(p => p.Name).ToList();

            var missingProperties = requiredProperties.Except(existingProperties).ToList();

            if (missingProperties.Any())
            {
                CreateDefaultSettingsFile(path);
                settings = deserializer.Deserialize<Settings>(File.ReadAllText(path));
            }

            return settings;
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
                LogError($"Error executing scalar query: {ex.Message}");
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
                LogError($"Error creating table: {ex.Message}");
                throw;
           
